namespace Wolfgang.Barefoot.Data.Schema;

/// <summary>
/// The schema version of a database against the one this build expects. <see cref="SchemaProbe"/>
/// makes it.
/// </summary>
public sealed class SchemaStatus
{
    internal SchemaStatus
    (
        string? current,
        string? expected,
        IReadOnlyList<string> pending,
        IReadOnlyList<string> unknown
    )
    {
        Current = current;
        Expected = expected;
        Pending = pending;
        Unknown = unknown;
    }



    /// <summary>
    /// The id of the latest migration the database holds, or <see langword="null"/> when it holds none.
    /// </summary>
    public string? Current { get; }



    /// <summary>
    /// The id of the latest migration this build ships, or <see langword="null"/> when it ships none.
    /// </summary>
    public string? Expected { get; }



    /// <summary>The ids of the migrations this build ships and the database does not hold, oldest first.</summary>
    public IReadOnlyList<string> Pending { get; }



    /// <summary>The ids of the migrations the database holds and this build does not ship, oldest first.</summary>
    public IReadOnlyList<string> Unknown { get; }



    /// <summary>
    /// <see cref="SchemaState.NewerSchema"/> when the database holds a migration this build does not
    /// ship, whatever else is pending; otherwise <see cref="SchemaState.MissingMigration"/> when one is
    /// pending; otherwise <see cref="SchemaState.UpToDate"/>.
    /// </summary>
    public SchemaState State
    {
        get
        {
            if (Unknown.Count > 0)
            {
                return SchemaState.NewerSchema;
            }

            return Pending.Count > 0 ? SchemaState.MissingMigration : SchemaState.UpToDate;
        }
    }



    /// <summary>Whether <see cref="State"/> is <see cref="SchemaState.UpToDate"/>.</summary>
    public bool UpToDate => State == SchemaState.UpToDate;
}
