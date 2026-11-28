namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// The stored value of one setting an Admin has changed (ADR 0011). A setting nobody has changed has
/// no row: its default comes from the registry.
/// </summary>
public sealed class Setting
{
    /// <summary>The setting's name, as its <c>SettingKey</c> gives it. It is the row's key.</summary>
    public required string Key { get; set; }



    /// <summary>The value as text, in the form the setting's type parses.</summary>
    public required string Value { get; set; }



    /// <summary>When the value was last changed.</summary>
    public DateTimeOffset UpdatedUtc { get; set; }



    /// <summary>
    /// The <see cref="AppUser"/> who last changed the value. The settings page shows their name
    /// beside the value.
    /// </summary>
    public Guid UpdatedByUserId { get; set; }



    /// <summary>The concurrency token, which is also the row's ETag (ADR 0004 §10).</summary>
    public long Version { get; set; }
}
