using Wolfgang.Barefoot.Core.Keys;

namespace Wolfgang.Barefoot.Core.Validation;

/// <summary>
/// One rule a value broke: which rule, which field, and the sentence to show.
/// </summary>
/// <param name="Code">The typed code of the rule.</param>
/// <param name="Field">The name of the field that broke it, as the definition names it.</param>
/// <param name="Message">The message for a person, with the code's placeholders filled in.</param>
public sealed record ValidationFailure(ErrorCode Code, string Field, string Message)
{
    /// <summary>
    /// Creates a failure whose message is the code's template filled with <paramref name="arguments"/>.
    /// </summary>
    /// <param name="code">The typed code of the rule.</param>
    /// <param name="field">The name of the field that broke it.</param>
    /// <param name="arguments">One value per placeholder of the code's template.</param>
    /// <exception cref="ArgumentNullException"><paramref name="code"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The number of values differs from what the code takes.</exception>
    public static ValidationFailure Of(ErrorCode code, string field, params object?[] arguments)
    {
        ArgumentNullException.ThrowIfNull(code);

        return new ValidationFailure(code, field, code.Format(arguments));
    }
}
