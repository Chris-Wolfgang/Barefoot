using Wolfgang.Barefoot.Core.Keys;

namespace Wolfgang.Barefoot.Core.Tests.Unit.Keys;

public sealed class PermissionTests
{
    [Fact]
    public void Constructor_when_given_a_definition_keeps_the_name_and_the_description()
    {
        var permission = new Permission("settings.write", "Change a setting.");

        Assert.Equal("settings.write", permission.Name);
        Assert.Equal("Change a setting.", permission.Description);
        Assert.Equal("settings.write", permission.ToString());
    }



    [Theory]
    [InlineData("SettingsWrite")]
    [InlineData("settings")]
    public void Constructor_when_the_name_is_not_a_key_name_throws(string name)
    {
        var exception = Assert.Throws<ArgumentException>(() => new Permission(name, "Change a setting."));

        Assert.Equal("name", exception.ParamName);
    }



    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_when_the_description_is_empty_throws(string description)
    {
        var exception = Assert.Throws<ArgumentException>(() => new Permission("settings.write", description));

        Assert.Equal("description", exception.ParamName);
        Assert.Contains("needs a description", exception.Message, StringComparison.Ordinal);
    }
}
