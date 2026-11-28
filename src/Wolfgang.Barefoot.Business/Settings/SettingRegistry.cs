namespace Wolfgang.Barefoot.Business.Settings;

/// <summary>
/// Every setting of the application, collected once at startup (ADR 0011 §2). Two settings of the
/// same name are refused.
/// </summary>
public sealed class SettingRegistry
{
    private readonly Dictionary<string, SettingEntry> _byName;



    /// <summary>
    /// Collects <paramref name="entries"/>.
    /// </summary>
    /// <param name="entries">One entry per setting.</param>
    /// <exception cref="ArgumentException">Two entries have the same name.</exception>
    public SettingRegistry(IEnumerable<SettingEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var given = entries.ToList();

        var twice = given
            .GroupBy(entry => entry.Key.Name, StringComparer.Ordinal)
            .FirstOrDefault(sameName => sameName.Skip(1).Any());

        if (twice is not null)
        {
            throw new ArgumentException($"The setting '{twice.Key}' is registered twice.", nameof(entries));
        }

        _byName = given.ToDictionary(entry => entry.Key.Name, StringComparer.Ordinal);

        All = _byName
            .Values
            .OrderBy(entry => entry.Key.Name, StringComparer.Ordinal)
            .ToList();
    }



    /// <summary>Every setting, by name.</summary>
    public IReadOnlyList<SettingEntry> All { get; }



    /// <summary>
    /// The setting called <paramref name="name"/>, or <see langword="null"/> when there is none.
    /// Names are compared exactly.
    /// </summary>
    /// <param name="name">A setting's name.</param>
    public SettingEntry? Find(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return _byName.GetValueOrDefault(name);
    }
}
