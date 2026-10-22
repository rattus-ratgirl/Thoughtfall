namespace Thoughfall.Tests;

using System.Globalization;
using NUnit.Framework;
using Thoughfall.Application;

public sealed class DiaryEntriesTests
{
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

    [Test]
    public void SelectRangePreservesInputOrderForIdenticalTimestamps()
    {
        var first = Entry("2026-10-22T00:00:00.0000000+00:00");
        var second = Entry("2026-10-22T00:00:00.0000000+00:00");

        var result = DiaryEntries.SelectRange([second, first], DateOnly.MinValue, DateOnly.MaxValue);

        Assert.That(result, Is.EqualTo(new[] { second, first }));
    }

    [Test]
    public void SelectRangeReturnsIndependentSnapshot()
    {
        var entry = Entry("2026-10-22T00:00:00.0000000+00:00");
        var source = new List<DiaryEntry> { entry };
        var result = DiaryEntries.SelectRange(source, DateOnly.MinValue, DateOnly.MaxValue);
        source.Clear();

        Assert.That(result, Is.EqualTo(new[] { entry }));
    }

    [Test]
    public void SelectRangeReturnsEmptyWhenNothingMatches()
    {
        var entry = Entry("2026-10-22T00:00:00.0000000+00:00");
        Assert.That(DiaryEntries.SelectRange([entry], new DateOnly(2026, 10, 23), new DateOnly(2026, 10, 24)), Is.Empty);
        Assert.That(DiaryEntries.SelectRange([], DateOnly.MinValue, DateOnly.MaxValue), Is.Empty);
    }

    [Test]
    public void SelectRangeRejectsReversedDates()
    {
        Assert.Throws<ArgumentException>(() => DiaryEntries.SelectRange([], new DateOnly(2026, 10, 23), new DateOnly(2026, 10, 22)));
    }

    [Test]
    public void SelectRangeRejectsNullSource()
    {
        Assert.Throws<ArgumentNullException>(() => DiaryEntries.SelectRange(null!, DateOnly.MinValue, DateOnly.MaxValue));
    }

    private static DiaryEntry Entry(string timestamp) =>
        new(Guid.NewGuid(), DateTimeOffset.ParseExact(timestamp, "O", CultureInfo.InvariantCulture), "thought");
}
