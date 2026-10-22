namespace Thoughtfall.Tests;

using System.Globalization;
using System.Text.Json;
using NUnit.Framework;
using Thoughtfall.Application;

/// <summary>Checks JSON and Markdown exports using in-memory destinations.</summary>
public sealed class DiaryExportTests
{
    /// <summary>Checks the JSON contract, exact entry values, and stream ownership.</summary>
    [Test]
    public void WriteJsonPreservesExistingExportShapeAndExactEntryData()
    {
        var entry = new DiaryEntry(
            Guid.Parse("94f48cfb-75a7-4b97-88ec-a5400524422b"),
            DateTimeOffset.ParseExact("2026-10-19T14:35:00.1234567+01:00", "O", CultureInfo.InvariantCulture),
            "A thought.\r\nAnother line. 🐀 \"quote\" \\ tab\t"
        );
        using var stream = new MemoryStream();

        DiaryExport.WriteJson(stream, [entry]);
        stream.Position = 0;
        using var document = JsonDocument.Parse(stream);
        var saved = document.RootElement.GetProperty("2026").GetProperty("10").GetProperty("19")[0];

        Assert.Multiple(() =>
        {
            Assert.That(saved.EnumerateObject().Select(property => property.Name), Is.EqualTo(new[] { "id", "timestamp", "text" }));
            Assert.That(saved.GetProperty("id").GetGuid(), Is.EqualTo(entry.Id));
            Assert.That(saved.GetProperty("timestamp").GetString(), Is.EqualTo("2026-10-19T14:35:00.1234567+01:00"));
            Assert.That(saved.GetProperty("text").GetString(), Is.EqualTo(entry.Text));
            Assert.That(stream.CanWrite, Is.True);
        });
    }

    /// <summary>Checks chronological JSON groups and entry ordering.</summary>
    [Test]
    public void WriteJsonGroupsAndOrdersAcrossYearsMonthsAndDays()
    {
        var entries = new[]
        {
            Entry("2027-01-01T12:00:00.0000000+00:00", "new year"),
            Entry("2026-12-31T23:59:00.0000000-05:00", "last day"),
            Entry("2026-11-30T10:00:00.0000000+00:00", "later"),
            Entry("2026-11-30T09:00:00.0000000+00:00", "earlier"),
            Entry("2026-11-29T12:00:00.0000000+00:00", "previous day"),
        };
        using var stream = new MemoryStream();
        DiaryExport.WriteJson(stream, entries);
        using var document = JsonDocument.Parse(stream.ToArray());
        var root = document.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(root.EnumerateObject().Select(property => property.Name), Is.EqualTo(new[] { "2026", "2027" }));
            Assert.That(root.GetProperty("2026").EnumerateObject().Select(property => property.Name), Is.EqualTo(new[] { "11", "12" }));
            Assert.That(
                root.GetProperty("2026").GetProperty("11").EnumerateObject().Select(property => property.Name),
                Is.EqualTo(new[] { "29", "30" })
            );
            Assert.That(
                root.GetProperty("2026").GetProperty("11").GetProperty("30").EnumerateArray().Select(item => item.GetProperty("text").GetString()),
                Is.EqualTo(new[] { "earlier", "later" })
            );
        });
    }

    /// <summary>Checks that large Unicode entries are exported without truncation.</summary>
    [Test]
    public void WriteJsonPreservesLargeText()
    {
        var text = string.Concat(Enumerable.Repeat("🐀 café\r\n", 50000));
        using var stream = new MemoryStream();
        DiaryExport.WriteJson(stream, [Entry("2026-10-22T12:00:00.0000000+00:00", text)]);
        using var document = JsonDocument.Parse(stream.ToArray());

        Assert.That(
            document.RootElement.GetProperty("2026").GetProperty("10").GetProperty("22")[0].GetProperty("text").GetString(),
            Is.EqualTo(text)
        );
    }

    /// <summary>Checks the JSON representation of an empty export.</summary>
    [Test]
    public void WriteJsonWithNoEntriesProducesEmptyObject()
    {
        using var stream = new MemoryStream();
        DiaryExport.WriteJson(stream, []);
        using var document = JsonDocument.Parse(stream.ToArray());

        Assert.That(document.RootElement.EnumerateObject(), Is.Empty);
    }

    /// <summary>Checks Markdown quotation, LF output, and caller-owned writer settings.</summary>
    [Test]
    public void WriteMarkdownQuotesTextAndNormalizesNewlinesWithoutChangingWriter()
    {
        var entry = Entry("2026-10-22T12:00:00.1234567+05:30", "# My heading\r\n\r\n🐀 café\rLast\n");
        using var writer = new StringWriter(CultureInfo.InvariantCulture) { NewLine = "\r\n" };

        DiaryExport.WriteMarkdown(writer, [entry]);

        var expected =
            "# Thoughtfall\n\n## 2026\n\n### 10 — October\n\n#### 2026-10-22\n\n"
            + "##### 2026-10-22T12:00:00.1234567+05:30\n\n"
            + $"ID: `{entry.Id}`\n\n> # My heading\n> \n> 🐀 café\n> Last\n\n";
        Assert.That(writer.ToString(), Is.EqualTo(expected));
        Assert.That(writer.NewLine, Is.EqualTo("\r\n"));
        Assert.DoesNotThrow(() => writer.Write("still open"));
    }

    /// <summary>Checks entry ordering and culture-independent date headings.</summary>
    [Test]
    public void WriteMarkdownOrdersEntriesAndUsesInvariantHeadings()
    {
        var later = Entry("2027-01-01T00:00:00.0000000-12:00", "later");
        var earlier = Entry("2026-12-31T23:59:59.0000000+14:00", "earlier");
        using var writer = new StringWriter(CultureInfo.GetCultureInfo("fr-FR"));

        DiaryExport.WriteMarkdown(writer, [later, earlier]);
        var text = writer.ToString();

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("### 12 — December\n"));
            Assert.That(text, Does.Contain("### 01 — January\n"));
            Assert.That(text.IndexOf("> earlier", StringComparison.Ordinal), Is.LessThan(text.IndexOf("> later", StringComparison.Ordinal)));
        });
    }

    /// <summary>Checks that an empty Markdown export contains only its title.</summary>
    [Test]
    public void WriteMarkdownWithNoEntriesProducesTitle()
    {
        using var writer = new StringWriter();
        DiaryExport.WriteMarkdown(writer, []);

        Assert.That(writer.ToString(), Is.EqualTo("# Thoughtfall\n\n"));
    }

    /// <summary>Checks that both formatters reject a null destination.</summary>
    [Test]
    public void WritersRejectNullOutput()
    {
        Assert.Throws<ArgumentNullException>(() => DiaryExport.WriteJson(null!, []));
        Assert.Throws<ArgumentNullException>(() => DiaryExport.WriteMarkdown(null!, []));
    }

    /// <summary>Checks that null entries are rejected before any output is written.</summary>
    [Test]
    public void WritersRejectNullEntriesBeforeWriting()
    {
        using var stream = new MemoryStream();
        using var writer = new StringWriter();

        Assert.Throws<ArgumentNullException>(() => DiaryExport.WriteJson(stream, null!));
        Assert.Throws<ArgumentNullException>(() => DiaryExport.WriteMarkdown(writer, null!));
        Assert.That(stream.Length, Is.Zero);
        Assert.That(writer.ToString(), Is.Empty);
    }

    private static DiaryEntry Entry(string timestamp, string text) =>
        new(Guid.NewGuid(), DateTimeOffset.ParseExact(timestamp, "O", CultureInfo.InvariantCulture), text);
}
