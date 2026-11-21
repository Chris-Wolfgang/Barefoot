using System.Reflection;

namespace Wolfgang.Barefoot.Tests.Architecture.Scanning;

/// <summary>
/// The assemblies built from <c>src/</c>, for the rules that read compiled code. A new project under
/// <c>src/</c> is added here and referenced from this test project; <c>SourceAssembliesTests</c> fails
/// until it is.
/// </summary>
internal static class SourceAssemblies
{
    /// <summary>
    /// One assembly per project under <c>src/</c>.
    /// </summary>
    public static IReadOnlyList<Assembly> All { get; } =
    [
        typeof(Contracts.ContractVersion).Assembly,
        typeof(Core.Keys.ErrorCode).Assembly,
    ];



    /// <summary>
    /// The names of the projects under <c>src/</c> that have no assembly in <paramref name="assemblies"/>,
    /// and of the assemblies that have no project, sorted.
    /// </summary>
    public static IReadOnlyList<string> MismatchesWith(IEnumerable<ProjectFile> projects, IEnumerable<Assembly> assemblies)
    {
        var projectNames = projects
            .Where(project => string.Equals(project.Area, "src", StringComparison.Ordinal))
            .Select(project => project.Name)
            .ToList();

        var assemblyNames = assemblies
            .Select(assembly => assembly.GetName().Name!)
            .ToList();

        return projectNames
            .Except(assemblyNames, StringComparer.Ordinal)
            .Select(name => $"{name}: is under src/ but not in SourceAssemblies")
            .Concat
            (
                assemblyNames
                    .Except(projectNames, StringComparer.Ordinal)
                    .Select(name => $"{name}: is in SourceAssemblies but not under src/")
            )
            .Order(StringComparer.Ordinal)
            .ToList();
    }
}
