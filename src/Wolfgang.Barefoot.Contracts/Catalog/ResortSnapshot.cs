namespace Wolfgang.Barefoot.Contracts.Catalog;

/// <summary>
/// Every resort the booking site showed at one moment. The API compares it with the catalog and
/// applies the differences, with a safety rule so that a bad read cannot take every resort offline
/// at once (DESIGN §5).
/// </summary>
/// <param name="Resorts">The resorts, as read. A code may repeat; the API deals with it.</param>
public sealed record ResortSnapshot(IReadOnlyList<ResortSnapshotEntry> Resorts);
