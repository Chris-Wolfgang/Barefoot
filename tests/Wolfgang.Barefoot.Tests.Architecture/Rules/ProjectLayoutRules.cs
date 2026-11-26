using System.Text.RegularExpressions;
using Wolfgang.Barefoot.Tests.Architecture.Scanning;

namespace Wolfgang.Barefoot.Tests.Architecture.Rules;

/// <summary>
/// Where projects live and what they are called: the project table of DESIGN §2 for <c>src/</c>, and
/// the naming of test projects from the coding conventions.
/// </summary>
internal static class ProjectLayoutRules
{
    private const string Prefix = "Wolfgang.Barefoot.";



    private static readonly Regex TestProjectName = new
    (
        @"\A(?<subject>.+)\.Tests\.(?:Unit|Integration)\z",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking
    );



    /// <summary>The one framework every project targets.</summary>
    public const string TargetFramework = "net10.0";



    /// <summary>The shared test kit, the one project under <c>tests/</c> that holds test doubles.</summary>
    public const string TestKit = Prefix + "TestKit";



    /// <summary>The architecture tests, which belong to no single source project.</summary>
    public const string ArchitectureTests = Prefix + "Tests.Architecture";



    /// <summary>
    /// The projects the design names. A project under <c>src/</c> is one of these.
    /// </summary>
    public static readonly IReadOnlyList<string> SourceProjects =
    [
        Prefix + "Admin",
        Prefix + "Api",
        Prefix + "ApiClient",
        Prefix + "Business",
        Prefix + "Contracts",
        Prefix + "Core",
        Prefix + "Data",
        Prefix + "Hosting",
        Prefix + "Maintenance",
        Prefix + "Obe",
        Prefix + "Presentation",
        Prefix + "Scraper",
        Prefix + "Web",
    ];



    /// <summary>
    /// One line per broken rule, as <c>path: what is wrong</c>, sorted.
    /// </summary>
    /// <param name="projects">Every project in the repository.</param>
    /// <param name="solution">The text of the solution file.</param>
    public static IReadOnlyList<string> ViolationsIn(IReadOnlyCollection<ProjectFile> projects, string solution)
    {
        var violations = new List<string>();

        foreach (var project in projects)
        {
            if (NameProblem(project) is { } problem)
            {
                violations.Add($"{project.Path}: {problem}");
            }

            if (!string.Equals(project.TargetFramework, TargetFramework, StringComparison.Ordinal))
            {
                violations.Add($"{project.Path}: targets '{project.TargetFramework}', not {TargetFramework}");
            }

            if (!project.Path.EndsWith($"/{project.Name}/{project.Name}.csproj", StringComparison.Ordinal))
            {
                violations.Add($"{project.Path}: is not in a directory of its own name");
            }

            if (!solution.Contains($"\"{project.Path}\"", StringComparison.Ordinal))
            {
                violations.Add($"{project.Path}: is not in the solution");
            }
        }

        return violations
            .Order(StringComparer.Ordinal)
            .ToList();
    }



    private static string? NameProblem(ProjectFile project)
    {
        return project.Area switch
        {
            "src" when !SourceProjects.Contains(project.Name, StringComparer.Ordinal)
                => "is not a project the design names (DESIGN §2)",
            "src" => null,
            "tests" when IsTestProjectName(project.Name) => null,
            "tests" => "is not the test kit, the architecture tests, or <project>.Tests.Unit / .Tests.Integration",
            _ => "is outside src/ and tests/",
        };
    }



    private static bool IsTestProjectName(string name)
    {
        if (name is TestKit or ArchitectureTests)
        {
            return true;
        }

        var match = TestProjectName.Match(name);

        return match.Success
            && (string.Equals(match.Groups["subject"].Value, TestKit, StringComparison.Ordinal)
                || SourceProjects.Contains(match.Groups["subject"].Value, StringComparer.Ordinal));
    }
}
