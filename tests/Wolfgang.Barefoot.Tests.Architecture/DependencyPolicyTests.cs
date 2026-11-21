using Wolfgang.Barefoot.Tests.Architecture.Rules;
using Wolfgang.Barefoot.Tests.Architecture.Scanning;

namespace Wolfgang.Barefoot.Tests.Architecture;

/// <summary>
/// The dependency deny-list is enforced by the build, not by memory, and every entry says why.
/// </summary>
public sealed class DependencyPolicyTests
{
    [Fact]
    public async Task ViolationsIn_when_given_the_repository_reports_nothing()
    {
        var projects = await RepositoryFiles.ProjectsAsync();

        Assert.NotEmpty(projects);
        Assert.Empty(DependencyPolicy.ViolationsIn(projects));
    }



    [Fact]
    public void ViolationsIn_when_a_project_references_denied_packages_reports_each_with_its_reason()
    {
        var project = ProjectFile.Parse
        (
            "tests/Sample/Sample.csproj",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="mediatr" Version="12.0.0" />
                <PackageReference Include="Moq.AutoMock" Version="3.0.0" />
                <PackageReference Include="Moqingbird" Version="1.0.0" />
                <PackageReference Include="xunit" Version="2.9.3" />
              </ItemGroup>
            </Project>
            """
        );

        Assert.Equal
        (
            [
                "tests/Sample/Sample.csproj: Moq.AutoMock (test doubles are hand-written and live in the test kit)",
                "tests/Sample/Sample.csproj: mediatr (services are plain classes registered explicitly; no in-process bus (DESIGN §2))",
            ],
            DependencyPolicy.ViolationsIn([project])
        );
    }



    [Fact]
    public void DeniedPackages_when_read_gives_every_entry_a_reason()
    {
        Assert.NotEmpty(DependencyPolicy.DeniedPackages);
        Assert.All(DependencyPolicy.DeniedPackages, entry => Assert.False(string.IsNullOrWhiteSpace(entry.Value)));
    }
}
