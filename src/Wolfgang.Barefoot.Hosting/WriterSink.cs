using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace Wolfgang.Barefoot.Hosting;

/// <summary>
/// Writes each log event to a <see cref="TextWriter"/> the host owns, which is how the log reaches
/// stderr. The writer is never closed here.
/// </summary>
internal sealed class WriterSink : ILogEventSink
{
    private readonly TextWriter _writer;

    private readonly ITextFormatter _formatter;

    private readonly Lock _gate = new();



    public WriterSink(TextWriter writer, ITextFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(formatter);

        _writer = writer;
        _formatter = formatter;
    }



    /// <summary>
    /// Writes <paramref name="logEvent"/> and flushes, one event at a time.
    /// </summary>
    /// <param name="logEvent">The event to write.</param>
    public void Emit(LogEvent logEvent)
    {
        lock (_gate)
        {
            _formatter.Format(logEvent, _writer);
            _writer.Flush();
        }
    }
}
