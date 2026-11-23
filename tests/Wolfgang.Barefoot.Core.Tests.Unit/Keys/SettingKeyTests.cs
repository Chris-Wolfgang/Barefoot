using Wolfgang.Barefoot.Core.Keys;

namespace Wolfgang.Barefoot.Core.Tests.Unit.Keys;

public sealed class SettingKeyTests
{
    private const string Description = "How long a claimed lookup stays leased to a worker.";



    [Fact]
    public void Constructor_when_given_a_definition_keeps_every_part()
    {
        var key = new SettingKey<int>("run.lease_seconds", 300, Description, AtLeastSixty, requiresRestart: true);

        Assert.Equal("run.lease_seconds", key.Name);
        Assert.Equal(300, key.Default);
        Assert.Equal(Description, key.Description);
        Assert.True(key.RequiresRestart);
        Assert.Equal(typeof(int), key.ValueType);
        Assert.Equal("run.lease_seconds", key.ToString());
    }



    [Fact]
    public void Constructor_when_only_the_required_parts_are_given_needs_no_restart_and_accepts_any_value()
    {
        var key = new SettingKey<string>("digest.sender_display_name", "Barefoot", "The name digests are sent from.");

        Assert.False(key.RequiresRestart);
        Assert.Null(key.Validate(string.Empty));
    }



    [Fact]
    public void Constructor_when_read_through_the_base_type_exposes_the_same_values()
    {
        SettingKey key = new SettingKey<TimeSpan>("run.grace", TimeSpan.FromSeconds(30), "A grace period.");

        Assert.Equal("run.grace", key.Name);
        Assert.Equal(typeof(TimeSpan), key.ValueType);
    }



    [Theory]
    [InlineData("RunLeaseSeconds")]
    [InlineData("lease")]
    public void Constructor_when_the_name_is_not_a_key_name_throws(string name)
    {
        var exception = Assert.Throws<ArgumentException>(() => new SettingKey<int>(name, 300, Description));

        Assert.Equal("name", exception.ParamName);
    }



    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_when_the_description_is_empty_throws(string description)
    {
        var exception = Assert.Throws<ArgumentException>(() => new SettingKey<int>("run.lease_seconds", 300, description));

        Assert.Equal("description", exception.ParamName);
        Assert.Contains("needs a description", exception.Message, StringComparison.Ordinal);
    }



    [Fact]
    public void Constructor_when_the_default_is_null_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SettingKey<string>("digest.sender_display_name", null!, "The sender."));
    }



    [Fact]
    public void Constructor_when_the_default_fails_the_validator_throws_with_the_reason()
    {
        var exception = Assert.Throws<ArgumentException>
        (
            () => new SettingKey<int>("run.lease_seconds", 30, Description, AtLeastSixty)
        );

        Assert.Equal("defaultValue", exception.ParamName);
        Assert.Contains("run.lease_seconds", exception.Message, StringComparison.Ordinal);
        Assert.Contains("at least 60", exception.Message, StringComparison.Ordinal);
    }



    [Theory]
    [InlineData(60)]
    [InlineData(61)]
    [InlineData(int.MaxValue)]
    public void Validate_when_the_value_is_allowed_returns_null(int value)
    {
        var key = new SettingKey<int>("run.lease_seconds", 300, Description, AtLeastSixty);

        Assert.Null(key.Validate(value));
    }



    [Theory]
    [InlineData(59)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_when_the_value_is_not_allowed_returns_the_reason(int value)
    {
        var key = new SettingKey<int>("run.lease_seconds", 300, Description, AtLeastSixty);

        Assert.Equal("The lease must be at least 60 seconds.", key.Validate(value));
    }



    [Fact]
    public void Validate_when_the_value_is_null_throws()
    {
        var key = new SettingKey<string>("digest.sender_display_name", "Barefoot", "The sender.");

        Assert.Throws<ArgumentNullException>(() => key.Validate(null!));
    }



    private static string? AtLeastSixty(int seconds)
    {
        return seconds >= 60 ? null : "The lease must be at least 60 seconds.";
    }
}
