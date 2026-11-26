using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Wolfgang.Barefoot.Core.Hosting;
using Wolfgang.Barefoot.Data;
using Wolfgang.Barefoot.Maintenance.Tests.Unit.Migrations;

namespace Wolfgang.Barefoot.Maintenance.Tests.Unit;

/// <summary>
/// <c>bf-maint migrate</c>, run in-process with captured streams (docs/MIGRATE.md). The database is
/// SQLite and the migrations are the two of <see cref="TestMigrations"/>, so these tests prove what the
/// verb decides, applies and reports. They do not prove that SQL Server accepts <c>InitialCreate</c>.
/// </summary>
public sealed class MigrateApplyTests
{
    private const string ConnectionString = "Server=db.example.com;Database=barefoot";

    private const string Later = "99990101000000_Later";

    private static readonly string[] Migrate = ["migrate"];



    [Fact]
    public async Task RunAsync_when_nothing_is_applied_applies_every_migration_and_exits_Ok()
    {
        await using var schema = await TestSchema.CreateRunnableAsync();

        var run = await RunAsync(schema.Open);

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal
        (
            $"applied: {TestMigrations.First}, {TestMigrations.Second}\ncurrent: {TestMigrations.Second}{Environment.NewLine}",
            run.Output
        );
        Assert.Empty(run.Error);
        Assert.Equal([TestMigrations.First, TestMigrations.Second], await schema.AppliedAsync());
    }



    [Fact]
    public async Task RunAsync_when_nothing_is_applied_runs_what_the_migrations_say()
    {
        await using var schema = await TestSchema.CreateRunnableAsync();

        await RunAsync(schema.Open);

        Assert.Equal(1, await schema.CountAsync("first_thing"));
        Assert.Equal(0, await schema.CountAsync("second_thing"));
    }



    [Fact]
    public async Task RunAsync_when_one_migration_is_pending_applies_that_one_alone()
    {
        await using var schema = await TestSchema.CreateRunnableAsync(TestMigrations.First);

        var run = await RunAsync(schema.Open);

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal($"applied: {TestMigrations.Second}\ncurrent: {TestMigrations.Second}{Environment.NewLine}", run.Output);
        Assert.Empty(run.Error);
        Assert.Equal([TestMigrations.First, TestMigrations.Second], await schema.AppliedAsync());
    }



    [Fact]
    public async Task RunAsync_when_the_database_is_at_the_expected_version_applies_nothing_and_exits_Ok()
    {
        await using var schema = await TestSchema.CreateRunnableAsync(TestMigrations.First, TestMigrations.Second);

        var run = await RunAsync(schema.Open);

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal($"applied: none\ncurrent: {TestMigrations.Second}{Environment.NewLine}", run.Output);
        Assert.Empty(run.Error);
        Assert.Equal([TestMigrations.First, TestMigrations.Second], await schema.AppliedAsync());
    }



    [Fact]
    public async Task RunAsync_when_the_database_is_newer_than_the_build_applies_nothing_and_exits_Refused()
    {
        await using var schema = await TestSchema.CreateRunnableAsync(TestMigrations.First, Later);

        var run = await RunAsync(schema.Open);

        Assert.Equal(ExitCode.Refused, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Equal
        (
            $"The database is newer than this build: it holds {Later}. Nothing was applied. Deploy the matching build.{Environment.NewLine}",
            run.Error
        );
        Assert.Equal([TestMigrations.First, Later], await schema.AppliedAsync());
    }



    [Fact]
    public async Task RunAsync_when_a_migration_fails_exits_Failed_and_does_not_record_it()
    {
        await using var schema = await TestSchema.CreateRunnableAsync(TestMigrations.First);
        await schema.ExecuteAsync("CREATE TABLE second_thing (id INTEGER)");

        var run = await RunAsync(schema.Open);

        Assert.Equal(ExitCode.Failed, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.StartsWith("bf-maint failed: ", run.Error, StringComparison.Ordinal);
        Assert.Equal([TestMigrations.First], await schema.AppliedAsync());
    }



    [Fact]
    public async Task RunAsync_when_given_a_connection_string_opens_the_database_it_names()
    {
        await using var schema = await TestSchema.CreateRunnableAsync();

        await RunAsync(schema.Open);

        Assert.Equal([ConnectionString], schema.ConnectionStrings);
    }



    [Fact]
    public async Task RunAsync_when_no_connection_string_is_configured_exits_Usage_and_opens_nothing()
    {
        await using var schema = await TestSchema.CreateRunnableAsync();

        var run = await RunAsync(schema.Open, connectionString: null);

        Assert.Equal(ExitCode.Usage, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Empty(schema.ConnectionStrings);
        Assert.Empty(await schema.AppliedAsync());
    }



    [Fact]
    public async Task RunAsync_when_the_database_does_not_exist_exits_DatabaseUnavailable_and_creates_nothing()
    {
        var path = Path.Combine(Path.GetTempPath(), $"barefoot-{Guid.NewGuid():N}.db");

        var run = await RunAsync(_ => OnFile(path));

        Assert.Equal(ExitCode.DatabaseUnavailable, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.False(File.Exists(path));
    }



    private static BarefootDbContext OnFile(string path)
    {
        var options = new DbContextOptionsBuilder<BarefootDbContext>()
            .UseSqlite($"Data Source={path};Mode=ReadWrite")
            .Options;

        return new BarefootDbContext(options);
    }



    private static async Task<HostRun> RunAsync
    (
        Func<string, BarefootDbContext> openContext,
        string? connectionString = ConnectionString
    )
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("ConnectionStrings:Barefoot", connectionString)])
            .Build();

        return await HostRun.StartAsync(Migrate, configuration, openContext);
    }
}
