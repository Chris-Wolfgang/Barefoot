using Wolfgang.Barefoot.Contracts;

namespace Wolfgang.Barefoot.Core.Versioning;

/// <summary>
/// Reads which contract the API serves (ADR 0010 §8). The API answers from constants; a client asks
/// over HTTP, without a token, before its first real call.
/// </summary>
public interface IVersionService
{
    /// <summary>
    /// The API's build version, contract version and contract revision.
    /// </summary>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<VersionResponse> GetVersionAsync(CancellationToken cancellationToken);
}
