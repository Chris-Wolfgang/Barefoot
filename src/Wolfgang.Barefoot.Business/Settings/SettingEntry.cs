using Wolfgang.Barefoot.Core.Keys;

namespace Wolfgang.Barefoot.Business.Settings;

/// <summary>
/// One setting as the registry holds it: its key, and what the administration surface needs without
/// knowing the type of the value (ADR 0011 §2).
/// </summary>
public sealed class SettingEntry
{
    private readonly Func<string, string?> _problem;



    private SettingEntry
    (
        SettingKey key,
        string typeName,
        string defaultText,
        Func<string, string?> problem
    )
    {
        Key = key;
        TypeName = typeName;
        DefaultText = defaultText;
        _problem = problem;
    }



    /// <summary>The setting's key.</summary>
    public SettingKey Key { get; }



    /// <summary>The name of the value's type as the Settings page shows it.</summary>
    public string TypeName { get; }



    /// <summary>The default as the text that would be stored.</summary>
    public string DefaultText { get; }



    /// <summary>
    /// Makes the entry of <paramref name="key"/>.
    /// </summary>
    /// <typeparam name="T">The type of the setting's value.</typeparam>
    /// <param name="key">The setting's key.</param>
    /// <exception cref="NotSupportedException">A setting cannot have this type.</exception>
    public static SettingEntry For<T>(SettingKey<T> key)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(key);

        var codec = SettingCodec.For<T>();

        return new SettingEntry
        (
            key,
            codec.TypeName,
            codec.Format(key.Default),
            text =>
            {
                var (ok, value) = codec.Parse(text);

                return ok
                    ? key.Validate(value)
                    : $"'{text}' is not a {codec.TypeName}.";
            }
        );
    }



    /// <summary>
    /// Why <paramref name="text"/> cannot be stored as this setting's value: it is not a value of
    /// the type, or the key's validator rejects it. Null when it can.
    /// </summary>
    /// <param name="text">The text an Admin gave.</param>
    public string? Problem(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return _problem(text);
    }
}
