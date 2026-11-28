using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Wolfgang.Barefoot.Data.Schema;

namespace Wolfgang.Barefoot.Data.Tests.Unit.Schema;

public sealed class SchemaProbeTests
{
    private const string First = "20260101000000_First";

    private const string Second = "20260201000000_Second";

    private const string Third = "20260301000000_Third";



    [Fact]
    public void Compare_when_the_database_holds_every_shipped_migration_is_up_to_date()
    {
        var status = SchemaProbe.Compare([First, Second], [First, Second]);

        Assert.Equal(SchemaState.UpToDate, status.State);
        Assert.True(status.UpToDate);
        Assert.Equal(Second, status.Current);
        Assert.Equal(Second, status.Expected);
        Assert.Empty(status.Pending);
        Assert.Empty(status.Unknown);
    }



    [Fact]
    public void Compare_when_the_database_holds_none_lists_every_shipped_migration_as_pending()
    {
        var status = SchemaProbe.Compare([First, Second], []);

        Assert.Equal(SchemaState.MissingMigration, status.State);
        Assert.False(status.UpToDate);
        Assert.Null(status.Current);
        Assert.Equal(Second, status.Expected);
        Assert.Equal([First, Second], status.Pending);
        Assert.Empty(status.Unknown);
    }



    [Fact]
    public void Compare_when_the_database_is_behind_lists_the_pending_migrations_oldest_first()
    {
        var status = SchemaProbe.Compare([Third, First, Second], [First]);

        Assert.Equal(SchemaState.MissingMigration, status.State);
        Assert.Equal(First, status.Current);
        Assert.Equal(Third, status.Expected);
        Assert.Equal([Second, Third], status.Pending);
        Assert.Empty(status.Unknown);
    }



    [Fact]
    public void Compare_when_the_database_holds_a_migration_the_build_does_not_ship_is_a_newer_schema()
    {
        var status = SchemaProbe.Compare([First], [Third, First, Second]);

        Assert.Equal(SchemaState.NewerSchema, status.State);
        Assert.False(status.UpToDate);
        Assert.Equal(Third, status.Current);
        Assert.Equal(First, status.Expected);
        Assert.Empty(status.Pending);
        Assert.Equal([Second, Third], status.Unknown);
    }



    [Fact]
    public void Compare_when_one_migration_is_unknown_and_another_pending_is_a_newer_schema()
    {
        var status = SchemaProbe.Compare([First, Second], [First, Third]);

        Assert.Equal(SchemaState.NewerSchema, status.State);
        Assert.False(status.UpToDate);
        Assert.Equal([Second], status.Pending);
        Assert.Equal([Third], status.Unknown);
    }



    [Fact]
    public void Compare_when_ids_differ_only_in_case_treats_them_as_the_same_migration()
    {
        var status = SchemaProbe.Compare([First], [First.ToUpperInvariant()]);

        Assert.Equal(SchemaState.UpToDate, status.State);
    }



    [Fact]
    public void Compare_when_the_build_ships_none_and_the_database_holds_none_is_up_to_date()
    {
        var status = SchemaProbe.Compare([], []);

        Assert.Equal(SchemaState.UpToDate, status.State);
        Assert.Null(status.Current);
        Assert.Null(status.Expected);
    }



    [Fact]
    public void Compare_when_shipped_is_null_throws()
    {
        Assert.Throws<ArgumentNullException>("shipped", () => SchemaProbe.Compare(null!, []));
    }



    [Fact]
    public void Compare_when_applied_is_null_throws()
    {
        Assert.Throws<ArgumentNullException>("applied", () => SchemaProbe.Compare([], null!));
    }



    [Fact]
    public async Task ReadAsync_when_context_is_null_throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>("context", () => SchemaProbe.ReadAsync(null!));
    }



    [Fact]
    public async Task ReadAsync_when_the_database_has_no_migration_history_lists_InitialCreate_as_pending()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();

        var status = await SchemaProbe.ReadAsync(context);

        Assert.Equal(SchemaState.MissingMigration, status.State);
        Assert.Null(status.Current);
        Assert.Equal(Shipped(context), status.Expected);
        Assert.Equal([Shipped(context)], status.Pending);
    }



    [Fact]
    public async Task ReadAsync_when_the_history_holds_the_shipped_migration_is_up_to_date()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        await WriteHistoryAsync(context, Shipped(context));

        var status = await SchemaProbe.ReadAsync(context);

        Assert.Equal(SchemaState.UpToDate, status.State);
        Assert.Equal(Shipped(context), status.Current);
        Assert.Equal(Shipped(context), status.Expected);
    }



    [Fact]
    public async Task ReadAsync_when_the_history_holds_a_later_migration_is_a_newer_schema()
    {
        const string Later = "99990101000000_Later";

        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        await WriteHistoryAsync(context, Shipped(context), Later);

        var status = await SchemaProbe.ReadAsync(context);

        Assert.Equal(SchemaState.NewerSchema, status.State);
        Assert.Equal(Later, status.Current);
        Assert.Equal(Shipped(context), status.Expected);
        Assert.Equal([Later], status.Unknown);
    }



    [Fact]
    public async Task ReadAsync_when_cancelled_throws()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => SchemaProbe.ReadAsync(context, cancellation.Token));
    }



    private static string Shipped(BarefootDbContext context)
    {
        return Assert.Single(context.Database.GetMigrations());
    }



    /// <summary>
    /// Writes the migration history table as EF Core would after applying <paramref name="ids"/>,
    /// without running a migration: <c>InitialCreate</c> is written for SQL Server and the tests run on
    /// SQLite.
    /// </summary>
    private static async Task WriteHistoryAsync(BarefootDbContext context, params string[] ids)
    {
        var history = context.GetService<IHistoryRepository>();

        await context.Database.ExecuteSqlRawAsync(history.GetCreateScript());
        foreach (var id in ids)
        {
            await context.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(id, "10.0.15")));
        }
    }
}
