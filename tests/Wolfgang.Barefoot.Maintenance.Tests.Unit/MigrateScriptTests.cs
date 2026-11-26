using Microsoft.Extensions.Configuration;
using Wolfgang.Barefoot.Core.Hosting;

namespace Wolfgang.Barefoot.Maintenance.Tests.Unit;

/// <summary>
/// <c>bf-maint migrate --script --output</c>, run in-process with captured streams (docs/MIGRATE.md,
/// ADR 0015). The SQL is the shipped migration rendered for SQL Server. No test here opens a
/// connection, so none proves that SQL Server accepts that SQL.
/// </summary>
public sealed class MigrateScriptTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"barefoot-{Guid.NewGuid():N}");

    private readonly string _output;



    public MigrateScriptTests()
    {
        Directory.CreateDirectory(_directory);
        _output = Path.Combine(_directory, "upgrade.sql");
    }



    /// <inheritdoc />
    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }



    [Fact]
    public async Task RunAsync_when_asked_for_a_script_writes_the_SQL_of_the_shipped_migration_to_the_file()
    {
        var run = await RunAsync("migrate", "--script", "--output", _output);

        Assert.Equal(ExitCode.Ok, run.ExitCode);

        var sql = await File.ReadAllTextAsync(_output);
        Assert.Contains("CREATE TABLE [app_user] (", sql, StringComparison.Ordinal);
        Assert.Contains($"VALUES (N'{TestSchema.Shipped}', N'", sql, StringComparison.Ordinal);
    }



    [Fact]
    public async Task RunAsync_when_asked_for_a_script_names_the_file_and_the_migrations()
    {
        var run = await RunAsync("migrate", "--script", "--output", _output);

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal($"script: {_output}\ndirection: up\nmigrations: {TestSchema.Shipped}{Environment.NewLine}", run.Output);
        Assert.Empty(run.Error);
    }



    [Fact]
    public async Task RunAsync_when_no_connection_string_is_configured_writes_the_script_all_the_same()
    {
        var run = await RunWithAsync(null, "migrate", "--script", "--output", _output);

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.True(File.Exists(_output));
    }



    [Fact]
    public async Task RunAsync_when_the_output_is_a_relative_path_reports_the_full_path()
    {
        var relative = Path.GetRelativePath(Directory.GetCurrentDirectory(), _output);

        var run = await RunAsync("migrate", "--script", "--output", relative);

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.StartsWith($"script: {_output}\n", run.Output, StringComparison.Ordinal);
        Assert.True(File.Exists(_output));
    }



    [Fact]
    public async Task RunAsync_when_the_output_file_exists_replaces_it()
    {
        await File.WriteAllTextAsync(_output, "-- an older script");

        var run = await RunAsync("migrate", "--script", "--output", _output);

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.DoesNotContain("-- an older script", await File.ReadAllTextAsync(_output), StringComparison.Ordinal);
    }



    [Fact]
    public async Task RunAsync_when_the_output_cannot_be_written_exits_Failed()
    {
        var run = await RunAsync("migrate", "--script", "--output", Path.Combine(_directory, "missing", "upgrade.sql"));

        Assert.Equal(ExitCode.Failed, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.StartsWith("bf-maint failed: ", run.Error, StringComparison.Ordinal);
    }



    [Fact]
    public async Task RunAsync_when_script_is_given_without_output_exits_Usage_and_writes_nothing()
    {
        var run = await RunAsync("migrate", "--script");

        Assert.Equal(ExitCode.Usage, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Equal($"--script needs --output <file>.{Environment.NewLine}", run.Error);
    }



    [Fact]
    public async Task RunAsync_when_script_is_given_a_blank_output_exits_Usage()
    {
        var run = await RunAsync("migrate", "--script", "--output", " ");

        Assert.Equal(ExitCode.Usage, run.ExitCode);
        Assert.Equal($"--script needs --output <file>.{Environment.NewLine}", run.Error);
    }



    [Theory]
    [InlineData("migrate")]
    [InlineData("migrate", "--status")]
    public async Task RunAsync_when_output_is_given_without_script_exits_Usage_and_writes_nothing(params string[] args)
    {
        var run = await RunAsync([.. args, "--output", _output]);

        Assert.Equal(ExitCode.Usage, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Equal($"--output is only used with --script.{Environment.NewLine}", run.Error);
        Assert.False(File.Exists(_output));
    }



    [Fact]
    public async Task RunAsync_when_script_and_status_are_both_given_exits_Usage_and_writes_nothing()
    {
        var run = await RunAsync("migrate", "--status", "--script", "--output", _output);

        Assert.Equal(ExitCode.Usage, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Equal($"--status and --script cannot be used together.{Environment.NewLine}", run.Error);
        Assert.False(File.Exists(_output));
    }



    [Fact]
    public async Task RunAsync_when_asked_for_help_on_migrate_describes_script_and_output()
    {
        var run = await RunAsync("migrate", "--help");

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Contains("--script", run.Output, StringComparison.Ordinal);
        Assert.Contains("Opens no connection.", run.Output, StringComparison.Ordinal);
        Assert.Contains("--from", run.Output, StringComparison.Ordinal);
        Assert.Contains("Default: 0, an empty schema.", run.Output, StringComparison.Ordinal);
        Assert.Contains("--output", run.Output, StringComparison.Ordinal);
        Assert.Contains("An existing file is replaced.", run.Output, StringComparison.Ordinal);
    }



    private static Task<HostRun> RunAsync(params string[] args)
    {
        return RunWithAsync("Server=db.example.com;Database=barefoot", args);
    }



    private static async Task<HostRun> RunWithAsync(string? connectionString, params string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("ConnectionStrings:Barefoot", connectionString)])
            .Build();

        return await HostRun.StartAsync(args, configuration, SqlServerDatabase.Open);
    }
}
