namespace Wolfgang.Barefoot.Data.Configurations;

/// <summary>
/// The longest text each kind of column holds. A kind has one length wherever it appears, so a value
/// that fits one table fits every other.
/// </summary>
internal static class ColumnLengths
{
    /// <summary>A person's or a recipient's name as it is shown.</summary>
    public const int DisplayName = 200;

    /// <summary>An e-mail address: 64 for the local part, the at sign, 255 for the domain.</summary>
    public const int EmailAddress = 320;

    /// <summary>An IANA time-zone id. The longest today is under 40 characters.</summary>
    public const int TimeZoneId = 64;

    /// <summary>The name of a setting, such as <c>digest.max_recipients_per_owner</c>.</summary>
    public const int SettingKey = 128;

    /// <summary>A setting's value in its stored text form.</summary>
    public const int SettingValue = 4000;

    /// <summary>One line of text a person reads: a subject or a title.</summary>
    public const int Line = 200;

    /// <summary>A sentence or two a person reads.</summary>
    public const int Sentence = 500;
}
