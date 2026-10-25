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

        using (var document = JsonDocument.Parse(stream))
        {
            var saved = document.RootElement.GetProperty("2026").GetProperty("10").GetProperty("19")[0];

            Assert.Multiple(() =>
            {
                Assert.That(saved.EnumerateObject().Select(property => property.Name), Is.EqualTo(["id", "timestamp", "text"]));
                Assert.That(saved.GetProperty("id").GetGuid(), Is.EqualTo(entry.Id));
                Assert.That(saved.GetProperty("timestamp").GetString(), Is.EqualTo("2026-10-19T14:35:00.1234567+01:00"));
                Assert.That(saved.GetProperty("text").GetString(), Is.EqualTo(entry.Text));
            });

            Assert.That(stream.CanWrite, Is.True);
        }
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
            Assert.That(root.EnumerateObject().Select(property => property.Name), Is.EqualTo(["2026", "2027"]));
            Assert.That(root.GetProperty("2026").EnumerateObject().Select(property => property.Name), Is.EqualTo(["11", "12"]));

            Assert.That(root.GetProperty("2026").GetProperty("11").EnumerateObject().Select(property => property.Name), Is.EqualTo(["29", "30"]));

            Assert.That(
                root.GetProperty("2026").GetProperty("11").GetProperty("30").EnumerateArray().Select(item => item.GetProperty("text").GetString()),
                Is.EqualTo(["earlier", "later"])
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

    /// <summary>Checks plain Markdown, LF output, and caller-owned writer settings.</summary>
    [Test]
    [SetCulture("en-GB")]
    public void WriteMarkdownPreservesTextAndNormalizesNewlinesWithoutChangingWriter()
    {
        var entry = Entry("2026-10-22T12:00:00.1234567+05:30", "# My heading\r\n\r\n🐀 café\rLast\n");

        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        writer.NewLine = "\r\n";

        DiaryExport.WriteMarkdown(writer, [entry]);

        const string expected = "# 2026\n\n## October\n\n### Thursday, 22 October 2026\n\n# My heading\n\n🐀 café\nLast\n\n";

        Assert.That(writer.ToString(), Is.EqualTo(expected));
        Assert.That(writer.NewLine, Is.EqualTo("\r\n"));
        writer.Write("still open");
        Assert.That(writer.ToString(), Is.EqualTo(expected + "still open"));
    }

    /// <summary>Checks calendar grouping, chronological ordering, and recorded-offset dates.</summary>
    [Test]
    [SetCulture("en-GB")]
    public void WriteMarkdownGroupsAndOrdersUsingRecordedLocalDates()
    {
        var entries = new[]
        {
            Entry("2027-01-01T00:00:00.0000000+14:00", "new year"),
            Entry("2026-12-31T23:59:59.0000000-12:00", "last day"),
            Entry("2026-11-30T10:00:00.0000000+00:00", "later"),
            Entry("2026-11-30T09:00:00.0000000+00:00", "earlier"),
            Entry("2026-11-29T12:00:00.0000000+00:00", "previous day"),
        };

        using var writer = new StringWriter();

        DiaryExport.WriteMarkdown(writer, entries);

        var expected =
            "# 2026\n\n## November\n\n### Sunday, 29 November 2026\n\nprevious day\n\n"
            + "### Monday, 30 November 2026\n\nearlier\n\nlater\n\n"
            + "## December\n\n### Thursday, 31 December 2026\n\nlast day\n\n"
            + "# 2027\n\n## January\n\n### Friday, 1 January 2027\n\nnew year\n\n";

        Assert.That(writer.ToString(), Is.EqualTo(expected));
    }

    /// <summary>Checks that headings follow the current culture rather than the writer's culture.</summary>
    [Test]
    [SetCulture("fr-FR")]
    public void WriteMarkdownUsesLocalizedMonthNamesAndLongDates()
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);

        DiaryExport.WriteMarkdown(writer, [Entry("2026-10-22T12:00:00.0000000+00:00", "Une pensée.")]);

        Assert.That(writer.ToString(), Is.EqualTo("# 2026\n\n## octobre\n\n### jeudi 22 octobre 2026\n\nUne pensée.\n\n"));
    }

    /// <summary>Checks Islamic year and month boundaries using the current regional calendar.</summary>
    [Test]
    [SetCulture("ar-SA")]
    public void WriteMarkdownGroupsUsingTheIslamicCalendar()
    {
        var calendar = CultureInfo.CurrentCulture.DateTimeFormat.Calendar;
        var lastDay = calendar.ToDateTime(1448, 12, calendar.GetDaysInMonth(1448, 12), 12, 0, 0, 0);
        var newYear = calendar.ToDateTime(1449, 1, 1, 12, 0, 0, 0);
        var nextMonth = calendar.ToDateTime(1449, 2, 1, 12, 0, 0, 0);
        using var writer = new StringWriter();

        DiaryExport.WriteMarkdown(
            writer,
            [
                new DiaryEntry(Guid.NewGuid(), new DateTimeOffset(nextMonth, TimeSpan.Zero), "next month"),
                new DiaryEntry(Guid.NewGuid(), new DateTimeOffset(lastDay, TimeSpan.Zero), "last day"),
                new DiaryEntry(Guid.NewGuid(), new DateTimeOffset(newYear, TimeSpan.Zero), "new year"),
            ]
        );

        var expected =
            $"# 1448\n\n## {lastDay:MMMM}\n\n### {lastDay:D}\n\nlast day\n\n"
            + $"# 1449\n\n## {newYear:MMMM}\n\n### {newYear:D}\n\nnew year\n\n"
            + $"## {nextMonth:MMMM}\n\n### {nextMonth:D}\n\nnext month\n\n";

        Assert.That(writer.ToString(), Is.EqualTo(expected));
    }

    /// <summary>Checks that a calendar override is honored independently of the culture's default.</summary>
    [Test]
    public void WriteMarkdownHonorsTheConfiguredCalendar()
    {
        var previousCulture = CultureInfo.CurrentCulture;

        var culture = new CultureInfo("ar-SA", false) { DateTimeFormat = { Calendar = new GregorianCalendar() } };

        try
        {
            CultureInfo.CurrentCulture = culture;
            var date = new DateTime(2026, 10, 22);
            using var writer = new StringWriter();

            DiaryExport.WriteMarkdown(writer, [Entry("2026-10-22T12:00:00.0000000+00:00", "note")]);

            Assert.That(writer.ToString(), Is.EqualTo($"# 2026\n\n## {date:MMMM}\n\n### {date:D}\n\nnote\n\n"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    /// <summary>Checks leap-month names against dates in leap and common Hebrew years.</summary>
    [Test]
    public void WriteMarkdownNamesHebrewLeapMonthsCorrectly()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var culture = new CultureInfo("he-IL", false);
        var calendar = new HebrewCalendar();
        culture.DateTimeFormat.Calendar = calendar;

        try
        {
            CultureInfo.CurrentCulture = culture;
            var firstAdar = calendar.ToDateTime(5784, 6, 1, 12, 0, 0, 0);
            var secondAdar = calendar.ToDateTime(5784, 7, 1, 12, 0, 0, 0);
            var commonAdar = calendar.ToDateTime(5785, 6, 1, 12, 0, 0, 0);
            using var writer = new StringWriter();

            DiaryExport.WriteMarkdown(
                writer,
                [
                    new DiaryEntry(Guid.NewGuid(), new DateTimeOffset(commonAdar, TimeSpan.Zero), "common year"),
                    new DiaryEntry(Guid.NewGuid(), new DateTimeOffset(secondAdar, TimeSpan.Zero), "second Adar"),
                    new DiaryEntry(Guid.NewGuid(), new DateTimeOffset(firstAdar, TimeSpan.Zero), "first Adar"),
                ]
            );

            var expected =
                $"# {firstAdar:yyyy}\n\n## {firstAdar:MMMM}\n\n### {firstAdar:D}\n\nfirst Adar\n\n"
                + $"## {secondAdar:MMMM}\n\n### {secondAdar:D}\n\nsecond Adar\n\n"
                + $"# {commonAdar:yyyy}\n\n## {commonAdar:MMMM}\n\n### {commonAdar:D}\n\ncommon year\n\n";

            Assert.That(writer.ToString(), Is.EqualTo(expected));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    /// <summary>Checks that matching year numbers in separate Japanese eras stay separate.</summary>
    [Test]
    public void WriteMarkdownSeparatesCalendarEras()
    {
        var previousCulture = CultureInfo.CurrentCulture;

        var culture = new CultureInfo("ja-JP", false) { DateTimeFormat = { Calendar = new JapaneseCalendar() } };

        try
        {
            CultureInfo.CurrentCulture = culture;

            var heisei = new DateTime(1989, 5, 1);
            var reiwa = new DateTime(2019, 5, 1);

            using var writer = new StringWriter();

            DiaryExport.WriteMarkdown(
                writer,
                [Entry("2019-05-01T12:00:00.0000000+09:00", "Reiwa"), Entry("1989-05-01T12:00:00.0000000+09:00", "Heisei")]
            );

            var expected =
                $"# {heisei:gg yyyy}\n\n## {heisei:MMMM}\n\n### {heisei:D}\n\nHeisei\n\n"
                + $"# {reiwa:gg yyyy}\n\n## {reiwa:MMMM}\n\n### {reiwa:D}\n\nReiwa\n\n";

            Assert.That(writer.ToString(), Is.EqualTo(expected));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    /// <summary>Checks calendar range failures before any part of the document is written.</summary>
    /// <param name="timestamp">An entry date outside the calendar's supported range.</param>
    [TestCase("0001-01-01T12:00:00.0000000+00:00")]
    [TestCase("9999-12-31T12:00:00.0000000+00:00")]
    [SetCulture("ar-SA")]
    public void WriteMarkdownRejectsUnsupportedDatesBeforeWriting(string timestamp)
    {
        using var writer = new StringWriter(CultureInfo.GetCultureInfo("fr-FR"));

        try
        {
            DiaryExport.WriteMarkdown(writer, [Entry("2026-10-22T12:00:00.0000000+00:00", "valid"), Entry(timestamp, "unsupported")]);
            Assert.Fail("Expected an InvalidDataException for a date outside the regional calendar range.");
        }
        catch (InvalidDataException error)
        {
            Assert.That(error.Message, Does.Contain("regional calendar"));
        }

        Assert.That(writer.ToString(), Is.Empty);
    }

    /// <summary>Checks that an empty Markdown export produces no document content.</summary>
    [Test]
    public void WriteMarkdownWithNoEntriesProducesEmptyDocument()
    {
        using var writer = new StringWriter();
        DiaryExport.WriteMarkdown(writer, []);

        Assert.That(writer.ToString(), Is.Empty);
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

        try
        {
            DiaryExport.WriteJson(stream, null!);
            Assert.Fail("Expected an ArgumentNullException for null JSON entries.");
        }
        catch (ArgumentNullException)
        {
            Assert.That(stream.Length, Is.Zero);
        }

        try
        {
            DiaryExport.WriteMarkdown(writer, null!);
            Assert.Fail("Expected an ArgumentNullException for null Markdown entries.");
        }
        catch (ArgumentNullException)
        {
            Assert.That(writer.ToString(), Is.Empty);
        }
    }

    private static DiaryEntry Entry(string timestamp, string text) =>
        new(Guid.NewGuid(), DateTimeOffset.ParseExact(timestamp, "O", CultureInfo.InvariantCulture), text);
}
