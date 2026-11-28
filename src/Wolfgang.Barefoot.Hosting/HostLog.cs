using System.Globalization;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Formatting.Display;

namespace Wolfgang.Barefoot.Hosting;

/// <summary>
/// The log of one run of a command-line host (ADR 0016, decisions 17 to 22). Warnings and errors go
/// to stderr; information and above goes to the host's file of the day. Code logs through
/// <see cref="Microsoft.Extensions.Logging.ILogger"/> only, from <see cref="Factory"/>.
/// </summary>
public sealed class HostLog : IDisposable
{
    /// <summary>How many daily files of one host are kept.</summary>
    public const int KeptFiles = 90;

    /// <summary>What one line of the file looks like. The time is in UTC.</summary>
    internal const string FileTemplate =
        "{UtcTimestamp:yyyy-MM-dd HH:mm:ss.fff}Z [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

    /// <summary>What one line on stderr looks like: the message alone, with no stack trace.</summary>
    internal const string ErrorTemplate = "{Message:lj}{NewLine}";

    private readonly SerilogLoggerFactory _factory;



    private HostLog(SerilogLoggerFactory factory, string? filePath)
    {
        _factory = factory;
        FilePath = filePath;
    }



    /// <summary>Makes the loggers of this run.</summary>
    public ILoggerFactory Factory => _factory;



    /// <summary>
    /// The file this run logs to, or <see langword="null"/> when the run logs to stderr only.
    /// </summary>
    public string? FilePath { get; }



    /// <summary>
    /// Starts the log of one run. It makes the folder, opens the file of the day and removes the
    /// host's files beyond the newest <see cref="KeptFiles"/>. When the folder cannot be written it
    /// says so in one line on <see cref="HostLogOptions.Error"/> and the run logs to stderr only; it
    /// never throws for that.
    /// </summary>
    /// <param name="options">The host's name, its log folder, its stderr and its clock.</param>
    public static HostLog Start(HostLogOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var filePath = PrepareFile(options);

        var configuration = new LoggerConfiguration()
            .MinimumLevel
            .Information()
            .WriteTo
            .Sink
            (
                new WriterSink
                (
                    options.Error,
                    new MessageTemplateTextFormatter(ErrorTemplate, CultureInfo.InvariantCulture)
                ),
                LogEventLevel.Warning
            );

        if (filePath is not null)
        {
            configuration = configuration
                .WriteTo
                .File
                (
                    filePath,
                    outputTemplate: FileTemplate,
                    formatProvider: CultureInfo.InvariantCulture,
                    shared: true
                );
        }

        return new HostLog
        (
            new SerilogLoggerFactory(configuration.CreateLogger(), dispose: true),
            filePath
        );
    }



    /// <summary>
    /// Closes the file. The stderr writer is left open.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }



    /// <summary>
    /// The path of the file of the day, ready to be appended to, or <see langword="null"/> when there
    /// is no folder or it cannot be written.
    /// </summary>
    private static string? PrepareFile(HostLogOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Folder))
        {
            options
                .Error
                .WriteLine("There is no log folder for this user. Logging to stderr only.");

            return null;
        }

        try
        {
            Directory.CreateDirectory(options.Folder);

            var filePath = Path.Combine
            (
                options.Folder,
                LogFiles.NameFor(options.HostName, options.Clock.GetUtcNow())
            );

            // Serilog swallows a file it cannot open, so the file is opened here first.
            File
                .Open(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)
                .Dispose();

            RemoveExpired(options);

            return filePath;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            options
                .Error
                .WriteLine($"Cannot write the log file in '{options.Folder}': {exception.Message} Logging to stderr only.");

            return null;
        }
    }



    private static void RemoveExpired(HostLogOptions options)
    {
        var fileNames = Directory
            .EnumerateFiles(options.Folder)
            .Select(path => Path.GetFileName(path));

        foreach (var fileName in LogFiles.Expired(fileNames, options.HostName, KeptFiles))
        {
            File.Delete(Path.Combine(options.Folder, fileName));
        }
    }
}
