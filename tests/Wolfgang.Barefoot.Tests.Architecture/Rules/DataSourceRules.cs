using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Wolfgang.Barefoot.Tests.Architecture.Scanning;

namespace Wolfgang.Barefoot.Tests.Architecture.Rules;

/// <summary>
/// The conventions of the <c>Data</c> project that can be read from its source. The rule reads syntax
/// only, so it needs no reference to the project it checks.
/// </summary>
internal static class DataSourceRules
{
    /// <summary>The directory of the <c>Data</c> project.</summary>
    public const string DataDirectory = "src/Wolfgang.Barefoot.Data/";



    /// <summary>The one directory of the <c>Data</c> project that may name the database provider.</summary>
    public const string MigrationsDirectory = DataDirectory + "Migrations/";



    /// <summary>
    /// What names the provider: part of an identifier or of a string, as in <c>UseSqlServer</c>,
    /// <c>Microsoft.Data.SqlClient</c> and <c>"SqlServer:Identity"</c>.
    /// </summary>
    public static readonly IReadOnlyList<string> ProviderNames = ["SqlServer", "SqlClient"];



    /// <summary>
    /// One line per place where a file of the <c>Data</c> project outside <c>Migrations/</c> names the
    /// provider in code, as <c>path(line): token</c>, in the order of the files and of their lines. A
    /// comment that names it is not reported.
    /// </summary>
    public static IReadOnlyList<string> ProviderNamesOutsideMigrationsIn(IEnumerable<SourceFile> sources)
    {
        return sources
            .Where(IsDataOutsideMigrations)
            .OrderBy(source => source.Path, StringComparer.Ordinal)
            .SelectMany
            (
                source => CSharpSyntaxTree
                    .ParseText(source.Text)
                    .GetRoot()
                    .DescendantTokens()
                    .Where(NamesProvider)
                    .Select(token => $"{source.Path}({LineOf(token)}): {token.ValueText}")
            )
            .ToList();
    }



    private static bool IsDataOutsideMigrations(SourceFile source)
    {
        return source.Path.StartsWith(DataDirectory, StringComparison.Ordinal)
            && !source.Path.StartsWith(MigrationsDirectory, StringComparison.Ordinal);
    }



    private static bool NamesProvider(SyntaxToken token)
    {
        return ProviderNames.Any(name => token.ValueText.Contains(name, StringComparison.Ordinal));
    }



    private static int LineOf(SyntaxToken token)
    {
        return token.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
    }
}
