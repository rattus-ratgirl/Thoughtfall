namespace Thoughfall.Application;

using System.Globalization;
using System.Text.Json;

public static class DiaryExport
{
    /// <summary>Writes year/month/day groups without closing the caller's stream.</summary>
    public static void WriteJson(Stream output, IEnumerable<DiaryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(output);
        var ordered = DiaryEntries.SelectRange(entries, DateOnly.MinValue, DateOnly.MaxValue);
        using var writer = new Utf8JsonWriter(output, new JsonWriterOptions { Indented = true });
        writer.WriteStartObject();
        foreach (var year in ordered.GroupBy(entry => entry.Timestamp.Year))
        {
            writer.WriteStartObject(year.Key.ToString("D4", CultureInfo.InvariantCulture));
            foreach (var month in year.GroupBy(entry => entry.Timestamp.Month))
            {
                writer.WriteStartObject(month.Key.ToString("D2", CultureInfo.InvariantCulture));
                foreach (var day in month.GroupBy(entry => entry.Timestamp.Day))
                {
                    writer.WriteStartArray(day.Key.ToString("D2", CultureInfo.InvariantCulture));
                    foreach (var entry in day)
                    {
                        writer.WriteStartObject();
                        writer.WriteString("id", entry.Id);
                        writer.WriteString("timestamp", entry.Timestamp.ToString("O", CultureInfo.InvariantCulture));
                        writer.WriteString("text", entry.Text);
                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray();
                }

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    /// <summary>Writes quoted entry text with LF newlines, leaving the caller's writer open and unchanged.</summary>
    public static void WriteMarkdown(TextWriter output, IEnumerable<DiaryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(output);
        var ordered = DiaryEntries.SelectRange(entries, DateOnly.MinValue, DateOnly.MaxValue);
        output.Write("# Thoughfall\n\n");
        foreach (var year in ordered.GroupBy(entry => entry.Timestamp.Year))
        {
            output.Write(FormattableString.Invariant($"## {year.Key:D4}\n\n"));
            foreach (var month in year.GroupBy(entry => entry.Timestamp.Month))
            {
                var monthName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month.Key);
                output.Write(FormattableString.Invariant($"### {month.Key:D2} — {monthName}\n\n"));
                foreach (var day in month.GroupBy(entry => entry.Timestamp.Day))
                {
                    output.Write(FormattableString.Invariant($"#### {year.Key:D4}-{month.Key:D2}-{day.Key:D2}\n\n"));
                    foreach (var entry in day)
                    {
                        output.Write($"##### {entry.Timestamp.ToString("O", CultureInfo.InvariantCulture)}\n\n");
                        output.Write($"ID: `{entry.Id}`\n\n");
                        // Quoting keeps headings in the entry from escaping its date group.
                        using var text = new StringReader(entry.Text);
                        while (text.ReadLine() is { } line)
                        {
                            output.Write($"> {line}\n");
                        }

                        output.Write('\n');
                    }
                }
            }
        }
    }
}
