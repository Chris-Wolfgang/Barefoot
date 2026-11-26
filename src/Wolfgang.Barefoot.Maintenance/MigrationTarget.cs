namespace Wolfgang.Barefoot.Maintenance;

/// <summary>
/// The migration an operator named as a target: its id, its name, a prefix of either, or <c>0</c> for
/// an empty schema (docs/MIGRATE.md, <c>--to</c>). <see cref="Resolve"/> makes it from the text and the
/// migrations this build ships. It reads no database.
/// </summary>
internal sealed class MigrationTarget
{
    /// <summary>The target that means an empty schema: every migration rolled back.</summary>
    public const string EmptySchema = "0";



    private MigrationTarget(MigrationTargetKind kind, string? id, IReadOnlyList<string> candidates)
    {
        Kind = kind;
        Id = id;
        Candidates = candidates;
    }



    /// <summary>Whether the text named one target, none or several.</summary>
    public MigrationTargetKind Kind { get; }



    /// <summary>
    /// The id of the target migration, or <see cref="EmptySchema"/>. <see langword="null"/> unless
    /// <see cref="Kind"/> is <see cref="MigrationTargetKind.Found"/>.
    /// </summary>
    public string? Id { get; }



    /// <summary>
    /// The ids the text matched, oldest first: several when <see cref="Kind"/> is
    /// <see cref="MigrationTargetKind.Ambiguous"/>, otherwise none.
    /// </summary>
    public IReadOnlyList<string> Candidates { get; }



    /// <summary>
    /// Finds the migration <paramref name="text"/> names among <paramref name="shipped"/>. Case is
    /// ignored. A whole id or a whole name is preferred over a prefix, so a migration whose name starts
    /// another's name can still be named.
    /// </summary>
    /// <param name="shipped">The ids of the migrations this build ships, such as <c>20261125193944_InitialCreate</c>.</param>
    /// <param name="text">An id, a name (the part of an id after its first underscore), a prefix of either, or <c>0</c>.</param>
    public static MigrationTarget Resolve(IEnumerable<string> shipped, string text)
    {
        ArgumentNullException.ThrowIfNull(shipped);
        ArgumentNullException.ThrowIfNull(text);

        var wanted = text.Trim();
        if (wanted.Length == 0)
        {
            return new MigrationTarget(MigrationTargetKind.NotFound, id: null, []);
        }

        if (string.Equals(wanted, EmptySchema, StringComparison.Ordinal))
        {
            return new MigrationTarget(MigrationTargetKind.Found, EmptySchema, []);
        }

        var ids = shipped
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var whole = ids
            .Where(id => Matches(id, wanted, (candidate, value) => candidate.Equals(value, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (whole.Count > 0)
        {
            return From(whole);
        }

        var prefixed = ids
            .Where(id => Matches(id, wanted, (candidate, value) => candidate.StartsWith(value, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return From(prefixed);
    }



    /// <summary>The name of the migration <paramref name="id"/>: the part after its first underscore.</summary>
    public static string NameOf(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        // Without an underscore IndexOf gives -1, so the range starts at 0 and the whole id is the name.
        var underscore = id.IndexOf('_', StringComparison.Ordinal);

        return id[(underscore + 1)..];
    }



    private static bool Matches(string id, string wanted, Func<string, string, bool> test)
    {
        return test(id, wanted) || test(NameOf(id), wanted);
    }



    private static MigrationTarget From(List<string> matches)
    {
        return matches.Count switch
        {
            0 => new MigrationTarget(MigrationTargetKind.NotFound, id: null, []),
            1 => new MigrationTarget(MigrationTargetKind.Found, matches[0], []),
            _ => new MigrationTarget(MigrationTargetKind.Ambiguous, id: null, matches),
        };
    }
}
