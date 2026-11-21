namespace Wolfgang.Barefoot.Tests.Architecture.Scanning;

/// <summary>
/// One C# file: its repository-relative path and its text.
/// </summary>
internal sealed class SourceFile
{
    public SourceFile(string path, string text)
    {
        Path = path;
        Text = text;
    }



    /// <summary>The repository-relative path, with forward slashes.</summary>
    public string Path { get; }



    /// <summary>The file's content.</summary>
    public string Text { get; }
}
