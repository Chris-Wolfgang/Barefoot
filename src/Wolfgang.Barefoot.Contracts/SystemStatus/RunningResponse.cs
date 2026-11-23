namespace Wolfgang.Barefoot.Contracts.SystemStatus;

/// <summary>
/// Which builds are running: the "What is running" panel of the Admin's Settings page (DESIGN §12).
/// The web site's own build is not here, because the web site knows it without asking.
/// </summary>
/// <param name="ApiBuildVersion">The version of the API build that answered.</param>
/// <param name="ContractVersion">The contract version the API serves, for example <c>v0</c>.</param>
/// <param name="Revision">The contract revision the API was built for.</param>
/// <param name="SchemaMigration">
/// The name of the last migration applied to the database, or <see langword="null"/> when it could
/// not be read.
/// </param>
/// <param name="SchemaUpToDate">Whether that is the last migration this build knows.</param>
/// <param name="WorkerName">The worker that last claimed work, or <see langword="null"/> when none ever has.</param>
/// <param name="WorkerBuildVersion">The version of that worker's build, or <see langword="null"/> when it is not known.</param>
/// <param name="WorkerLastSeenUtc">When it last claimed work, or <see langword="null"/>.</param>
public sealed record RunningResponse
(
    string ApiBuildVersion,
    string ContractVersion,
    int Revision,
    string? SchemaMigration,
    bool SchemaUpToDate,
    string? WorkerName,
    string? WorkerBuildVersion,
    DateTimeOffset? WorkerLastSeenUtc
);
