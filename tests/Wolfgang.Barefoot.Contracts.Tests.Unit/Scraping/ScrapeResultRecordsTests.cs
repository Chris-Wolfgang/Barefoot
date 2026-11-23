using Wolfgang.Barefoot.Contracts.Scraping;

namespace Wolfgang.Barefoot.Contracts.Tests.Unit.Scraping;

public sealed class ScrapeResultRecordsTests
{
    private static readonly Guid Attempt = Guid.Parse("7b0e3c52-1f4a-4d6b-9a57-0c2d8e4f6a11");

    private static readonly ScrapedLeg Leg = new
    (
        "Example Air",
        "1252",
        "Example Regional",
        "Standard Economy",
        "Q",
        new DateOnly(2026, 8, 9),
        new TimeOnly(6, 0),
        "AAA",
        new TimeOnly(8, 52),
        "BBB",
        Overnight: true
    );



    [Fact]
    public void ScrapedRoom_when_built_for_a_priced_card_keeps_each_value()
    {
        var room = new ScrapedRoom("OWB", "Overwater Bungalow", "Butler", 28418m, SoldOut: false, 4, "Oceanfront", "Bungalow", ["Patio"]);

        Assert.Equal("OWB", room.CategoryCode);
        Assert.Equal("Overwater Bungalow", room.Name);
        Assert.Equal("Butler", room.Level);
        Assert.Equal(28418m, room.TotalAmount);
        Assert.False(room.SoldOut);
        Assert.Equal(4, room.RemainingRoomCount);
        Assert.Equal("Oceanfront", room.View);
        Assert.Equal("Bungalow", room.Type);
        Assert.Equal(["Patio"], room.Features);
    }



    [Fact]
    public void ScrapedRoom_when_built_for_a_sold_out_card_has_no_price_and_no_count()
    {
        var room = new ScrapedRoom("LX", "Luxury Oceanview", "Luxury", null, SoldOut: true, null, null, null, []);

        Assert.True(room.SoldOut);
        Assert.Null(room.TotalAmount);
        Assert.Null(room.RemainingRoomCount);
        Assert.Null(room.View);
        Assert.Null(room.Type);
        Assert.Empty(room.Features);
    }



    [Fact]
    public void ScrapedLeg_when_built_keeps_each_value()
    {
        Assert.Equal("Example Air", Leg.Carrier);
        Assert.Equal("1252", Leg.FlightNumber);
        Assert.Equal("Example Regional", Leg.OperatingCarrier);
        Assert.Equal("Standard Economy", Leg.CabinName);
        Assert.Equal("Q", Leg.FareCode);
        Assert.Equal(new DateOnly(2026, 8, 9), Leg.DepartureDate);
        Assert.Equal(new TimeOnly(6, 0), Leg.DepartureTime);
        Assert.Equal("AAA", Leg.DepartureAirportCode);
        Assert.Equal(new TimeOnly(8, 52), Leg.ArrivalTime);
        Assert.Equal("BBB", Leg.ArrivalAirportCode);
        Assert.True(Leg.Overnight);
    }



    [Fact]
    public void ScrapedItinerary_when_built_keeps_the_total_the_room_and_both_directions()
    {
        var outbound = new ScrapedDirection([Leg, Leg], Overnight: false);
        var back = new ScrapedDirection([Leg], Overnight: true);
        var itinerary = new ScrapedItinerary(15750.66m, "RP", outbound, back);

        Assert.Equal(15750.66m, itinerary.TotalAmount);
        Assert.Equal("RP", itinerary.RoomCategoryCode);
        Assert.Same(outbound, itinerary.Outbound);
        Assert.Same(back, itinerary.Return);
        Assert.Equal(2, itinerary.Outbound.Legs.Count);
        Assert.False(itinerary.Outbound.Overnight);
        Assert.True(itinerary.Return.Overnight);
    }



    [Fact]
    public void RoomsResult_when_the_lookup_succeeded_carries_the_rooms_and_no_error()
    {
        var result = new RoomsResult(Attempt, [], null);

        Assert.Equal(Attempt, result.Attempt);
        Assert.NotNull(result.Rooms);
        Assert.Null(result.Error);
    }



    [Fact]
    public void FlightsResult_when_the_lookup_failed_carries_the_error_and_no_options()
    {
        var result = new FlightsResult(Attempt, null, "The itineraries did not load.");

        Assert.Equal(Attempt, result.Attempt);
        Assert.Null(result.Options);
        Assert.Equal("The itineraries did not load.", result.Error);
    }
}
