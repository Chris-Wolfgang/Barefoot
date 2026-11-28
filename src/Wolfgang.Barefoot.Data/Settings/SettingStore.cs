using Microsoft.EntityFrameworkCore;
using Wolfgang.Barefoot.Core.Settings;

namespace Wolfgang.Barefoot.Data.Settings;

/// <summary>
/// The stored settings as the reader needs them, from the <c>setting</c> table (ADR 0011 §4). Both
/// reads are one statement each and track nothing.
/// </summary>
public sealed class SettingStore : ISettingStore
{
    private readonly BarefootDbContext _context;



    /// <summary>
    /// Makes the store on <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The unit of work of the request.</param>
    public SettingStore(BarefootDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
    }



    /// <inheritdoc />
    public async Task<SettingGeneration> ProbeAsync(CancellationToken cancellationToken)
    {
        // One group over the whole table: the count and the newest change in one statement.
        // An empty table gives no group.
        var probe = await _context
            .Settings
            .GroupBy(_ => 1)
            .Select
            (
                all => new
                {
                    Count = all.Count(),
                    LastUpdatedUtc = all.Max(setting => setting.UpdatedUtc),
                }
            )
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return probe is null
            ? new SettingGeneration(0, LastUpdatedUtc: null)
            : new SettingGeneration(probe.Count, probe.LastUpdatedUtc);
    }



    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, string>> ReadAllAsync(CancellationToken cancellationToken)
    {
        return await _context
            .Settings
            .AsNoTracking()
            .ToDictionaryAsync
            (
                setting => setting.Key,
                setting => setting.Value,
                StringComparer.Ordinal,
                cancellationToken
            )
            .ConfigureAwait(false);
    }
}
