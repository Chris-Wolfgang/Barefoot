using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Wolfgang.Barefoot.Data;

namespace Wolfgang.Barefoot.Maintenance.Tests.Unit;

/// <summary>
/// A database in memory, on SQLite, that starts with a migration history and nothing else. It lives as
/// long as its connection. <see cref="Open"/> stands in for SQL Server when a test runs <c>bf-maint</c>.
/// </summary>
internal sealed class TestSchema : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private readonly DbContextOptions<BarefootDbContext> _options;

    private readonly List<string> _connectionStrings = [];



    private TestSchema(SqliteConnection connection, bool runnable)
    {
        _connection = connection;

        var options = new DbContextOptionsBuilder<BarefootDbContext>();
        _options = runnable
            ? options
                .UseSqlite(connection, sqlite => sqlite.MigrationsAssembly(typeof(TestSchema).Assembly))
                .Options
            : options
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



    /// <summary>
    /// A context on SQLite that names no database and knows the two migrations of
    /// <see cref="Migrations.TestMigrations"/>. It can render their SQL and cannot open a connection.
    /// </summary>
    public static BarefootDbContext Offline()
    {
        var options = new DbContextOptionsBuilder<BarefootDbContext>()
            .UseSqlite(sqlite => sqlite.MigrationsAssembly(typeof(TestSchema).Assembly))
            .Options;

        return new BarefootDbContext(options);
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
    public static Task<TestSchema> CreateAsync(params string[] applied)
    {
        return CreateAsync(runnable: false, applied);
    }



    /// <summary>
    /// As <see cref="CreateAsync(string[])"/>, but the migrations this database knows are the two of
    /// <see cref="Migrations.TestMigrations"/>, which SQLite can run.
    /// </summary>
    public static Task<TestSchema> CreateRunnableAsync(params string[] applied)
    {
        return CreateAsync(runnable: true, applied);
    }



    /// <summary>
    /// Opens a database on which the migrations of <see cref="Migrations.TestMigrations"/> were really
    /// run, up to and including <paramref name="upTo"/>.
    /// </summary>
    public static async Task<TestSchema> CreateMigratedAsync(string upTo)
    {
        var schema = await CreateAsync(runnable: true, []);

        await using var context = new BarefootDbContext(schema._options);
        await context.GetService<IMigrator>().MigrateAsync(upTo);

        return schema;
    }



    /// <summary>
    /// Opens a database that holds every table of the model, made from the model and not by a
    /// migration, and whose history says the shipped migration was applied. So the tool finds it up
    /// to date, and rows can be stored in it.
    /// </summary>
    public static async Task<TestSchema> CreateModelAsync()
    {
        var schema = await CreateAsync(runnable: false, []);

        await using var context = schema.NewContext();
        await context.Database.EnsureCreatedAsync();

        var history = context.GetService<IHistoryRepository>();
        await context.Database.ExecuteSqlRawAsync(history.GetCreateScript());
        await context.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(Shipped, "10.0.15")));

        return schema;
    }



    private static async Task<TestSchema> CreateAsync(bool runnable, string[] applied)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var schema = new TestSchema(connection, runnable);
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



    /// <summary>A context of its own on this database, with nothing tracked.</summary>
    public BarefootDbContext NewContext()
    {
        return new BarefootDbContext(_options);
    }



    /// <summary>The migrations this database's history lists, oldest first.</summary>
    public async Task<IReadOnlyList<string>> AppliedAsync()
    {
        await using var context = new BarefootDbContext(_options);

        return [.. await context.Database.GetAppliedMigrationsAsync()];
    }



    /// <summary>
    /// Whether this database holds a table or an index called <paramref name="name"/>.
    /// </summary>
    public async Task<bool> HasAsync(string name)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE name = $name";
        command.Parameters.AddWithValue("$name", name);

        return (long)(await command.ExecuteScalarAsync())! == 1;
    }



    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}
