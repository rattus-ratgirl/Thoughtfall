namespace Thoughtfall.Tests;

using System.Globalization;
using NUnit.Framework;
using Thoughtfall.Application;
using Thoughtfall.Desktop.Capture;

internal sealed class CaptureHistoryTests
{
    [Test]
    public async Task PagesRemainStableWhenAnotherEntryIsSaved()
    {
        var folder = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"history-{Guid.NewGuid():N}");
        try
        {
            var store = new DiaryStore(folder);
            for (var i = 0; i < 7; i++)
                await store.SaveAsync($"entry {i}");
            var newest = await store.ReadPreviousAsync(null, 3);
            await store.SaveAsync("new entry from another capture");
            var middle = await store.ReadPreviousAsync(newest[0].Id, 3);
            var oldest = await store.ReadPreviousAsync(middle[0].Id, 3);
            Assert.Multiple(() =>
            {
                Assert.That(newest.Select(entry => entry.Text), Is.EqualTo(new[] { "entry 4", "entry 5", "entry 6" }));
                Assert.That(middle.Select(entry => entry.Text), Is.EqualTo(new[] { "entry 1", "entry 2", "entry 3" }));
                Assert.That(oldest.Select(entry => entry.Text), Is.EqualTo(new[] { "entry 0" }));
            });
            Assert.That(await store.ReadPreviousAsync(oldest[0].Id, 3), Is.Empty);
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }
    }

    [TestCase("en-GB", "Thursday, 5 November 2026 19:30")]
    [TestCase("en-US", "Thursday, November 5, 2026 7:30 PM")]
    [TestCase("de-DE", "Donnerstag, 5. November 2026 19:30")]
    public void HistoryUsesTheCurrentCulture(string culture, string expected)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var entry = new DiaryEntry(Guid.NewGuid(), new DateTimeOffset(2026, 11, 5, 19, 30, 0, TimeSpan.Zero), "  exact text\n");
            var history = new CaptureHistoryEntry(entry);
            Assert.That(history.Timestamp, Is.EqualTo(expected));
            Assert.That(history.Text, Is.EqualTo(entry.Text));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
