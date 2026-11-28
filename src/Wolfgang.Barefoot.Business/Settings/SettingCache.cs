using Wolfgang.Barefoot.Core.Settings;

namespace Wolfgang.Barefoot.Business.Settings;

/// <summary>
/// The stored settings as one API instance last read them (ADR 0011 §4). There is one cache per
/// instance; every <see cref="CachedSettings"/> of the instance shares it.
/// </summary>
/// <remarks>
/// The cache asks the store whether anything changed only when a value is read and the last answer
/// is older than <see cref="ProbeInterval"/>. It never asks from a timer: a query in the background
/// would keep the serverless database awake (ADR 0001).
/// </remarks>
public sealed class SettingCache : IDisposable
{
    /// <summary>How old the cache may be before a read asks the store whether anything changed.</summary>
    public static readonly TimeSpan ProbeInterval = TimeSpan.FromSeconds(30);

    private readonly SemaphoreSlim _gate = new(1, 1);

    private IReadOnlyDictionary<string, string>? _values;

    private SettingGeneration _generation;

    private DateTimeOffset _checkedUtc;



    /// <summary>
    /// Makes the next read load the values again. The instance that changed a setting calls this,
    /// so that it sees its own change at once.
    /// </summary>
    public void Invalidate()
    {
        _values = null;
    }



    /// <inheritdoc />
    public void Dispose()
    {
        _gate.Dispose();
    }



    /// <summary>
    /// The stored values, read again when there are none yet or when the store says they changed.
    /// </summary>
    /// <param name="store">Reads the stored settings.</param>
    /// <param name="clock">Tells how old the cache is.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    internal async ValueTask<IReadOnlyDictionary<string, string>> ValuesAsync
    (
        ISettingStore store,
        TimeProvider clock,
        CancellationToken cancellationToken
    )
    {
        var now = clock.GetUtcNow();

        var values = _values;
        if (values is not null && now - _checkedUtc < ProbeInterval)
        {
            return values;
        }

        await _gate
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            // The generation is read before the values. A change that lands between the two reads
            // then shows as a new generation at the next probe, and the values are read again.
            var generation = await store
                .ProbeAsync(cancellationToken)
                .ConfigureAwait(false);

            values = _values;
            if (values is null || generation != _generation)
            {
                values = await store
                    .ReadAllAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            _generation = generation;
            _checkedUtc = now;
            _values = values;

            return values;
        }
        finally
        {
            _gate.Release();
        }
    }
}
