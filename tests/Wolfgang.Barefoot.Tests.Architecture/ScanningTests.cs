using Wolfgang.Barefoot.Tests.Architecture.Scanning;

namespace Wolfgang.Barefoot.Tests.Architecture;

/// <summary>
/// The readers the architecture rules stand on: a rule is only as good as the list of files it is given.
/// </summary>
public sealed class ScanningTests
{
    [Fact]
    public void FindRoot_when_started_inside_the_repository_returns_the_directory_with_the_solution()
    {
        var root = RepositoryFiles.FindRoot(AppContext.BaseDirectory);

        Assert.True(File.Exists(Path.Combine(root, RepositoryFiles.SolutionFileName)));
        Assert.Equal(RepositoryFiles.Root, root);
    }



    [Fact]
    public void FindRoot_when_no_ancestor_holds_the_solution_throws()
    {
        var outside = Path.GetPathRoot(Path.GetTempPath())!;

        var exception = Assert.Throws<InvalidOperationException>(() => RepositoryFiles.FindRoot(outside));

        Assert.Contains(RepositoryFiles.SolutionFileName, exception.Message, StringComparison.Ordinal);
    }



    [Fact]
    public void PathsEndingIn_when_asked_for_projects_returns_sorted_relative_paths_outside_build_output()
    {
        var paths = RepositoryFiles.PathsEndingIn(".csproj");

        Assert.Contains("tests/Wolfgang.Barefoot.Tests.Architecture/Wolfgang.Barefoot.Tests.Architecture.csproj", paths);
        Assert.Equal(paths.Order(StringComparer.Ordinal), paths);
        Assert.DoesNotContain(paths, RepositoryFiles.IsBuildOutput);
    }



    [Theory]
    [InlineData("tests/Sample/bin/Release/net10.0/Sample.csproj", true)]
    [InlineData("tests/Sample/obj/project.cs", true)]
    [InlineData(".claude/worktrees/x/tests/Sample/Sample.csproj", true)]
    [InlineData("tests/Sample/Sample.csproj", false)]
    [InlineData("tests/Sample/bin.cs", false)]
    [InlineData("tests/Sample/.editorconfig", false)]
    public void IsBuildOutput_when_given_a_path_recognises_bin_obj_and_dot_directories(string path, bool expected)
    {
        Assert.Equal(expected, RepositoryFiles.IsBuildOutput(path));
    }



    [Fact]
    public async Task TestSourcesAsync_when_read_returns_this_file_with_its_text()
    {
        var sources = await RepositoryFiles.TestSourcesAsync();

        var self = Assert.Single(sources, source => source.Path.EndsWith("/ScanningTests.cs", StringComparison.Ordinal));
        Assert.Contains("public sealed class ScanningTests", self.Text, StringComparison.Ordinal);
        Assert.All(sources, source => Assert.StartsWith("tests/", source.Path, StringComparison.Ordinal));
    }



    [Fact]
    public void Parse_when_given_a_project_reads_its_sdk_framework_and_references()
    {
        var project = ProjectFile.Parse
        (
            "src/Wolfgang.Barefoot.Api/Wolfgang.Barefoot.Api.csproj",
            """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework> net10.0 </TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\Wolfgang.Barefoot.Data\Wolfgang.Barefoot.Data.csproj" />
                <ProjectReference Include="../Wolfgang.Barefoot.Business/Wolfgang.Barefoot.Business.csproj" />
                <PackageReference Include="Zeta" Version="1.0.0" />
                <PackageReference Include="Alpha" Version="1.0.0" />
                <PackageReference Update="Ignored" Version="1.0.0" />
                <FrameworkReference Include="Microsoft.AspNetCore.App" />
              </ItemGroup>
            </Project>
            """
        );

        Assert.Equal("Wolfgang.Barefoot.Api", project.Name);
        Assert.Equal("src", project.Area);
        Assert.Equal("Microsoft.NET.Sdk.Web", project.Sdk);
        Assert.Equal("Wolfgang.Barefoot.Api", project.AssemblyName);
        Assert.Equal("net10.0", project.TargetFramework);
        Assert.Equal(["Wolfgang.Barefoot.Business", "Wolfgang.Barefoot.Data"], project.ProjectReferences);
        Assert.Equal(["Alpha", "Zeta"], project.PackageReferences);
        Assert.Equal(["Microsoft.AspNetCore.App"], project.FrameworkReferences);
    }



    [Fact]
    public void Parse_when_the_project_sets_nothing_returns_empty_values()
    {
        var project = ProjectFile.Parse("tests/Sample/Sample.csproj", "<Project />");

        Assert.Equal(string.Empty, project.Sdk);
        Assert.Equal(string.Empty, project.TargetFramework);
        Assert.Empty(project.ProjectReferences);
        Assert.Empty(project.PackageReferences);
        Assert.Empty(project.FrameworkReferences);
    }



    [Fact]
    public void Compile_when_the_source_is_valid_returns_an_assembly_with_the_given_name()
    {
        var assembly = FixtureCompiler.Compile("CompiledFixture", "namespace Fixture; public sealed class Sample { }");

        Assert.Equal("CompiledFixture", assembly.GetName().Name);
        Assert.Equal("Fixture.Sample", Assert.Single(assembly.GetExportedTypes()).FullName);
    }



    [Fact]
    public void Compile_when_the_source_does_not_compile_throws_with_the_errors()
    {
        var exception = Assert.Throws<InvalidOperationException>
        (
            () => FixtureCompiler.Compile("BrokenFixture", "public sealed class Sample { Missing field; }")
        );

        Assert.Contains("CS0246", exception.Message, StringComparison.Ordinal);
    }



    [Fact]
    public async Task MismatchesWith_when_given_the_repository_reports_nothing()
    {
        var projects = await RepositoryFiles.ProjectsAsync();

        Assert.Empty(SourceAssemblies.MismatchesWith(projects, SourceAssemblies.All));
    }



    [Fact]
    public void MismatchesWith_when_a_project_or_an_assembly_has_no_partner_reports_both()
    {
        var projects = new[]
        {
            ProjectFile.Parse("src/Wolfgang.Barefoot.Core/Wolfgang.Barefoot.Core.csproj", "<Project />"),
            ProjectFile.Parse("src/Wolfgang.Barefoot.Data/Wolfgang.Barefoot.Data.csproj", "<Project />"),
            ProjectFile.Parse("tests/Wolfgang.Barefoot.TestKit/Wolfgang.Barefoot.TestKit.csproj", "<Project />"),
            ProjectFile.Parse
            (
                "src/Wolfgang.Barefoot.Maintenance/Wolfgang.Barefoot.Maintenance.csproj",
                "<Project><PropertyGroup><AssemblyName>bf-maint</AssemblyName></PropertyGroup></Project>"
            ),
            ProjectFile.Parse
            (
                "src/Wolfgang.Barefoot.Admin/Wolfgang.Barefoot.Admin.csproj",
                "<Project><PropertyGroup><AssemblyName>bf</AssemblyName></PropertyGroup></Project>"
            ),
        };
        var assemblies = new[]
        {
            FixtureCompiler.Compile("Wolfgang.Barefoot.Data", "namespace Fixture;"),
            FixtureCompiler.Compile("Wolfgang.Barefoot.Api", "namespace Fixture;"),
            FixtureCompiler.Compile("bf-maint", "namespace Fixture;"),
        };

        Assert.Equal
        (
            [
                "Wolfgang.Barefoot.Api: is in SourceAssemblies but not under src/",
                "Wolfgang.Barefoot.Core: is under src/ but not in SourceAssemblies",
                "bf: is under src/ but not in SourceAssemblies",
            ],
            SourceAssemblies.MismatchesWith(projects, assemblies)
        );
    }
}
