namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// One resort a search looks up. A search names a resort at most once.
/// </summary>
public sealed class SearchResort
{
    /// <summary>The row's key.</summary>
    public required Guid Id { get; set; }



    /// <summary>The <see cref="SearchMatrix"/> that names the resort.</summary>
    public required Guid SearchMatrixId { get; set; }



    /// <summary>The <see cref="Resort"/> it names.</summary>
    public required Guid ResortId { get; set; }
}
