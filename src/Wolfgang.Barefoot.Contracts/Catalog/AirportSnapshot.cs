namespace Wolfgang.Barefoot.Contracts.Catalog;

/// <summary>
/// Every airport the booking site showed at one moment. The API compares it with the catalog and
/// applies the differences (DESIGN §5).
/// </summary>
/// <param name="Airports">The airports, as read. A code may repeat; the API deals with it.</param>
public sealed record AirportSnapshot(IReadOnlyList<AirportSnapshotEntry> Airports);
