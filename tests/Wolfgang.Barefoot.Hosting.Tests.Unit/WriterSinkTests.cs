using System.Globalization;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Parsing;

namespace Wolfgang.Barefoot.Hosting.Tests.Unit;

/// <summary>
/// The sink that carries the log to the host's stderr writer.
/// </summary>
public sealed class WriterSinkTests
{
    private static readonly MessageTemplateTextFormatter Formatter = new("{Message:lj}|", CultureInfo.InvariantCulture);



    [Fact]
    public void Emit_when_given_an_event_writes_it_and_flushes()
    {
        using var writer = new CountingWriter();
        var sink = new WriterSink(writer, Formatter);

        sink.Emit(Event("first"));
        sink.Emit(Event("second"));

        Assert.Equal("first|second|", writer.ToString());
        Assert.Equal(2, writer.Flushes);
    }



    [Fact]
    public void Constructor_when_given_no_writer_throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new WriterSink(null!, Formatter));

        Assert.Equal("writer", exception.ParamName);
    }



    [Fact]
    public void Constructor_when_given_no_formatter_throws()
    {
        using var writer = new StringWriter();

        var exception = Assert.Throws<ArgumentNullException>(() => new WriterSink(writer, null!));

        Assert.Equal("formatter", exception.ParamName);
    }



    private static LogEvent Event(string text)
    {
        return new LogEvent
        (
            DateTimeOffset.UnixEpoch,
            LogEventLevel.Warning,
            exception: null,
            new MessageTemplateParser().Parse(text),
            []
        );
    }



    /// <summary>A writer that counts how often it is flushed.</summary>
    private sealed class CountingWriter : StringWriter
    {
        public int Flushes { get; private set; }



        public override void Flush()
        {
            Flushes++;
            base.Flush();
        }
    }
}
