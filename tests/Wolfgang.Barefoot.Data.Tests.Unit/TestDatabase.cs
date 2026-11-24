using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Wolfgang.Barefoot.Data.Tests.Unit;

/// <summary>
/// An empty Barefoot database in memory, on SQLite. It lives as long as its connection, so every
/// context made from it sees the same rows.
/// </summary>
internal sealed class TestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private readonly DbContextOptions<BarefootDbContext> _options;



    private TestDatabase(SqliteConnection connection)
    {
        _connection = connection;
        _options = new DbContextOptionsBuilder<BarefootDbContext>()
            .UseSqlite(connection)
            .Options;
    }



    /// <summary>Opens a database and creates every table of the model in it.</summary>
    public static async Task<TestDatabase> CreateAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var database = new TestDatabase(connection);

        await using var context = database.NewContext();
        await context.Database.EnsureCreatedAsync();

        return database;
    }



    /// <summary>A context of its own, with nothing tracked, over the same rows.</summary>
    public BarefootDbContext NewContext()
    {
        return new BarefootDbContext(_options);
    }



    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}
