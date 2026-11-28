using Wolfgang.Barefoot.Core.Settings;

namespace Wolfgang.Barefoot.TestKit;

/// <summary>
/// The stored settings held in memory, for tests of code that reads settings. It counts how often
/// it is asked, so a test can tell a read from the cache from a read of the store.
/// </summary>
public sealed class InMemorySettingStore : ISettingStore
{
    private readonly Dictionary<string, (string Value, DateTimeOffset UpdatedUtc)> _rows = new(StringComparer.Ordinal);



    /// <summary>How often <see cref="ProbeAsync"/> was called.</summary>
    public int ProbeCount { get; private set; }



    /// <summary>How often <see cref="ReadAllAsync"/> was called.</summary>
    public int ReadCount { get; private set; }



    /// <summary>
    /// Stores <paramref name="value"/> for the setting called <paramref name="name"/>, as changed at
    /// <paramref name="updatedUtc"/>.
    /// </summary>
    /// <param name="name">The setting's name.</param>
    /// <param name="value">The value as stored text.</param>
    /// <param name="updatedUtc">When the value was changed.</param>
    public void Set(string name, string value, DateTimeOffset updatedUtc)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);

        _rows[name] = (value, updatedUtc);
    }



    /// <summary>
    /// Removes the stored value of the setting called <paramref name="name"/>, which returns it to
    /// its default.
    /// </summary>
    /// <param name="name">The setting's name.</param>
    public void Reset(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        _rows.Remove(name);
    }



    /// <inheritdoc />
    public Task<SettingGeneration> ProbeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ProbeCount++;

        DateTimeOffset? newest = _rows.Count == 0
            ? null
            : _rows.Values.Max(row => row.UpdatedUtc);

        return Task.FromResult(new SettingGeneration(_rows.Count, newest));
    }



    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, string>> ReadAllAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReadCount++;

        IReadOnlyDictionary<string, string> values = _rows.ToDictionary
        (
            row => row.Key,
            row => row.Value.Value,
            StringComparer.Ordinal
        );

        return Task.FromResult(values);
    }
}
