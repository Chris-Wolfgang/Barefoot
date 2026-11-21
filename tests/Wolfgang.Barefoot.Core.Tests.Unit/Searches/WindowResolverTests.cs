using Wolfgang.Barefoot.Core.Searches;

namespace Wolfgang.Barefoot.Core.Tests.Unit.Searches;

public sealed class WindowResolverTests
{
    private const int Lead = 4;



    [Fact]
    public void Resolve_when_a_recurring_window_is_still_ahead_this_year_returns_it_whole()
    {
        var window = new RecurringWindow(new MonthDay(3, 10), new MonthDay(3, 20));

        var resolved = WindowResolver.Resolve(window, new DateOnly(2027, 1, 14), Lead);

        Assert.Equal(new ResolvedWindow(new DateOnly(2027, 3, 10), new DateOnly(2027, 3, 20)), resolved);
    }



    [Fact]
    public void Resolve_when_a_recurring_window_is_under_way_starts_at_the_earliest_searchable_date()
    {
        var window = new RecurringWindow(new MonthDay(3, 10), new MonthDay(3, 20));

        var resolved = WindowResolver.Resolve(window, new DateOnly(2027, 3, 12), Lead);

        Assert.Equal(new ResolvedWindow(new DateOnly(2027, 3, 16), new DateOnly(2027, 3, 20)), resolved);
    }



    [Theory]
    [InlineData(2027, 3, 6, 2027, 3, 10)]
    [InlineData(2027, 3, 7, 2027, 3, 11)]
    public void Resolve_when_the_earliest_searchable_date_is_at_the_start_of_a_recurring_window_starts_there
    (
        int year,
        int month,
        int day,
        int expectedYear,
        int expectedMonth,
        int expectedDay
    )
    {
        var window = new RecurringWindow(new MonthDay(3, 10), new MonthDay(3, 20));

        var resolved = WindowResolver.Resolve(window, new DateOnly(year, month, day), Lead);

        Assert.Equal(new DateOnly(expectedYear, expectedMonth, expectedDay), resolved!.Value.First);
        Assert.Equal(new DateOnly(2027, 3, 20), resolved.Value.Last);
    }



    [Fact]
    public void Resolve_when_only_the_last_day_of_a_recurring_window_is_still_searchable_returns_that_day()
    {
        var window = new RecurringWindow(new MonthDay(3, 10), new MonthDay(3, 20));

        var resolved = WindowResolver.Resolve(window, new DateOnly(2027, 3, 16), Lead);

        Assert.Equal(new ResolvedWindow(new DateOnly(2027, 3, 20), new DateOnly(2027, 3, 20)), resolved);
    }



    [Fact]
    public void Resolve_when_a_recurring_window_has_passed_rolls_to_next_year()
    {
        var window = new RecurringWindow(new MonthDay(3, 10), new MonthDay(3, 20));

        var resolved = WindowResolver.Resolve(window, new DateOnly(2027, 3, 17), Lead);

        Assert.Equal(new ResolvedWindow(new DateOnly(2028, 3, 10), new DateOnly(2028, 3, 20)), resolved);
    }



    [Fact]
    public void Resolve_when_a_recurring_window_wraps_the_year_and_is_ahead_spans_the_new_year()
    {
        var window = new RecurringWindow(new MonthDay(12, 20), new MonthDay(1, 5));

        var resolved = WindowResolver.Resolve(window, new DateOnly(2027, 11, 1), Lead);

        Assert.Equal(new ResolvedWindow(new DateOnly(2027, 12, 20), new DateOnly(2028, 1, 5)), resolved);
    }



    [Fact]
    public void Resolve_when_today_is_in_the_december_part_of_a_wrapping_window_starts_at_the_earliest_searchable_date()
    {
        var window = new RecurringWindow(new MonthDay(12, 20), new MonthDay(1, 5));

        var resolved = WindowResolver.Resolve(window, new DateOnly(2027, 12, 24), Lead);

        Assert.Equal(new ResolvedWindow(new DateOnly(2027, 12, 28), new DateOnly(2028, 1, 5)), resolved);
    }



    [Fact]
    public void Resolve_when_today_is_in_the_january_part_of_a_wrapping_window_keeps_the_occurrence_that_began_last_year()
    {
        var window = new RecurringWindow(new MonthDay(12, 20), new MonthDay(1, 5));

        var resolved = WindowResolver.Resolve(window, new DateOnly(2028, 1, 1), 2);

        Assert.Equal(new ResolvedWindow(new DateOnly(2028, 1, 3), new DateOnly(2028, 1, 5)), resolved);
    }



    [Fact]
    public void Resolve_when_the_lead_carries_today_past_the_end_of_a_wrapping_window_rolls_to_the_next_occurrence()
    {
        var window = new RecurringWindow(new MonthDay(12, 20), new MonthDay(1, 5));

        var resolved = WindowResolver.Resolve(window, new DateOnly(2028, 1, 2), Lead);

        Assert.Equal(new ResolvedWindow(new DateOnly(2028, 12, 20), new DateOnly(2029, 1, 5)), resolved);
    }



    [Fact]
    public void Resolve_when_the_lead_crosses_the_new_year_resolves_in_the_year_of_the_earliest_searchable_date()
    {
        var window = new RecurringWindow(new MonthDay(1, 2), new MonthDay(1, 10));

        var resolved = WindowResolver.Resolve(window, new DateOnly(2027, 12, 30), Lead);

        Assert.Equal(new ResolvedWindow(new DateOnly(2028, 1, 3), new DateOnly(2028, 1, 10)), resolved);
    }



    [Fact]
    public void Resolve_when_a_recurring_window_is_one_day_returns_that_day_each_year()
    {
        var window = new RecurringWindow(new MonthDay(7, 4), new MonthDay(7, 4));

        Assert.Equal
        (
            new ResolvedWindow(new DateOnly(2027, 7, 4), new DateOnly(2027, 7, 4)),
            WindowResolver.Resolve(window, new DateOnly(2027, 6, 30), Lead)
        );
        Assert.Equal
        (
            new ResolvedWindow(new DateOnly(2028, 7, 4), new DateOnly(2028, 7, 4)),
            WindowResolver.Resolve(window, new DateOnly(2027, 7, 1), Lead)
        );
    }



    [Theory]
    [InlineData(2027, 28)]
    [InlineData(2028, 29)]
    public void Resolve_when_a_recurring_window_ends_on_29_February_ends_on_the_last_day_February_has(int year, int lastDay)
    {
        var window = new RecurringWindow(new MonthDay(2, 20), new MonthDay(2, 29));

        var resolved = WindowResolver.Resolve(window, new DateOnly(year, 1, 14), Lead);

        Assert.Equal(new ResolvedWindow(new DateOnly(year, 2, 20), new DateOnly(year, 2, lastDay)), resolved);
    }



    [Fact]
    public void Resolve_when_the_lead_is_zero_treats_today_as_searchable()
    {
        var window = new RecurringWindow(new MonthDay(3, 10), new MonthDay(3, 20));

        var resolved = WindowResolver.Resolve(window, new DateOnly(2027, 3, 20), 0);

        Assert.Equal(new ResolvedWindow(new DateOnly(2027, 3, 20), new DateOnly(2027, 3, 20)), resolved);
    }



    [Fact]
    public void Resolve_when_a_one_time_window_is_ahead_returns_it_whole()
    {
        var window = new OneTimeWindow(new DateOnly(2027, 6, 1), new DateOnly(2027, 6, 14));

        var resolved = WindowResolver.Resolve(window, new DateOnly(2027, 1, 14), Lead);

        Assert.Equal(new ResolvedWindow(new DateOnly(2027, 6, 1), new DateOnly(2027, 6, 14)), resolved);
    }



    [Fact]
    public void Resolve_when_a_one_time_window_is_under_way_starts_at_the_earliest_searchable_date()
    {
        var window = new OneTimeWindow(new DateOnly(2027, 6, 1), new DateOnly(2027, 6, 14));

        var resolved = WindowResolver.Resolve(window, new DateOnly(2027, 6, 3), Lead);

        Assert.Equal(new ResolvedWindow(new DateOnly(2027, 6, 7), new DateOnly(2027, 6, 14)), resolved);
    }



    [Fact]
    public void Resolve_when_only_the_last_date_of_a_one_time_window_is_searchable_returns_that_date()
    {
        var window = new OneTimeWindow(new DateOnly(2027, 6, 1), new DateOnly(2027, 6, 14));

        var resolved = WindowResolver.Resolve(window, new DateOnly(2027, 6, 10), Lead);

        Assert.Equal(new ResolvedWindow(new DateOnly(2027, 6, 14), new DateOnly(2027, 6, 14)), resolved);
    }



    [Theory]
    [InlineData(2027, 6, 11)]
    [InlineData(2027, 6, 15)]
    [InlineData(2028, 1, 1)]
    public void Resolve_when_a_one_time_window_can_no_longer_be_searched_returns_null(int year, int month, int day)
    {
        var window = new OneTimeWindow(new DateOnly(2027, 6, 1), new DateOnly(2027, 6, 14));

        Assert.Null(WindowResolver.Resolve(window, new DateOnly(year, month, day), Lead));
    }



    [Fact]
    public void Resolve_when_the_window_is_null_throws()
    {
        Assert.Throws<ArgumentNullException>(() => WindowResolver.Resolve(null!, new DateOnly(2027, 1, 14), Lead));
    }



    [Fact]
    public void Resolve_when_the_lead_is_negative_throws()
    {
        var window = new OneTimeWindow(new DateOnly(2027, 6, 1), new DateOnly(2027, 6, 14));

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => WindowResolver.Resolve(window, new DateOnly(2027, 1, 14), -1));

        Assert.Equal("minimumLeadDays", exception.ParamName);
    }



    [Fact]
    public void Resolve_when_the_window_is_of_an_unknown_kind_throws_naming_it()
    {
        var exception = Assert.Throws<NotSupportedException>
        (
            () => WindowResolver.Resolve(new UnknownWindow(), new DateOnly(2027, 1, 14), Lead)
        );

        Assert.Contains(nameof(UnknownWindow), exception.Message, StringComparison.Ordinal);
    }



    private sealed record UnknownWindow : CheckInWindow;
}
