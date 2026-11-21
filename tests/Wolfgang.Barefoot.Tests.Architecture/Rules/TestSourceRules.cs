using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Wolfgang.Barefoot.Tests.Architecture.Scanning;

namespace Wolfgang.Barefoot.Tests.Architecture.Rules;

/// <summary>
/// The test conventions that can be read from source: how a test is named, where process-wide state may
/// be changed, and where test doubles live. Each rule reads syntax only, so it needs no reference to the
/// project it checks.
/// </summary>
internal static class TestSourceRules
{
    private static readonly Regex TestName = new
    (
        @"\A[A-Z][A-Za-z0-9]*_when_[A-Za-z0-9]+(?:_[A-Za-z0-9]+)+\z",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking
    );

    private static readonly Regex DoubleName = new
    (
        @"\A(?:Fake|Stub|Mock)[A-Z0-9].*\z|\A.*(?:Fake|Stub|Mock|Double)\z",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking
    );



    /// <summary>The directory of the shared test kit, the one home of test doubles.</summary>
    public const string TestKitDirectory = "tests/Wolfgang.Barefoot.TestKit/";



    /// <summary>
    /// Calls and assignments that change state every test in the process shares.
    /// <c>CultureInfo.CurrentCulture</c> is not here: it flows with the test's own execution context.
    /// </summary>
    public static readonly IReadOnlyList<string> ProcessWideState =
    [
        "Console.SetError",
        "Console.SetIn",
        "Console.SetOut",
        "CultureInfo.DefaultThreadCurrentCulture",
        "CultureInfo.DefaultThreadCurrentUICulture",
        "Directory.SetCurrentDirectory",
        "Environment.CurrentDirectory",
        "Environment.ExitCode",
        "Environment.SetEnvironmentVariable",
    ];



    /// <summary>
    /// One line per test whose name is not <c>MethodUnderTest_when_condition_expected_result</c>, as
    /// <c>path: name</c>, sorted.
    /// </summary>
    public static IReadOnlyList<string> MisnamedTestsIn(IEnumerable<SourceFile> sources)
    {
        return sources
            .SelectMany
            (
                source => MethodsIn(source)
                    .Where(IsTest)
                    .Select(method => method.Identifier.ValueText)
                    .Where(name => !TestName.IsMatch(name))
                    .Select(name => $"{source.Path}: {name}")
            )
            .Order(StringComparer.Ordinal)
            .ToList();
    }



    /// <summary>
    /// One line per class that changes process-wide state without being in a collection that runs alone,
    /// as <c>path: class changes what</c>, sorted.
    /// </summary>
    public static IReadOnlyList<string> UnserializedStateChangesIn(IReadOnlyCollection<SourceFile> sources)
    {
        var serialized = sources
            .SelectMany(ClassesIn)
            .SelectMany(type => AttributesOf(type, "CollectionDefinition"))
            .Where(attribute => attribute.ToString().Replace(" ", string.Empty, StringComparison.Ordinal).Contains("DisableParallelization=true", StringComparison.Ordinal))
            .Select(FirstArgument)
            .ToHashSet(StringComparer.Ordinal);

        return sources
            .SelectMany
            (
                source => ClassesIn(source)
                    .Where(type => !AttributesOf(type, "Collection").Select(FirstArgument).Any(serialized.Contains))
                    .SelectMany(type => StateChangesIn(type).Select(change => $"{source.Path}: {type.Identifier.ValueText} changes {change}"))
            )
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
    }



    /// <summary>
    /// One line per test double defined or built outside the test kit, as <c>path: what</c>, sorted:
    /// a class named as a double, a class that derives from a clock, or a fake clock constructed directly.
    /// </summary>
    public static IReadOnlyList<string> DoublesOutsideTheKitIn(IEnumerable<SourceFile> sources)
    {
        return sources
            .Where(source => !source.Path.StartsWith(TestKitDirectory, StringComparison.Ordinal))
            .SelectMany
            (
                source => ClassesIn(source)
                    .SelectMany(DoubleProblemsIn)
                    .Concat(DirectClocksIn(source))
                    .Select(problem => $"{source.Path}: {problem}")
            )
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
    }



    private static IEnumerable<MethodDeclarationSyntax> MethodsIn(SourceFile source)
    {
        return CSharpSyntaxTree
            .ParseText(source.Text)
            .GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>();
    }



    private static IEnumerable<ClassDeclarationSyntax> ClassesIn(SourceFile source)
    {
        return CSharpSyntaxTree
            .ParseText(source.Text)
            .GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>();
    }



    private static bool IsTest(MethodDeclarationSyntax method)
    {
        return method
            .AttributeLists
            .SelectMany(list => list.Attributes)
            .Select(SimpleName)
            .Any(name => name.EndsWith("Fact", StringComparison.Ordinal) || name.EndsWith("Theory", StringComparison.Ordinal));
    }



    private static IEnumerable<AttributeSyntax> AttributesOf(ClassDeclarationSyntax type, string name)
    {
        return type
            .AttributeLists
            .SelectMany(list => list.Attributes)
            .Where(attribute => string.Equals(SimpleName(attribute), name, StringComparison.Ordinal));
    }



    /// <summary>
    /// The attribute's name without namespace and without the <c>Attribute</c> suffix.
    /// </summary>
    private static string SimpleName(AttributeSyntax attribute)
    {
        var name = attribute.Name.ToString();
        name = name[(name.LastIndexOf('.') + 1)..];

        return name.EndsWith("Attribute", StringComparison.Ordinal) ? name[..^"Attribute".Length] : name;
    }



    private static string FirstArgument(AttributeSyntax attribute)
    {
        return attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression.ToString() ?? string.Empty;
    }



    private static IEnumerable<string> StateChangesIn(ClassDeclarationSyntax type)
    {
        var calls = type
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(call => call.Expression.ToString());

        var assignments = type
            .DescendantNodes()
            .OfType<AssignmentExpressionSyntax>()
            .Select(assignment => assignment.Left.ToString());

        return calls
            .Concat(assignments)
            .SelectMany(text => ProcessWideState.Where(state => string.Equals(text, state, StringComparison.Ordinal) || text.EndsWith("." + state, StringComparison.Ordinal)));
    }



    private static IEnumerable<string> DoubleProblemsIn(ClassDeclarationSyntax type)
    {
        var name = type.Identifier.ValueText;
        if (DoubleName.IsMatch(name))
        {
            yield return $"{name} is named as a test double";
        }

        var bases = type.BaseList?.Types.Select(baseType => baseType.Type.ToString()) ?? [];
        if (bases.Any(baseType => baseType.EndsWith("TimeProvider", StringComparison.Ordinal)))
        {
            yield return $"{name} derives from a clock";
        }
    }



    private static IEnumerable<string> DirectClocksIn(SourceFile source)
    {
        return CSharpSyntaxTree
            .ParseText(source.Text)
            .GetRoot()
            .DescendantNodes()
            .OfType<ObjectCreationExpressionSyntax>()
            .Where(creation => creation.Type.ToString().EndsWith("FakeTimeProvider", StringComparison.Ordinal))
            .Select(_ => "constructs FakeTimeProvider instead of asking TestClock");
    }
}
