using Wolfgang.Barefoot.Contracts.Searches;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Core.Tests.Unit.Entities;

public sealed class SearchEntitiesTests
{
    private static readonly Guid Id = Guid.Parse("3c4d5e6f-7081-4293-a4b5-c6d7e8f9012a");

    private static readonly Guid Owner = Guid.Parse("7b1c2d3e-4f50-4a61-9b72-8c93a4b5c6d7");

    private static readonly Guid Other = Guid.Parse("6d5c4b3a-2918-4071-8f6e-5d4c3b2a1908");

    private static readonly DateTimeOffset Instant = new(2026, 11, 4, 6, 40, 0, TimeSpan.Zero);



    [Fact]
    public void SearchMatrix_when_it_is_a_recurring_search_with_flights_keeps_each_value()
    {
        var search = new SearchMatrix
        {
            Id = Id,
            OwnerId = Owner,
            Name = "February week",
            NormalizedName = "FEBRUARY WEEK",
            Brand = Brand.Beaches,
            Status = SearchStatus.Active,
            RunOrder = 75,
            NightCount = 7,
            AdultCount = 2,
            ChildCount = 1,
            InfantCount = 1,
            WindowKind = WindowKind.Recurring,
            RecurringFirstMonth = 2,
            RecurringFirstDay = 1,
            RecurringLastMonth = 2,
            RecurringLastDay = 21,
            IncludeFlights = true,
            Cabin = Cabin.Economy,
            MaxTotalAmount = 9500.50m,
            MaxDriveMinutes = 45,
            MaxFlightMinutes = 480,
            MaxStopCount = 1,
            CreatedUtc = Instant,
            Version = 6,
        };

        Assert.Equal(Id, search.Id);
        Assert.Equal(Owner, search.OwnerId);
        Assert.Equal("February week", search.Name);
        Assert.Equal("FEBRUARY WEEK", search.NormalizedName);
        Assert.Equal(Brand.Beaches, search.Brand);
        Assert.Equal(SearchStatus.Active, search.Status);
        Assert.Equal(75, search.RunOrder);
        Assert.Equal(7, search.NightCount);
        Assert.Equal(2, search.AdultCount);
        Assert.Equal(1, search.ChildCount);
        Assert.Equal(1, search.InfantCount);
        Assert.Equal(WindowKind.Recurring, search.WindowKind);
        Assert.Equal(2, search.RecurringFirstMonth);
        Assert.Equal(1, search.RecurringFirstDay);
        Assert.Equal(2, search.RecurringLastMonth);
        Assert.Equal(21, search.RecurringLastDay);
        Assert.True(search.IncludeFlights);
        Assert.Equal(Cabin.Economy, search.Cabin);
        Assert.Equal(9500.50m, search.MaxTotalAmount);
        Assert.Equal(45, search.MaxDriveMinutes);
        Assert.Equal(480, search.MaxFlightMinutes);
        Assert.Equal(1, search.MaxStopCount);
        Assert.Equal(Instant, search.CreatedUtc);
        Assert.Equal(6, search.Version);
    }



    [Fact]
    public void SearchMatrix_when_it_is_a_one_time_search_keeps_its_two_dates()
    {
        var search = OneTime();

        Assert.Equal(WindowKind.OneTime, search.WindowKind);
        Assert.Equal(new DateOnly(2027, 2, 13), search.FirstCheckIn);
        Assert.Equal(new DateOnly(2027, 2, 20), search.LastCheckIn);
    }



    [Fact]
    public void SearchMatrix_when_only_the_required_values_are_given_is_a_draft_at_the_normal_run_order_with_no_limits()
    {
        var search = new SearchMatrix
        {
            Id = Id,
            OwnerId = Owner,
            Name = "Anniversary",
            NormalizedName = "ANNIVERSARY",
            Brand = Brand.Sandals,
            NightCount = 7,
            AdultCount = 2,
            WindowKind = WindowKind.OneTime,
        };

        Assert.Equal(SearchStatus.Draft, search.Status);
        Assert.Equal(SearchDefinition.NormalRunOrder, search.RunOrder);
        Assert.Equal(0, search.ChildCount);
        Assert.Equal(0, search.InfantCount);
        Assert.Null(search.RecurringFirstMonth);
        Assert.Null(search.RecurringFirstDay);
        Assert.Null(search.RecurringLastMonth);
        Assert.Null(search.RecurringLastDay);
        Assert.Null(search.FirstCheckIn);
        Assert.Null(search.LastCheckIn);
        Assert.False(search.IncludeFlights);
        Assert.Null(search.Cabin);
        Assert.Null(search.MaxTotalAmount);
        Assert.Null(search.MaxDriveMinutes);
        Assert.Null(search.MaxFlightMinutes);
        Assert.Null(search.MaxStopCount);
        Assert.Equal(0, search.Version);
    }



    [Theory]
    [InlineData("February week", "FEBRUARY WEEK")]
    [InlineData("  February week\t", "FEBRUARY WEEK")]
    [InlineData("FEBRUARY WEEK", "FEBRUARY WEEK")]
    [InlineData("i", "I")]
    [InlineData("", "")]
    public void NormalizeName_when_given_a_name_trims_it_and_upper_cases_it(string name, string expected)
    {
        Assert.Equal(expected, SearchMatrix.NormalizeName(name));
    }



    [Fact]
    public void NormalizeName_when_the_name_is_null_throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => SearchMatrix.NormalizeName(null!));

        Assert.Equal("name", exception.ParamName);
    }



    [Fact]
    public void SearchResort_when_every_value_is_given_keeps_each_value()
    {
        var link = new SearchResort { Id = Id, SearchMatrixId = Owner, ResortId = Other };

        Assert.Equal(Id, link.Id);
        Assert.Equal(Owner, link.SearchMatrixId);
        Assert.Equal(Other, link.ResortId);
    }



    [Fact]
    public void SearchDepartureAirport_when_every_value_is_given_keeps_each_value()
    {
        var link = new SearchDepartureAirport { Id = Id, SearchMatrixId = Owner, AirportId = Other };

        Assert.Equal(Id, link.Id);
        Assert.Equal(Owner, link.SearchMatrixId);
        Assert.Equal(Other, link.AirportId);
    }



    [Fact]
    public void SearchRoomFilter_when_every_value_is_given_keeps_each_value()
    {
        var link = new SearchRoomFilter { Id = Id, SearchMatrixId = Owner, VocabularyValueId = Other };

        Assert.Equal(Id, link.Id);
        Assert.Equal(Owner, link.SearchMatrixId);
        Assert.Equal(Other, link.VocabularyValueId);
    }



    [Fact]
    public void SearchRecipient_when_every_value_is_given_keeps_each_value()
    {
        var link = new SearchRecipient { Id = Id, SearchMatrixId = Owner, RecipientId = Other };

        Assert.Equal(Id, link.Id);
        Assert.Equal(Owner, link.SearchMatrixId);
        Assert.Equal(Other, link.RecipientId);
    }



    private static SearchMatrix OneTime()
    {
        return new SearchMatrix
        {
            Id = Id,
            OwnerId = Owner,
            Name = "Anniversary",
            NormalizedName = "ANNIVERSARY",
            Brand = Brand.Sandals,
            NightCount = 7,
            AdultCount = 2,
            WindowKind = WindowKind.OneTime,
            FirstCheckIn = new DateOnly(2027, 2, 13),
            LastCheckIn = new DateOnly(2027, 2, 20),
        };
    }
}
