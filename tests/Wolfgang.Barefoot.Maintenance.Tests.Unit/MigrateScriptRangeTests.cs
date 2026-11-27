using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Wolfgang.Barefoot.Core.Hosting;
using Wolfgang.Barefoot.Data;
using Wolfgang.Barefoot.Maintenance.Tests.Unit.Migrations;

namespace Wolfgang.Barefoot.Maintenance.Tests.Unit;

/// <summary>
/// <c>bf-maint migrate --script</c> with <c>--from</c>, <c>--to</c> and <c>--confirm-data-loss</c>, run
/// in-process with captured streams (docs/MIGRATE.md, ADR 0015). Most tests script the two migrations
/// of <see cref="TestMigrations"/> for SQLite; the last ones script the rollback of the shipped
/// migration for SQL Server. No test here opens a connection or runs a script.
/// </summary>
public sealed class MigrateScriptRangeTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"barefoot-{Guid.NewGuid():N}");

    private readonly string _output;

    private const string Preamble = "-- first line\n\n";



    public MigrateScriptRangeTests()
    {
        Directory.CreateDirectory(_directory);
        _output = Path.Combine(_directory, "move.sql");
    }



    /// <inheritdoc />
    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }



    [Fact]
    public async Task RunAsync_when_given_a_start_scripts_only_the_migrations_after_it()
    {
        var run = await RunAsync(TestSchema.Offline, "--from", "First");

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal($"script: {_output}\ndirection: up\nmigrations: {TestMigrations.Second}{Environment.NewLine}", run.Output);
        Assert.Empty(run.Error);

        var sql = await File.ReadAllTextAsync(_output);
        Assert.Contains($"CREATE INDEX \"{TestMigrations.Index}\"", sql, StringComparison.Ordinal);
        Assert.DoesNotContain($"CREATE TABLE {TestMigrations.Table}", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("-- Downgrade", sql, StringComparison.Ordinal);
    }



    [Fact]
    public async Task RunAsync_when_given_an_end_scripts_only_the_migrations_up_to_it()
    {
        var run = await RunAsync(TestSchema.Offline, "--to", "First");

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal($"script: {_output}\ndirection: up\nmigrations: {TestMigrations.First}{Environment.NewLine}", run.Output);

        var sql = await File.ReadAllTextAsync(_output);
        Assert.Contains($"CREATE TABLE {TestMigrations.Table}", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("CREATE INDEX", sql, StringComparison.Ordinal);
    }



    [Fact]
    public async Task RunAsync_when_given_neither_scripts_every_migration()
    {
        var run = await RunAsync(TestSchema.Offline);

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal
        (
            $"script: {_output}\ndirection: up\nmigrations: {TestMigrations.First}, {TestMigrations.Second}{Environment.NewLine}",
            run.Output
        );
    }



    [Fact]
    public async Task RunAsync_when_the_end_is_before_the_start_and_nothing_is_lost_writes_a_downgrade_without_confirmation()
    {
        var run = await RunAsync(TestSchema.Offline, "--from", "Second", "--to", "First");

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal($"script: {_output}\ndirection: down\nmigrations: {TestMigrations.Second}{Environment.NewLine}", run.Output);
        Assert.Empty(run.Error);

        await using var context = TestSchema.Offline();
        var rollback = context
            .GetService<IMigrator>()
            .GenerateScript(TestMigrations.Second, TestMigrations.First);

        var sql = await File.ReadAllTextAsync(_output);
        Assert.Equal
        (
            $"-- Downgrade from {TestMigrations.Second} to {TestMigrations.First}.\n-- No step loses data.\n\n{rollback}",
            sql
        );
        Assert.Contains($"DROP INDEX \"{TestMigrations.Index}\"", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("DROP TABLE", sql, StringComparison.Ordinal);
    }



    [Fact]
    public async Task RunAsync_when_a_downgrade_would_lose_data_names_each_step_writes_nothing_and_exits_DataLossNotConfirmed()
    {
        var run = await RunAsync(TestSchema.Offline, "--from", "Second", "--to", "0");

        Assert.Equal(ExitCode.DataLossNotConfirmed, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Equal
        (
            $"Moving down to 0 would lose data:\n  {TestMigrations.First}: drops table {TestMigrations.Table}\nNothing was written. Give --confirm-data-loss to go ahead.{Environment.NewLine}",
            run.Error
        );
        Assert.False(File.Exists(_output));
    }



    [Fact]
    public async Task RunAsync_when_data_loss_is_confirmed_writes_a_downgrade_whose_header_names_each_step()
    {
        var run = await RunAsync(TestSchema.Offline, "--from", "Second", "--to", "0", "--confirm-data-loss");

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal
        (
            $"script: {_output}\ndirection: down\nmigrations: {TestMigrations.Second}, {TestMigrations.First}{Environment.NewLine}",
            run.Output
        );
        Assert.Empty(run.Error);

        var sql = await File.ReadAllTextAsync(_output);
        Assert.StartsWith
        (
            $"-- Downgrade from {TestMigrations.Second} to 0.\n-- Steps that lose data:\n--   {TestMigrations.First}: drops table {TestMigrations.Table}\n\n",
            sql,
            StringComparison.Ordinal
        );
        Assert.Contains($"DROP TABLE \"{TestMigrations.Table}\"", sql, StringComparison.Ordinal);
    }



    [Fact]
    public async Task RunAsync_when_confirmation_is_given_and_not_needed_writes_the_script_all_the_same()
    {
        var run = await RunAsync(TestSchema.Offline, "--confirm-data-loss");

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.True(File.Exists(_output));
    }



    [Fact]
    public async Task RunAsync_when_the_start_is_the_end_writes_an_empty_script()
    {
        var run = await RunAsync(TestSchema.Offline, "--from", "First", "--to", "First");

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal($"script: {_output}\ndirection: none\nmigrations: none{Environment.NewLine}", run.Output);
        Assert.Empty(await File.ReadAllTextAsync(_output));
    }



    [Theory]
    [InlineData("--from")]
    [InlineData("--to")]
    public async Task RunAsync_when_an_end_of_the_range_matches_no_migration_exits_Usage_and_writes_nothing(string option)
    {
        var run = await RunAsync(TestSchema.Offline, option, "Third");

        Assert.Equal(ExitCode.Usage, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Equal
        (
            $"No migration matches 'Third'. This build ships: {TestMigrations.First}, {TestMigrations.Second}.{Environment.NewLine}",
            run.Error
        );
        Assert.False(File.Exists(_output));
    }



    [Theory]
    [InlineData("--from")]
    [InlineData("--to")]
    public async Task RunAsync_when_an_end_of_the_range_matches_several_migrations_exits_Usage_and_writes_nothing(string option)
    {
        var run = await RunAsync(TestSchema.Offline, option, "2000");

        Assert.Equal(ExitCode.Usage, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Equal
        (
            $"'2000' matches more than one migration: {TestMigrations.First}, {TestMigrations.Second}. Give more of the id or the name.{Environment.NewLine}",
            run.Error
        );
        Assert.False(File.Exists(_output));
    }



    [Fact]
    public async Task RunAsync_when_asked_to_roll_the_shipped_migration_back_names_the_tables_it_would_drop()
    {
        var run = await RunAsync(SqlServerDatabase.Offline, "--from", "InitialCreate", "--to", "0");

        Assert.Equal(ExitCode.DataLossNotConfirmed, run.ExitCode);
        Assert.Contains($"  {TestSchema.Shipped}: drops table app_user\n", run.Error, StringComparison.Ordinal);
        Assert.False(File.Exists(_output));
    }



    [Fact]
    public async Task RunAsync_when_the_rollback_of_the_shipped_migration_is_confirmed_writes_it_for_SQL_Server()
    {
        var run = await RunAsync(SqlServerDatabase.Offline, "--from", "InitialCreate", "--to", "0", "--confirm-data-loss");

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal($"script: {_output}\ndirection: down\nmigrations: {TestSchema.Shipped}{Environment.NewLine}", run.Output);

        var sql = await File.ReadAllTextAsync(_output);
        Assert.StartsWith($"-- Downgrade from {TestSchema.Shipped} to 0.\n-- Steps that lose data:\n", sql, StringComparison.Ordinal);
        Assert.Contains($"--   {TestSchema.Shipped}: drops table app_user\n", sql, StringComparison.Ordinal);
        Assert.Contains("DROP TABLE [app_user];", sql, StringComparison.Ordinal);
    }



    [Fact]
    public async Task RunAsync_when_given_a_preamble_starts_an_upgrade_script_with_it()
    {
        var run = await RunWithPreambleAsync("--to", "First");

        await using var context = TestSchema.Offline();
        var upgrade = context
            .GetService<IMigrator>()
            .GenerateScript("0", TestMigrations.First);

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal($"{Preamble}{upgrade}", await File.ReadAllTextAsync(_output));
    }



    [Fact]
    public async Task RunAsync_when_given_a_preamble_puts_it_between_the_downgrade_header_and_the_SQL()
    {
        var run = await RunWithPreambleAsync("--from", "Second", "--to", "First");

        await using var context = TestSchema.Offline();
        var rollback = context
            .GetService<IMigrator>()
            .GenerateScript(TestMigrations.Second, TestMigrations.First);

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal
        (
            $"-- Downgrade from {TestMigrations.Second} to {TestMigrations.First}.\n-- No step loses data.\n\n{Preamble}{rollback}",
            await File.ReadAllTextAsync(_output)
        );
    }



    [Fact]
    public async Task RunAsync_when_given_a_preamble_and_the_start_is_the_end_still_writes_an_empty_script()
    {
        var run = await RunWithPreambleAsync("--from", "First", "--to", "First");

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Empty(await File.ReadAllTextAsync(_output));
    }



    private async Task<HostRun> RunAsync(Func<BarefootDbContext> offlineContext, params string[] range)
    {
        var configuration = new ConfigurationBuilder().Build();

        return await HostRun.StartAsync
        (
            ["migrate", "--script", "--output", _output, .. range],
            configuration,
            SqlServerDatabase.Open,
            offlineContext: offlineContext
        );
    }



    private async Task<HostRun> RunWithPreambleAsync(params string[] range)
    {
        var configuration = new ConfigurationBuilder().Build();

        return await HostRun.StartAsync
        (
            ["migrate", "--script", "--output", _output, .. range],
            configuration,
            SqlServerDatabase.Open,
            offlineContext: TestSchema.Offline,
            scriptPreamble: Preamble
        );
    }
}
