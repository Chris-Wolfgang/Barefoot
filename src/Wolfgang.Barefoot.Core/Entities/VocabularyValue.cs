using Wolfgang.Barefoot.Contracts.Catalog;

namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// One value of a room vocabulary (a level, a view, a type or a feature), discovered from results
/// and never authored (DESIGN §7). A value is never deleted: one no result has shown for a while is
/// greyed where a search is edited.
/// </summary>
public sealed class VocabularyValue
{
    /// <summary>The row's key.</summary>
    public required Guid Id { get; set; }



    /// <summary>Which vocabulary the value belongs to.</summary>
    public required VocabularyKind Kind { get; set; }



    /// <summary>The value as the site words it, unique within its vocabulary.</summary>
    public required string Value { get; set; }



    /// <summary>When a result first showed the value.</summary>
    public DateTimeOffset FirstSeenUtc { get; set; }



    /// <summary>When a result last showed the value.</summary>
    public DateTimeOffset LastSeenUtc { get; set; }
}
