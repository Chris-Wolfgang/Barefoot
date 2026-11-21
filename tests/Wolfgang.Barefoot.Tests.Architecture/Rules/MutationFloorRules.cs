using System.Text.Json;

namespace Wolfgang.Barefoot.Tests.Architecture.Rules;

/// <summary>
/// DESIGN §14: Stryker runs per test project, and a run only gates a pull request when its
/// configuration sets a break threshold above zero. Every unit-test project therefore carries a
/// <c>stryker-config.json</c> that names the project it mutates, pins the target framework and sets
/// that threshold.
/// </summary>
internal static class MutationFloorRules
{
    /// <summary>The file a unit-test project keeps beside its project file.</summary>
    public const string ConfigFileName = "stryker-config.json";



    /// <summary>The suffix of a project the rule applies to.</summary>
    public const string UnitTestSuffix = ".Tests.Unit";



    /// <summary>
    /// What is wrong with one unit-test project's configuration, or nothing.
    /// </summary>
    /// <param name="projectName">The test project's name, ending in <see cref="UnitTestSuffix"/>.</param>
    /// <param name="config">The text of its <see cref="ConfigFileName"/>, or <see langword="null"/> when it has none.</param>
    public static IReadOnlyList<string> ViolationsIn(string projectName, string? config)
    {
        if (config is null)
        {
            return [$"{projectName}: has no {ConfigFileName}"];
        }

        using var document = JsonDocument.Parse(config);
        if (!document.RootElement.TryGetProperty("stryker-config", out var settings))
        {
            return [$"{projectName}: {ConfigFileName} has no stryker-config section"];
        }

        var violations = new List<string>();
        var subject = projectName[..^UnitTestSuffix.Length] + ".csproj";

        if (!string.Equals(Text(settings, "project"), subject, StringComparison.Ordinal))
        {
            violations.Add($"{projectName}: project is '{Text(settings, "project")}', not {subject}");
        }

        if (!string.Equals(Text(settings, "target-framework"), ProjectLayoutRules.TargetFramework, StringComparison.Ordinal))
        {
            violations.Add($"{projectName}: target-framework is '{Text(settings, "target-framework")}', not {ProjectLayoutRules.TargetFramework}");
        }

        var floor = settings.TryGetProperty("thresholds", out var thresholds)
            && thresholds.TryGetProperty("break", out var value)
            && value.ValueKind == JsonValueKind.Number
                ? value.GetInt32()
                : 0;

        if (floor <= 0)
        {
            violations.Add($"{projectName}: thresholds.break is not above zero");
        }

        return violations;
    }



    private static string Text(JsonElement settings, string name)
    {
        return settings.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()!
            : string.Empty;
    }
}
