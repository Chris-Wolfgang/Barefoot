using Wolfgang.Barefoot.Core.Keys;
using Wolfgang.Barefoot.Core.Settings;

namespace Wolfgang.Barefoot.Business.Settings;

/// <summary>
/// Reads a setting from the instance's <see cref="SettingCache"/> (ADR 0011 §4). A setting with no
/// stored value has its default. So has one whose stored text is not a value of its type, or one
/// its validator no longer accepts: a bad row must not stop the code that reads the setting.
/// </summary>
public sealed class CachedSettings : ISettings
{
    private readonly SettingCache _cache;

    private readonly ISettingStore _store;

    private readonly TimeProvider _clock;



    /// <summary>
    /// Makes a reader.
    /// </summary>
    /// <param name="cache">The instance's cache.</param>
    /// <param name="store">Reads the stored settings.</param>
    /// <param name="clock">Tells how old the cache is.</param>
    public CachedSettings
    (
        SettingCache cache,
        ISettingStore store,
        TimeProvider clock
    )
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(clock);

        _cache = cache;
        _store = store;
        _clock = clock;
    }



    /// <inheritdoc />
    public async ValueTask<T> GetAsync<T>(SettingKey<T> key, CancellationToken cancellationToken)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(key);

        var codec = SettingCodec.For<T>();

        var values = await _cache
            .ValuesAsync(_store, _clock, cancellationToken)
            .ConfigureAwait(false);

        if (!values.TryGetValue(key.Name, out var text))
        {
            return key.Default;
        }

        var (ok, value) = codec.Parse(text);

        return ok && key.Validate(value) is null
            ? value
            : key.Default;
    }
}
