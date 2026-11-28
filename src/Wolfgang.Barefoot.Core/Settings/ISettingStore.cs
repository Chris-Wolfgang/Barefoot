namespace Wolfgang.Barefoot.Core.Settings;

/// <summary>
/// The stored settings as the reader needs them (ADR 0011 §4): every row at once, and a cheap
/// question that tells whether any row changed since.
/// </summary>
public interface ISettingStore
{
    /// <summary>
    /// How many values are stored and when the newest was changed.
    /// </summary>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<SettingGeneration> ProbeAsync(CancellationToken cancellationToken);



    /// <summary>
    /// Every stored value as text, by the setting's name. A setting nobody has changed is not in it.
    /// </summary>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<IReadOnlyDictionary<string, string>> ReadAllAsync(CancellationToken cancellationToken);
}
