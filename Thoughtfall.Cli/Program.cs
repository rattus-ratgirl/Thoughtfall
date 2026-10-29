namespace Thoughtfall.Cli;

using System.Diagnostics;
using System.Text;
using Thoughtfall.Application;

/// <summary>Captures one diary entry using an external editor.</summary>
internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || (args.Length == 1 && args[0] is "--help" or "-h"))
        {
            Console.WriteLine(
                "Usage: thoughtfall capture\nOpens VISUAL, then EDITOR, or a platform editor. Save and close to submit.\nConfigure GUI editors to wait (for example: EDITOR=code --wait). Blank text cancels."
            );
            return 0;
        }

        if (args.Length != 1 || args[0] != "capture")
        {
            Console.Error.WriteLine("Usage: thoughtfall capture");
            return 2;
        }

        string? draft = null;
        try
        {
            using var marker = OperatingSystem.IsWindows() ? new Mutex(false, @"Local\Thoughtfall.Running") : null;
            var store = new DiaryStore(DataPaths.GetDefaultFolder());
            var directory = Path.Combine(store.Folder, "drafts");
            Directory.CreateDirectory(directory);
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            draft = Path.Combine(directory, $"capture-{Guid.NewGuid():N}.txt");
            await File.WriteAllTextAsync(draft, string.Empty, new UTF8Encoding(false));
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(draft, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            var editor = Environment.GetEnvironmentVariable("VISUAL");
            if (string.IsNullOrWhiteSpace(editor))
                editor = Environment.GetEnvironmentVariable("EDITOR");
            if (string.IsNullOrWhiteSpace(editor))
                editor = OperatingSystem.IsWindows() ? "notepad.exe" : FindEditor();
            Console.WriteLine("Save and close the editor to capture. Leave the draft blank to cancel.");
            using var process = Process.Start(EditorCommand.Create(editor, draft)) ?? throw new IOException("The editor did not start.");
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
                throw new IOException($"The editor exited with code {process.ExitCode}. Configure VISUAL or EDITOR and retry.");
            var text = await File.ReadAllTextAsync(draft, new UTF8Encoding(false, true));
            var saved = await store.SaveAsync(text);
            try
            {
                File.Delete(draft);
            }
            catch (IOException)
            {
                Console.Error.WriteLine($"Entry processed, but the draft could not be deleted: {draft}");
            }
            catch (UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Entry processed, but the draft could not be deleted: {draft}");
            }
            draft = null;
            Console.WriteLine(saved ? "Thought saved." : "Capture cancelled.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Capture failed: {ex.Message}");
            if (draft is not null)
                Console.Error.WriteLine($"Draft recovery path: {draft}");
            return 1;
        }
    }

    private static string FindEditor()
    {
        if (OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException();
        foreach (var name in new[] { "nano", "vi" })
        {
            foreach (var folder in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator))
            {
                var candidate = Path.Combine(folder, name);
                if (
                    File.Exists(candidate)
                    && (File.GetUnixFileMode(candidate) & (UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute)) != 0
                )
                    return "\"" + candidate + "\"";
            }
        }

        throw new IOException("No editor found. Install nano/vi or configure VISUAL or EDITOR.");
    }
}
