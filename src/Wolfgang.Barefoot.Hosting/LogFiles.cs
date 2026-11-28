using System.Globalization;

namespace Wolfgang.Barefoot.Hosting;

/// <summary>
/// The names of a host's log files, <c>&lt;host&gt;-yyyyMMdd.log</c> with the day in UTC, and which of
/// them are old enough to go (ADR 0016, decision 18).
/// </summary>
internal static class LogFiles
{
    private const string Extension = ".log";

    private const int DayLength = 8;



    /// <summary>
    /// The name of the file <paramref name="hostName"/> logs to at <paramref name="now"/>.
    /// </summary>
    /// <param name="hostName">The name of the executable.</param>
    /// <param name="now">The moment the host started, in any offset.</param>
    public static string NameFor(string hostName, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostName);

        var day = now
            .UtcDateTime
            .ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        return $"{hostName}-{day}{Extension}";
    }



    /// <summary>
    /// The names among <paramref name="fileNames"/> that are log files of <paramref name="hostName"/>
    /// and are not among its newest <paramref name="kept"/>, oldest first. Files of another host, and
    /// files that are not named for a day, are never returned.
    /// </summary>
    /// <param name="fileNames">File names without a folder.</param>
    /// <param name="hostName">The name of the executable.</param>
    /// <param name="kept">How many of the host's files stay.</param>
    public static IReadOnlyList<string> Expired(IEnumerable<string> fileNames, string hostName, int kept)
    {
        ArgumentNullException.ThrowIfNull(fileNames);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostName);
        ArgumentOutOfRangeException.ThrowIfNegative(kept);

        return fileNames
            .Where(name => IsLogOf(name, hostName))
            .OrderDescending(StringComparer.Ordinal)
            .Skip(kept)
            .Reverse()
            .ToList();
    }



    private static bool IsLogOf(string fileName, string hostName)
    {
        var start = hostName + "-";

        return fileName.Length == start.Length + DayLength + Extension.Length
            && fileName.StartsWith(start, StringComparison.Ordinal)
            && fileName.EndsWith(Extension, StringComparison.Ordinal)
            && !fileName
                .AsSpan(start.Length, DayLength)
                .ContainsAnyExceptInRange('0', '9');
    }
}
