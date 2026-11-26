using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Conventions;

/// <summary>
/// Checks a model against docs/DATABASE-CONVENTIONS.md (ADR 0004 §12). A test asserts that the
/// check of <see cref="BarefootDbContext"/>'s model finds nothing.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Every table is in the default schema.</description></item>
/// <item><description>Every table, column, key, foreign key and index has the name <see cref="SchemaNames"/> gives it, and no name is longer than <see cref="MaxNameLength"/> characters.</description></item>
/// <item><description>Every primary key is one <see cref="Guid"/> column; a <see cref="Setting"/> alone is keyed by its name.</description></item>
/// <item><description>Every foreign key restricts its delete.</description></item>
/// <item><description>Every <c>Version</c> is a <see cref="long"/> concurrency token.</description></item>
/// <item><description>Every instant is a <see cref="DateTimeOffset"/>, stored in UTC to the millisecond.</description></item>
/// </list>
/// </remarks>
internal static class ModelConventions
{
    /// <summary>The longest name SQL Server accepts for a table, column, constraint or index.</summary>
    internal const int MaxNameLength = 128;



    /// <summary>
    /// Lists every place where <paramref name="model"/> breaks a convention, one sentence each. The
    /// list is empty when it breaks none.
    /// </summary>
    /// <param name="model">The model to check.</param>
    /// <exception cref="ArgumentNullException"><paramref name="model"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Verify(IReadOnlyModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return
        [
            .. model
                .GetEntityTypes()
                .SelectMany
                (
                    entity => Problems(entity).Select(problem => $"{entity.ClrType.Name}: {problem}")
                ),
        ];
    }



    private static IEnumerable<string> Problems(IReadOnlyEntityType entity)
    {
        return SchemaProblems(entity)
            .Concat(NameProblems(entity))
            .Concat(KeyProblems(entity))
            .Concat(ForeignKeyProblems(entity))
            .Concat(VersionProblems(entity))
            .Concat(entity.GetProperties().SelectMany(InstantProblems));
    }



    private static IEnumerable<string> SchemaProblems(IReadOnlyEntityType entity)
    {
        if (entity.GetSchema() is { } schema)
        {
            yield return $"the table is in the schema '{schema}', not the default one.";
        }
    }



    private static IEnumerable<string> NameProblems(IReadOnlyEntityType entity)
    {
        foreach (var (what, actual, expected) in Names(entity))
        {
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                yield return $"the {what} is named '{actual}', not '{expected}'.";
            }

            if (expected.Length > MaxNameLength)
            {
                yield return $"the name '{expected}' has {expected.Length} characters, more than {MaxNameLength}.";
            }
        }
    }



    private static IEnumerable<(string What, string? Actual, string Expected)> Names(IReadOnlyEntityType entity)
    {
        var table = SchemaNames.TableName(entity);

        yield return ("table", entity.GetTableName(), table);

        foreach (var property in entity.GetProperties())
        {
            yield return ($"column of {property.Name}", property.GetColumnName(), SchemaNames.ColumnName(property));
        }

        foreach (var key in entity.GetKeys())
        {
            yield return ("key", key.GetName(), SchemaNames.KeyName(table, key));
        }

        foreach (var foreignKey in entity.GetForeignKeys())
        {
            yield return ("foreign key", foreignKey.GetConstraintName(), SchemaNames.ForeignKeyName(table, foreignKey));
        }

        foreach (var index in entity.GetIndexes())
        {
            yield return ("index", index.GetDatabaseName(), SchemaNames.IndexName(table, index));
        }
    }



    private static IEnumerable<string> KeyProblems(IReadOnlyEntityType entity)
    {
        var isOneGuid = entity.FindPrimaryKey() is { Properties: [var column] } && column.ClrType == typeof(Guid);

        // A setting is keyed by its name (docs/DATABASE-CONVENTIONS.md, Keys).
        if (!isOneGuid && entity.ClrType != typeof(Setting))
        {
            yield return "the primary key is not one Guid column.";
        }
    }



    private static IEnumerable<string> ForeignKeyProblems(IReadOnlyEntityType entity)
    {
        return entity
            .GetForeignKeys()
            .Where(foreignKey => foreignKey.DeleteBehavior != DeleteBehavior.Restrict)
            .Select
            (
                foreignKey => $"the foreign key on {string.Join(", ", foreignKey.Properties.Select(property => property.Name))} deletes with {foreignKey.DeleteBehavior}, not Restrict."
            );
    }



    private static IEnumerable<string> VersionProblems(IReadOnlyEntityType entity)
    {
        if (entity.FindProperty(VersionStamp.PropertyName) is { } version
            && !(version.ClrType == typeof(long) && version.IsConcurrencyToken))
        {
            yield return "Version is not a long concurrency token.";
        }
    }



    private static IEnumerable<string> InstantProblems(IReadOnlyProperty property)
    {
        var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;

        if (type == typeof(DateTime))
        {
            yield return $"{property.Name} is a DateTime; an instant is a DateTimeOffset.";
        }

        if (type == typeof(DateTimeOffset)
            && !(property.GetValueConverter() is UtcInstantConverter && property.GetPrecision() == BarefootModel.InstantPrecision))
        {
            yield return $"{property.Name} is not stored as a UTC date and time to the millisecond.";
        }
    }
}
