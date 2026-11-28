using Wolfgang.Barefoot.Contracts.Catalog;

namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// One catalog snapshot the API received, and what came of it (ADR 0017 §5). A row is written for
/// every snapshot, also for one that changed nothing and for one that was refused, so "reference
/// data last refreshed" can be read from here. A row is never changed after it is written.
/// </summary>
public sealed class CatalogRefresh
{
    /// <summary>The row's key.</summary>
    public required Guid Id { get; set; }



    /// <summary>Which catalog the snapshot was of.</summary>
    public required CatalogKind Kind { get; set; }



    /// <summary>When the API received the snapshot.</summary>
    public DateTimeOffset ReceivedUtc { get; set; }



    /// <summary>How many entries were new. Zero for a refused snapshot.</summary>
    public int AddedCount { get; set; }



    /// <summary>How many entries changed. Zero for a refused snapshot.</summary>
    public int UpdatedCount { get; set; }



    /// <summary>How many entries are no longer offered. Zero for a refused snapshot.</summary>
    public int DeactivatedCount { get; set; }



    /// <summary>How many entries were left untouched with a warning.</summary>
    public int WarningCount { get; set; }



    /// <summary>
    /// Why the whole snapshot was refused, in one sentence, or <see langword="null"/> when it was
    /// applied.
    /// </summary>
    public string? RefusalReason { get; set; }
}
