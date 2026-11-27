using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Wolfgang.Barefoot.Maintenance;

/// <summary>
/// Describes a schema as lines <c>item|detail</c>, the form <see cref="SchemaDrift"/> compares:
/// <list type="bullet">
/// <item><description><c>table app_user|</c></description></item>
/// <item><description><c>column app_user.email|nvarchar(320) null</c></description></item>
/// <item><description><c>key app_user.pk_app_user|primary (oid)</c></description></item>
/// <item><description><c>index run.ix_run_status|not unique (status) where status=0</c></description></item>
/// <item><description><c>foreign key recipient.fk_recipient_owner_id|(owner_id) -&gt; app_user (oid) on delete NO_ACTION</c></description></item>
/// </list>
/// The query a database is read with gives the same lines; <see cref="Normalized"/> evens out how
/// the two sides write an index filter.
/// </summary>
internal static class SchemaLines
{
    private const string FilterMark = " where ";



    /// <summary>
    /// The lines of the model <paramref name="context"/> was built with, for the provider it uses.
    /// </summary>
    /// <param name="context">A context. No connection is opened.</param>
    public static IReadOnlyList<string> Expected(DbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var model = context
            .GetService<IDesignTimeModel>()
            .Model
            .GetRelationalModel();

        return model
            .Tables
            .SelectMany(Of)
            .Select(Normalized)
            .ToList();
    }



    /// <summary>
    /// A line with the filter of an index, when it has one, in one spelling: lower case, with no
    /// brackets, parentheses or spaces. SQL Server stores <c>status = 0</c> as <c>([status]=(0))</c>.
    /// </summary>
    /// <param name="line">A line <c>item|detail</c>.</param>
    public static string Normalized(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        var cut = line.IndexOf(FilterMark, StringComparison.Ordinal);
        if (cut < 0)
        {
            return line;
        }

        var filter = line[(cut + FilterMark.Length)..]
            .Where(letter => letter is not ('[' or ']' or '(' or ')' or ' '))
            .Select(char.ToLowerInvariant);

        return line[..cut] + FilterMark + string.Concat(filter);
    }



    private static IEnumerable<string> Of(ITable table)
    {
        yield return $"table {table.Name}|";

        foreach (var column in table.Columns)
        {
            yield return $"column {table.Name}.{column.Name}|{column.StoreType} {(column.IsNullable ? "null" : "not null")}";
        }

        foreach (var key in table.UniqueConstraints)
        {
            yield return $"key {table.Name}.{key.Name}|{(key.GetIsPrimaryKey() ? "primary" : "unique")} ({Names(key.Columns)})";
        }

        foreach (var index in table.Indexes)
        {
            var filter = index.Filter is null ? string.Empty : FilterMark + index.Filter;

            yield return $"index {table.Name}.{index.Name}|{(index.IsUnique ? "unique" : "not unique")} ({Names(index.Columns)}){filter}";
        }

        foreach (var foreignKey in table.ForeignKeyConstraints)
        {
            yield return $"foreign key {table.Name}.{foreignKey.Name}|({Names(foreignKey.Columns)}) -> {foreignKey.PrincipalTable.Name} ({Names(foreignKey.PrincipalColumns)}) on delete {OnDelete(foreignKey.OnDeleteAction)}";
        }
    }



    private static string Names(IEnumerable<IColumn> columns)
    {
        return string.Join(',', columns.Select(column => column.Name));
    }



    /// <summary>
    /// The delete rule as SQL Server's catalog names it. <c>Restrict</c> and <c>NoAction</c> are the
    /// same rule there.
    /// </summary>
    internal static string OnDelete(ReferentialAction action)
    {
        return action switch
        {
            ReferentialAction.Cascade => "CASCADE",
            ReferentialAction.SetNull => "SET_NULL",
            ReferentialAction.SetDefault => "SET_DEFAULT",
            _ => "NO_ACTION",
        };
    }
}
