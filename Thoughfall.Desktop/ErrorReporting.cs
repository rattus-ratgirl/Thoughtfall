namespace Thoughfall.Desktop;

using System.Diagnostics;

internal static class ErrorReporting
{
    internal static void Unexpected(string operation, Exception error)
    {
        // Exception messages can include user input. Keep diagnostics to types, codes and call sites.
        for (Exception? current = error; current is not null; current = current.InnerException)
        {
            var diagnostic = $"Error {operation}: {current.GetType().FullName} (0x{current.HResult:X8})\n{current.StackTrace}";
            Trace.TraceError(diagnostic);
            Console.Error.WriteLine(diagnostic);
        }
    }
}
