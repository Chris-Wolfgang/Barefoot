using Wolfgang.Barefoot.Tests.Architecture.Rules;
using Wolfgang.Barefoot.Tests.Architecture.Scanning;

namespace Wolfgang.Barefoot.Tests.Architecture;

/// <summary>
/// Every unit-test project has a Stryker configuration with a break threshold above zero, so a pull
/// request that lowers its mutation score below the floor fails.
/// </summary>
public sealed class MutationFloorTests
{
    private const string Project = "Wolfgang.Barefoot.Core.Tests.Unit";



    [Fact]
    public async Task ViolationsIn_when_given_the_repository_reports_nothing()
    {
        var projects = await RepositoryFiles.ProjectsAsync();
        var unitTests = projects
            .Where(project => project.Name.EndsWith(MutationFloorRules.UnitTestSuffix, StringComparison.Ordinal))
            .ToList();

        var configs = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var path in RepositoryFiles.PathsEndingIn(MutationFloorRules.ConfigFileName))
        {
            configs[path] = await RepositoryFiles.ReadAsync(path);
        }

        var violations = unitTests
            .SelectMany
            (
                project => MutationFloorRules.ViolationsIn
                (
                    project.Name,
                    configs.GetValueOrDefault(project.Path[..(project.Path.LastIndexOf('/') + 1)] + MutationFloorRules.ConfigFileName)
                )
            )
            .ToList();

        Assert.NotEmpty(unitTests);
        Assert.Empty(violations);
    }



    [Fact]
    public void ViolationsIn_when_the_configuration_sets_a_floor_reports_nothing()
    {
        var violations = MutationFloorRules.ViolationsIn
        (
            Project,
            """
            { "stryker-config": { "project": "Wolfgang.Barefoot.Core.csproj", "target-framework": "net10.0", "thresholds": { "break": 60 } } }
            """
        );

        Assert.Empty(violations);
    }



    [Fact]
    public void ViolationsIn_when_there_is_no_configuration_reports_it()
    {
        Assert.Equal
        (
            [$"{Project}: has no stryker-config.json"],
            MutationFloorRules.ViolationsIn(Project, null)
        );
    }



    [Fact]
    public void ViolationsIn_when_the_section_is_missing_reports_it()
    {
        Assert.Equal
        (
            [$"{Project}: stryker-config.json has no stryker-config section"],
            MutationFloorRules.ViolationsIn(Project, "{ }")
        );
    }



    [Theory]
    [InlineData("""{ "stryker-config": { } }""")]
    [InlineData("""{ "stryker-config": { "project": 1, "target-framework": null, "thresholds": { } } }""")]
    [InlineData("""{ "stryker-config": { "project": "", "target-framework": "", "thresholds": { "break": "60" } } }""")]
    public void ViolationsIn_when_every_setting_is_missing_or_mistyped_reports_all_three(string config)
    {
        Assert.Equal
        (
            [
                $"{Project}: project is '', not Wolfgang.Barefoot.Core.csproj",
                $"{Project}: target-framework is '', not net10.0",
                $"{Project}: thresholds.break is not above zero",
            ],
            MutationFloorRules.ViolationsIn(Project, config)
        );
    }



    [Fact]
    public void ViolationsIn_when_the_settings_are_wrong_reports_each()
    {
        var violations = MutationFloorRules.ViolationsIn
        (
            Project,
            """
            { "stryker-config": { "project": "Wolfgang.Barefoot.Data.csproj", "target-framework": "net8.0", "thresholds": { "break": 0 } } }
            """
        );

        Assert.Equal
        (
            [
                $"{Project}: project is 'Wolfgang.Barefoot.Data.csproj', not Wolfgang.Barefoot.Core.csproj",
                $"{Project}: target-framework is 'net8.0', not net10.0",
                $"{Project}: thresholds.break is not above zero",
            ],
            violations
        );
    }
}
