using Wolfgang.Barefoot.Tests.Architecture.Scanning;

namespace Wolfgang.Barefoot.Tests.Architecture.Rules;

/// <summary>
/// The dependency deny-list. This is the one place the list and its reasons are written;
/// <c>docs/CODING-CONVENTIONS.md</c> points here.
/// </summary>
internal static class DependencyPolicy
{
    /// <summary>
    /// Packages that never enter this repository, each with the reason. A name matches its own id and
    /// every id under it.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> DeniedPackages =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["AutoMapper"] = "conversions are hand-written ToDto()/ToEntity() extension methods (ADR 0002)",
            ["Dapper"] = "the data layer has no raw SQL outside the migration (DESIGN §11)",
            ["FluentAssertions"] = "xUnit's built-in assertions only; no assertion library",
            ["Hangfire"] = "background work is a table the scraper leases from, not a job queue (DESIGN §2)",
            ["MassTransit"] = "there is no message bus",
            ["MediatR"] = "services are plain classes registered explicitly; no in-process bus (DESIGN §2)",
            ["Microsoft.Kiota"] = "the API client is hand-written over the committed OpenAPI document (ADR 0010)",
            ["Moq"] = "test doubles are hand-written and live in the test kit",
            ["NodaTime"] = "TimeZoneInfo and DateTimeOffset are the time API; a time-zone library needs a written reason first",
            ["NSubstitute"] = "test doubles are hand-written and live in the test kit",
            ["NSwag"] = "the API client is hand-written over the committed OpenAPI document (ADR 0010)",
            ["Refit"] = "the API client is hand-written over the committed OpenAPI document (ADR 0010)",
        };



    /// <summary>
    /// One line per denied package a project references, as <c>path: package (reason)</c>, sorted.
    /// </summary>
    public static IReadOnlyList<string> ViolationsIn(IEnumerable<ProjectFile> projects)
    {
        return projects
            .SelectMany
            (
                project => project
                    .PackageReferences
                    .Select(id => (Id: id, Denied: DeniedPackages.Keys.FirstOrDefault(denied => ProjectDependencyRules.IsInFamily(id, denied))))
                    .Where(reference => reference.Denied is not null)
                    .Select(reference => $"{project.Path}: {reference.Id} ({DeniedPackages[reference.Denied!]})")
            )
            .Order(StringComparer.Ordinal)
            .ToList();
    }
}
