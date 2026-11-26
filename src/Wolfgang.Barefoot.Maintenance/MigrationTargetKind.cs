namespace Wolfgang.Barefoot.Maintenance;

/// <summary>
/// What became of the text an operator gave as a target migration (docs/MIGRATE.md, <c>--to</c>).
/// </summary>
internal enum MigrationTargetKind
{
    /// <summary>The text names one migration, or the empty schema.</summary>
    Found = 0,

    /// <summary>The text names no migration this build ships.</summary>
    NotFound = 1,

    /// <summary>The text names more than one migration this build ships.</summary>
    Ambiguous = 2,
}
