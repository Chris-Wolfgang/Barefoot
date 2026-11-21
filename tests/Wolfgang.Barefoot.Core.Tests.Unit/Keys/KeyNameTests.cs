using Wolfgang.Barefoot.Core.Keys;

namespace Wolfgang.Barefoot.Core.Tests.Unit.Keys;

public sealed class KeyNameTests
{
    [Theory]
    [InlineData("run.lease_seconds")]
    [InlineData("search.name_taken")]
    [InlineData("a.b")]
    [InlineData("results.flight_options_page_size")]
    [InlineData("database.http2.max_streams")]
    [InlineData("auth.oauth2")]
    public void IsValid_when_the_name_is_lower_case_and_dotted_returns_true(string name)
    {
        Assert.True(KeyName.IsValid(name));
    }



    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("run")]
    [InlineData("Run.lease")]
    [InlineData("run.Lease")]
    [InlineData("run.leaseSeconds")]
    [InlineData("run..lease")]
    [InlineData(".run.lease")]
    [InlineData("run.lease.")]
    [InlineData("run.1lease")]
    [InlineData("1run.lease")]
    [InlineData("run._lease")]
    [InlineData("run.lease_")]
    [InlineData("run.lease__seconds")]
    [InlineData("run-lease.seconds")]
    [InlineData("run.lease seconds")]
    [InlineData("run.lease\n")]
    [InlineData("rün.lease")]
    public void IsValid_when_the_name_breaks_the_pattern_returns_false(string? name)
    {
        Assert.False(KeyName.IsValid(name));
    }



    [Fact]
    public void Require_when_the_name_is_valid_returns_it()
    {
        Assert.Equal("run.lease_seconds", KeyName.Require("run.lease_seconds", "name"));
    }



    [Theory]
    [InlineData(null)]
    [InlineData("Run.Lease")]
    public void Require_when_the_name_is_invalid_throws_naming_the_parameter_and_the_value(string? name)
    {
        var exception = Assert.Throws<ArgumentException>(() => KeyName.Require(name, "settingName"));

        Assert.Equal("settingName", exception.ParamName);
        Assert.Contains($"'{name}'", exception.Message, StringComparison.Ordinal);
    }
}
