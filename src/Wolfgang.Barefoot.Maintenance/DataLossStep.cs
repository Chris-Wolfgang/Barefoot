namespace Wolfgang.Barefoot.Maintenance;

/// <summary>
/// One step of a downgrade that would lose data (docs/MIGRATE.md). <see cref="DataLossScreen"/> finds it.
/// </summary>
/// <param name="MigrationId">The id of the migration whose rollback holds the step.</param>
/// <param name="Description">What the step does, such as <c>drops column run.status</c>.</param>
internal sealed record DataLossStep(string MigrationId, string Description);
