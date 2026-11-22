using System.Xml.Linq;

namespace Wolfgang.Barefoot.Tests.Architecture.Scanning;

/// <summary>
/// What the architecture rules need to know about one project file.
/// </summary>
internal sealed class ProjectFile
{
    private ProjectFile
    (
        string path,
        string sdk,
        string targetFramework,
        IReadOnlyList<string> projectReferences,
        IReadOnlyList<string> packageReferences,
        IReadOnlyList<string> frameworkReferences
    )
    {
        Path = path;
        Sdk = sdk;
        TargetFramework = targetFramework;
        ProjectReferences = projectReferences;
        PackageReferences = packageReferences;
        FrameworkReferences = frameworkReferences;
    }



    /// <summary>The repository-relative path, with forward slashes.</summary>
    public string Path { get; }



    /// <summary>The project's name: its file name without the extension.</summary>
    public string Name => System.IO.Path.GetFileNameWithoutExtension(Path);



    /// <summary>The top-level directory the project is under, for example <c>src</c> or <c>tests</c>.</summary>
    public string Area => Path.Split('/')[0];



    /// <summary>The <c>Sdk</c> attribute of the project element.</summary>
    public string Sdk { get; }



    /// <summary>
    /// The text of the <c>TargetFramework</c> element, or of <c>TargetFrameworks</c> when that is what the
    /// project sets; empty when it sets neither.
    /// </summary>
    public string TargetFramework { get; }



    /// <summary>The names of the projects this one references, sorted.</summary>
    public IReadOnlyList<string> ProjectReferences { get; }



    /// <summary>The ids of the packages this one references, sorted.</summary>
    public IReadOnlyList<string> PackageReferences { get; }



    /// <summary>The shared frameworks this one references, sorted.</summary>
    public IReadOnlyList<string> FrameworkReferences { get; }



    /// <summary>
    /// Reads a project from its XML.
    /// </summary>
    /// <param name="path">The repository-relative path, with forward slashes.</param>
    /// <param name="xml">The content of the project file.</param>
    public static ProjectFile Parse(string path, string xml)
    {
        var document = XDocument.Parse(xml);

        return new ProjectFile
        (
            path,
            (string?)document.Root?.Attribute("Sdk") ?? string.Empty,
            ElementText(document, "TargetFramework") ?? ElementText(document, "TargetFrameworks") ?? string.Empty,
            Includes(document, "ProjectReference")
                .Select(include => System.IO.Path.GetFileNameWithoutExtension(include.Replace('\\', '/')))
                .Order(StringComparer.Ordinal)
                .ToList(),
            Includes(document, "PackageReference")
                .Order(StringComparer.Ordinal)
                .ToList(),
            Includes(document, "FrameworkReference")
                .Order(StringComparer.Ordinal)
                .ToList()
        );
    }



    private static string? ElementText(XDocument document, string name)
    {
        return document
            .Descendants()
            .Where(element => string.Equals(element.Name.LocalName, name, StringComparison.Ordinal))
            .Select(element => element.Value.Trim())
            .FirstOrDefault();
    }



    private static IEnumerable<string> Includes(XDocument document, string itemName)
    {
        return document
            .Descendants()
            .Where(element => string.Equals(element.Name.LocalName, itemName, StringComparison.Ordinal))
            .Select(element => (string?)element.Attribute("Include") ?? string.Empty)
            .Where(include => include.Length > 0);
    }
}
