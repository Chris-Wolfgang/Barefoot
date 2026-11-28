using Wolfgang.Barefoot.Core.Settings;

namespace Wolfgang.Barefoot.Core.Tests.Unit.Settings;

public sealed class SettingGenerationTests
{
    private static readonly DateTimeOffset Noon = new(2026, 1, 14, 12, 0, 0, TimeSpan.Zero);



    [Fact]
    public void Constructor_when_given_a_count_and_an_instant_holds_both()
    {
        var generation = new SettingGeneration(2, Noon);

        Assert.Equal(2, generation.Count);
        Assert.Equal(Noon, generation.LastUpdatedUtc);
    }



    [Fact]
    public void Equals_when_the_count_and_the_instant_agree_is_true()
    {
        Assert.Equal(new SettingGeneration(2, Noon), new SettingGeneration(2, Noon));
        Assert.Equal(default, new SettingGeneration(0, null));
    }



    [Fact]
    public void Equals_when_the_count_or_the_instant_differs_is_false()
    {
        Assert.NotEqual(new SettingGeneration(2, Noon), new SettingGeneration(3, Noon));
        Assert.NotEqual(new SettingGeneration(2, Noon), new SettingGeneration(2, Noon.AddTicks(1)));
        Assert.NotEqual(new SettingGeneration(0, null), new SettingGeneration(0, Noon));
    }
}
