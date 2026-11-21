using Wolfgang.Barefoot.Contracts;

namespace Wolfgang.Barefoot.Core.Versioning;

/// <summary>
/// The version check every client makes before its first real call (ADR 0010 §8): it compares the
/// contract revision the API reports with the one the client was built for.
/// </summary>
/// <remarks>
/// While the contract is <c>v0</c> the two revisions must be equal, so an API that is too old and one
/// that is too new are both detected. Once a version is frozen it changes only additively, so only a
/// too-old API fails.
/// </remarks>
public static class VersionCheck
{
    /// <summary>The contract version that is not frozen.</summary>
    private const string Unstable = "v0";



    /// <summary>
    /// Compares what the API reported with the contract this client was built for.
    /// </summary>
    /// <param name="api">The answer of <c>GET /version</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="api"/> is <see langword="null"/>.</exception>
    public static VersionCompatibility Evaluate(VersionResponse api)
    {
        ArgumentNullException.ThrowIfNull(api);

        return Compare(api.Revision, ContractVersion.Revision, IsFrozen(ContractVersion.Name));
    }



    /// <summary>
    /// Compares two contract revisions.
    /// </summary>
    /// <param name="apiRevision">The revision the API reported.</param>
    /// <param name="clientRevision">The revision the client was built for.</param>
    /// <param name="contractIsFrozen">
    /// Whether the contract version is frozen, so that a newer API still serves an older client.
    /// </param>
    public static VersionCompatibility Compare
    (
        int apiRevision,
        int clientRevision,
        bool contractIsFrozen
    )
    {
        if (apiRevision < clientRevision)
        {
            return VersionCompatibility.ApiTooOld;
        }

        return apiRevision > clientRevision && !contractIsFrozen
            ? VersionCompatibility.ApiTooNew
            : VersionCompatibility.Compatible;
    }



    /// <summary>
    /// Whether <paramref name="contractVersion"/> is frozen: every version except <c>v0</c> is.
    /// </summary>
    /// <param name="contractVersion">A contract version, for example <c>v0</c> or <c>v1</c>.</param>
    public static bool IsFrozen(string contractVersion)
    {
        return !string.Equals(contractVersion, Unstable, StringComparison.Ordinal);
    }



    /// <summary>
    /// The sentence a client shows for <paramref name="compatibility"/>, saying which side to update,
    /// or <see langword="null"/> when there is nothing to say.
    /// </summary>
    /// <param name="compatibility">The outcome of the check.</param>
    public static string? MessageFor(VersionCompatibility compatibility)
    {
        return compatibility switch
        {
            VersionCompatibility.ApiTooOld => "The API is older than this tool. Deploy the API.",
            VersionCompatibility.ApiTooNew => "This tool is older than the API. Update it.",
            _ => null,
        };
    }
}
