namespace Thoughtfall.Desktop.Capture;

using System.Globalization;
using Thoughtfall.Application;

/// <summary>A saved thought and its locale-formatted recorded date and time.</summary>
public sealed record CaptureHistoryEntry(DiaryEntry Entry)
{
    public string Text => Entry.Text;
    public string Timestamp => Entry.Timestamp.ToString("f", CultureInfo.CurrentCulture);
}
