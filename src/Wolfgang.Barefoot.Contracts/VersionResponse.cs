namespace Wolfgang.Barefoot.Contracts;

/// <summary>
/// What <c>GET /version</c> returns (ADR 0010 §8). The call is anonymous and never touches the database.
/// </summary>
/// <param name="BuildVersion">The version of the API build that answered, for a person to read.</param>
/// <param name="ContractVersion">The contract version the API serves, for example <c>v0</c>.</param>
/// <param name="Revision">The contract revision the API was built for; see <see cref="Contracts.ContractVersion.Revision"/>.</param>
public sealed record VersionResponse
(
    string BuildVersion,
    string ContractVersion,
    int Revision
);
