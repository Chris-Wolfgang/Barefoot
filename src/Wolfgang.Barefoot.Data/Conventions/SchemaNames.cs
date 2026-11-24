using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

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
            var table = SnakeCase.Of(entity.ClrType.Name);

            entity.SetTableName(table);

            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(SnakeCase.Of(property.Name));
            }

            foreach (var key in entity.GetKeys())
            {
                key.SetName(key.IsPrimaryKey() ? $"pk_{table}" : $"ak_{table}_{Columns(key.Properties)}");
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                foreignKey.SetConstraintName($"fk_{table}_{Columns(foreignKey.Properties)}");
            }

            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName($"{(index.IsUnique ? "ux" : "ix")}_{table}_{Columns(index.Properties)}");
            }
        }
    }



    private static string Columns(IEnumerable<IReadOnlyProperty> properties)
    {
        return string.Join('_', properties.Select(property => SnakeCase.Of(property.Name)));
    }
}
