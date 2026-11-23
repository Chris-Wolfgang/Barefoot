using Wolfgang.Barefoot.Tests.Architecture.Rules;
using Wolfgang.Barefoot.Tests.Architecture.Scanning;

namespace Wolfgang.Barefoot.Tests.Architecture;

/// <summary>
/// DESIGN §2: <c>Scraper</c>, <c>Admin</c> and <c>Web</c> never reference <c>Data</c> or a data-access
/// package; <c>Admin</c> never references <c>Obe</c>; <c>Maintenance</c> never references
/// <c>ApiClient</c> or <c>Obe</c>; <c>Api</c> never references <c>Obe</c>; <c>Core</c>,
/// <c>Contracts</c> and <c>Presentation</c> reference no framework; <c>Contracts</c> references no
/// project and <c>Core</c> only <c>Contracts</c>.
/// </summary>
public sealed class ProjectDependencyTests
{
    [Fact]
    public async Task ViolationsIn_when_given_the_repository_reports_nothing()
    {
        var projects = await RepositoryFiles.ProjectsAsync();

        Assert.NotEmpty(projects);
        Assert.Empty(ProjectDependencyRules.ViolationsIn(projects));
    }



    [Fact]
    public void ViolationsIn_when_a_client_references_Data_reports_the_project()
    {
        var projects = new[]
        {
            Project("Wolfgang.Barefoot.Web", projects: ["Wolfgang.Barefoot.Data", "Wolfgang.Barefoot.ApiClient"]),
            Project("Wolfgang.Barefoot.Data"),
            Project("Wolfgang.Barefoot.ApiClient"),
        };

        Assert.Equal
        (
            ["Wolfgang.Barefoot.Web: project Wolfgang.Barefoot.Data"],
            ProjectDependencyRules.ViolationsIn(projects)
        );
    }



    [Fact]
    public void ViolationsIn_when_a_client_reaches_Data_through_another_project_reports_the_project()
    {
        var projects = new[]
        {
            Project("Wolfgang.Barefoot.Scraper", projects: ["Wolfgang.Barefoot.ApiClient"]),
            Project("Wolfgang.Barefoot.ApiClient", projects: ["Wolfgang.Barefoot.Business"]),
            Project("Wolfgang.Barefoot.Business", projects: ["Wolfgang.Barefoot.Data", "Wolfgang.Barefoot.ApiClient"]),
            Project("Wolfgang.Barefoot.Data"),
        };

        Assert.Equal
        (
            ["Wolfgang.Barefoot.Scraper: project Wolfgang.Barefoot.Data"],
            ProjectDependencyRules.ViolationsIn(projects)
        );
    }



    [Fact]
    public void ViolationsIn_when_a_client_has_a_data_package_reports_the_package()
    {
        var projects = new[]
        {
            Project("Wolfgang.Barefoot.Admin", packages: ["Microsoft.EntityFrameworkCore.SqlServer", "System.CommandLine"]),
            Project("Wolfgang.Barefoot.Web", projects: ["Wolfgang.Barefoot.Shared"]),
            Project("Wolfgang.Barefoot.Shared", packages: ["dapper"]),
        };

        Assert.Equal
        (
            [
                "Wolfgang.Barefoot.Admin: package Microsoft.EntityFrameworkCore.SqlServer",
                "Wolfgang.Barefoot.Web: package dapper",
            ],
            ProjectDependencyRules.ViolationsIn(projects)
        );
    }



    [Fact]
    public void ViolationsIn_when_a_host_references_Obe_against_the_design_reports_the_project()
    {
        var projects = new[]
        {
            Project("Wolfgang.Barefoot.Admin", projects: ["Wolfgang.Barefoot.Obe"]),
            Project("Wolfgang.Barefoot.Api", projects: ["Wolfgang.Barefoot.Obe"]),
            Project("Wolfgang.Barefoot.Maintenance", projects: ["Wolfgang.Barefoot.Obe", "Wolfgang.Barefoot.ApiClient"]),
            Project("Wolfgang.Barefoot.Scraper", projects: ["Wolfgang.Barefoot.Obe"]),
        };

        Assert.Equal
        (
            [
                "Wolfgang.Barefoot.Admin: project Wolfgang.Barefoot.Obe",
                "Wolfgang.Barefoot.Api: project Wolfgang.Barefoot.Obe",
                "Wolfgang.Barefoot.Maintenance: project Wolfgang.Barefoot.ApiClient",
                "Wolfgang.Barefoot.Maintenance: project Wolfgang.Barefoot.Obe",
            ],
            ProjectDependencyRules.ViolationsIn(projects)
        );
    }



    [Fact]
    public void ViolationsIn_when_Core_uses_a_framework_reports_the_package_the_reference_and_the_sdk()
    {
        var projects = new[]
        {
            ProjectFile.Parse
            (
                "src/Wolfgang.Barefoot.Core/Wolfgang.Barefoot.Core.csproj",
                """
                <Project Sdk="Microsoft.NET.Sdk.Web">
                  <ItemGroup>
                    <FrameworkReference Include="Microsoft.AspNetCore.App" />
                    <PackageReference Include="Microsoft.Playwright" Version="1.0.0" />
                    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.0" />
                  </ItemGroup>
                </Project>
                """
            ),
        };

        Assert.Equal
        (
            [
                "Wolfgang.Barefoot.Core: framework reference Microsoft.AspNetCore.App",
                "Wolfgang.Barefoot.Core: package Microsoft.Playwright",
                "Wolfgang.Barefoot.Core: sdk Microsoft.NET.Sdk.Web",
            ],
            ProjectDependencyRules.ViolationsIn(projects)
        );
    }



    [Fact]
    public void ViolationsIn_when_Contracts_references_a_project_or_Core_another_than_Contracts_reports_each()
    {
        var projects = new[]
        {
            Project("Wolfgang.Barefoot.Contracts", projects: ["Wolfgang.Barefoot.Core"]),
            Project("Wolfgang.Barefoot.Core", projects: ["Wolfgang.Barefoot.Contracts", "Wolfgang.Barefoot.Presentation"]),
            Project("Wolfgang.Barefoot.Presentation"),
        };

        Assert.Equal
        (
            [
                "Wolfgang.Barefoot.Contracts: project Wolfgang.Barefoot.Core",
                "Wolfgang.Barefoot.Core: project Wolfgang.Barefoot.Presentation",
            ],
            ProjectDependencyRules.ViolationsIn(projects)
        );
    }



    [Fact]
    public void ViolationsIn_when_Contracts_has_a_framework_package_reports_it_for_everything_above()
    {
        var projects = new[]
        {
            Project("Wolfgang.Barefoot.Core", projects: ["Wolfgang.Barefoot.Contracts"]),
            Project("Wolfgang.Barefoot.Contracts", packages: ["Microsoft.AspNetCore.Http.Abstractions"]),
        };

        Assert.Equal
        (
            [
                "Wolfgang.Barefoot.Contracts: package Microsoft.AspNetCore.Http.Abstractions",
                "Wolfgang.Barefoot.Core: package Microsoft.AspNetCore.Http.Abstractions",
            ],
            ProjectDependencyRules.ViolationsIn(projects)
        );
    }



    [Fact]
    public void ViolationsIn_when_Presentation_reaches_a_framework_through_another_project_reports_it()
    {
        var projects = new[]
        {
            Project("Wolfgang.Barefoot.Presentation", projects: ["Wolfgang.Barefoot.Core"]),
            Project("Wolfgang.Barefoot.Core", packages: ["Microsoft.AspNetCore.Components"]),
        };

        Assert.Equal
        (
            [
                "Wolfgang.Barefoot.Core: package Microsoft.AspNetCore.Components",
                "Wolfgang.Barefoot.Presentation: package Microsoft.AspNetCore.Components",
            ],
            ProjectDependencyRules.ViolationsIn(projects)
        );
    }



    [Fact]
    public void ViolationsIn_when_every_project_follows_the_design_reports_nothing()
    {
        var projects = new[]
        {
            Project("Wolfgang.Barefoot.Core", projects: ["Wolfgang.Barefoot.Contracts"]),
            Project("Wolfgang.Barefoot.Contracts"),
            Project("Wolfgang.Barefoot.Data", projects: ["Wolfgang.Barefoot.Core"], packages: ["Microsoft.EntityFrameworkCore.SqlServer"]),
            Project("Wolfgang.Barefoot.Api", projects: ["Wolfgang.Barefoot.Data"]),
            Project("Wolfgang.Barefoot.ApiClient", projects: ["Wolfgang.Barefoot.Core"]),
            Project("Wolfgang.Barefoot.Scraper", projects: ["Wolfgang.Barefoot.ApiClient", "Wolfgang.Barefoot.Obe"]),
            Project("Wolfgang.Barefoot.Obe", packages: ["Microsoft.Playwright"]),
            Project("Wolfgang.Barefoot.Maintenance", projects: ["Wolfgang.Barefoot.Data"]),
            Project("Wolfgang.Barefoot.Web", projects: ["Wolfgang.Barefoot.ApiClient", "Wolfgang.Barefoot.Missing"]),
        };

        Assert.Empty(ProjectDependencyRules.ViolationsIn(projects));
    }



    [Theory]
    [InlineData("Dapper", "Dapper", true)]
    [InlineData("dapper.contrib", "Dapper", true)]
    [InlineData("DapperExtensions", "Dapper", false)]
    [InlineData("Microsoft.Data.Sqlite.Core", "Microsoft.Data.Sqlite", true)]
    public void IsInFamily_when_given_an_id_matches_the_family_and_ids_under_it
    (
        string packageId,
        string family,
        bool expected
    )
    {
        Assert.Equal(expected, ProjectDependencyRules.IsInFamily(packageId, family));
    }



    private static ProjectFile Project
    (
        string name,
        string[]? projects = null,
        string[]? packages = null
    )
    {
        var items = (projects ?? [])
            .Select(reference => $"<ProjectReference Include=\"..\\{reference}\\{reference}.csproj\" />")
            .Concat((packages ?? []).Select(id => $"<PackageReference Include=\"{id}\" Version=\"1.0.0\" />"));

        return ProjectFile.Parse
        (
            $"src/{name}/{name}.csproj",
            $"<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup>{string.Concat(items)}</ItemGroup></Project>"
        );
    }
}
