namespace Thoughfall.Application;

/// <summary>A saved thought, preserving its text and the offset at which it was recorded.</summary>
public sealed record DiaryEntry
{
    public DiaryEntry(Guid id, DateTimeOffset timestamp, string text)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("An entry must have an ID.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(text);
        Id = id;
        Timestamp = timestamp;
        Text = text;
    }

    public Guid Id { get; }

    public DateTimeOffset Timestamp { get; }

    public string Text { get; }
}
