using Wolfgang.Barefoot.Tests.Architecture.Rules;
using Wolfgang.Barefoot.Tests.Architecture.Scanning;

namespace Wolfgang.Barefoot.Tests.Architecture;

/// <summary>
/// Every project is one the design names, is called what the conventions say, targets the one
/// framework, sits in a directory of its own name and is in the solution.
/// </summary>
public sealed class ProjectLayoutTests
{
    private const string PlainProject = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>";



    [Fact]
    public async Task ViolationsIn_when_given_the_repository_reports_nothing()
    {
        var projects = await RepositoryFiles.ProjectsAsync();
        var solution = await RepositoryFiles.ReadAsync(RepositoryFiles.SolutionFileName);

        Assert.NotEmpty(projects);
        Assert.Empty(ProjectLayoutRules.ViolationsIn(projects, solution));
    }



    [Fact]
    public void ViolationsIn_when_projects_follow_the_layout_reports_nothing()
    {
        var paths = new[]
        {
            "src/Wolfgang.Barefoot.Core/Wolfgang.Barefoot.Core.csproj",
            "tests/Wolfgang.Barefoot.Core.Tests.Unit/Wolfgang.Barefoot.Core.Tests.Unit.csproj",
            "tests/Wolfgang.Barefoot.Api.Tests.Integration/Wolfgang.Barefoot.Api.Tests.Integration.csproj",
            "tests/Wolfgang.Barefoot.TestKit/Wolfgang.Barefoot.TestKit.csproj",
            "tests/Wolfgang.Barefoot.TestKit.Tests.Unit/Wolfgang.Barefoot.TestKit.Tests.Unit.csproj",
            "tests/Wolfgang.Barefoot.Tests.Architecture/Wolfgang.Barefoot.Tests.Architecture.csproj",
        };

        var violations = ProjectLayoutRules.ViolationsIn
        (
            paths.Select(path => ProjectFile.Parse(path, PlainProject)).ToList(),
            SolutionListing(paths)
        );

        Assert.Empty(violations);
    }



    [Fact]
    public void ViolationsIn_when_a_project_has_a_name_the_design_does_not_know_reports_it()
    {
        var paths = new[]
        {
            "src/Wolfgang.Barefoot.Helpers/Wolfgang.Barefoot.Helpers.csproj",
            "tests/Wolfgang.Barefoot.Core.Tests/Wolfgang.Barefoot.Core.Tests.csproj",
            "tests/Wolfgang.Barefoot.Helpers.Tests.Unit/Wolfgang.Barefoot.Helpers.Tests.Unit.csproj",
            "tools/Wolfgang.Barefoot.Core/Wolfgang.Barefoot.Core.csproj",
        };

        var violations = ProjectLayoutRules.ViolationsIn
        (
            paths.Select(path => ProjectFile.Parse(path, PlainProject)).ToList(),
            SolutionListing(paths)
        );

        Assert.Equal
        (
            [
                "src/Wolfgang.Barefoot.Helpers/Wolfgang.Barefoot.Helpers.csproj: is not a project the design names (DESIGN §2)",
                "tests/Wolfgang.Barefoot.Core.Tests/Wolfgang.Barefoot.Core.Tests.csproj: is not the test kit, the architecture tests, or <project>.Tests.Unit / .Tests.Integration",
                "tests/Wolfgang.Barefoot.Helpers.Tests.Unit/Wolfgang.Barefoot.Helpers.Tests.Unit.csproj: is not the test kit, the architecture tests, or <project>.Tests.Unit / .Tests.Integration",
                "tools/Wolfgang.Barefoot.Core/Wolfgang.Barefoot.Core.csproj: is outside src/ and tests/",
            ],
            violations
        );
    }



    [Theory]
    [InlineData("<TargetFramework>net8.0</TargetFramework>", "targets 'net8.0', not net10.0")]
    [InlineData("<TargetFrameworks>net10.0;net8.0</TargetFrameworks>", "targets 'net10.0;net8.0', not net10.0")]
    [InlineData("", "targets '', not net10.0")]
    public void ViolationsIn_when_a_project_targets_something_else_reports_it(string property, string expected)
    {
        const string Path = "src/Wolfgang.Barefoot.Core/Wolfgang.Barefoot.Core.csproj";
        var project = ProjectFile.Parse(Path, $"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup>{property}</PropertyGroup></Project>");

        var violations = ProjectLayoutRules.ViolationsIn([project], SolutionListing([Path]));

        Assert.Equal([$"{Path}: {expected}"], violations);
    }



    [Fact]
    public void ViolationsIn_when_a_project_is_misplaced_or_missing_from_the_solution_reports_both()
    {
        const string Path = "src/Core/Wolfgang.Barefoot.Core.csproj";
        var project = ProjectFile.Parse(Path, PlainProject);

        var violations = ProjectLayoutRules.ViolationsIn([project], SolutionListing([]));

        Assert.Equal
        (
            [
                $"{Path}: is not in a directory of its own name",
                $"{Path}: is not in the solution",
            ],
            violations
        );
    }



    private static string SolutionListing(IEnumerable<string> paths)
    {
        return "<Solution>" + string.Concat(paths.Select(path => $"<Project Path=\"{path}\" />")) + "</Solution>";
    }
}
