using System.Runtime.InteropServices;

namespace Wolfgang.Barefoot.Core.Settings;

/// <summary>
/// What tells one state of the stored settings from another without reading them: the number of
/// rows and the newest change (ADR 0011 §4). A value set, changed or reset moves at least one of
/// the two.
/// </summary>
/// <param name="Count">How many values are stored.</param>
/// <param name="LastUpdatedUtc">When the newest stored value was changed, or <see langword="null"/> when none is stored.</param>
[StructLayout(LayoutKind.Auto)]
public readonly record struct SettingGeneration(int Count, DateTimeOffset? LastUpdatedUtc);
