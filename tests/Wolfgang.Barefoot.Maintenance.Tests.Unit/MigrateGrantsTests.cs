using Microsoft.Extensions.Configuration;
using Wolfgang.Barefoot.Core.Hosting;
using Wolfgang.Barefoot.Maintenance.Tests.Unit.Migrations;

namespace Wolfgang.Barefoot.Maintenance.Tests.Unit;

/// <summary>
/// <c>bf-maint migrate</c> giving the API its roles (docs/MIGRATE.md, ADR 0016): it runs the grant
/// when the database ends on the latest migration and reports it in the <c>roles:</c> line. As in
/// <see cref="GrantRolesTests"/>, the role script here makes a table, which SQLite can do.
/// </summary>
public sealed class MigrateGrantsTests
{
    private const string Granted = "granted_marker";

    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .AddInMemoryCollection([new KeyValuePair<string, string?>("ConnectionStrings:Barefoot", "Server=db.example.com;Database=barefoot")])
        .Build();

    private static readonly RoleScript Roles = new
    (
        "SELECT 1 AS \"Value\"",
        [$"CREATE TABLE IF NOT EXISTS {Granted} (id INTEGER)"],
        "not this kind of server"
    );



    [Fact]
    public async Task RunAsync_when_migrations_are_applied_up_to_the_latest_grants_the_roles_after_them()
    {
        await using var schema = await TestSchema.CreateRunnableAsync();

        var run = await RunAsync(schema, Roles, "migrate");

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal
        (
            $"direction: up\napplied: {TestMigrations.First}, {TestMigrations.Second}\nreverted: none\ncurrent: {TestMigrations.Second}\nroles: granted{Environment.NewLine}",
            run.Output
        );
        Assert.Empty(run.Error);
        Assert.True(await schema.HasAsync(Granted));
    }



    [Fact]
    public async Task RunAsync_when_the_database_is_already_on_the_latest_migration_grants_the_roles_all_the_same()
    {
        await using var schema = await TestSchema.CreateMigratedAsync(TestMigrations.Second);

        var run = await RunAsync(schema, Roles, "migrate");

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal
        (
            $"direction: none\napplied: none\nreverted: none\ncurrent: {TestMigrations.Second}\nroles: granted{Environment.NewLine}",
            run.Output
        );
        Assert.True(await schema.HasAsync(Granted));
    }



    [Fact]
    public async Task RunAsync_when_the_target_named_is_the_latest_migration_grants_the_roles()
    {
        await using var schema = await TestSchema.CreateMigratedAsync(TestMigrations.First);

        var run = await RunAsync(schema, Roles, "migrate", "--to", "Second");

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.EndsWith($"\nroles: granted{Environment.NewLine}", run.Output, StringComparison.Ordinal);
        Assert.True(await schema.HasAsync(Granted));
    }



    [Theory]
    [InlineData("First")]
    [InlineData("0")]
    public async Task RunAsync_when_the_database_ends_before_the_latest_migration_does_not_grant(string target)
    {
        await using var schema = await TestSchema.CreateMigratedAsync(TestMigrations.Second);

        var run = await RunAsync(schema, Roles, "migrate", "--to", target, "--confirm-data-loss");

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.EndsWith($"\nroles: not run{Environment.NewLine}", run.Output, StringComparison.Ordinal);
        Assert.False(await schema.HasAsync(Granted));
    }



    [Fact]
    public async Task RunAsync_when_the_server_cannot_make_the_user_says_skipped_and_exits_Ok()
    {
        await using var schema = await TestSchema.CreateRunnableAsync();

        var run = await RunAsync(schema, HostRun.NoRoles, "migrate");

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.EndsWith($"\nroles: skipped (not this kind of server){Environment.NewLine}", run.Output, StringComparison.Ordinal);
        Assert.Equal([TestMigrations.First, TestMigrations.Second], await schema.AppliedAsync());
    }



    [Fact]
    public async Task RunAsync_when_the_grant_fails_exits_Failed_and_leaves_the_migrations_applied()
    {
        await using var schema = await TestSchema.CreateRunnableAsync();
        var broken = new RoleScript("SELECT 1 AS \"Value\"", ["CREATE TABLE"], "not this kind of server");

        var run = await RunAsync(schema, broken, "migrate");

        Assert.Equal(ExitCode.Failed, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.StartsWith("bf-maint failed: ", run.Error, StringComparison.Ordinal);
        Assert.Equal([TestMigrations.First, TestMigrations.Second], await schema.AppliedAsync());
    }



    [Fact]
    public async Task RunAsync_when_asked_for_the_status_does_not_grant()
    {
        await using var schema = await TestSchema.CreateMigratedAsync(TestMigrations.Second);

        var run = await RunAsync(schema, Roles, "migrate", "--status");

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.DoesNotContain("roles:", run.Output, StringComparison.Ordinal);
        Assert.False(await schema.HasAsync(Granted));
    }



    [Fact]
    public async Task RunAsync_when_a_move_is_refused_does_not_grant()
    {
        await using var schema = await TestSchema.CreateMigratedAsync(TestMigrations.Second);

        var run = await RunAsync(schema, Roles, "migrate", "--to", "0");

        Assert.Equal(ExitCode.DataLossNotConfirmed, run.ExitCode);
        Assert.False(await schema.HasAsync(Granted));
    }



    private static async Task<HostRun> RunAsync(TestSchema schema, RoleScript roles, params string[] args)
    {
        return await HostRun.StartAsync
        (
            args,
            Configuration,
            schema.Open,
            roles: roles
        );
    }
}
