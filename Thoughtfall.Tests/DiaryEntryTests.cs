namespace Thoughtfall.Tests;

using NUnit.Framework;
using Thoughtfall.Application;

/// <summary>Checks entry identity and exact value preservation.</summary>
public sealed class DiaryEntryTests
{
    /// <summary>Checks that construction preserves text, timestamp precision, and offset.</summary>
    [Test]
    public void ConstructorPreservesTextAndTimestamp()
    {
        var id = Guid.NewGuid();
        var timestamp = new DateTimeOffset(2026, 10, 22, 12, 30, 0, TimeSpan.FromHours(5.5)).AddTicks(1234567);
        const string text = "  café 🐀\r\n\tA second line.\n\n";
        var entry = new DiaryEntry(id, timestamp, text);

        Assert.Multiple(() =>
        {
            Assert.That(entry.Id, Is.EqualTo(id));
            Assert.That(entry.Timestamp.EqualsExact(timestamp), Is.True);
            Assert.That(entry.Text, Is.EqualTo(text));
        });
    }

    /// <summary>Checks that an empty UUID is rejected.</summary>
    [Test]
    public void ConstructorRejectsEmptyId()
    {
        Assert.Throws<ArgumentException>(() => new DiaryEntry(Guid.Empty, DateTimeOffset.UnixEpoch, "text"));
    }

    /// <summary>Checks that null entry text is rejected.</summary>
    [Test]
    public void ConstructorRejectsNullText()
    {
        Assert.Throws<ArgumentNullException>(() => new DiaryEntry(Guid.NewGuid(), DateTimeOffset.UnixEpoch, null!));
    }
}
