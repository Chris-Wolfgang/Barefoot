using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Wolfgang.Extensions.String;

namespace Wolfgang.Barefoot.Data.Conventions;

/// <summary>
/// Names every table, column, key, foreign key and index of a model the way
/// docs/DATABASE-CONVENTIONS.md says, so no configuration names one by hand.
/// </summary>
/// <remarks>
/// <list type="table">
/// <item><term>table</term><description>the entity's name, singular: <c>search_matrix</c></description></item>
/// <item><term>column</term><description>the property's name: <c>owner_id</c></description></item>
/// <item><term>primary key</term><description><c>pk_&lt;table&gt;</c></description></item>
/// <item><term>alternate key</term><description><c>ak_&lt;table&gt;_&lt;columns&gt;</c></description></item>
/// <item><term>foreign key</term><description><c>fk_&lt;table&gt;_&lt;columns&gt;</c></description></item>
/// <item><term>index</term><description><c>ix_&lt;table&gt;_&lt;columns&gt;</c>, or <c>ux_</c> when it is unique</description></item>
/// </list>
/// </remarks>
internal static class SchemaNames
{
    /// <summary>
    /// Names everything in the model. It runs last in <c>OnModelCreating</c>, after every
    /// configuration, so that it sees every key and index they made.
    /// </summary>
    /// <param name="modelBuilder">The model being built.</param>
    public static void Apply(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var table = TableName(entity);

            entity.SetTableName(table);

            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ColumnName(property));
            }

            foreach (var key in entity.GetKeys())
            {
                key.SetName(KeyName(table, key));
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                foreignKey.SetConstraintName(ForeignKeyName(table, foreignKey));
            }

            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(IndexName(table, index));
            }
        }
    }



    /// <summary>The name of an entity's table.</summary>
    /// <param name="entity">The entity.</param>
    public static string TableName(IReadOnlyEntityType entity)
    {
        return entity
            .ClrType
            .Name
            .ToSnakeCase();
    }



    /// <summary>The name of a property's column.</summary>
    /// <param name="property">The property.</param>
    public static string ColumnName(IReadOnlyProperty property)
    {
        return property
            .Name
            .ToSnakeCase();
    }



    /// <summary>The name of a key of <paramref name="table"/>.</summary>
    /// <param name="table">The table's name.</param>
    /// <param name="key">The key.</param>
    public static string KeyName(string table, IReadOnlyKey key)
    {
        return key.IsPrimaryKey() ? $"pk_{table}" : $"ak_{table}_{Columns(key.Properties)}";
    }



    /// <summary>The name of a foreign key of <paramref name="table"/>.</summary>
    /// <param name="table">The table's name.</param>
    /// <param name="foreignKey">The foreign key.</param>
    public static string ForeignKeyName(string table, IReadOnlyForeignKey foreignKey)
    {
        return $"fk_{table}_{Columns(foreignKey.Properties)}";
    }



    /// <summary>The name of an index of <paramref name="table"/>.</summary>
    /// <param name="table">The table's name.</param>
    /// <param name="index">The index.</param>
    public static string IndexName(string table, IReadOnlyIndex index)
    {
        return $"{(index.IsUnique ? "ux" : "ix")}_{table}_{Columns(index.Properties)}";
    }



    private static string Columns(IEnumerable<IReadOnlyProperty> properties)
    {
        return string.Join('_', properties.Select(ColumnName));
    }
}
