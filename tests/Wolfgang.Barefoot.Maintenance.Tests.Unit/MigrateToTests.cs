using Microsoft.Extensions.Configuration;
using Wolfgang.Barefoot.Core.Hosting;
using Wolfgang.Barefoot.Maintenance.Tests.Unit.Migrations;

namespace Wolfgang.Barefoot.Maintenance.Tests.Unit;

/// <summary>
/// <c>bf-maint migrate --to</c> and <c>--confirm-data-loss</c>, run in-process with captured streams
/// (docs/MIGRATE.md). The database is SQLite and the migrations are the two of
/// <see cref="TestMigrations"/>: rolling <see cref="TestMigrations.Second"/> back loses nothing,
/// rolling <see cref="TestMigrations.First"/> back drops a table.
/// </summary>
public sealed class MigrateToTests
{
    private const string Later = "99990101000000_Later";



    [Theory]
    [InlineData(TestMigrations.First)]
    [InlineData("first")]
    [InlineData("20000101")]
    public async Task RunAsync_when_the_target_is_ahead_moves_up_to_it_and_no_further(string target)
    {
        await using var schema = await TestSchema.CreateRunnableAsync();

        var run = await RunAsync(schema, "migrate", "--to", target);

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal
        (
            $"direction: up\napplied: {TestMigrations.First}\nreverted: none\ncurrent: {TestMigrations.First}\nroles: not run{Environment.NewLine}",
            run.Output
        );
        Assert.Empty(run.Error);
        Assert.Equal([TestMigrations.First], await schema.AppliedAsync());
        Assert.False(await schema.HasAsync(TestMigrations.Index));
    }



    [Fact]
    public async Task RunAsync_when_the_target_is_behind_and_nothing_is_lost_moves_down_without_confirmation()
    {
        await using var schema = await TestSchema.CreateMigratedAsync(TestMigrations.Second);

        var run = await RunAsync(schema, "migrate", "--to", TestMigrations.First);

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal
        (
            $"direction: down\napplied: none\nreverted: {TestMigrations.Second}\ncurrent: {TestMigrations.First}\nroles: not run{Environment.NewLine}",
            run.Output
        );
        Assert.Empty(run.Error);
        Assert.Equal([TestMigrations.First], await schema.AppliedAsync());
        Assert.False(await schema.HasAsync(TestMigrations.Index));
        Assert.True(await schema.HasAsync(TestMigrations.Table));
    }



    [Fact]
    public async Task RunAsync_when_moving_down_would_lose_data_names_each_step_changes_nothing_and_exits_DataLossNotConfirmed()
    {
        await using var schema = await TestSchema.CreateMigratedAsync(TestMigrations.Second);

        var run = await RunAsync(schema, "migrate", "--to", "0");

        Assert.Equal(ExitCode.DataLossNotConfirmed, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Equal
        (
            $"Moving down to 0 would lose data:\n  {TestMigrations.First}: drops table {TestMigrations.Table}\nNothing was changed. Give --confirm-data-loss to go ahead.{Environment.NewLine}",
            run.Error
        );
        Assert.Equal([TestMigrations.First, TestMigrations.Second], await schema.AppliedAsync());
        Assert.True(await schema.HasAsync(TestMigrations.Table));
        Assert.True(await schema.HasAsync(TestMigrations.Index));
    }



    [Fact]
    public async Task RunAsync_when_data_loss_is_confirmed_moves_down_and_lists_what_it_rolled_back_newest_first()
    {
        await using var schema = await TestSchema.CreateMigratedAsync(TestMigrations.Second);

        var run = await RunAsync(schema, "migrate", "--to", "0", "--confirm-data-loss");

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal
        (
            $"direction: down\napplied: none\nreverted: {TestMigrations.Second}, {TestMigrations.First}\ncurrent: none\nroles: not run{Environment.NewLine}",
            run.Output
        );
        Assert.Empty(run.Error);
        Assert.Empty(await schema.AppliedAsync());
        Assert.False(await schema.HasAsync(TestMigrations.Table));
    }



    [Fact]
    public async Task RunAsync_when_confirmation_is_given_and_not_needed_moves_all_the_same()
    {
        await using var schema = await TestSchema.CreateRunnableAsync();

        var run = await RunAsync(schema, "migrate", "--confirm-data-loss");

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal([TestMigrations.First, TestMigrations.Second], await schema.AppliedAsync());
    }



    [Theory]
    [InlineData(TestMigrations.Second)]
    [InlineData("Second")]
    public async Task RunAsync_when_the_database_is_at_the_target_changes_nothing_and_exits_Ok(string target)
    {
        await using var schema = await TestSchema.CreateMigratedAsync(TestMigrations.Second);

        var run = await RunAsync(schema, "migrate", "--to", target);

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal
        (
            $"direction: none\napplied: none\nreverted: none\ncurrent: {TestMigrations.Second}\nroles: skipped (not this kind of server){Environment.NewLine}",
            run.Output
        );
        Assert.Empty(run.Error);
    }



    [Fact]
    public async Task RunAsync_when_the_target_is_the_empty_schema_and_nothing_is_applied_changes_nothing_and_exits_Ok()
    {
        await using var schema = await TestSchema.CreateRunnableAsync();

        var run = await RunAsync(schema, "migrate", "--to", "0");

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal($"direction: none\napplied: none\nreverted: none\ncurrent: none\nroles: not run{Environment.NewLine}", run.Output);
        Assert.Empty(run.Error);
    }



    [Theory]
    [InlineData("Third")]
    [InlineData("")]
    public async Task RunAsync_when_no_migration_matches_the_target_lists_what_is_shipped_and_exits_Usage(string target)
    {
        await using var schema = await TestSchema.CreateRunnableAsync();

        var run = await RunAsync(schema, "migrate", "--to", target);

        Assert.Equal(ExitCode.Usage, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Equal
        (
            $"No migration matches '{target}'. This build ships: {TestMigrations.First}, {TestMigrations.Second}.{Environment.NewLine}",
            run.Error
        );
        Assert.Empty(await schema.AppliedAsync());
    }



    [Fact]
    public async Task RunAsync_when_the_target_matches_several_migrations_lists_them_and_exits_Usage()
    {
        await using var schema = await TestSchema.CreateRunnableAsync();

        var run = await RunAsync(schema, "migrate", "--to", "2000");

        Assert.Equal(ExitCode.Usage, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Equal
        (
            $"'2000' matches more than one migration: {TestMigrations.First}, {TestMigrations.Second}. Give more of the id or the name.{Environment.NewLine}",
            run.Error
        );
        Assert.Empty(await schema.AppliedAsync());
    }



    [Fact]
    public async Task RunAsync_when_the_database_is_newer_than_the_build_refuses_whatever_the_target()
    {
        await using var schema = await TestSchema.CreateRunnableAsync(TestMigrations.First, Later);

        var run = await RunAsync(schema, "migrate", "--to", "0", "--confirm-data-loss");

        Assert.Equal(ExitCode.Refused, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Equal([TestMigrations.First, Later], await schema.AppliedAsync());
    }



    [Theory]
    [InlineData("migrate")]
    [InlineData("migrate", "--to", "First")]
    [InlineData("migrate", "--to", "0", "--confirm-data-loss")]
    public async Task RunAsync_when_the_history_has_a_gap_refuses_whatever_the_target(params string[] args)
    {
        await using var schema = await TestSchema.CreateRunnableAsync(TestMigrations.Second);

        var run = await RunAsync(schema, args);

        Assert.Equal(ExitCode.Refused, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Equal
        (
            $"The migration history has a gap: {TestMigrations.First} is not applied and a later migration is. Nothing was applied.{Environment.NewLine}",
            run.Error
        );
        Assert.Equal([TestMigrations.Second], await schema.AppliedAsync());
    }



    [Theory]
    [InlineData("--to is not used with --status.", "migrate", "--status", "--to", "0")]
    [InlineData("--confirm-data-loss is not used with --status.", "migrate", "--status", "--confirm-data-loss")]
    [InlineData("--from is only used with --script.", "migrate", "--from", "0")]
    [InlineData("--from is only used with --script.", "migrate", "--status", "--from", "0")]
    public async Task RunAsync_when_an_option_is_given_to_a_verb_that_does_not_use_it_exits_Usage(string expected, params string[] args)
    {
        await using var schema = await TestSchema.CreateRunnableAsync();

        var run = await RunAsync(schema, args);

        Assert.Equal(ExitCode.Usage, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Equal($"{expected}{Environment.NewLine}", run.Error);
        Assert.Empty(schema.ConnectionStrings);
    }



    [Fact]
    public async Task RunAsync_when_asked_for_help_on_migrate_describes_to_and_confirm_data_loss()
    {
        await using var schema = await TestSchema.CreateRunnableAsync();

        var run = await RunAsync(schema, "migrate", "--help");

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Contains("Moves the schema to a migration: by default up to the latest this build ships.", run.Output, StringComparison.Ordinal);
        Assert.Contains("--to", run.Output, StringComparison.Ordinal);
        Assert.Contains("or 0 for an empty schema. Default: the latest.", run.Output, StringComparison.Ordinal);
        Assert.Contains("--confirm-data-loss", run.Output, StringComparison.Ordinal);
        Assert.Contains("Go ahead with a move down that would lose data.", run.Output, StringComparison.Ordinal);
    }



    private static async Task<HostRun> RunAsync(TestSchema schema, params string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("ConnectionStrings:Barefoot", "Server=db.example.com;Database=barefoot")])
            .Build();

        return await HostRun.StartAsync(args, configuration, schema.Open);
    }
}
