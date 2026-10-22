namespace Thoughtfall.Desktop;

using System.Diagnostics;

/// <summary>Reports unexpected failures without including exception messages that may contain diary text.</summary>
internal static class ErrorReporting
{
    /// <summary>Writes exception types, error codes, and stack traces to diagnostics and standard error.</summary>
    /// <param name="operation">The operation that failed.</param>
    /// <param name="error">The failure and its inner exceptions.</param>
    internal static void Unexpected(string operation, Exception error)
    {
        // Exception messages can include user input. Keep diagnostics to types, codes and call sites.
        for (var current = error; current is not null; current = current.InnerException)
        {
            var diagnostic = $"Error {operation}: {current.GetType().FullName} (0x{current.HResult:X8})\n{current.StackTrace}";
            Trace.TraceError(diagnostic);
            Console.Error.WriteLine(diagnostic);
        }
    }
}
