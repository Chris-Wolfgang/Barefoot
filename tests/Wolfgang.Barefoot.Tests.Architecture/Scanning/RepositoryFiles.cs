namespace Wolfgang.Barefoot.Tests.Architecture.Scanning;

/// <summary>
/// Reads the repository checkout the tests run from, so the architecture rules can be applied to the
/// real project and source files.
/// </summary>
internal static class RepositoryFiles
{
    /// <summary>The solution file that marks the repository root.</summary>
    public const string SolutionFileName = "Wolfgang.Barefoot.slnx";



    /// <summary>
    /// The repository root: the nearest ancestor of the test output directory that holds the solution file.
    /// </summary>
    public static string Root { get; } = FindRoot(AppContext.BaseDirectory);



    /// <summary>
    /// Walks up from <paramref name="startDirectory"/> to the directory that holds the solution file.
    /// </summary>
    /// <exception cref="InvalidOperationException">No ancestor of <paramref name="startDirectory"/> holds it.</exception>
    public static string FindRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException($"{SolutionFileName} not found above {startDirectory}");
    }



    /// <summary>
    /// Every file under the repository whose name ends in <paramref name="extension"/>, outside build
    /// output, as repository-relative paths with forward slashes, sorted.
    /// </summary>
    /// <param name="extension">The extension with its dot, for example <c>.csproj</c>.</param>
    public static IReadOnlyList<string> PathsEndingIn(string extension)
    {
        return Directory
            .EnumerateFiles(Root, "*" + extension, SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(Root, path).Replace('\\', '/'))
            .Where(path => !IsBuildOutput(path))
            .Order(StringComparer.Ordinal)
            .ToList();
    }



    /// <summary>
    /// Whether a repository-relative path is inside a <c>bin</c> or <c>obj</c> directory, or inside a
    /// directory whose name starts with a dot.
    /// </summary>
    public static bool IsBuildOutput(string relativePath)
    {
        return relativePath
            .Split('/')
            .SkipLast(1)
            .Any(segment => segment is "bin" or "obj" || segment.StartsWith('.'));
    }



    /// <summary>
    /// The text of a file, by repository-relative path.
    /// </summary>
    public static Task<string> ReadAsync(string relativePath)
    {
        return File.ReadAllTextAsync(Path.Combine(Root, relativePath));
    }



    /// <summary>
    /// Every project in the repository, sorted by path.
    /// </summary>
    public static async Task<IReadOnlyList<ProjectFile>> ProjectsAsync()
    {
        var projects = new List<ProjectFile>();
        foreach (var path in PathsEndingIn(".csproj"))
        {
            projects.Add(ProjectFile.Parse(path, await ReadAsync(path)));
        }

        return projects;
    }



    /// <summary>
    /// Every C# file under <c>tests/</c>, as path and text, sorted by path.
    /// </summary>
    public static Task<IReadOnlyList<SourceFile>> TestSourcesAsync()
    {
        return SourcesUnderAsync("tests/");
    }



    /// <summary>
    /// Every C# file under <paramref name="directory"/>, as path and text, sorted by path.
    /// </summary>
    /// <param name="directory">The repository-relative directory, with forward slashes and a trailing one.</param>
    public static async Task<IReadOnlyList<SourceFile>> SourcesUnderAsync(string directory)
    {
        var sources = new List<SourceFile>();
        foreach (var path in PathsEndingIn(".cs").Where(path => path.StartsWith(directory, StringComparison.Ordinal)))
        {
            sources.Add(new SourceFile(path, await ReadAsync(path)));
        }

        return sources;
    }
}
