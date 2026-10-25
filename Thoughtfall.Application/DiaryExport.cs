namespace Thoughtfall.Application;

using System.Globalization;
using System.Text.Json;

/// <summary>Formats diary entries as grouped JSON or Markdown.</summary>
public static class DiaryExport
{
    /// <summary>Writes year/month/day groups without closing the caller's stream.</summary>
    /// <param name="output">The destination stream.</param>
    /// <param name="entries">The entries to order and export.</param>
    public static void WriteJson(Stream output, IEnumerable<DiaryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(output);

        var ordered = DiaryEntries.SelectRange(entries, DateOnly.MinValue, DateOnly.MaxValue);

        using (var writer = new Utf8JsonWriter(output, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();

            foreach (var year in ordered.GroupBy(entry => entry.Timestamp.Year))
                WriteJsonYear(writer, year);

            writer.WriteEndObject();
        }
    }

    /// <summary>Writes notes beneath localized calendar headings with LF newlines, leaving the writer open and its settings unchanged.</summary>
    /// <param name="output">The destination writer.</param>
    /// <param name="entries">The entries to order and export.</param>
    public static void WriteMarkdown(TextWriter output, IEnumerable<DiaryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(output);

        var ordered = DiaryEntries.SelectRange(entries, DateOnly.MinValue, DateOnly.MaxValue);
        var culture = CultureInfo.CurrentCulture;
        var calendar = culture.DateTimeFormat.Calendar;

        var containsUnsupportedDate = ordered.Any(entry =>
            entry.Timestamp.DateTime < calendar.MinSupportedDateTime || entry.Timestamp.DateTime > calendar.MaxSupportedDateTime
        );

        if (containsUnsupportedDate)
            throw new InvalidDataException("An entry's date is outside the range supported by your regional calendar. Choose a narrower date range.");

        var yearFormat = calendar.Eras.Length > 1 ? "gg yyyy" : "yyyy";

        (int Era, int Year)? previousYear = null;

        int? previousMonth = null;
        DateTime? previousDay = null;

        foreach (var entry in ordered)
        {
            var date = entry.Timestamp.DateTime;
            var year = (calendar.GetEra(date), calendar.GetYear(date));
            var month = calendar.GetMonth(date);

            if (year != previousYear)
            {
                output.Write($"# {date.ToString(yearFormat, culture)}\n\n");
                previousYear = year;
                previousMonth = null;
            }

            if (month != previousMonth)
            {
                output.Write($"## {date.ToString("MMMM", culture)}\n\n");
                previousMonth = month;
            }

            if (date.Date != previousDay)
            {
                output.Write($"### {date.ToString("D", culture)}\n\n");
                previousDay = date.Date;
            }

            var text = entry.Text.ReplaceLineEndings("\n");
            output.Write(text);

            if (!text.EndsWith('\n'))
                output.Write('\n');

            output.Write('\n');
        }
    }

    private static void WriteJsonYear(Utf8JsonWriter writer, IGrouping<int, DiaryEntry> year)
    {
        writer.WriteStartObject(year.Key.ToString("D4", CultureInfo.InvariantCulture));

        foreach (var month in year.GroupBy(entry => entry.Timestamp.Month))
            WriteJsonMonth(writer, month);

        writer.WriteEndObject();
    }

    private static void WriteJsonMonth(Utf8JsonWriter writer, IGrouping<int, DiaryEntry> month)
    {
        writer.WriteStartObject(month.Key.ToString("D2", CultureInfo.InvariantCulture));

        foreach (var day in month.GroupBy(entry => entry.Timestamp.Day))
            WriteJsonDay(writer, day);

        writer.WriteEndObject();
    }

    private static void WriteJsonDay(Utf8JsonWriter writer, IGrouping<int, DiaryEntry> day)
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
}
