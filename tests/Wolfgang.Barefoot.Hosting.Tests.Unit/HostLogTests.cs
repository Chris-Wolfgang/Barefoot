using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Wolfgang.Barefoot.TestKit;

namespace Wolfgang.Barefoot.Hosting.Tests.Unit;

/// <summary>
/// The log of a command-line host, on a folder of its own under the temporary directory (ADR 0016,
/// decisions 17 to 22).
/// </summary>
public sealed class HostLogTests : IDisposable
{
    private const string Host = "bf-maint";

    private const string Today = "bf-maint-20261126.log";

    private const string Category = "Wolfgang.Barefoot.Sample";

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"barefoot-{Guid.NewGuid():N}");

    private readonly string _folder;

    private readonly StringWriter _error = new();



    public HostLogTests()
    {
        Directory.CreateDirectory(_root);
        _folder = Path.Combine(_root, "Barefoot", "logs");
    }



    /// <inheritdoc />
    public void Dispose()
    {
        _error.Dispose();
        Directory.Delete(_root, recursive: true);
    }



    [Fact]
    public void Start_when_the_folder_does_not_exist_makes_it_and_the_file_of_the_day()
    {
        using var log = Start();

        Assert.Equal(Path.Combine(_folder, Today), log.FilePath);
        Assert.True(File.Exists(log.FilePath));
        Assert.Equal(string.Empty, _error.ToString());
    }



    [Fact]
    public void Start_when_the_day_differs_in_UTC_names_the_file_for_the_UTC_day()
    {
        var options = Options() with
        {
            Clock = TestClock.At(new DateTimeOffset(2026, 11, 26, 22, 0, 0, TimeSpan.FromHours(-5))),
        };

        using var log = HostLog.Start(options);

        Assert.Equal(Path.Combine(_folder, "bf-maint-20261127.log"), log.FilePath);
    }



    [Fact]
    public async Task Start_when_information_is_logged_writes_it_to_the_file_and_not_to_stderr()
    {
        using (var log = Start())
        {
            Log(log).LogInformation("applied {Count} migrations", 2);
        }

        var line = Assert.Single(await LinesAsync());
        Assert.Matches
        (
            new Regex(@"\A\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}Z \[INF\] Wolfgang\.Barefoot\.Sample: applied 2 migrations\z", RegexOptions.None, TimeSpan.FromSeconds(1)),
            line
        );
        Assert.Equal(string.Empty, _error.ToString());
    }



    [Fact]
    public async Task Start_when_a_warning_is_logged_writes_it_to_the_file_and_to_stderr()
    {
        using (var log = Start())
        {
            Log(log).LogWarning("the grant was skipped");
        }

        var line = Assert.Single(await LinesAsync());
        Assert.EndsWith("Z [WRN] Wolfgang.Barefoot.Sample: the grant was skipped", line, StringComparison.Ordinal);
        Assert.Equal($"the grant was skipped{Environment.NewLine}", _error.ToString());
    }



    [Fact]
    public async Task Start_when_debug_is_logged_writes_it_nowhere()
    {
        using (var log = Start())
        {
            Log(log).LogDebug("a detail");
        }

        Assert.Empty(await LinesAsync());
        Assert.Equal(string.Empty, _error.ToString());
    }



    [Fact]
    public async Task Start_when_an_exception_is_logged_keeps_the_stack_trace_out_of_stderr()
    {
        using (var log = Start())
        {
            Log(log).LogError(new InvalidOperationException("the inner reason"), "bf-maint failed");
        }

        var lines = await LinesAsync();
        Assert.EndsWith("Z [ERR] Wolfgang.Barefoot.Sample: bf-maint failed", lines[0], StringComparison.Ordinal);
        Assert.Equal("System.InvalidOperationException: the inner reason", lines[1]);
        Assert.Equal($"bf-maint failed{Environment.NewLine}", _error.ToString());
    }



    [Fact]
    public async Task Start_when_a_number_is_logged_writes_it_the_same_in_every_culture()
    {
        using (var log = Start())
        {
            Log(log).LogWarning("free {Amount}", 1234.5);
        }

        var line = Assert.Single(await LinesAsync());
        Assert.EndsWith(": free 1234.5", line, StringComparison.Ordinal);
        Assert.Equal($"free 1234.5{Environment.NewLine}", _error.ToString());
    }



    [Fact]
    public async Task Start_when_the_file_of_the_day_exists_appends_to_it()
    {
        using (var first = Start())
        {
            Log(first).LogInformation("first run");
        }

        using (var second = Start())
        {
            Log(second).LogInformation("second run");
        }

        var lines = await LinesAsync();
        Assert.Equal(2, lines.Count);
        Assert.EndsWith(": first run", lines[0], StringComparison.Ordinal);
        Assert.EndsWith(": second run", lines[1], StringComparison.Ordinal);
    }



    [Fact]
    public async Task Start_when_two_runs_are_open_at_once_lets_both_write_to_the_file()
    {
        using (var first = Start())
        using (var second = Start())
        {
            Log(first).LogInformation("from the first");
            Log(second).LogInformation("from the second");
        }

        Assert.Equal(2, (await LinesAsync()).Count);
        Assert.Equal(string.Empty, _error.ToString());
    }



    [Fact]
    public void Start_when_the_host_has_more_files_than_are_kept_removes_the_oldest()
    {
        Directory.CreateDirectory(_folder);
        var day = new DateOnly(2026, 1, 1);
        for (var index = 0; index <= HostLog.KeptFiles; index++)
        {
            Touch($"{Host}-{day.AddDays(index):yyyyMMdd}.log");
        }

        Touch("bf-20250101.log");
        Touch("notes.txt");

        using var log = Start();

        var left = Directory
            .EnumerateFiles(_folder)
            .Select(path => Path.GetFileName(path))
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Equal(HostLog.KeptFiles + 2, left.Count);
        Assert.DoesNotContain("bf-maint-20260101.log", left);
        Assert.DoesNotContain("bf-maint-20260102.log", left);
        Assert.Contains("bf-maint-20260103.log", left);
        Assert.Contains(Today, left);
        Assert.Contains("bf-20250101.log", left);
        Assert.Contains("notes.txt", left);
    }



    [Fact]
    public void KeptFiles_when_read_is_ninety()
    {
        Assert.Equal(90, HostLog.KeptFiles);
    }



    [Fact]
    public void Start_when_the_folder_is_a_file_says_so_once_and_logs_to_stderr_only()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_folder)!);
        Touch(_folder);

        using var log = Start();
        Log(log).LogInformation("not kept");
        Log(log).LogWarning("still shown");

        Assert.Null(log.FilePath);
        var lines = ErrorLines();
        Assert.Equal(2, lines.Count);
        Assert.StartsWith($"Cannot write the log file in '{_folder}': ", lines[0], StringComparison.Ordinal);
        Assert.EndsWith(" Logging to stderr only.", lines[0], StringComparison.Ordinal);
        Assert.Equal("still shown", lines[1]);
    }



    [Fact]
    public void Start_when_the_file_of_the_day_cannot_be_opened_says_so_once_and_logs_to_stderr_only()
    {
        Directory.CreateDirectory(Path.Combine(_folder, Today));

        using var log = Start();

        Assert.Null(log.FilePath);
        var line = Assert.Single(ErrorLines());
        Assert.StartsWith($"Cannot write the log file in '{_folder}': ", line, StringComparison.Ordinal);
        Assert.EndsWith(" Logging to stderr only.", line, StringComparison.Ordinal);
    }



    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Start_when_there_is_no_folder_says_so_once_and_logs_to_stderr_only(string folder)
    {
        using var log = HostLog.Start(Options() with { Folder = folder });
        Log(log).LogError("still shown");

        Assert.Null(log.FilePath);
        Assert.Equal
        (
            ["There is no log folder for this user. Logging to stderr only.", "still shown"],
            ErrorLines()
        );
        Assert.False(Directory.Exists(_folder));
    }



    [Fact]
    public void Start_when_given_null_throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => HostLog.Start(null!));

        Assert.Equal("options", exception.ParamName);
    }



    [Fact]
    public async Task Dispose_when_called_closes_the_file_and_leaves_stderr_open()
    {
        var log = Start();
        Log(log).LogInformation("one line");

        log.Dispose();

        await using (new FileStream(log.FilePath!, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
        }

        await _error.WriteAsync("still open");
        Assert.Equal("still open", _error.ToString());
    }



    private HostLog Start()
    {
        return HostLog.Start(Options());
    }



    private HostLogOptions Options()
    {
        return new HostLogOptions(Host, _folder, _error, TestClock.AtUtc(2026, 11, 26, 9, 30));
    }



    private static ILogger Log(HostLog log)
    {
        return log
            .Factory
            .CreateLogger(Category);
    }



    private async Task<IReadOnlyList<string>> LinesAsync()
    {
        return await File.ReadAllLinesAsync(Path.Combine(_folder, Today));
    }



    private IReadOnlyList<string> ErrorLines()
    {
        return _error
            .ToString()
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
    }



    private void Touch(string name)
    {
        using (File.Create(Path.Combine(_folder, name)))
        {
        }
    }
}
