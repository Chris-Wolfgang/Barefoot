using System.Globalization;

namespace Wolfgang.Barefoot.Core.Keys;

/// <summary>
/// A typed error code: the stable name a client can branch on, the HTTP status it travels with, the
/// message a person reads and how serious it is (ADR 0012 §1).
/// </summary>
/// <remarks>
/// A code is defined once, as a <see langword="static"/> <see langword="readonly"/> instance. Its name
/// and its message template are checked when it is constructed, so a bad definition fails when the
/// program starts and never while an error is being reported.
/// </remarks>
public sealed class ErrorCode
{
    /// <summary>
    /// Defines an error code.
    /// </summary>
    /// <param name="name">A lower-case dotted identifier, for example <c>search.name_taken</c>.</param>
    /// <param name="status">The HTTP status the error is reported with, 400 to 599.</param>
    /// <param name="messageTemplate">
    /// The message, with positional placeholders (<c>{0}</c>, <c>{1}</c>) numbered from zero with no gap,
    /// and doubled braces for a literal brace.
    /// </param>
    /// <param name="severity">How serious the error is for whoever receives it.</param>
    /// <exception cref="ArgumentException">The name is not a key name, or the template is empty or malformed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The status is not an HTTP error status, or the severity is not defined.</exception>
    public ErrorCode
    (
        string name,
        int status,
        string messageTemplate,
        ErrorSeverity severity = ErrorSeverity.Error
    )
    {
        Name = KeyName.Require(name, nameof(name));

        if (status is < 400 or > 599)
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "An error code's status is between 400 and 599.");
        }

        if (!Enum.IsDefined(severity))
        {
            throw new ArgumentOutOfRangeException(nameof(severity), severity, "The severity is not a defined value.");
        }

        if (string.IsNullOrWhiteSpace(messageTemplate))
        {
            throw new ArgumentException("An error code needs a message.", nameof(messageTemplate));
        }

        try
        {
            ArgumentCount = MessageTemplate.CountArguments(messageTemplate);
        }
        catch (FormatException exception)
        {
            throw new ArgumentException($"The message template of '{name}' is malformed. {exception.Message}", nameof(messageTemplate), exception);
        }

        Status = status;
        Template = messageTemplate;
        Severity = severity;
    }



    /// <summary>The stable name of the code, for example <c>search.name_taken</c>.</summary>
    public string Name { get; }



    /// <summary>The HTTP status the error is reported with.</summary>
    public int Status { get; }



    /// <summary>The message template, with positional placeholders.</summary>
    public string Template { get; }



    /// <summary>How serious the error is for whoever receives it.</summary>
    public ErrorSeverity Severity { get; }



    /// <summary>The number of arguments <see cref="Format"/> takes.</summary>
    public int ArgumentCount { get; }



    /// <summary>
    /// The anchor of this code in the generated error catalogue: the name with its dots as hyphens.
    /// </summary>
    public string Anchor => Name.Replace('.', '-');



    /// <summary>
    /// The message with its placeholders filled in, formatted with the invariant culture.
    /// </summary>
    /// <param name="arguments">One value per placeholder, in order.</param>
    /// <exception cref="ArgumentException">The number of values differs from <see cref="ArgumentCount"/>.</exception>
    public string Format(params object?[] arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        if (arguments.Length != ArgumentCount)
        {
            throw new ArgumentException
            (
                $"'{Name}' takes {ArgumentCount} argument(s) and was given {arguments.Length}.",
                nameof(arguments)
            );
        }

        return string.Format(CultureInfo.InvariantCulture, Template, arguments);
    }



    /// <summary>
    /// The name of the code.
    /// </summary>
    public override string ToString()
    {
        return Name;
    }
}
