using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Wolfgang.Barefoot.Core.Hosting;

namespace Wolfgang.Barefoot.Maintenance.Tests.Unit;

/// <summary>
/// <c>bf-maint verify-schema</c>, run in-process on SQLite (docs/MIGRATE.md, ADR 0016). The query
/// that reads SQL Server's catalog cannot run here, so each test hands the verb a query that gives
/// the lines the test wants the database to have. <c>scripts/check-migration.sh</c> runs the real
/// query on SQL Server.
/// </summary>
public sealed class VerifySchemaTests
{
    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .AddInMemoryCollection([new KeyValuePair<string, string?>("ConnectionStrings:Barefoot", "Server=db.example.com")])
        .Build();



    [Fact]
    public async Task RunAsync_when_the_database_has_what_the_model_expects_reports_no_difference_and_exits_Ok()
    {
        await using var schema = await TestSchema.CreateModelAsync();

        var run = await RunAsync(schema, Expected(schema));

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal($"differences: 0{Environment.NewLine}", run.Output);
        Assert.Empty(run.Error);
    }



    [Fact]
    public async Task RunAsync_when_the_database_differs_names_each_difference_and_exits_Failed()
    {
        await using var schema = await TestSchema.CreateModelAsync();
        var lines = Expected(schema)
            .Where(line => !line.StartsWith("index run.ux_run_status|", StringComparison.Ordinal))
            .Select(line => line.StartsWith("column app_user.version|", StringComparison.Ordinal) ? "column app_user.version|INTEGER null" : line)
            .Append("table stray_table|")
            .ToList();

        var run = await RunAsync(schema, lines);

        Assert.Equal(ExitCode.Failed, run.ExitCode);
        Assert.Equal($"differences: 3{Environment.NewLine}", run.Output);
        Assert.Equal
        (
            string.Join
            (
                Environment.NewLine,
                "different: column app_user.version is INTEGER null, expected INTEGER not null",
                "missing: index run.ux_run_status",
                "extra: table stray_table",
                string.Empty
            ),
            run.Error
        );
    }



    [Fact]
    public async Task RunAsync_when_the_database_writes_a_filter_its_own_way_reports_no_difference()
    {
        await using var schema = await TestSchema.CreateModelAsync();
        var lines = Expected(schema)
            .Select(line => line.Replace(" where status=0", " where ([Status]=(0))", StringComparison.Ordinal))
            .ToList();

        var run = await RunAsync(schema, lines);

        Assert.Contains(lines, line => line.EndsWith(" where ([Status]=(0))", StringComparison.Ordinal));
        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Empty(run.Error);
    }



    [Fact]
    public async Task RunAsync_when_the_database_is_behind_the_build_exits_MissingMigration_and_compares_nothing()
    {
        await using var schema = await TestSchema.CreateAsync();

        var run = await HostRun.StartAsync(["verify-schema"], Configuration, schema.Open, catalogQuery: "not SQL");

        Assert.Equal(ExitCode.MissingMigration, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Equal($"Missing migration: {TestSchema.Shipped}. Run `bf-maint migrate`.{Environment.NewLine}", run.Error);
    }



    [Fact]
    public async Task RunAsync_when_the_database_is_newer_than_the_build_exits_Refused_and_compares_nothing()
    {
        await using var schema = await TestSchema.CreateAsync(TestSchema.Shipped, "99990101000000_Later");

        var run = await HostRun.StartAsync(["verify-schema"], Configuration, schema.Open, catalogQuery: "not SQL");

        Assert.Equal(ExitCode.Refused, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.NotEmpty(run.Error);
    }



    [Fact]
    public async Task RunAsync_when_no_connection_string_is_set_exits_Usage_and_opens_nothing()
    {
        await using var schema = await TestSchema.CreateModelAsync();

        var run = await HostRun.StartAsync(["verify-schema"], new ConfigurationBuilder().Build(), schema.Open);

        Assert.Equal(ExitCode.Usage, run.ExitCode);
        Assert.Empty(schema.ConnectionStrings);
    }



    [Fact]
    public async Task RunAsync_when_given_an_argument_exits_Usage()
    {
        await using var schema = await TestSchema.CreateModelAsync();

        var run = await HostRun.StartAsync(["verify-schema", "extra"], Configuration, schema.Open);

        Assert.Equal(ExitCode.Usage, run.ExitCode);
        Assert.Empty(schema.ConnectionStrings);
    }



    [Fact]
    public async Task RunAsync_when_asked_for_help_says_that_it_changes_nothing()
    {
        await using var schema = await TestSchema.CreateModelAsync();

        var run = await HostRun.StartAsync(["verify-schema", "--help"], Configuration, schema.Open);

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Contains
        (
            "Compares the tables, columns, keys and indexes of the database with what this build expects. Changes nothing.",
            run.Output,
            StringComparison.Ordinal
        );
    }



    [Fact]
    public async Task RunAsync_when_run_changes_nothing_in_the_database()
    {
        await using var schema = await TestSchema.CreateModelAsync();
        var before = await schema.AppliedAsync();

        await RunAsync(schema, ["table stray_table|"]);

        Assert.Equal(before, await schema.AppliedAsync());
        Assert.False(await schema.HasAsync("stray_table"));
    }



    private static IReadOnlyList<string> Expected(TestSchema schema)
    {
        using var context = schema.NewContext();

        return SchemaLines.Expected(context);
    }



    /// <summary>
    /// Runs the verb with a catalog query that gives exactly <paramref name="lines"/>.
    /// </summary>
    private static async Task<HostRun> RunAsync(TestSchema schema, IReadOnlyList<string> lines)
    {
        var json = JsonSerializer
            .Serialize(lines)
            .Replace("'", "''", StringComparison.Ordinal)
            .Replace("{", "{{", StringComparison.Ordinal)
            .Replace("}", "}}", StringComparison.Ordinal);

        return await HostRun.StartAsync
        (
            ["verify-schema"],
            Configuration,
            schema.Open,
            catalogQuery: $"SELECT value AS \"Value\" FROM json_each('{json}')"
        );
    }
}
