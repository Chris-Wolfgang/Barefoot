using Wolfgang.Barefoot.Contracts.Catalog;

namespace Wolfgang.Barefoot.Business.Catalog;

/// <summary>
/// Deals with a code that appears more than once in a catalog snapshot, so that it never fails the
/// refresh (DESIGN §5): entries that say the same thing count once, and entries that disagree are
/// all left out and named in a warning.
/// </summary>
public static class SnapshotCodes
{
    /// <summary>
    /// The airports of <paramref name="snapshot"/> to apply, one per code, and a warning for every
    /// code that was left out.
    /// </summary>
    /// <param name="snapshot">The airports as the scraper read them.</param>
    public static SnapshotEntries<AirportSnapshotEntry> Settle(AirportSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return Settle
        (
            snapshot.Airports,
            "airport",
            entry => entry.Code,
            (entry, code) => entry with { Code = code }
        );
    }



    /// <summary>
    /// The resorts of <paramref name="snapshot"/> to apply, one per code, and a warning for every
    /// code that was left out.
    /// </summary>
    /// <param name="snapshot">The resorts as the scraper read them.</param>
    public static SnapshotEntries<ResortSnapshotEntry> Settle(ResortSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return Settle
        (
            snapshot.Resorts,
            "resort",
            entry => entry.Code,
            (entry, code) => entry with { Code = code }
        );
    }



    /// <summary>
    /// A code as the catalog stores it: without spaces around it and in upper case.
    /// </summary>
    /// <param name="code">A code as read.</param>
    public static string Normalized(string code)
    {
        ArgumentNullException.ThrowIfNull(code);

        return code
            .Trim()
            .ToUpperInvariant();
    }



    private static SnapshotEntries<T> Settle<T>
    (
        IReadOnlyList<T> entries,
        string noun,
        Func<T, string> codeOf,
        Func<T, string, T> withCode
    )
        where T : notnull
    {
        var kept = new List<T>();
        var warnings = new List<string>();

        var byCode = entries
            .Select(entry => withCode(entry, Normalized(codeOf(entry))))
            .GroupBy(codeOf, StringComparer.Ordinal)
            .OrderBy(sameCode => sameCode.Key, StringComparer.Ordinal);

        foreach (var sameCode in byCode)
        {
            var different = sameCode
                .Distinct()
                .ToList();

            if (sameCode.Key.Length == 0)
            {
                warnings.Add($"{sameCode.Count()} {noun} entries have no code. They were skipped.");
            }
            else if (different.Count > 1)
            {
                warnings.Add($"The {noun} {sameCode.Key} appears {sameCode.Count()} times with different details. It was left untouched.");
            }
            else
            {
                kept.Add(different[0]);
            }
        }

        return new SnapshotEntries<T>(kept, warnings);
    }
}
