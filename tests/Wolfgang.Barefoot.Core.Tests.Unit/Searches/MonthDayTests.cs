using Wolfgang.Barefoot.Core.Searches;

namespace Wolfgang.Barefoot.Core.Tests.Unit.Searches;

public sealed class MonthDayTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 31)]
    [InlineData(2, 29)]
    [InlineData(4, 30)]
    [InlineData(12, 31)]
    public void Constructor_when_the_day_exists_keeps_the_month_and_the_day(int month, int day)
    {
        var value = new MonthDay(month, day);

        Assert.Equal(month, value.Month);
        Assert.Equal(day, value.Day);
    }



    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public void Constructor_when_the_month_does_not_exist_throws(int month)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new MonthDay(month, 1));

        Assert.Equal("month", exception.ParamName);
        Assert.Contains("between 1 and 12", exception.Message, StringComparison.Ordinal);
    }



    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, 32)]
    [InlineData(2, 30)]
    [InlineData(4, 31)]
    [InlineData(6, -3)]
    public void Constructor_when_the_month_has_no_such_day_throws(int month, int day)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new MonthDay(month, day));

        Assert.Equal("day", exception.ParamName);
        Assert.Contains($"Month {month} has no day {day}", exception.Message, StringComparison.Ordinal);
    }



    [Theory]
    [InlineData(3, 15, 3, 14, true)]
    [InlineData(4, 1, 3, 31, true)]
    [InlineData(3, 15, 3, 15, false)]
    [InlineData(3, 14, 3, 15, false)]
    [InlineData(3, 31, 4, 1, false)]
    [InlineData(1, 5, 12, 20, false)]
    [InlineData(12, 20, 1, 5, true)]
    public void IsAfter_when_compared_within_a_year_orders_by_month_then_day
    (
        int month,
        int day,
        int otherMonth,
        int otherDay,
        bool expected
    )
    {
        Assert.Equal(expected, new MonthDay(month, day).IsAfter(new MonthDay(otherMonth, otherDay)));
    }



    [Fact]
    public void InYear_when_the_day_exists_in_that_year_returns_it()
    {
        Assert.Equal(new DateOnly(2027, 3, 15), new MonthDay(3, 15).InYear(2027));
    }



    [Theory]
    [InlineData(2028, 29)]
    [InlineData(2027, 28)]
    [InlineData(2100, 28)]
    public void InYear_when_the_day_is_29_February_uses_the_last_day_February_has(int year, int expectedDay)
    {
        Assert.Equal(new DateOnly(year, 2, expectedDay), new MonthDay(2, 29).InYear(year));
    }



    [Theory]
    [InlineData(3, 5, "03-05")]
    [InlineData(12, 31, "12-31")]
    public void ToString_when_called_writes_month_and_day_with_two_digits(int month, int day, string expected)
    {
        Assert.Equal(expected, new MonthDay(month, day).ToString());
    }



    [Fact]
    public void Equals_when_two_values_name_the_same_day_is_true()
    {
        Assert.Equal(new MonthDay(3, 15), new MonthDay(3, 15));
        Assert.NotEqual(new MonthDay(3, 15), new MonthDay(3, 16));
    }
}
