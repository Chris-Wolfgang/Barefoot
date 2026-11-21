using Wolfgang.Barefoot.Core.Searches;

namespace Wolfgang.Barefoot.Core.Tests.Unit.Searches;

public sealed class CheckInWindowTests
{
    [Theory]
    [InlineData(3, 10, 3, 20, false)]
    [InlineData(3, 10, 3, 10, false)]
    [InlineData(12, 20, 1, 5, true)]
    [InlineData(3, 11, 3, 10, true)]
    public void WrapsYear_when_the_last_day_is_earlier_in_the_year_than_the_first_is_true
    (
        int firstMonth,
        int firstDay,
        int lastMonth,
        int lastDay,
        bool expected
    )
    {
        var window = new RecurringWindow(new MonthDay(firstMonth, firstDay), new MonthDay(lastMonth, lastDay));

        Assert.Equal(expected, window.WrapsYear);
    }



    [Fact]
    public void RecurringWindow_when_two_have_the_same_days_are_equal()
    {
        var one = new RecurringWindow(new MonthDay(3, 10), new MonthDay(3, 20));
        var other = new RecurringWindow(new MonthDay(3, 10), new MonthDay(3, 20));

        Assert.Equal(one, other);
        Assert.Equal(new MonthDay(3, 10), one.First);
        Assert.Equal(new MonthDay(3, 20), one.Last);
    }



    [Fact]
    public void OneTimeWindow_when_given_dates_in_order_keeps_them()
    {
        var window = new OneTimeWindow(new DateOnly(2027, 6, 1), new DateOnly(2027, 6, 14));

        Assert.Equal(new DateOnly(2027, 6, 1), window.First);
        Assert.Equal(new DateOnly(2027, 6, 14), window.Last);
    }



    [Fact]
    public void OneTimeWindow_when_first_and_last_are_the_same_date_is_allowed()
    {
        var window = new OneTimeWindow(new DateOnly(2027, 6, 1), new DateOnly(2027, 6, 1));

        Assert.Equal(window.First, window.Last);
    }



    [Fact]
    public void OneTimeWindow_when_the_last_date_is_before_the_first_throws_with_both_dates()
    {
        var exception = Assert.Throws<ArgumentException>
        (
            () => new OneTimeWindow(new DateOnly(2027, 6, 14), new DateOnly(2027, 6, 13))
        );

        Assert.Equal("last", exception.ParamName);
        Assert.Contains("2027-06-13", exception.Message, StringComparison.Ordinal);
        Assert.Contains("2027-06-14", exception.Message, StringComparison.Ordinal);
    }



    [Fact]
    public void ResolvedWindow_when_given_dates_in_order_counts_and_lists_every_date()
    {
        var window = new ResolvedWindow(new DateOnly(2027, 2, 27), new DateOnly(2027, 3, 2));

        Assert.Equal(4, window.DayCount);
        Assert.Equal
        (
            [
                new DateOnly(2027, 2, 27),
                new DateOnly(2027, 2, 28),
                new DateOnly(2027, 3, 1),
                new DateOnly(2027, 3, 2),
            ],
            window.Dates()
        );
    }



    [Fact]
    public void ResolvedWindow_when_it_is_one_day_lists_that_day()
    {
        var window = new ResolvedWindow(new DateOnly(2027, 7, 4), new DateOnly(2027, 7, 4));

        Assert.Equal(1, window.DayCount);
        Assert.Equal([new DateOnly(2027, 7, 4)], window.Dates());
    }



    [Fact]
    public void ResolvedWindow_when_it_ends_on_the_last_representable_date_lists_it_without_overflowing()
    {
        var window = new ResolvedWindow(DateOnly.MaxValue.AddDays(-1), DateOnly.MaxValue);

        Assert.Equal([DateOnly.MaxValue.AddDays(-1), DateOnly.MaxValue], window.Dates());
    }



    [Fact]
    public void ResolvedWindow_when_the_last_date_is_before_the_first_throws_with_both_dates()
    {
        var exception = Assert.Throws<ArgumentException>
        (
            () => new ResolvedWindow(new DateOnly(2027, 6, 14), new DateOnly(2027, 6, 13))
        );

        Assert.Equal("last", exception.ParamName);
        Assert.Contains("2027-06-13", exception.Message, StringComparison.Ordinal);
        Assert.Contains("2027-06-14", exception.Message, StringComparison.Ordinal);
    }
}
