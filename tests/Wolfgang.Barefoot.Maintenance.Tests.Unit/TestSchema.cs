using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Wolfgang.Barefoot.Data;

namespace Wolfgang.Barefoot.Maintenance.Tests.Unit;

/// <summary>
/// A database in memory, on SQLite, that holds a migration history and nothing else. It lives as long
/// as its connection. <see cref="Open"/> stands in for SQL Server when a test runs <c>bf-maint</c>.
/// </summary>
internal sealed class TestSchema : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private readonly DbContextOptions<BarefootDbContext> _options;

    private readonly List<string> _connectionStrings = [];



    private TestSchema(SqliteConnection connection)
    {
        _connection = connection;
        _options = new DbContextOptionsBuilder<BarefootDbContext>()
            .UseSqlite(connection)
            .Options;
    }



    /// <summary>The id of the one migration this build ships.</summary>
    public static string Shipped
    {
        get
        {
            using var context = new BarefootDbContext
            (
                new DbContextOptionsBuilder<BarefootDbContext>()
                    .UseSqlite("DataSource=:memory:")
                    .Options
            );

            return Assert.Single(context.Database.GetMigrations());
        }
    }



    /// <summary>The connection strings <see cref="Open"/> was called with, in order.</summary>
    public IReadOnlyList<string> ConnectionStrings => _connectionStrings;



    /// <summary>
    /// Opens a database whose history says <paramref name="applied"/> were applied. With none given
    /// the database has no history table at all.
    /// </summary>
    /// <remarks>
    /// The history is written directly, without running a migration: <c>InitialCreate</c> is written
    /// for SQL Server.
    /// </remarks>
    public static async Task<TestSchema> CreateAsync(params string[] applied)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var schema = new TestSchema(connection);
        if (applied.Length == 0)
        {
            return schema;
        }

        await using var context = new BarefootDbContext(schema._options);
        var history = context.GetService<IHistoryRepository>();

        await context.Database.ExecuteSqlRawAsync(history.GetCreateScript());
        foreach (var id in applied)
        {
            await context.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(id, "10.0.15")));
        }

        return schema;
    }



    /// <summary>A context on this database, whatever <paramref name="connectionString"/> says.</summary>
    public BarefootDbContext Open(string connectionString)
    {
        _connectionStrings.Add(connectionString);

        return new BarefootDbContext(_options);
    }



    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}
