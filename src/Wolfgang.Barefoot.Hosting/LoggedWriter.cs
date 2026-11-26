using System.Text;
using Microsoft.Extensions.Logging;

namespace Wolfgang.Barefoot.Hosting;

/// <summary>
/// A writer that passes everything on to another writer and also logs each finished line as
/// information. A host wraps its stderr in one, so what the operator was told is in the log file too.
/// The other writer is never closed here.
/// </summary>
public sealed class LoggedWriter : TextWriter
{
    private readonly TextWriter _inner;

    private readonly ILogger _log;

    private readonly StringBuilder _line = new();



    /// <summary>
    /// Wraps <paramref name="inner"/>.
    /// </summary>
    /// <param name="inner">The writer that takes every character.</param>
    /// <param name="log">Takes each finished line.</param>
    public LoggedWriter(TextWriter inner, ILogger log)
        : base(Required(inner).FormatProvider)
    {
        ArgumentNullException.ThrowIfNull(log);

        _inner = inner;
        _log = log;
    }



    /// <inheritdoc />
    public override Encoding Encoding => _inner.Encoding;



    /// <inheritdoc />
    public override void Write(char value)
    {
        _inner.Write(value);

        if (value == '\n')
        {
            LogLine();
        }
        else if (value != '\r')
        {
            _line.Append(value);
        }
    }



    /// <inheritdoc />
    public override void Flush()
    {
        _inner.Flush();
    }



    /// <summary>
    /// Logs a last line that did not end, and leaves the other writer open.
    /// </summary>
    /// <param name="disposing">Whether this is a call to <see cref="TextWriter.Dispose()"/>.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && _line.Length > 0)
        {
            LogLine();
        }

        // Stryker disable once Statement : TextWriter.Dispose(bool) does nothing, so removing the call changes nothing a test can see.
        base.Dispose(disposing);
    }



    private static TextWriter Required(TextWriter inner)
    {
        ArgumentNullException.ThrowIfNull(inner);

        return inner;
    }



    private void LogLine()
    {
        _log.LogInformation("stderr: {Line}", _line.ToString());
        _line.Clear();
    }
}
