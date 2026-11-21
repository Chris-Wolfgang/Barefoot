namespace Wolfgang.Barefoot.Contracts.Settings;

/// <summary>
/// The body of <c>PUT /settings/{key}</c> (ADR 0011 §5).
/// </summary>
/// <param name="Value">The new value as invariant text. The API parses and validates it.</param>
public sealed record SetSettingRequest(string Value);
