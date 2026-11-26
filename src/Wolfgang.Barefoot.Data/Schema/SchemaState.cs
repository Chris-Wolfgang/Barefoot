namespace Wolfgang.Barefoot.Data.Schema;

/// <summary>
/// How the schema of a database stands against the migrations this build ships (ADR 0001 §4).
/// </summary>
public enum SchemaState
{
    /// <summary>The database holds every migration this build ships and no other.</summary>
    UpToDate = 0,

    /// <summary>This build ships a migration the database does not hold. <c>bf-maint migrate</c> applies it.</summary>
    MissingMigration = 1,

    /// <summary>
    /// The database holds a migration this build does not ship. Nothing may read, write or migrate it
    /// until the matching build is deployed.
    /// </summary>
    NewerSchema = 2,
}
