namespace Wolfgang.Barefoot.Contracts.Settings;

/// <summary>
/// One entry of the settings registry, as <c>GET /settings</c> and <c>GET /settings/{key}</c> return
/// it (ADR 0011 §2). Values travel as the invariant text they are stored as.
/// </summary>
/// <param name="Key">The setting's name, for example <c>run.lease_seconds</c>.</param>
/// <param name="Type">The name of the value's type, for a person to read.</param>
/// <param name="Default">The value used when none is stored.</param>
/// <param name="Description">One line saying what the setting controls.</param>
/// <param name="Value">The current value: the stored one, or the default when none is stored.</param>
/// <param name="Version">
/// The row version, which is the setting's ETag. A setting with no stored value reports 0, and its
/// first write names 0 (ADR 0011 §5).
/// </param>
/// <param name="RequiresRestart">Whether a changed value takes effect only after the API restarts.</param>
/// <param name="UpdatedUtc">When the value was last stored, or <see langword="null"/> when none is stored.</param>
/// <param name="UpdatedBy">Who last stored the value, or <see langword="null"/> when none is stored.</param>
public sealed record SettingResponse
(
    string Key,
    string Type,
    string Default,
    string Description,
    string Value,
    long Version,
    bool RequiresRestart,
    DateTimeOffset? UpdatedUtc,
    string? UpdatedBy
);
