namespace Thoughfall.Desktop;

using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;

// The lock owns the data folder; the pipe carries activation requests from desktop shortcuts.
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

    internal bool IsOwner => _lock is not null;

    internal static DesktopInstance Open(string folder) => new(folder);

    private static bool IsLockContention(IOException error)
    {
        var code = error.HResult & 0xFFFF;
        return OperatingSystem.IsWindows() ? code is 32 or 33 : code is 11 or 32 or 33;
    }

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

    internal Task StopAsync() => _stopTask ??= StopCoreAsync();

    private async Task StopCoreAsync()
    {
        await _stop.CancelAsync().ConfigureAwait(false);
        if (_listener is not null)
        {
            await _listener.ConfigureAwait(false);
        }
    }

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
