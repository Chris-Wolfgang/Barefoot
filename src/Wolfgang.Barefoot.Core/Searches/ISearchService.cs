using Wolfgang.Barefoot.Contracts;
using Wolfgang.Barefoot.Contracts.Searches;

namespace Wolfgang.Barefoot.Core.Searches;

/// <summary>
/// Reads and edits searches (DESIGN §4). The caller sees only the searches visible to them: their own,
/// or every search for an Admin. A search that is not visible is reported as not found.
/// </summary>
/// <remarks>
/// Every change names the <c>version</c> the search was read at; a stale version is refused
/// (ADR 0012 §3).
/// </remarks>
public interface ISearchService
{
    /// <summary>
    /// One page of the searches visible to the caller.
    /// </summary>
    /// <param name="page">Which page.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<Page<SearchResponse>> ListAsync(PageRequest page, CancellationToken cancellationToken);



    /// <summary>
    /// One search.
    /// </summary>
    /// <param name="id">The search's id.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<SearchResponse> GetAsync(Guid id, CancellationToken cancellationToken);



    /// <summary>
    /// Creates a search as a Draft.
    /// </summary>
    /// <param name="definition">What the search looks for. It is validated; a name already used by the owner is refused.</param>
    /// <param name="ownerId">
    /// The user the search is for, when an Admin creates it for someone else; <see langword="null"/>
    /// for the caller's own.
    /// </param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<SearchResponse> CreateAsync
    (
        SearchDefinition definition,
        Guid? ownerId,
        CancellationToken cancellationToken
    );



    /// <summary>
    /// Replaces a search's definition.
    /// </summary>
    /// <param name="id">The search's id.</param>
    /// <param name="version">The version the search was read at.</param>
    /// <param name="definition">The new definition. It is validated.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<SearchResponse> UpdateAsync
    (
        Guid id,
        long version,
        SearchDefinition definition,
        CancellationToken cancellationToken
    );



    /// <summary>
    /// Makes a Draft or an Inactive search Active, so that it runs with every run.
    /// </summary>
    /// <param name="id">The search's id.</param>
    /// <param name="version">The version the search was read at.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<SearchResponse> ActivateAsync
    (
        Guid id,
        long version,
        CancellationToken cancellationToken
    );



    /// <summary>
    /// Makes an Active search Inactive. Its results are kept.
    /// </summary>
    /// <param name="id">The search's id.</param>
    /// <param name="version">The version the search was read at.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<SearchResponse> DeactivateAsync
    (
        Guid id,
        long version,
        CancellationToken cancellationToken
    );



    /// <summary>
    /// Deletes a Draft that never ran. Any other search is refused.
    /// </summary>
    /// <param name="id">The search's id.</param>
    /// <param name="version">The version the search was read at.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task DeleteAsync
    (
        Guid id,
        long version,
        CancellationToken cancellationToken
    );
}
