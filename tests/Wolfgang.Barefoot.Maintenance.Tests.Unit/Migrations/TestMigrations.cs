using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Wolfgang.Barefoot.Data;

namespace Wolfgang.Barefoot.Maintenance.Tests.Unit.Migrations;

/// <summary>
/// Two small migrations SQLite can run. They stand in for the ones the build ships, which are written
/// for SQL Server, when a test has <c>bf-maint</c> apply migrations or roll them back.
/// </summary>
internal static class TestMigrations
{
    /// <summary>
    /// The id of the older migration. It creates the table <c>first_thing</c> with one row; rolling it
    /// back drops the table, which loses data.
    /// </summary>
    public const string First = "20000101000000_First";

    /// <summary>
    /// The id of the newer migration. It creates the index <see cref="Index"/> on <c>first_thing</c>;
    /// rolling it back drops the index, which loses nothing.
    /// </summary>
    public const string Second = "20000102000000_Second";

    /// <summary>The table <see cref="First"/> creates.</summary>
    public const string Table = "first_thing";

    /// <summary>The index <see cref="Second"/> creates.</summary>
    public const string Index = "ix_first_thing_id";
}



[DbContext(typeof(BarefootDbContext))]
[Migration(TestMigrations.First)]
internal sealed class FirstMigration : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"CREATE TABLE {TestMigrations.Table} (id INTEGER NOT NULL PRIMARY KEY)");
        migrationBuilder.Sql($"INSERT INTO {TestMigrations.Table} (id) VALUES (1)");
    }



    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(TestMigrations.Table);
    }
}



[DbContext(typeof(BarefootDbContext))]
[Migration(TestMigrations.Second)]
internal sealed class SecondMigration : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(TestMigrations.Index, TestMigrations.Table, "id");
    }



    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(TestMigrations.Index, TestMigrations.Table);
    }
}
