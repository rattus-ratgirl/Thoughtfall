namespace Thoughfall.Application;

/// <summary>Selects and orders entries by their recorded local dates.</summary>
public static class DiaryEntries
{
    /// <summary>
    /// Selects inclusive dates at each entry's original offset, ordered by local time then instant.
    /// Entries with identical timestamps retain their input order.
    /// </summary>
    /// <param name="entries">The entries to select from.</param>
    /// <param name="from">The inclusive first date.</param>
    /// <param name="to">The inclusive last date.</param>
    /// <returns>A materialized selection in diary order.</returns>
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
