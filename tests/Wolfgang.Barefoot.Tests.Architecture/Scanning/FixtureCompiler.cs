using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Wolfgang.Barefoot.Tests.Architecture.Scanning;

/// <summary>
/// Compiles a fixture from source text into an assembly a reflection scanner can read. A scanner's
/// positive control is written as the code it must reject, kept as text so it is never part of a
/// real assembly.
/// </summary>
internal static class FixtureCompiler
{
    /// <summary>
    /// Compiles <paramref name="source"/> into an in-memory assembly named <paramref name="assemblyName"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The source does not compile; the message lists the errors.</exception>
    public static Assembly Compile(string assemblyName, string source)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));

        var compilation = CSharpCompilation.Create
        (
            assemblyName,
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        using var image = new MemoryStream();
        var result = compilation.Emit(image);
        if (!result.Success)
        {
            var errors = result
                .Diagnostics
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Select(diagnostic => diagnostic.ToString());

            throw new InvalidOperationException($"The fixture does not compile: {string.Join("; ", errors)}");
        }

        return Assembly.Load(image.ToArray());
    }
}
