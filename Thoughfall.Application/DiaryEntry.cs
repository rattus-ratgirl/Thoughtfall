namespace Thoughfall.Application;

/// <summary>A saved thought, preserving its text and the offset at which it was recorded.</summary>
public sealed record DiaryEntry
{
    /// <summary>Initializes a new instance of the <see cref="DiaryEntry"/> class.</summary>
    /// <param name="id">The entry's nonempty UUID.</param>
    /// <param name="timestamp">The recorded time and original UTC offset.</param>
    /// <param name="text">The exact entry text.</param>
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

    /// <summary>Gets the entry's UUID.</summary>
    public Guid Id { get; }

    /// <summary>Gets the recorded time and original UTC offset.</summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>Gets the exact entry text.</summary>
    public string Text { get; }
}
