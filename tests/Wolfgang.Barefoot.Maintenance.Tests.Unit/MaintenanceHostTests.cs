using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Wolfgang.Barefoot.Core.Hosting;
using Wolfgang.Barefoot.Data;

namespace Wolfgang.Barefoot.Maintenance.Tests.Unit;

public sealed class MaintenanceHostTests
{
    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .AddInMemoryCollection([new KeyValuePair<string, string?>("ConnectionStrings:Barefoot", "Server=db.example.com")])
        .Build();



    [Fact]
    public async Task RunAsync_when_asked_for_help_writes_the_verbs_to_output_and_exits_Ok()
    {
        var run = await HostRun.StartAsync(["--help"], Configuration, NeverOpened);

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Contains("Changes and reports the schema of Barefoot's database.", run.Output, StringComparison.Ordinal);
        Assert.Contains("migrate", run.Output, StringComparison.Ordinal);
        Assert.Contains("Moves the schema to a migration: by default up to the latest this build ships.", run.Output, StringComparison.Ordinal);
        Assert.Contains("grant-roles", run.Output, StringComparison.Ordinal);
        Assert.Contains("assign-owner", run.Output, StringComparison.Ordinal);
        Assert.Contains("Moves everything the \"Unassigned\" user owns to the user named. Run it once.", run.Output, StringComparison.Ordinal);
        Assert.Contains("Makes the API's database user and adds it to its roles. Running it again changes nothing.", run.Output, StringComparison.Ordinal);
        Assert.Empty(run.Error);
    }



    [Fact]
    public async Task RunAsync_when_asked_for_help_on_migrate_describes_status()
    {
        var run = await HostRun.StartAsync(["migrate", "--help"], Configuration, NeverOpened);

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Contains("--status", run.Output, StringComparison.Ordinal);
        Assert.Contains("Changes nothing.", run.Output, StringComparison.Ordinal);
    }



    [Fact]
    public async Task RunAsync_when_given_no_verb_exits_Usage()
    {
        var run = await HostRun.StartAsync([], Configuration, NeverOpened);

        Assert.Equal(ExitCode.Usage, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.NotEmpty(run.Error);
    }



    [Theory]
    [InlineData("migrat")]
    [InlineData("migrate", "--statu")]
    [InlineData("migrate", "--status", "extra")]
    public async Task RunAsync_when_the_arguments_do_not_parse_names_each_problem_and_exits_Usage(params string[] args)
    {
        var run = await HostRun.StartAsync(args, Configuration, NeverOpened);

        Assert.Equal(ExitCode.Usage, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Contains(args[^1], run.Error, StringComparison.Ordinal);
    }



    [Fact]
    public async Task RunAsync_when_a_verb_throws_writes_one_line_and_exits_Failed()
    {
        var run = await HostRun.StartAsync(["migrate", "--status"], Configuration, NeverOpened);

        Assert.Equal(ExitCode.Failed, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Equal
        (
            $"bf-maint failed: No test here opens a database, and one asked for Server=db.example.com.{Environment.NewLine}",
            run.Error
        );
    }



    [Fact]
    public async Task RunAsync_when_run_from_Program_exits_Ok_and_writes_the_log_file_of_the_day()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"barefoot-{Guid.NewGuid():N}");
        try
        {
            var exitCode = await Program.RunAsync(["--help"], folder);

            Assert.Equal((int)ExitCode.Ok, exitCode);
            var file = Assert.Single(Directory.EnumerateFiles(folder));
            Assert.Matches
            (
                new Regex(@"\Abf-maint-\d{8}\.log\z", RegexOptions.None, TimeSpan.FromSeconds(1)),
                Path.GetFileName(file)
            );
            Assert.EndsWith
            (
                $"Z [INF] bf-maint: exit code 0 (Ok){Environment.NewLine}",
                await File.ReadAllTextAsync(file),
                StringComparison.Ordinal
            );
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }



    [Fact]
    public async Task RunAsync_when_a_run_succeeds_logs_the_command_line_and_the_exit_code()
    {
        var run = await HostRun.StartAsync(["--help"], Configuration, NeverOpened);

        var lines = LogLines(run);
        Assert.Equal(2, lines.Count);
        Assert.EndsWith("Z [INF] bf-maint: bf-maint --help", lines[0], StringComparison.Ordinal);
        Assert.EndsWith("Z [INF] bf-maint: exit code 0 (Ok)", lines[1], StringComparison.Ordinal);
    }



    [Fact]
    public async Task RunAsync_when_the_arguments_do_not_parse_logs_what_stderr_was_told()
    {
        var run = await HostRun.StartAsync(["migrate", "--status", "extra"], Configuration, NeverOpened);

        var told = run
            .Error
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        var lines = LogLines(run);

        Assert.NotEmpty(told);
        Assert.Equal(told.Length + 2, lines.Count);
        Assert.EndsWith("Z [INF] bf-maint: bf-maint migrate --status extra", lines[0], StringComparison.Ordinal);
        Assert.All
        (
            told.Zip(lines.Skip(1)),
            pair => Assert.EndsWith($"Z [INF] bf-maint: stderr: {pair.First}", pair.Second, StringComparison.Ordinal)
        );
        Assert.EndsWith("Z [INF] bf-maint: exit code 4 (Usage)", lines[^1], StringComparison.Ordinal);
    }



    [Fact]
    public async Task RunAsync_when_a_verb_throws_logs_the_stack_trace_to_the_file_only()
    {
        var run = await HostRun.StartAsync(["migrate", "--status"], Configuration, NeverOpened);

        var lines = LogLines(run);
        Assert.EndsWith
        (
            "Z [ERR] bf-maint: bf-maint failed: No test here opens a database, and one asked for Server=db.example.com.",
            lines[1],
            StringComparison.Ordinal
        );
        Assert.Equal
        (
            "System.InvalidOperationException: No test here opens a database, and one asked for Server=db.example.com.",
            lines[2]
        );
        Assert.StartsWith("   at ", lines[3], StringComparison.Ordinal);
        Assert.EndsWith("Z [INF] bf-maint: exit code 1 (Failed)", lines[^1], StringComparison.Ordinal);
        Assert.DoesNotContain("   at ", run.Error, StringComparison.Ordinal);
    }



    private static IReadOnlyList<string> LogLines(HostRun run)
    {
        return run
            .Log
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
    }



    private static BarefootDbContext NeverOpened(string connectionString)
    {
        throw new InvalidOperationException($"No test here opens a database, and one asked for {connectionString}.");
    }
}
