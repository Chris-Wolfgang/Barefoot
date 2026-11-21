using System.Text.RegularExpressions;

namespace Wolfgang.Barefoot.Core.Keys;

/// <summary>
/// The one rule for the name of a typed key: a lower-case dotted identifier such as
/// <c>run.lease_seconds</c> or <c>search.name_taken</c>.
/// </summary>
/// <remarks>
/// A name has at least two segments separated by dots. A segment starts with a letter and holds
/// lower-case letters, digits and single underscores between them.
/// </remarks>
public static class KeyName
{
    private static readonly Regex Pattern = new
    (
        @"\A[a-z][a-z0-9]*(?:_[a-z0-9]+)*(?:\.[a-z][a-z0-9]*(?:_[a-z0-9]+)*)+\z",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking
    );



    /// <summary>
    /// Whether <paramref name="name"/> is a valid key name.
    /// </summary>
    /// <param name="name">The candidate name.</param>
    public static bool IsValid(string? name)
    {
        return name is not null && Pattern.IsMatch(name);
    }



    /// <summary>
    /// Returns <paramref name="name"/> when it is a valid key name.
    /// </summary>
    /// <param name="name">The candidate name.</param>
    /// <param name="parameterName">The name of the caller's parameter, for the exception.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a lower-case dotted identifier.</exception>
    public static string Require(string? name, string parameterName)
    {
        if (!IsValid(name))
        {
            throw new ArgumentException
            (
                $"'{name}' is not a key name. A key name is a lower-case dotted identifier such as 'run.lease_seconds'.",
                parameterName
            );
        }

        return name!;
    }
}
