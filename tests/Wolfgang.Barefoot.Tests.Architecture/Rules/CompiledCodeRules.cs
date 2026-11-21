using System.Reflection;

namespace Wolfgang.Barefoot.Tests.Architecture.Rules;

/// <summary>
/// The conventions that are read from compiled code: no <see cref="DateTime"/> on a public surface, and
/// conversion methods only in the layer the conventions name.
/// </summary>
internal static class CompiledCodeRules
{
    private const BindingFlags Declared =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private const BindingFlags DeclaredPublic =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;



    /// <summary>
    /// Which assembly may define each conversion method (ADR 0002 §3). No other assembly defines one.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> ConversionHomes =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ToDto"] = "Wolfgang.Barefoot.Api",
            ["FromDto"] = "Wolfgang.Barefoot.Api",
            ["ToEntity"] = "Wolfgang.Barefoot.Data",
            ["FromEntity"] = "Wolfgang.Barefoot.Data",
        };



    /// <summary>
    /// One line per public property, field, method return, parameter or constructor parameter whose type
    /// is <see cref="DateTime"/> or contains it, sorted.
    /// </summary>
    public static IReadOnlyList<string> DateTimeExposuresIn(IEnumerable<Assembly> assemblies)
    {
        return assemblies
            .SelectMany(assembly => assembly.GetExportedTypes())
            .SelectMany(DateTimeExposuresOf)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
    }



    /// <summary>
    /// One line per conversion method defined outside its home assembly, as
    /// <c>assembly: type.method belongs in home</c>, sorted.
    /// </summary>
    public static IReadOnlyList<string> MisplacedConversionsIn(IEnumerable<Assembly> assemblies)
    {
        return assemblies
            .SelectMany
            (
                assembly => assembly
                    .GetTypes()
                    .SelectMany(type => type.GetMethods(Declared))
                    .Where
                    (
                        method => ConversionHomes.TryGetValue(method.Name, out var home)
                            && !string.Equals(home, assembly.GetName().Name, StringComparison.Ordinal)
                    )
                    .Select(method => $"{assembly.GetName().Name}: {method.DeclaringType!.FullName}.{method.Name} belongs in {ConversionHomes[method.Name]}")
            )
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
    }



    private static IEnumerable<string> DateTimeExposuresOf(Type type)
    {
        var methods = type
            .GetMethods(DeclaredPublic)
            .Where(method => !method.IsSpecialName)
            .ToList();

        return type
            .GetProperties(DeclaredPublic)
            .Where(property => ContainsDateTime(property.PropertyType))
            .Select(property => $"{type.FullName}.{property.Name}")
            .Concat
            (
                type
                    .GetFields(DeclaredPublic)
                    .Where(field => ContainsDateTime(field.FieldType))
                    .Select(field => $"{type.FullName}.{field.Name}")
            )
            .Concat
            (
                methods
                    .Where(method => ContainsDateTime(method.ReturnType))
                    .Select(method => $"{type.FullName}.{method.Name}()")
            )
            .Concat
            (
                methods.SelectMany
                (
                    method => method
                        .GetParameters()
                        .Where(parameter => ContainsDateTime(parameter.ParameterType))
                        .Select(parameter => $"{type.FullName}.{method.Name}({parameter.Name})")
                )
            )
            .Concat
            (
                type
                    .GetConstructors(DeclaredPublic)
                    .SelectMany(constructor => constructor.GetParameters())
                    .Where(parameter => ContainsDateTime(parameter.ParameterType))
                    .Select(parameter => $"{type.FullName}..ctor({parameter.Name})")
            );
    }



    private static bool ContainsDateTime(Type type)
    {
        if (type == typeof(DateTime))
        {
            return true;
        }

        if (type.HasElementType)
        {
            return ContainsDateTime(type.GetElementType()!);
        }

        return type.IsGenericType && type.GetGenericArguments().Any(ContainsDateTime);
    }
}
