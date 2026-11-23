using Wolfgang.Barefoot.Contracts.Searches;

namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// The link that says one search wants one lookup in one run (ADR 0004 §2). Several searches may
/// link to the same <see cref="SearchRequest"/>; a search links to it at most once.
/// </summary>
public sealed class SearchItem
{
    /// <summary>The row's key.</summary>
    public required Guid Id { get; set; }



    /// <summary>The <see cref="MatrixRun"/> of the search that wants the lookup.</summary>
    public required Guid MatrixRunId { get; set; }



    /// <summary>The <see cref="SearchRequest"/> it wants.</summary>
    public required Guid SearchRequestId { get; set; }



    /// <summary>
    /// The cabin the search wants flights in, or <see langword="null"/> when it wants none. The cabin
    /// affects flights, not rooms, so it is here and not on the lookup.
    /// </summary>
    public Cabin? Cabin { get; set; }
}
