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

    /// <summary>An airport's three-letter code.</summary>
    public const int AirportCode = 3;

    /// <summary>The booking site's own code for a resort or a room.</summary>
    public const int SiteCode = 32;

    /// <summary>The name of a city, a state, a country or an island.</summary>
    public const int PlaceName = 100;

    /// <summary>The version a build gives itself, such as <c>2.1.0+3f9c2ab</c>.</summary>
    public const int BuildVersion = 64;

    /// <summary>An error as a worker reports it. A longer one is cut before it is stored.</summary>
    public const int ErrorText = 2000;

    /// <summary>A SHA-256 hash written as hexadecimal digits.</summary>
    public const int Sha256Hex = 64;
}
