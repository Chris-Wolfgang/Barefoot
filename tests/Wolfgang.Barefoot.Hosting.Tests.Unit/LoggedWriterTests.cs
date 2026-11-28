using System.Globalization;
using Microsoft.Extensions.Logging;
using Wolfgang.Barefoot.TestKit;

namespace Wolfgang.Barefoot.Hosting.Tests.Unit;

/// <summary>
/// The writer a host wraps its stderr in. Each test logs to a real <see cref="HostLog"/> on a folder
/// of its own and reads the file back.
/// </summary>
public sealed class LoggedWriterTests : IDisposable
{
    private const string Mark = "Z [INF] stderr-test: stderr: ";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"barefoot-{Guid.NewGuid():N}");

    private readonly StringWriter _logError = new();

    private readonly CountingWriter _inner = new();

    private readonly HostLog _log;



    public LoggedWriterTests()
    {
        _log = HostLog.Start(new HostLogOptions("bf-maint", _folder, _logError, TestClock.Create()));
    }



    /// <inheritdoc />
    public void Dispose()
    {
        _log.Dispose();
        _logError.Dispose();
        _inner.Dispose();
        Directory.Delete(_folder, recursive: true);
    }



    [Fact]
    public async Task Write_when_lines_are_written_passes_every_character_on_and_logs_each_line()
    {
        using (var writer = Writer())
        {
            writer.WriteLine("first");
            writer.Write("sec");
            writer.Write("ond\n");
        }

        Assert.Equal($"first{Environment.NewLine}second\n", _inner.ToString());
        Assert.Equal(["first", "second"], await LoggedAsync());
        Assert.Equal(string.Empty, _logError.ToString());
    }



    [Fact]
    public async Task Write_when_a_line_ends_in_a_carriage_return_and_a_line_feed_logs_it_without_either()
    {
        using (var writer = Writer())
        {
            writer.Write("one\r\ntwo\r\n");
        }

        Assert.Equal("one\r\ntwo\r\n", _inner.ToString());
        Assert.Equal(["one", "two"], await LoggedAsync());
    }



    [Fact]
    public async Task Write_when_a_line_is_empty_logs_an_empty_line()
    {
        using (var writer = Writer())
        {
            writer.Write("\n");
        }

        Assert.Equal([string.Empty], await LoggedAsync());
    }



    [Fact]
    public async Task WriteLineAsync_when_awaited_logs_the_line()
    {
        using (var writer = Writer())
        {
            await writer.WriteLineAsync("refused");
        }

        Assert.Equal($"refused{Environment.NewLine}", _inner.ToString());
        Assert.Equal(["refused"], await LoggedAsync());
    }



    [Fact]
    public async Task Write_when_a_line_has_not_ended_logs_nothing_yet()
    {
        var writer = Writer();
        writer.Write("half");
        _log.Dispose();

        Assert.Empty(await LoggedAsync());
        Assert.Equal("half", _inner.ToString());
    }



    [Fact]
    public async Task Dispose_when_a_line_has_not_ended_logs_it_and_leaves_the_other_writer_open()
    {
        var writer = Writer();
        writer.Write("no end");

        writer.Dispose();
        await _inner.WriteAsync("!");

        Assert.Equal(["no end"], await LoggedAsync());
        Assert.Equal("no end!", _inner.ToString());
    }



    [Fact]
    public async Task Dispose_when_every_line_has_ended_logs_nothing_more()
    {
        var writer = Writer();
        writer.Write("done\n");

        writer.Dispose();

        Assert.Equal(["done"], await LoggedAsync());
    }



    [Fact]
    public void Flush_when_called_flushes_the_other_writer()
    {
        using var writer = Writer();

        writer.Flush();

        Assert.Equal(1, _inner.Flushes);
    }



    [Fact]
    public void Encoding_when_read_is_that_of_the_other_writer()
    {
        using var writer = Writer();

        Assert.Same(_inner.Encoding, writer.Encoding);
    }



    [Fact]
    public void FormatProvider_when_read_is_that_of_the_other_writer()
    {
        using var french = new StringWriter(CultureInfo.GetCultureInfo("fr-FR"));
        using var writer = new LoggedWriter(french, Logger());

        writer.Write(1.5);

        Assert.Equal("1,5", french.ToString());
    }



    [Fact]
    public void Constructor_when_given_no_writer_throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new LoggedWriter(null!, Logger()));

        Assert.Equal("inner", exception.ParamName);
    }



    [Fact]
    public void Constructor_when_given_no_log_throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new LoggedWriter(_inner, null!));

        Assert.Equal("log", exception.ParamName);
    }



    private LoggedWriter Writer()
    {
        return new LoggedWriter(_inner, Logger());
    }



    private ILogger Logger()
    {
        return _log
            .Factory
            .CreateLogger("stderr-test");
    }



    /// <summary>
    /// What was logged after <see cref="Mark"/> on each line of the file, once the log is closed.
    /// </summary>
    private async Task<IReadOnlyList<string>> LoggedAsync()
    {
        _log.Dispose();

        var lines = await File.ReadAllLinesAsync(_log.FilePath!);

        return lines
            .Select(line => line[(line.IndexOf(Mark, StringComparison.Ordinal) + Mark.Length)..])
            .ToList();
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
