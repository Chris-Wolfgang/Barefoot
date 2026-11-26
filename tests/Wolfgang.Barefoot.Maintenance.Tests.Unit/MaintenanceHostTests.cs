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
    public async Task Main_when_asked_for_help_exits_Ok()
    {
        var exitCode = await Program.Main(["--help"]);

        Assert.Equal((int)ExitCode.Ok, exitCode);
    }



    private static BarefootDbContext NeverOpened(string connectionString)
    {
        throw new InvalidOperationException($"No test here opens a database, and one asked for {connectionString}.");
    }
}
