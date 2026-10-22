namespace Thoughfall.Tests;

using System.Globalization;
using NUnit.Framework;
using Thoughfall.Application;

/// <summary>Checks recorded-date selection and stable diary ordering.</summary>
public sealed class DiaryEntriesTests
{
    /// <summary>Checks inclusive date boundaries independently of UTC conversion.</summary>
    [Test]
    public void SelectRangeIncludesWholeBoundaryDaysAtRecordedOffsets()
    {
        var first = Entry("2026-12-31T00:00:00.0000000+14:00");
        var last = Entry("2027-01-01T23:59:59.9999999-12:00");
        var before = Entry("2026-12-30T23:59:59.9999999-12:00");
        var after = Entry("2027-01-02T00:00:00.0000000+14:00");

        var result = DiaryEntries.SelectRange([after, last, before, first], new DateOnly(2026, 12, 31), new DateOnly(2027, 1, 1));

        Assert.That(result, Is.EqualTo(new[] { first, last }));
    }

    /// <summary>Checks ordering when a clock change repeats a local time.</summary>
    [Test]
    public void SelectRangeOrdersLocalTimeThenInstantDuringClockChange()
    {
        var secondOccurrence = Entry("2026-10-25T01:30:00.0000000+00:00");
        var firstOccurrence = Entry("2026-10-25T01:30:00.0000000+01:00");
        var earlierClockTime = Entry("2026-10-25T01:15:00.0000000+00:00");

        var result = DiaryEntries.SelectRange(
            [secondOccurrence, firstOccurrence, earlierClockTime],
            new DateOnly(2026, 10, 25),
            new DateOnly(2026, 10, 25)
        );

        Assert.That(result, Is.EqualTo(new[] { earlierClockTime, firstOccurrence, secondOccurrence }));
    }

    /// <summary>Checks stable ordering for exact timestamp ties.</summary>
    [Test]
    public void SelectRangePreservesInputOrderForIdenticalTimestamps()
    {
        var first = Entry("2026-10-22T00:00:00.0000000+00:00");
        var second = Entry("2026-10-22T00:00:00.0000000+00:00");

        var result = DiaryEntries.SelectRange([second, first], DateOnly.MinValue, DateOnly.MaxValue);

        Assert.That(result, Is.EqualTo(new[] { second, first }));
    }

    /// <summary>Checks that later source changes do not alter the selection.</summary>
    [Test]
    public void SelectRangeReturnsIndependentSnapshot()
    {
        var entry = Entry("2026-10-22T00:00:00.0000000+00:00");
        var source = new List<DiaryEntry> { entry };
        var result = DiaryEntries.SelectRange(source, DateOnly.MinValue, DateOnly.MaxValue);
        source.Clear();

        Assert.That(result, Is.EqualTo(new[] { entry }));
    }

    /// <summary>Checks empty sources and ranges without matches.</summary>
    [Test]
    public void SelectRangeReturnsEmptyWhenNothingMatches()
    {
        var entry = Entry("2026-10-22T00:00:00.0000000+00:00");
        Assert.That(DiaryEntries.SelectRange([entry], new DateOnly(2026, 10, 23), new DateOnly(2026, 10, 24)), Is.Empty);
        Assert.That(DiaryEntries.SelectRange([], DateOnly.MinValue, DateOnly.MaxValue), Is.Empty);
    }

    /// <summary>Checks that a reversed date range is rejected.</summary>
    [Test]
    public void SelectRangeRejectsReversedDates()
    {
        Assert.Throws<ArgumentException>(() => DiaryEntries.SelectRange([], new DateOnly(2026, 10, 23), new DateOnly(2026, 10, 22)));
    }

    /// <summary>Checks that a null entry source is rejected.</summary>
    [Test]
    public void SelectRangeRejectsNullSource()
    {
        Assert.Throws<ArgumentNullException>(() => DiaryEntries.SelectRange(null!, DateOnly.MinValue, DateOnly.MaxValue));
    }

    private static DiaryEntry Entry(string timestamp) =>
        new(Guid.NewGuid(), DateTimeOffset.ParseExact(timestamp, "O", CultureInfo.InvariantCulture), "thought");
}
