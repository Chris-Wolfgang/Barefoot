using System.Globalization;
using System.Text.Json;

namespace Wolfgang.Barefoot.Business.Settings;

/// <summary>
/// Turns a setting's value into the text the <c>Setting</c> table stores and back (ADR 0011 §3).
/// The text is the same in every culture.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
/// <param name="TypeName">The name of the type as the Settings page shows it.</param>
/// <param name="Format">The text of a value.</param>
/// <param name="Parse">The value of a text, or nothing when the text is not one.</param>
public sealed record SettingCodec<T>
(
    string TypeName,
    Func<T, string> Format,
    Func<string, (bool Ok, T Value)> Parse
)
    where T : notnull;



/// <summary>
/// The codecs of the types a setting may have: bool, int, long, double, string,
/// <see cref="TimeSpan"/>, <see cref="DateTimeOffset"/> and a list of strings.
/// </summary>
public static class SettingCodec
{
    private const NumberStyles WholeNumber = NumberStyles.AllowLeadingSign;

    private const NumberStyles Fraction = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <summary><c>true</c> or <c>false</c>, in lower case.</summary>
    public static readonly SettingCodec<bool> Boolean = new
    (
        "boolean",
        value => value ? "true" : "false",
        text => text switch
        {
            "true" => (true, true),
            "false" => (true, false),
            _ => (false, false),
        }
    );

    /// <summary>A whole number in digits, with a leading minus when negative.</summary>
    public static readonly SettingCodec<int> Integer = new
    (
        "integer",
        value => value.ToString(Invariant),
        text => (int.TryParse(text, WholeNumber, Invariant, out var value), value)
    );

    /// <summary>A whole number in digits, with a leading minus when negative.</summary>
    public static readonly SettingCodec<long> Long = new
    (
        "long",
        value => value.ToString(Invariant),
        text => (long.TryParse(text, WholeNumber, Invariant, out var value), value)
    );

    /// <summary>A number with a point as the decimal separator, written so that it reads back the same.</summary>
    public static readonly SettingCodec<double> Number = new
    (
        "number",
        value => value.ToString(Invariant),
        text => (double.TryParse(text, Fraction, Invariant, out var value) && double.IsFinite(value), value)
    );

    /// <summary>The text itself.</summary>
    public static readonly SettingCodec<string> Text = new
    (
        "string",
        value => value,
        text => (true, text)
    );

    /// <summary>A length of time as <c>[d.]hh:mm:ss[.fffffff]</c>.</summary>
    public static readonly SettingCodec<TimeSpan> Duration = new
    (
        "duration",
        value => value.ToString(),
        text => (TimeSpan.TryParseExact(text, "c", Invariant, out var value), value)
    );

    /// <summary>An instant in UTC as <c>yyyy-MM-ddTHH:mm:ss.fffffffZ</c>.</summary>
    public static readonly SettingCodec<DateTimeOffset> Instant = new
    (
        "instant",
        value => value
            .UtcDateTime
            .ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", Invariant),
        text =>
        (
            DateTimeOffset.TryParseExact
            (
                text,
                "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'",
                Invariant,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var value
            ),
            value
        )
    );

    /// <summary>A list of strings as a JSON array of strings.</summary>
    public static readonly SettingCodec<IReadOnlyList<string>> TextList = new
    (
        "string-list",
        value => JsonSerializer.Serialize(value),
        ParseList
    );



    /// <summary>
    /// The codec of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of a setting's value.</typeparam>
    /// <exception cref="NotSupportedException">A setting cannot have this type.</exception>
    public static SettingCodec<T> For<T>()
        where T : notnull
    {
        object? codec = typeof(T) switch
        {
            var type when type == typeof(bool) => Boolean,
            var type when type == typeof(int) => Integer,
            var type when type == typeof(long) => Long,
            var type when type == typeof(double) => Number,
            var type when type == typeof(string) => Text,
            var type when type == typeof(TimeSpan) => Duration,
            var type when type == typeof(DateTimeOffset) => Instant,
            var type when type == typeof(IReadOnlyList<string>) => TextList,
            _ => null,
        };

        return codec as SettingCodec<T>
            ?? throw new NotSupportedException($"A setting cannot be a {typeof(T).Name}.");
    }



    private static (bool Ok, IReadOnlyList<string> Value) ParseList(string text)
    {
        try
        {
            var items = JsonSerializer.Deserialize<string?[]>(text);
            if (items is null || items.Contains(null))
            {
                return (false, []);
            }

            return (true, items.OfType<string>().ToList());
        }
        catch (JsonException)
        {
            return (false, []);
        }
    }
}
