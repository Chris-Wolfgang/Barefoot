namespace Wolfgang.Barefoot.Core.Keys;

/// <summary>
/// What every setting key has in common, whatever the type of its value. The registry and the
/// Settings page list keys through this type (ADR 0011).
/// </summary>
public abstract class SettingKey
{
    private protected SettingKey
    (
        string name,
        string description,
        bool requiresRestart
    )
    {
        Name = KeyName.Require(name, nameof(name));

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("A setting needs a description.", nameof(description));
        }

        Description = description;
        RequiresRestart = requiresRestart;
    }



    /// <summary>The stable name of the setting, for example <c>run.lease_seconds</c>.</summary>
    public string Name { get; }



    /// <summary>One line saying what the setting controls.</summary>
    public string Description { get; }



    /// <summary>Whether a changed value takes effect only after the API restarts.</summary>
    public bool RequiresRestart { get; }



    /// <summary>The type of the setting's value.</summary>
    public abstract Type ValueType { get; }



    /// <summary>
    /// The name of the setting.
    /// </summary>
    public override string ToString()
    {
        return Name;
    }
}



/// <summary>
/// A typed setting key: an operational value an Admin can change without a redeploy (ADR 0011).
/// </summary>
/// <typeparam name="T">The type of the setting's value.</typeparam>
/// <remarks>
/// A key is defined once, as a <see langword="static"/> <see langword="readonly"/> instance, together
/// with the code that reads it. Settings are read and written with the key; there is no overload that
/// takes the name as a string.
/// </remarks>
public sealed class SettingKey<T> : SettingKey
    where T : notnull
{
    private readonly Func<T, string?>? _validator;



    /// <summary>
    /// Defines a setting.
    /// </summary>
    /// <param name="name">A lower-case dotted identifier, for example <c>run.lease_seconds</c>.</param>
    /// <param name="defaultValue">The value used when none is stored.</param>
    /// <param name="description">One line saying what the setting controls.</param>
    /// <param name="validator">
    /// Returns <see langword="null"/> for a value that is allowed, or a sentence saying why it is not.
    /// </param>
    /// <param name="requiresRestart">Whether a changed value takes effect only after the API restarts.</param>
    /// <exception cref="ArgumentException">
    /// The name is not a key name, the description is empty, or the default value fails the validator.
    /// </exception>
    public SettingKey
    (
        string name,
        T defaultValue,
        string description,
        Func<T, string?>? validator = null,
        bool requiresRestart = false
    )
        : base(name, description, requiresRestart)
    {
        ArgumentNullException.ThrowIfNull(defaultValue);

        _validator = validator;
        Default = defaultValue;

        if (Validate(defaultValue) is { } problem)
        {
            throw new ArgumentException($"The default of '{name}' fails its own validator: {problem}", nameof(defaultValue));
        }
    }



    /// <summary>The value used when none is stored.</summary>
    public T Default { get; }



    /// <inheritdoc />
    public override Type ValueType => typeof(T);



    /// <summary>
    /// Checks a candidate value.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns><see langword="null"/> when the value is allowed; otherwise a sentence saying why it is not.</returns>
    public string? Validate(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return _validator?.Invoke(value);
    }
}
