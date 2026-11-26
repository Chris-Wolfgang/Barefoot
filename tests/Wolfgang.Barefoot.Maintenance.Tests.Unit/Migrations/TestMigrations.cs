using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Wolfgang.Barefoot.Data;

namespace Wolfgang.Barefoot.Maintenance.Tests.Unit.Migrations;

/// <summary>
/// Two small migrations SQLite can run. They stand in for the ones the build ships, which are written
/// for SQL Server, when a test has <c>bf-maint</c> apply migrations.
/// </summary>
internal static class TestMigrations
{
    /// <summary>The id of the older migration. It creates <c>first_thing</c> with one row.</summary>
    public const string First = "20000101000000_First";

    /// <summary>The id of the newer migration. It creates <c>second_thing</c>.</summary>
    public const string Second = "20000102000000_Second";
}



[DbContext(typeof(BarefootDbContext))]
[Migration(TestMigrations.First)]
internal sealed class FirstMigration : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE TABLE first_thing (id INTEGER NOT NULL PRIMARY KEY)");
        migrationBuilder.Sql("INSERT INTO first_thing (id) VALUES (1)");
    }
}



[DbContext(typeof(BarefootDbContext))]
[Migration(TestMigrations.Second)]
internal sealed class SecondMigration : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE TABLE second_thing (id INTEGER NOT NULL PRIMARY KEY)");
    }
}
