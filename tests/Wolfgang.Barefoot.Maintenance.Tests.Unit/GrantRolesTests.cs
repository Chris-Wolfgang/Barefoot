using Microsoft.Extensions.Configuration;
using Wolfgang.Barefoot.Core.Hosting;

namespace Wolfgang.Barefoot.Maintenance.Tests.Unit;

/// <summary>
/// <c>bf-maint grant-roles</c>, run in-process with captured streams (docs/MIGRATE.md, ADR 0016).
/// SQLite has no users or roles, so these tests hand the verb a script of their own, whose statements
/// make tables, and check that the verb runs it or skips it. The SQL for SQL Server is pinned in
/// <see cref="SqlServerDatabaseTests"/> and is not run by any test.
/// </summary>
public sealed class GrantRolesTests
{
    private const string First = "granted_first";

    private const string Second = "granted_second";

    private const string ConnectionString = "Server=db.example.com;Database=barefoot";

    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .AddInMemoryCollection([new KeyValuePair<string, string?>("ConnectionStrings:Barefoot", ConnectionString)])
        .Build();



    [Fact]
    public async Task RunAsync_when_the_server_can_make_the_user_runs_every_statement_and_says_granted()
    {
        await using var schema = await TestSchema.CreateAsync();

        var run = await RunAsync(schema, Script(canGrant: 1));

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal($"roles: granted{Environment.NewLine}", run.Output);
        Assert.Empty(run.Error);
        Assert.True(await schema.HasAsync(First));
        Assert.True(await schema.HasAsync(Second));
        Assert.Equal([ConnectionString], schema.ConnectionStrings);
    }



    [Fact]
    public async Task RunAsync_when_the_server_cannot_make_the_user_runs_nothing_and_says_why()
    {
        await using var schema = await TestSchema.CreateAsync();

        var run = await RunAsync(schema, Script(canGrant: 0));

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal($"roles: skipped (not this kind of server){Environment.NewLine}", run.Output);
        Assert.Empty(run.Error);
        Assert.False(await schema.HasAsync(First));
        Assert.False(await schema.HasAsync(Second));
    }



    [Fact]
    public async Task RunAsync_when_run_a_second_time_changes_nothing_and_says_granted_again()
    {
        await using var schema = await TestSchema.CreateAsync();
        await RunAsync(schema, Script(canGrant: 1));

        var run = await RunAsync(schema, Script(canGrant: 1));

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal($"roles: granted{Environment.NewLine}", run.Output);
    }



    [Fact]
    public async Task RunAsync_when_the_server_refuses_a_statement_exits_Failed_and_keeps_what_ran_before_it()
    {
        await using var schema = await TestSchema.CreateAsync();
        var script = new RoleScript
        (
            "SELECT 1 AS \"Value\"",
            [$"CREATE TABLE {First} (id INTEGER)", "CREATE TABLE", $"CREATE TABLE {Second} (id INTEGER)"],
            "not this kind of server"
        );

        var run = await RunAsync(schema, script);

        Assert.Equal(ExitCode.Failed, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.StartsWith("bf-maint failed: ", run.Error, StringComparison.Ordinal);
        Assert.True(await schema.HasAsync(First));
        Assert.False(await schema.HasAsync(Second));
    }



    [Fact]
    public async Task RunAsync_when_no_connection_string_is_set_exits_Usage_and_opens_nothing()
    {
        await using var schema = await TestSchema.CreateAsync();

        var run = await HostRun.StartAsync
        (
            ["grant-roles"],
            new ConfigurationBuilder().Build(),
            schema.Open,
            roles: Script(canGrant: 1)
        );

        Assert.Equal(ExitCode.Usage, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Empty(schema.ConnectionStrings);
        Assert.False(await schema.HasAsync(First));
    }



    [Fact]
    public async Task RunAsync_when_given_an_argument_exits_Usage_and_opens_nothing()
    {
        await using var schema = await TestSchema.CreateAsync();

        var run = await HostRun.StartAsync
        (
            ["grant-roles", "someone"],
            Configuration,
            schema.Open,
            roles: Script(canGrant: 1)
        );

        Assert.Equal(ExitCode.Usage, run.ExitCode);
        Assert.Empty(schema.ConnectionStrings);
    }



    [Fact]
    public async Task RunAsync_when_context_or_script_is_null_throws_ArgumentNullException()
    {
        await using var schema = await TestSchema.CreateAsync();
        await using var context = schema.Open(ConnectionString);

        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() => RoleGrant.RunAsync(null!, Script(canGrant: 1)))).ParamName);
        Assert.Equal("script", (await Assert.ThrowsAsync<ArgumentNullException>(() => RoleGrant.RunAsync(context, null!))).ParamName);
    }



    /// <summary>A script SQLite can run: each statement makes a table unless it is there.</summary>
    private static RoleScript Script(int canGrant)
    {
        return new RoleScript
        (
            $"SELECT {canGrant} AS \"Value\"",
            [$"CREATE TABLE IF NOT EXISTS {First} (id INTEGER)", $"CREATE TABLE IF NOT EXISTS {Second} (id INTEGER)"],
            "not this kind of server"
        );
    }



    private static async Task<HostRun> RunAsync(TestSchema schema, RoleScript script)
    {
        return await HostRun.StartAsync
        (
            ["grant-roles"],
            Configuration,
            schema.Open,
            roles: script
        );
    }
}
