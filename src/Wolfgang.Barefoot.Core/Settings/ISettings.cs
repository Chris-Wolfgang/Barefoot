using Wolfgang.Barefoot.Core.Keys;

namespace Wolfgang.Barefoot.Core.Settings;

/// <summary>
/// How code reads a setting: by its typed key, and no other way (ADR 0011 §4). The value is the
/// stored one, or the key's default when nothing is stored.
/// </summary>
public interface ISettings
{
    /// <summary>
    /// The current value of <paramref name="key"/>.
    /// </summary>
    /// <typeparam name="T">The type of the setting's value.</typeparam>
    /// <param name="key">The setting.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    ValueTask<T> GetAsync<T>(SettingKey<T> key, CancellationToken cancellationToken)
        where T : notnull;
}
