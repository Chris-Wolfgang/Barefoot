using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Wolfgang.Barefoot.Core.Hosting;
using Wolfgang.Barefoot.Data;

namespace Wolfgang.Barefoot.Maintenance.Tests.Unit;

/// <summary>
/// <c>bf-maint migrate --status</c>, run in-process with captured streams: one test per exit code
/// (docs/MIGRATE.md).
/// </summary>
public sealed class MigrateStatusTests
{
    private const string ConnectionString = "Server=db.example.com;Database=barefoot";

    private const string Later = "99990101000000_Later";

    private static readonly string[] Status = ["migrate", "--status"];



    [Fact]
    public async Task RunAsync_when_the_database_is_at_the_expected_version_reports_it_and_exits_Ok()
    {
        await using var schema = await TestSchema.CreateAsync(TestSchema.Shipped);

        var run = await RunAsync(Status, schema.Open);

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal
        (
            $"current: {TestSchema.Shipped}\nexpected: {TestSchema.Shipped}\nstate: UpToDate\npending: none\nunknown: none{Environment.NewLine}",
            run.Output
        );
        Assert.Empty(run.Error);
    }



    [Fact]
    public async Task RunAsync_when_the_database_is_behind_names_the_pending_migration_and_exits_MissingMigration()
    {
        await using var schema = await TestSchema.CreateAsync();

        var run = await RunAsync(Status, schema.Open);

        Assert.Equal(ExitCode.MissingMigration, run.ExitCode);
        Assert.Equal
        (
            $"current: none\nexpected: {TestSchema.Shipped}\nstate: MissingMigration\npending: {TestSchema.Shipped}\nunknown: none{Environment.NewLine}",
            run.Output
        );
        Assert.Equal($"Missing migration: {TestSchema.Shipped}. Run `bf-maint migrate`.{Environment.NewLine}", run.Error);
    }



    [Fact]
    public async Task RunAsync_when_the_database_is_newer_than_the_build_exits_Refused()
    {
        await using var schema = await TestSchema.CreateAsync(TestSchema.Shipped, Later, "99990201000000_Latest");

        var run = await RunAsync(Status, schema.Open);

        Assert.Equal(ExitCode.Refused, run.ExitCode);
        Assert.Equal
        (
            $"current: 99990201000000_Latest\nexpected: {TestSchema.Shipped}\nstate: NewerSchema\npending: none\nunknown: {Later}, 99990201000000_Latest{Environment.NewLine}",
            run.Output
        );
        Assert.Equal($"The database is newer than this build. Deploy the matching build.{Environment.NewLine}", run.Error);
    }



    [Fact]
    public async Task RunAsync_when_given_a_connection_string_opens_the_database_it_names()
    {
        await using var schema = await TestSchema.CreateAsync(TestSchema.Shipped);

        await RunAsync(Status, schema.Open);

        Assert.Equal([ConnectionString], schema.ConnectionStrings);
    }



    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RunAsync_when_no_connection_string_is_configured_exits_Usage_and_opens_nothing(string? connectionString)
    {
        await using var schema = await TestSchema.CreateAsync(TestSchema.Shipped);

        var run = await RunAsync(Status, schema.Open, connectionString);

        Assert.Equal(ExitCode.Usage, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Equal
        (
            $"No connection string. Set the environment variable ConnectionStrings__Barefoot, or the user secret ConnectionStrings:Barefoot.{Environment.NewLine}",
            run.Error
        );
        Assert.Empty(schema.ConnectionStrings);
    }



    [Fact]
    public async Task RunAsync_when_the_connection_string_is_not_valid_exits_Usage_without_quoting_it()
    {
        var run = await RunAsync(Status, NotAConnectionString);

        Assert.Equal(ExitCode.Usage, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Equal($"The connection string ConnectionStrings:Barefoot is not a valid connection string.{Environment.NewLine}", run.Error);
    }



    [Fact]
    public async Task RunAsync_when_the_database_does_not_exist_exits_DatabaseUnavailable_and_creates_nothing()
    {
        var path = Path.Combine(Path.GetTempPath(), $"barefoot-{Guid.NewGuid():N}.db");

        var run = await RunAsync(Status, _ => OnFile(path));

        Assert.Equal(ExitCode.DatabaseUnavailable, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Equal
        (
            $"The database could not be reached. bf-maint never creates a database: check that it exists, and that the firewall rule allows this machine.{Environment.NewLine}",
            run.Error
        );
        Assert.False(File.Exists(path));
    }



    [Fact]
    public async Task RunAsync_when_cancelled_before_it_starts_exits_Failed()
    {
        await using var schema = await TestSchema.CreateAsync(TestSchema.Shipped);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var run = await RunAsync(Status, schema.Open, cancellationToken: cancellation.Token);

        Assert.Equal(ExitCode.Failed, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.StartsWith("bf-maint failed: ", run.Error, StringComparison.Ordinal);
    }



    [Fact]
    public async Task RunAsync_when_migrate_is_given_without_status_exits_Usage_and_opens_nothing()
    {
        await using var schema = await TestSchema.CreateAsync();

        var run = await RunAsync(["migrate"], schema.Open);

        Assert.Equal(ExitCode.Usage, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Equal
        (
            $"bf-maint migrate does not apply migrations yet. Only `bf-maint migrate --status` is built.{Environment.NewLine}",
            run.Error
        );
        Assert.Empty(schema.ConnectionStrings);
    }



    private static BarefootDbContext NotAConnectionString(string connectionString)
    {
        throw new ArgumentException($"Not a connection string: '{connectionString}'.", nameof(connectionString));
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
        string[] args,
        Func<string, BarefootDbContext> openContext,
        string? connectionString = ConnectionString,
        CancellationToken cancellationToken = default
    )
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("ConnectionStrings:Barefoot", connectionString)])
            .Build();

        return await HostRun.StartAsync(args, configuration, openContext, cancellationToken);
    }
}
