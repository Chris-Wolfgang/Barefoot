namespace Wolfgang.Barefoot.Core.Digests;

/// <summary>
/// Everything one digest says, already decided: which rooms matched, what changed, what needs
/// attention. <see cref="DigestRenderer"/> only lays it out.
/// </summary>
/// <param name="ProductName">The product's name, shown in the header, the links and the footer.</param>
/// <param name="Tagline">The line under the product's name.</param>
/// <param name="SearchName">The search the digest is for.</param>
/// <param name="SearchedOn">The date of the run, in the reader's time zone.</param>
/// <param name="MatchCount">How many room options match the search's filters.</param>
/// <param name="Attention">Sentences the reader must not miss, for example a resort that was skipped; empty for none.</param>
/// <param name="Criteria">The lines of "What was searched": the search as resolved for this run, and how much of it completed.</param>
/// <param name="Resorts">The resorts, in the order to show them.</param>
/// <param name="ResultsUrl">The link that opens the whole digest's results.</param>
public sealed record DigestModel
(
    string ProductName,
    string Tagline,
    string SearchName,
    DateOnly SearchedOn,
    int MatchCount,
    IReadOnlyList<string> Attention,
    IReadOnlyList<string> Criteria,
    IReadOnlyList<DigestResort> Resorts,
    Uri ResultsUrl
);
