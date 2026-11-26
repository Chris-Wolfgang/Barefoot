namespace Wolfgang.Barefoot.Hosting.Tests.Unit;

/// <summary>
/// The names of the log files and which of them expire (ADR 0016, decision 18).
/// </summary>
public sealed class LogFilesTests
{
    [Fact]
    public void NameFor_when_given_a_moment_names_the_file_for_the_host_and_the_day()
    {
        var name = LogFiles.NameFor("bf-maint", new DateTimeOffset(2026, 11, 26, 9, 30, 0, TimeSpan.Zero));

        Assert.Equal("bf-maint-20261126.log", name);
    }



    [Fact]
    public void NameFor_when_the_moment_is_another_day_in_UTC_names_the_file_for_the_UTC_day()
    {
        var lateEvening = new DateTimeOffset(2026, 12, 31, 22, 0, 0, TimeSpan.FromHours(-5));

        Assert.Equal("bf-20270101.log", LogFiles.NameFor("bf", lateEvening));
    }



    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void NameFor_when_the_host_has_no_name_throws(string hostName)
    {
        var exception = Assert.Throws<ArgumentException>
        (
            () => LogFiles.NameFor(hostName, DateTimeOffset.UnixEpoch)
        );

        Assert.Equal("hostName", exception.ParamName);
    }



    [Fact]
    public void Expired_when_the_host_has_more_files_than_are_kept_returns_the_oldest_first()
    {
        string[] names =
        [
            "bf-maint-20261103.log",
            "bf-maint-20261101.log",
            "bf-maint-20261104.log",
            "bf-maint-20261102.log",
        ];

        Assert.Equal
        (
            ["bf-maint-20261101.log", "bf-maint-20261102.log"],
            LogFiles.Expired(names, "bf-maint", 2)
        );
    }



    [Fact]
    public void Expired_when_the_host_has_as_many_files_as_are_kept_returns_nothing()
    {
        string[] names = ["bf-maint-20261101.log", "bf-maint-20261102.log"];

        Assert.Empty(LogFiles.Expired(names, "bf-maint", 2));
    }



    [Fact]
    public void Expired_when_nothing_is_kept_returns_every_file_of_the_host()
    {
        string[] names = ["bf-maint-20261102.log", "bf-maint-20261101.log"];

        Assert.Equal
        (
            ["bf-maint-20261101.log", "bf-maint-20261102.log"],
            LogFiles.Expired(names, "bf-maint", 0)
        );
    }



    [Theory]
    [InlineData("bf-20261101.log")]
    [InlineData("bf-scraper-20261101.log")]
    [InlineData("xbf-maint-20261101.log")]
    [InlineData("bf-maint-2026110a.log")]
    [InlineData("bf-maint-a0261101.log")]
    [InlineData("bf-maint-2026110/.log")]
    [InlineData("bf-maint-2026110:.log")]
    [InlineData("bf-maint-202611011.log")]
    [InlineData("bf-maint-2026110.log")]
    [InlineData("bf-maint-20261101.txt")]
    [InlineData("bf-maint-20261101.logs")]
    [InlineData("bf-maint_20261101.log")]
    [InlineData("notes.txt")]
    public void Expired_when_a_file_is_not_a_daily_log_of_the_host_never_returns_it(string other)
    {
        string[] names = [other, "bf-maint-20261102.log"];

        Assert.Equal
        (
            ["bf-maint-20261102.log"],
            LogFiles.Expired(names, "bf-maint", 0)
        );
    }



    [Fact]
    public void Expired_when_given_no_names_throws()
    {
        var exception = Assert.Throws<ArgumentNullException>
        (
            () => LogFiles.Expired(null!, "bf-maint", 1)
        );

        Assert.Equal("fileNames", exception.ParamName);
    }



    [Fact]
    public void Expired_when_the_host_has_no_name_throws()
    {
        var exception = Assert.Throws<ArgumentException>
        (
            () => LogFiles.Expired([], " ", 1)
        );

        Assert.Equal("hostName", exception.ParamName);
    }



    [Fact]
    public void Expired_when_the_number_kept_is_negative_throws()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>
        (
            () => LogFiles.Expired([], "bf-maint", -1)
        );

        Assert.Equal("kept", exception.ParamName);
    }
}
