namespace Wolfgang.Barefoot.Contracts;

/// <summary>
/// The version of the API contract this assembly was built for (ADR 0010 §8).
/// </summary>
/// <remarks>
/// The API reports these two values from <c>GET /version</c>, and every client compares the revision
/// with its own before its first real call.
/// </remarks>
public static class ContractVersion
{
    /// <summary>
    /// The contract version, which is also the path segment every route lives under:
    /// <c>/api/v0/…</c>. <c>v0</c> is unstable and may change with a breaking change.
    /// </summary>
    public const string Name = "v0";



    /// <summary>
    /// The contract revision. Raise it by one with every breaking change to <c>v0</c>; the
    /// <c>openapi-diff</c> job fails a pull request whose change is breaking and leaves it as it was.
    /// </summary>
    public const int Revision = 1;
}
