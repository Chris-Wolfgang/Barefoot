namespace Wolfgang.Barefoot.Data.Migrations;

/// <summary>
/// The collation of the text columns that carry a uniqueness rule. The rule is checked in memory by
/// ordinal comparison of a normalized form, and SQL Server's default collation compares otherwise
/// (it ignores case, for one). A binary collation makes the database agree with .NET
/// (docs/DATABASE-CONVENTIONS.md, Types; Barefoot#128).
/// </summary>
/// <remarks>
/// The collation is in the migration and not in the model, because its name belongs to SQL Server
/// and the tests build the model on SQLite, whose default comparison is already binary.
/// <c>dotnet ef migrations add</c> does not write it: after <c>InitialCreate</c> is replaced, add
/// <c>collation: OrdinalCollation.Name</c> to each column of <see cref="Columns"/> again.
/// <c>InitialCreateTests</c> fails until that is done.
/// </remarks>
internal static class OrdinalCollation
{
    /// <summary>
    /// Compares UTF-16 code units, as <see cref="StringComparison.Ordinal"/> does.
    /// </summary>
    public const string Name = "Latin1_General_100_BIN2";



    /// <summary>
    /// Every column that has the collation, as table and column name.
    /// </summary>
    public static IReadOnlyList<(string Table, string Column)> Columns { get; } =
    [
        ("airport", "code"),
        ("recipient", "address"),
        ("resort", "code"),
        ("room_category", "code"),
        ("search_matrix", "normalized_name"),
        ("setting", "key"),
        ("vocabulary_value", "value"),
    ];
}
