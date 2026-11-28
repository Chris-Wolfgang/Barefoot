using Wolfgang.Barefoot.Business.Settings;
using Wolfgang.Barefoot.Core.Keys;

namespace Wolfgang.Barefoot.Business.Tests.Unit.Settings;

/// <summary>
/// The registry of settings and its entries (ADR 0011 §2, §5). The keys here are made for the
/// tests: a real key is added with the code that reads it.
/// </summary>
public sealed class SettingRegistryTests
{
    private static readonly SettingKey<int> Lease = new
    (
        "sample.lease_seconds",
        300,
        "How long a sample lease lasts.",
        value => value >= 60 ? null : "A lease lasts at least 60 seconds."
    );

    private static readonly SettingKey<string> Sender = new("sample.sender", "Barefoot", "Who a sample is from.");

    private static readonly SettingKey<bool> Switch = new("sample.enabled", true, "Whether the sample is on.");



    [Fact]
    public void For_when_given_a_key_holds_its_type_name_and_its_default_as_text()
    {
        var entry = SettingEntry.For(Lease);

        Assert.Same(Lease, entry.Key);
        Assert.Equal("integer", entry.TypeName);
        Assert.Equal("300", entry.DefaultText);
    }



    [Fact]
    public void For_when_the_type_is_not_one_a_setting_may_have_throws()
    {
        var key = new SettingKey<decimal>("sample.price", 1m, "A sample price.");

        Assert.Throws<NotSupportedException>(() => SettingEntry.For(key));
    }



    [Fact]
    public void For_when_given_null_throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => SettingEntry.For<int>(null!));

        Assert.Equal("key", exception.ParamName);
    }



    [Fact]
    public void Problem_when_the_text_is_a_value_the_validator_accepts_is_null()
    {
        Assert.Null(SettingEntry.For(Lease).Problem("60"));
        Assert.Null(SettingEntry.For(Sender).Problem(string.Empty));
    }



    [Fact]
    public void Problem_when_the_text_is_not_a_value_of_the_type_says_so()
    {
        Assert.Equal("'soon' is not a integer.", SettingEntry.For(Lease).Problem("soon"));
        Assert.Equal("'yes' is not a boolean.", SettingEntry.For(Switch).Problem("yes"));
    }



    [Fact]
    public void Problem_when_the_validator_rejects_the_value_gives_its_sentence()
    {
        Assert.Equal("A lease lasts at least 60 seconds.", SettingEntry.For(Lease).Problem("59"));
    }



    [Fact]
    public void Problem_when_given_null_throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => SettingEntry.For(Lease).Problem(null!));

        Assert.Equal("text", exception.ParamName);
    }



    [Fact]
    public void All_when_read_lists_every_setting_by_name()
    {
        var registry = new SettingRegistry([SettingEntry.For(Sender), SettingEntry.For(Lease), SettingEntry.For(Switch)]);

        Assert.Equal
        (
            ["sample.enabled", "sample.lease_seconds", "sample.sender"],
            registry.All.Select(entry => entry.Key.Name)
        );
    }



    [Fact]
    public void All_when_nothing_is_registered_is_empty()
    {
        Assert.Empty(new SettingRegistry([]).All);
    }



    [Fact]
    public void Find_when_the_name_is_registered_gives_the_entry()
    {
        var lease = SettingEntry.For(Lease);
        var registry = new SettingRegistry([SettingEntry.For(Sender), lease]);

        Assert.Same(lease, registry.Find("sample.lease_seconds"));
    }



    [Theory]
    [InlineData("sample.missing")]
    [InlineData("Sample.Lease_Seconds")]
    [InlineData("")]
    public void Find_when_the_name_is_not_registered_exactly_gives_null(string name)
    {
        var registry = new SettingRegistry([SettingEntry.For(Lease)]);

        Assert.Null(registry.Find(name));
    }



    [Fact]
    public void Find_when_given_null_throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new SettingRegistry([]).Find(null!));

        Assert.Equal("name", exception.ParamName);
    }



    [Fact]
    public void Constructor_when_two_settings_have_the_same_name_throws_and_names_it()
    {
        var twin = new SettingKey<int>("sample.lease_seconds", 1, "A second setting of the same name.");

        var exception = Assert.Throws<ArgumentException>
        (
            () => new SettingRegistry([SettingEntry.For(Lease), SettingEntry.For(twin)])
        );

        Assert.Equal("entries", exception.ParamName);
        Assert.StartsWith("The setting 'sample.lease_seconds' is registered twice.", exception.Message, StringComparison.Ordinal);
    }



    [Fact]
    public void Constructor_when_given_null_throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new SettingRegistry(null!));

        Assert.Equal("entries", exception.ParamName);
    }
}
