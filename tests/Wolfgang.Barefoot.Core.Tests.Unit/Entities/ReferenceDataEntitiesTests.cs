using Wolfgang.Barefoot.Contracts.Catalog;
using Wolfgang.Barefoot.Contracts.Searches;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Core.Tests.Unit.Entities;

public sealed class ReferenceDataEntitiesTests
{
    private static readonly Guid Id = Guid.Parse("2b3c4d5e-6f70-4182-93a4-b5c6d7e8f901");

    private static readonly Guid Other = Guid.Parse("8e7d6c5b-4a39-4281-9706-f5e4d3c2b1a0");

    private static readonly DateTimeOffset Instant = new(2026, 11, 4, 6, 40, 0, TimeSpan.Zero);



    [Fact]
    public void Airport_when_every_value_is_given_keeps_each_value()
    {
        var airport = new Airport
        {
            Id = Id,
            Code = "AAA",
            Name = "Example International",
            City = "Example City",
            State = "EX",
            Country = "Exampleland",
            TimeZoneId = "America/New_York",
            IsDeparture = true,
            IsGateway = true,
            FirstSeenUtc = Instant,
            Version = 3,
        };

        Assert.Equal(Id, airport.Id);
        Assert.Equal("AAA", airport.Code);
        Assert.Equal("Example International", airport.Name);
        Assert.Equal("Example City", airport.City);
        Assert.Equal("EX", airport.State);
        Assert.Equal("Exampleland", airport.Country);
        Assert.Equal("America/New_York", airport.TimeZoneId);
        Assert.True(airport.IsDeparture);
        Assert.True(airport.IsGateway);
        Assert.Equal(Instant, airport.FirstSeenUtc);
        Assert.Equal(3, airport.Version);
    }



    [Fact]
    public void Airport_when_only_the_required_values_are_given_has_no_state_no_zone_and_no_use()
    {
        var airport = new Airport { Id = Id, Code = "BBB", Name = "Example Field", City = "Example Town", Country = "Exampleland" };

        Assert.Null(airport.State);
        Assert.Null(airport.TimeZoneId);
        Assert.False(airport.IsDeparture);
        Assert.False(airport.IsGateway);
    }



    [Fact]
    public void Resort_when_every_value_is_given_keeps_each_value()
    {
        var resort = new Resort
        {
            Id = Id,
            Code = "HB",
            Name = "Halcyon Beach Resort and Spa",
            ShortName = "Halcyon Beach",
            Brand = Brand.Sandals,
            Island = "Saint Example",
            GatewayAirportId = Other,
            DriveMinutes = 25,
            FirstSeenUtc = Instant,
            Version = 2,
        };

        Assert.Equal(Id, resort.Id);
        Assert.Equal("HB", resort.Code);
        Assert.Equal("Halcyon Beach Resort and Spa", resort.Name);
        Assert.Equal("Halcyon Beach", resort.ShortName);
        Assert.Equal(Brand.Sandals, resort.Brand);
        Assert.Equal("Saint Example", resort.Island);
        Assert.Equal(Other, resort.GatewayAirportId);
        Assert.Equal(25, resort.DriveMinutes);
        Assert.Equal(Instant, resort.FirstSeenUtc);
        Assert.Equal(2, resort.Version);
    }



    [Fact]
    public void Resort_when_only_the_required_values_are_given_has_no_short_name_no_gateway_and_no_drive()
    {
        var resort = new Resort { Id = Id, Code = "HB", Name = "Halcyon Beach Resort and Spa", Brand = Brand.Beaches, Island = "Saint Example" };

        Assert.Null(resort.ShortName);
        Assert.Null(resort.GatewayAirportId);
        Assert.Null(resort.DriveMinutes);
    }



    [Theory]
    [InlineData(ResortSpanStatus.NotSet, 0)]
    [InlineData(ResortSpanStatus.Online, 1)]
    [InlineData(ResortSpanStatus.Offline, 2)]
    public void ResortSpanStatus_when_cast_to_a_number_keeps_its_declared_value(ResortSpanStatus status, int expected)
    {
        Assert.Equal(expected, (int)status);
    }



    [Fact]
    public void ResortSpan_when_no_dates_are_given_runs_from_the_open_start_to_the_open_end()
    {
        var span = new ResortSpan { Id = Id, ResortId = Other, Status = ResortSpanStatus.Online };

        Assert.Equal(Id, span.Id);
        Assert.Equal(Other, span.ResortId);
        Assert.Equal(ResortSpanStatus.Online, span.Status);
        Assert.Equal(new DateOnly(1900, 1, 1), span.EffDate);
        Assert.Equal(new DateOnly(9999, 12, 31), span.EndDate);
        Assert.Equal(span.EffDate, ResortSpan.OpenStart);
        Assert.Equal(span.EndDate, ResortSpan.OpenEnd);
    }



    [Theory]
    [InlineData(2027, 1, 31, false)]
    [InlineData(2027, 2, 1, true)]
    [InlineData(2027, 2, 15, true)]
    [InlineData(2027, 2, 28, true)]
    [InlineData(2027, 3, 1, false)]
    public void Covers_when_asked_about_a_day_counts_the_first_and_the_last_day_and_nothing_outside(int year, int month, int day, bool expected)
    {
        var span = new ResortSpan
        {
            Id = Id,
            ResortId = Other,
            EffDate = new DateOnly(2027, 2, 1),
            EndDate = new DateOnly(2027, 2, 28),
            Status = ResortSpanStatus.Offline,
        };

        Assert.Equal(expected, span.Covers(new DateOnly(year, month, day)));
    }



    [Fact]
    public void RoomCategory_when_every_value_is_given_keeps_each_value()
    {
        var category = new RoomCategory
        {
            Id = Id,
            ResortId = Other,
            Code = "OVB",
            Name = "Oceanview Butler Suite",
            FirstSeenUtc = Instant,
            LastSeenUtc = Instant.AddDays(7),
        };

        Assert.Equal(Id, category.Id);
        Assert.Equal(Other, category.ResortId);
        Assert.Equal("OVB", category.Code);
        Assert.Equal("Oceanview Butler Suite", category.Name);
        Assert.Equal(Instant, category.FirstSeenUtc);
        Assert.Equal(Instant.AddDays(7), category.LastSeenUtc);
    }



    [Fact]
    public void VocabularyValue_when_every_value_is_given_keeps_each_value()
    {
        var value = new VocabularyValue
        {
            Id = Id,
            Kind = VocabularyKind.RoomView,
            Value = "Oceanview",
            FirstSeenUtc = Instant,
            LastSeenUtc = Instant.AddDays(7),
        };

        Assert.Equal(Id, value.Id);
        Assert.Equal(VocabularyKind.RoomView, value.Kind);
        Assert.Equal("Oceanview", value.Value);
        Assert.Equal(Instant, value.FirstSeenUtc);
        Assert.Equal(Instant.AddDays(7), value.LastSeenUtc);
    }
}
