namespace Thoughfall.Application;

public static class DiaryEntries
{
    /// <summary>
    /// Selects inclusive dates at each entry's original offset, ordered by local time then instant.
    /// Entries with identical timestamps retain their input order.
    /// </summary>
    public static IReadOnlyList<DiaryEntry> SelectRange(IEnumerable<DiaryEntry> entries, DateOnly from, DateOnly to)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (from > to)
        {
            throw new ArgumentException("The From date must be on or before the To date.");
        }

        return entries
            .Where(entry => DateOnly.FromDateTime(entry.Timestamp.DateTime) >= from && DateOnly.FromDateTime(entry.Timestamp.DateTime) <= to)
            .OrderBy(entry => entry.Timestamp.DateTime)
            .ThenBy(entry => entry.Timestamp)
            .ToArray();
    }
}
