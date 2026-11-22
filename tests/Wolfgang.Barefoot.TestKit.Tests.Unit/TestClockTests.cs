namespace Wolfgang.Barefoot.TestKit.Tests.Unit;

public sealed class TestClockTests
{
    [Fact]
    public void Create_when_called_starts_at_the_default_instant()
    {
        var clock = TestClock.Create();

        Assert.Equal(TestClock.DefaultStart, clock.GetUtcNow());
    }



    [Fact]
    public void Create_when_called_starts_on_a_midweek_midday()
    {
        var now = TestClock
            .Create()
            .GetUtcNow();

        Assert.Equal(DayOfWeek.Wednesday, now.DayOfWeek);
        Assert.Equal(12, now.Hour);
        Assert.Equal(TimeSpan.Zero, now.Offset);
    }



    [Fact]
    public void Create_when_called_uses_utc_as_the_local_time_zone()
    {
        var clock = TestClock.Create();

        Assert.Equal(TimeSpan.Zero, clock.LocalTimeZone.BaseUtcOffset);
        Assert.Equal(clock.GetUtcNow(), clock.GetLocalNow());
    }



    [Fact]
    public void Create_when_called_twice_returns_independent_clocks()
    {
        var first = TestClock.Create();
        var second = TestClock.Create();

        first.Advance(TimeSpan.FromDays(1));

        Assert.Equal(TestClock.DefaultStart.AddDays(1), first.GetUtcNow());
        Assert.Equal(TestClock.DefaultStart, second.GetUtcNow());
    }



    [Fact]
    public void At_when_given_a_utc_instant_starts_there()
    {
        var start = new DateTimeOffset(2027, 3, 1, 6, 30, 15, TimeSpan.Zero);

        var clock = TestClock.At(start);

        Assert.Equal(start, clock.GetUtcNow());
    }



    [Fact]
    public void At_when_given_an_instant_with_an_offset_reports_the_same_instant_in_utc()
    {
        var start = new DateTimeOffset(2027, 3, 1, 1, 0, 0, TimeSpan.FromHours(-5));

        var now = TestClock
            .At(start)
            .GetUtcNow();

        Assert.Equal(start, now);
        Assert.Equal(TimeSpan.Zero, now.Offset);
        Assert.Equal(6, now.Hour);
    }



    [Fact]
    public void AtUtc_when_given_only_a_date_starts_at_midnight_utc()
    {
        var clock = TestClock.AtUtc(2026, 12, 31);

        Assert.Equal
        (
            new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero),
            clock.GetUtcNow()
        );
    }



    [Fact]
    public void AtUtc_when_given_a_time_of_day_starts_at_that_time()
    {
        var clock = TestClock.AtUtc(2026, 12, 31, 23, 59);

        Assert.Equal
        (
            new DateTimeOffset(2026, 12, 31, 23, 59, 0, TimeSpan.Zero),
            clock.GetUtcNow()
        );
    }



    [Fact]
    public void AtUtc_when_given_a_date_that_does_not_exist_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TestClock.AtUtc(2026, 2, 30));
    }
}
