using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Wolfgang.Barefoot.Core.Entities;
using Wolfgang.Barefoot.Data.Migrations;

namespace Wolfgang.Barefoot.Data.Tests.Unit.Migrations;

/// <summary>
/// The one migration, <c>InitialCreate</c>: that it is in step with the model, and what SQL it
/// renders for SQL Server. No test here opens a connection, so none proves that SQL Server accepts
/// that SQL.
/// </summary>
public sealed class InitialCreateTests
{
    private const string SqlServer = "Microsoft.EntityFrameworkCore.SqlServer";

    private const string Name = "InitialCreate";



    [Fact]
    public void CreateDbContext_when_called_gives_a_context_on_SQL_Server()
    {
        using var context = NewContext();

        Assert.Equal(SqlServer, context.Database.ProviderName);
    }



    [Fact]
    public void GetMigrations_when_asked_of_the_context_lists_InitialCreate_alone()
    {
        using var context = NewContext();

        var migration = Assert.Single(context.Database.GetMigrations());

        Assert.EndsWith("_" + Name, migration, StringComparison.Ordinal);
    }



    [Fact]
    public void HasPendingModelChanges_when_the_migration_holds_the_whole_model_is_false()
    {
        using var context = NewContext();

        Assert.False(context.Database.HasPendingModelChanges());
    }



    [Fact]
    public void GenerateScript_when_going_up_creates_every_table_of_the_model()
    {
        using var context = NewContext();

        var script = UpScript(context);

        Assert.All
        (
            TableNames(context),
            table => Assert.Contains($"CREATE TABLE [{table}] (", script, StringComparison.Ordinal)
        );
    }



    [Fact]
    public void GenerateScript_when_going_up_inserts_the_Unassigned_user()
    {
        using var context = NewContext();

        var script = UpScript(context);

        Assert.Contains("INSERT INTO [app_user]", script, StringComparison.Ordinal);
        Assert.Contains
        (
            $"VALUES ('{AppUser.UnassignedOid}', 0, 0, 0, NULL, NULL, N'{AppUser.UnassignedDisplayName}', N'', '1900-01-01T00:00:00.000Z', NULL, NULL, CAST(1 AS bigint))",
            script,
            StringComparison.Ordinal
        );
    }



    [Fact]
    public void GenerateScript_when_going_up_restricts_the_delete_of_every_foreign_key()
    {
        using var context = NewContext();
        var foreignKeys = context
            .Model
            .GetEntityTypes()
            .SelectMany(entity => entity.GetForeignKeys())
            .Select(foreignKey => foreignKey.GetConstraintName())
            .ToList();

        var script = UpScript(context);

        Assert.NotEmpty(foreignKeys);
        Assert.All
        (
            foreignKeys,
            foreignKey => Assert.Contains($"CONSTRAINT [{foreignKey}] FOREIGN KEY", script, StringComparison.Ordinal)
        );
        Assert.Equal(foreignKeys.Count, Count(script, "ON DELETE NO ACTION"));
        Assert.Equal(0, Count(script, "ON DELETE CASCADE"));
        Assert.Equal(0, Count(script, "ON DELETE SET"));
    }



    [Fact]
    public void GenerateScript_when_going_down_drops_every_table_of_the_model()
    {
        using var context = NewContext();
        var migration = context.Database.GetMigrations().Single();

        var script = context
            .GetService<IMigrator>()
            .GenerateScript(fromMigration: migration, toMigration: Migration.InitialDatabase);

        Assert.All
        (
            TableNames(context),
            table => Assert.Contains($"DROP TABLE [{table}];", script, StringComparison.Ordinal)
        );
    }



    private static BarefootDbContext NewContext()
    {
        return new DesignTimeContextFactory().CreateDbContext([]);
    }



    private static string UpScript(BarefootDbContext context)
    {
        return context
            .GetService<IMigrator>()
            .GenerateScript();
    }



    private static List<string> TableNames(BarefootDbContext context)
    {
        var tables = context
            .Model
            .GetEntityTypes()
            .Select(entity => entity.GetTableName())
            .OfType<string>()
            .ToList();

        Assert.NotEmpty(tables);

        return tables;
    }



    private static int Count(string text, string part)
    {
        return text.Split(part).Length - 1;
    }
}
