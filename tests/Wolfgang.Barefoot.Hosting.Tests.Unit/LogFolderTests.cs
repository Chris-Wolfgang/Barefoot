namespace Wolfgang.Barefoot.Hosting.Tests.Unit;

/// <summary>
/// Where the log files go (ADR 0016, decision 17).
/// </summary>
public sealed class LogFolderTests
{
    [Fact]
    public void Under_when_given_a_folder_returns_Barefoot_logs_under_it()
    {
        var root = Path.Combine(Path.GetTempPath(), "local");

        Assert.Equal
        (
            Path.Combine(root, "Barefoot", "logs"),
            LogFolder.Under(root)
        );
    }



    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Under_when_given_no_folder_returns_an_empty_text(string localApplicationData)
    {
        Assert.Equal(string.Empty, LogFolder.Under(localApplicationData));
    }



    [Fact]
    public void Under_when_given_null_throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => LogFolder.Under(null!));

        Assert.Equal("localApplicationData", exception.ParamName);
    }



    [Fact]
    public void Default_when_read_is_the_folder_under_the_local_application_data_of_this_user()
    {
        var local = Environment.GetFolderPath
        (
            Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.DoNotVerify
        );

        Assert.Equal(LogFolder.Under(local), LogFolder.Default());
    }
}
