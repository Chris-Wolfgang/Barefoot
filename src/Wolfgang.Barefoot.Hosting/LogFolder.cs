namespace Wolfgang.Barefoot.Hosting;

/// <summary>
/// Where the command-line hosts keep their log files: <c>Barefoot/logs</c> under the user's local
/// application data, one folder for every host (ADR 0016, decision 17).
/// </summary>
public static class LogFolder
{
    /// <summary>
    /// The log folder of the user who runs the host, or an empty text when that user has no local
    /// application data folder. The folder is not created here and need not exist.
    /// </summary>
    public static string Default()
    {
        return Under
        (
            Environment.GetFolderPath
            (
                Environment.SpecialFolder.LocalApplicationData,
                Environment.SpecialFolderOption.DoNotVerify
            )
        );
    }



    /// <summary>
    /// The log folder under <paramref name="localApplicationData"/>, or an empty text when
    /// <paramref name="localApplicationData"/> is empty.
    /// </summary>
    /// <param name="localApplicationData">The user's local application data folder.</param>
    public static string Under(string localApplicationData)
    {
        ArgumentNullException.ThrowIfNull(localApplicationData);

        return string.IsNullOrWhiteSpace(localApplicationData)
            ? string.Empty
            : Path.Combine(localApplicationData, "Barefoot", "logs");
    }
}
