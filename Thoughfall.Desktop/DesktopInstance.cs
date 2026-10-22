namespace Thoughfall.Desktop;

using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;

/// <summary>Locks the data folder and carries capture requests between desktop instances.</summary>
internal sealed class DesktopInstance : IDisposable
{
    private readonly FileStream? _lock;
    private readonly IOException? _lockError;
    private readonly string _pipeName;
    private readonly CancellationTokenSource _stop = new();
    private readonly NamedPipeServerStream? _server;
    private Task? _listener;
    private Task? _stopTask;
    private bool _disposed;

    private DesktopInstance(string folder)
    {
        Directory.CreateDirectory(folder);
        var identity = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
        if (OperatingSystem.IsWindows())
        {
            identity = identity.ToUpperInvariant();
        }

        _pipeName = "Thoughfall-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))[..24];
        try
        {
            _lock = new FileStream(Path.Combine(folder, ".instance.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException ex) when (IsLockContention(ex))
        {
            _lockError = ex;
            return;
        }

        try
        {
            _server = new NamedPipeServerStream(
                _pipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly
            );
        }
        catch
        {
            _lock.Dispose();
            _stop.Dispose();
            throw;
        }
    }

    /// <summary>Gets a value indicating whether this instance holds the data-folder lock.</summary>
    internal bool IsOwner => _lock is not null;

    /// <summary>Attempts to acquire the data-folder lock.</summary>
    /// <param name="folder">The resolved data directory.</param>
    /// <returns>An instance whose ownership is indicated by <see cref="IsOwner"/>.</returns>
    internal static DesktopInstance Open(string folder) => new(folder);

    private static bool IsLockContention(IOException error)
    {
        var code = error.HResult & 0xFFFF;
        return OperatingSystem.IsWindows() ? code is 32 or 33 : code is 11 or 32 or 33;
    }

    /// <summary>Requests capture from the running owner, with a five-second timeout.</summary>
    /// <returns>A task completing when the owner acknowledges the request.</returns>
    internal async Task RequestCaptureAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            await client.ConnectAsync(timeout.Token).ConfigureAwait(false);
            await client.WriteAsync(new byte[] { 1 }, timeout.Token).ConfigureAwait(false);
            var response = new byte[1];
            if (await client.ReadAsync(response, timeout.Token).ConfigureAwait(false) != 1 || response[0] != 1)
            {
                throw new IOException("The running copy did not acknowledge the capture request.");
            }
        }
        catch (Exception ex) when (ex is IOException or OperationCanceledException or UnauthorizedAccessException)
        {
            throw new IOException(
                "The diary folder is locked, but the running copy could not be activated.",
                new AggregateException(_lockError ?? ex, ex)
            );
        }
    }

    /// <summary>Starts the owner's activation listener once.</summary>
    /// <param name="capture">The callback for a valid capture request.</param>
    /// <param name="failed">The callback for an unexpected listener failure.</param>
    internal void Listen(Action capture, Action failed)
    {
        if (!IsOwner || _listener is not null)
        {
            throw new InvalidOperationException("Only the owning instance can listen, once.");
        }

        _listener = Task.Run(() => ListenAsync(capture, failed));
    }

    private async Task ListenAsync(Action capture, Action failed)
    {
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                await _server!.WaitForConnectionAsync(_stop.Token).ConfigureAwait(false);
                try
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
                    timeout.CancelAfter(TimeSpan.FromSeconds(5));
                    var request = new byte[1];
                    if (await _server.ReadAsync(request, timeout.Token).ConfigureAwait(false) == 1 && request[0] == 1)
                    {
                        capture();
                        await _server.WriteAsync(new byte[] { 1 }, timeout.Token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    // A silent client times out; shutdown uses the linked cancellation token.
                }
                catch (IOException)
                {
                    // A client may disappear before reading its acknowledgement.
                }
                finally
                {
                    if (_server.IsConnected)
                    {
                        _server.Disconnect();
                    }
                }
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
        catch (Exception ex)
        {
            ErrorReporting.Unexpected("listening for activation", ex);
            failed();
        }
    }

    /// <summary>Stops activation handling while retaining the data-folder lock.</summary>
    /// <returns>A task completing when the listener has stopped.</returns>
    internal Task StopAsync() => _stopTask ??= StopCoreAsync();

    private async Task StopCoreAsync()
    {
        await _stop.CancelAsync().ConfigureAwait(false);
        if (_listener is not null)
        {
            await _listener.ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            StopAsync().GetAwaiter().GetResult();
        }
        finally
        {
            try
            {
                _server?.Dispose();
            }
            finally
            {
                try
                {
                    _lock?.Dispose();
                }
                finally
                {
                    _stop.Dispose();
                }
            }
        }
    }
}
