namespace Thoughfall.Application;

using System.Diagnostics;
using System.Text;

internal static class ExportFile
{
    internal static void Write(string destination, IReadOnlyList<DiaryEntry> entries, ExportFormat format)
    {
        var temporary = Path.Combine(Path.GetDirectoryName(destination)!, $".thoughfall-export-{Guid.NewGuid():N}.tmp");
        try
        {
            using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                switch (format)
                {
                    case ExportFormat.Json:
                        DiaryExport.WriteJson(output, entries);
                        break;
                    case ExportFormat.Markdown:
                        using (var writer = new StreamWriter(output, new UTF8Encoding(false, true), leaveOpen: true))
                        {
                            DiaryExport.WriteMarkdown(writer, entries);
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(format));
                }

                output.Flush(flushToDisk: true);
            }

            File.Move(temporary, destination, overwrite: true);
        }
        finally
        {
            try
            {
                File.Delete(temporary);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Trace.TraceWarning($"Export temporary-file cleanup failed: {ex.Message}");
            }
        }
    }
}
