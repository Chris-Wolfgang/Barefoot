namespace Wolfgang.Barefoot.Business.Catalog;

/// <summary>
/// What is left of a catalog snapshot once repeated codes are settled (<see cref="SnapshotCodes"/>).
/// </summary>
/// <typeparam name="T">The type of an entry.</typeparam>
/// <param name="Entries">The entries to apply, one per code, in the order of their codes. Each code is in its stored form.</param>
/// <param name="Warnings">One sentence per code that was left out, in the order of the codes.</param>
public sealed record SnapshotEntries<T>(IReadOnlyList<T> Entries, IReadOnlyList<string> Warnings)
    where T : notnull;
