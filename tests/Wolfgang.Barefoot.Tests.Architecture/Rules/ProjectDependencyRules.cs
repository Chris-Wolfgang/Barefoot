using Wolfgang.Barefoot.Tests.Architecture.Scanning;

namespace Wolfgang.Barefoot.Tests.Architecture.Rules;

/// <summary>
/// The dependency rules of DESIGN §2: every client reaches the database through the API, and
/// <c>Core</c> and <c>Presentation</c> reference no framework. A rule applies to what a project
/// references directly and to everything it reaches through other projects.
/// </summary>
internal static class ProjectDependencyRules
{
    private const string Prefix = "Wolfgang.Barefoot.";



    /// <summary>
    /// Projects a given project must never reach, each with the sentence of the design that says so.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> ForbiddenProjects =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [Prefix + "Scraper"] = [Prefix + "Data"],
            [Prefix + "Admin"] = [Prefix + "Data", Prefix + "Obe"],
            [Prefix + "Web"] = [Prefix + "Data"],
            [Prefix + "Maintenance"] = [Prefix + "ApiClient", Prefix + "Obe"],
            [Prefix + "Api"] = [Prefix + "Obe"],
        };



    /// <summary>
    /// The clients of the API. They hold no connection string, so they reference no data-access package.
    /// </summary>
    public static readonly IReadOnlyList<string> ApiClients =
    [
        Prefix + "Admin",
        Prefix + "Scraper",
        Prefix + "Web",
    ];



    /// <summary>
    /// Package families that open a database. A family matches its own id and every id under it.
    /// </summary>
    public static readonly IReadOnlyList<string> DataPackageFamilies =
    [
        "Dapper",
        "Microsoft.Data.SqlClient",
        "Microsoft.Data.Sqlite",
        "Microsoft.EntityFrameworkCore",
        "System.Data.SqlClient",
        "Wolfgang.AuditTrail",
        "Wolfgang.DbContextBuilder",
    ];



    /// <summary>
    /// The projects that hold logic with no framework under it, so it can be tested without one.
    /// </summary>
    public static readonly IReadOnlyList<string> FrameworkFreeProjects =
    [
        Prefix + "Core",
        Prefix + "Presentation",
    ];



    /// <summary>
    /// Package families that are a framework: a web host, a data layer, a browser driver, a UI library,
    /// an identity library, a cloud SDK.
    /// </summary>
    public static readonly IReadOnlyList<string> FrameworkPackageFamilies =
    [
        "Azure",
        "Microsoft.AspNetCore",
        "Microsoft.Data",
        "Microsoft.EntityFrameworkCore",
        "Microsoft.FluentUI",
        "Microsoft.Identity",
        "Microsoft.Playwright",
    ];



    /// <summary>The only SDK a framework-free project may use.</summary>
    public const string PlainSdk = "Microsoft.NET.Sdk";



    /// <summary>
    /// One line per broken rule, as <c>project: what it must not have</c>, sorted.
    /// </summary>
    public static IReadOnlyList<string> ViolationsIn(IReadOnlyCollection<ProjectFile> projects)
    {
        var byName = projects.ToDictionary(project => project.Name, StringComparer.Ordinal);
        var violations = new List<string>();

        foreach (var project in projects)
        {
            var reached = Reach(project, byName);

            if (ForbiddenProjects.TryGetValue(project.Name, out var forbidden))
            {
                violations.AddRange
                (
                    reached
                        .Select(other => other.Name)
                        .Where(name => forbidden.Contains(name, StringComparer.Ordinal))
                        .Select(name => $"{project.Name}: project {name}")
                );
            }

            if (ApiClients.Contains(project.Name, StringComparer.Ordinal))
            {
                violations.AddRange(PackagesIn(project, reached, DataPackageFamilies));
            }

            if (FrameworkFreeProjects.Contains(project.Name, StringComparer.Ordinal))
            {
                violations.AddRange(PackagesIn(project, reached, FrameworkPackageFamilies));
                violations.AddRange(FrameworkUseIn(project, reached));
            }
        }

        return violations
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
    }



    /// <summary>
    /// Whether a package id is the family's own id or an id under it (<c>Dapper.Contrib</c> is still Dapper).
    /// </summary>
    public static bool IsInFamily(string packageId, string family)
    {
        return string.Equals(packageId, family, StringComparison.OrdinalIgnoreCase)
            || packageId.StartsWith(family + ".", StringComparison.OrdinalIgnoreCase);
    }



    /// <summary>
    /// The project itself and every project it references, directly or through another project.
    /// A reference to a project that is not in <paramref name="byName"/> is kept by name only.
    /// </summary>
    private static List<ProjectFile> Reach(ProjectFile start, Dictionary<string, ProjectFile> byName)
    {
        var seen = new Dictionary<string, ProjectFile>(StringComparer.Ordinal) { [start.Name] = start };
        var pending = new Queue<ProjectFile>([start]);

        while (pending.Count > 0)
        {
            foreach (var name in pending.Dequeue().ProjectReferences.Where(name => !seen.ContainsKey(name)))
            {
                var next = byName.GetValueOrDefault(name) ?? ProjectFile.Parse($"unknown/{name}.csproj", "<Project />");
                seen[name] = next;
                pending.Enqueue(next);
            }
        }

        return seen
            .Values
            .Where(project => !ReferenceEquals(project, start))
            .Prepend(start)
            .ToList();
    }



    private static IEnumerable<string> PackagesIn
    (
        ProjectFile project,
        IEnumerable<ProjectFile> reached,
        IReadOnlyList<string> families
    )
    {
        return reached
            .SelectMany(other => other.PackageReferences)
            .Where(id => families.Any(family => IsInFamily(id, family)))
            .Select(id => $"{project.Name}: package {id}");
    }



    private static IEnumerable<string> FrameworkUseIn(ProjectFile project, IReadOnlyCollection<ProjectFile> reached)
    {
        var frameworks = reached
            .SelectMany(other => other.FrameworkReferences)
            .Select(name => $"{project.Name}: framework reference {name}");

        var sdks = reached
            .Where(other => other.Sdk.Length > 0 && !string.Equals(other.Sdk, PlainSdk, StringComparison.Ordinal))
            .Select(other => $"{project.Name}: sdk {other.Sdk}");

        return frameworks.Concat(sdks);
    }
}
