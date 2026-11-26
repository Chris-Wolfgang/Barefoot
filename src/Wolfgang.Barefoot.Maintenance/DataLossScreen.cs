using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Wolfgang.Barefoot.Maintenance;

/// <summary>
/// Screens the operations of a migration's rollback for data loss (ADR 0001 §4, docs/MIGRATE.md).
/// These count: a dropped table, a dropped column, a narrowing column change, and raw SQL. It reads
/// no database, so it says what a rollback would do, not whether the table holds rows.
/// </summary>
internal static class DataLossScreen
{
    /// <summary>
    /// Finds the steps among <paramref name="operations"/> that would lose data, in the order given.
    /// </summary>
    /// <param name="migrationId">The id of the migration whose rollback <paramref name="operations"/> are.</param>
    /// <param name="operations">The operations of the rollback.</param>
    public static IReadOnlyList<DataLossStep> Find(string migrationId, IEnumerable<MigrationOperation> operations)
    {
        ArgumentNullException.ThrowIfNull(migrationId);
        ArgumentNullException.ThrowIfNull(operations);

        return
        [
            .. operations
                .Select(Describe)
                .OfType<string>()
                .Select(description => new DataLossStep(migrationId, description)),
        ];
    }



    private static string? Describe(MigrationOperation operation)
    {
        return operation switch
        {
            DropTableOperation table => $"drops table {table.Name}",
            DropColumnOperation column => $"drops column {column.Table}.{column.Name}",
            AlterColumnOperation column when Narrows(column) => $"narrows column {column.Table}.{column.Name}",
            // The tool does not inspect raw SQL, so it treats it as data-losing.
            SqlOperation => "runs raw SQL",
            _ => null,
        };
    }



    /// <summary>
    /// Whether the change can hold less than the column held before: a size that shrinks or becomes
    /// limited, another CLR type, or another column type where no size says which way it went.
    /// </summary>
    private static bool Narrows(AlterColumnOperation column)
    {
        var old = column.OldColumn;

        if (Shrinks(old.MaxLength, column.MaxLength)
            || Shrinks(old.Precision, column.Precision)
            || Shrinks(old.Scale, column.Scale)
            || old.ClrType != column.ClrType)
        {
            return true;
        }

        var sized = old.MaxLength.HasValue
            || column.MaxLength.HasValue
            || old.Precision.HasValue
            || column.Precision.HasValue;

        return !sized && !string.Equals(old.ColumnType, column.ColumnType, StringComparison.OrdinalIgnoreCase);
    }



    private static bool Shrinks(int? before, int? after)
    {
        return after.HasValue && (!before.HasValue || after.Value < before.Value);
    }
}
