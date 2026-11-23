using Wolfgang.Barefoot.Contracts.Catalog;
using Wolfgang.Barefoot.Contracts.Searches;

namespace Wolfgang.Barefoot.Contracts.Tests.Unit.Catalog;

public sealed class ReferenceDataRecordsTests
{
    private static readonly Guid Id = Guid.Parse("5d0c6f0e-3a51-4c1d-8a43-1b2c3d4e5f60");

    private static readonly DateTimeOffset Seen = new(2026, 11, 7, 6, 0, 0, TimeSpan.Zero);



    [Fact]
    public void ResortResponse_when_a_resort_is_named_and_offline_keeps_each_value()
    {
        var resort = new ResortResponse
        (
            Id,
            "SHC",
            "Sandals Halcyon Beach",
            "Halcyon Beach",
            Brand.Sandals,
            "St. Lucia",
            "AAA",
            80,
            new DateOnly(2026, 11, 1),
            Seen,
            3,
            4
        );

        Assert.Equal(Id, resort.Id);
        Assert.Equal("SHC", resort.Code);
        Assert.Equal("Sandals Halcyon Beach", resort.Name);
        Assert.Equal("Halcyon Beach", resort.ShortName);
        Assert.Equal(Brand.Sandals, resort.Brand);
        Assert.Equal("St. Lucia", resort.Island);
        Assert.Equal("AAA", resort.GatewayAirportCode);
        Assert.Equal(80, resort.DriveMinutes);
        Assert.Equal(new DateOnly(2026, 11, 1), resort.OfflineSince);
        Assert.Equal(Seen, resort.FirstSeenUtc);
        Assert.Equal(3, resort.SearchCount);
        Assert.Equal(4, resort.Version);
    }



    [Fact]
    public void ResortResponse_when_a_resort_is_newly_seen_and_read_by_an_editor_leaves_the_rest_empty()
    {
        var resort = new ResortResponse(Id, "SSV", "Sandals Saint Vincent", null, Brand.Beaches, "St. Vincent", null, null, null, Seen, null, 1);

        Assert.Null(resort.ShortName);
        Assert.Equal(Brand.Beaches, resort.Brand);
        Assert.Null(resort.GatewayAirportCode);
        Assert.Null(resort.DriveMinutes);
        Assert.Null(resort.OfflineSince);
        Assert.Null(resort.SearchCount);
    }



    [Fact]
    public void UpdateResortRequest_when_built_keeps_both_values()
    {
        var request = new UpdateResortRequest("Halcyon Beach", 80);

        Assert.Equal("Halcyon Beach", request.ShortName);
        Assert.Equal(80, request.DriveMinutes);
    }



    [Fact]
    public void AirportResponse_when_an_airport_has_a_zone_keeps_each_value()
    {
        var airport = new AirportResponse(Id, "AAA", "First Airport", "America/New_York", IsDeparture: true, IsGateway: false, Seen, 7, 2);

        Assert.Equal(Id, airport.Id);
        Assert.Equal("AAA", airport.Code);
        Assert.Equal("First Airport", airport.Name);
        Assert.Equal("America/New_York", airport.TimeZoneId);
        Assert.True(airport.IsDeparture);
        Assert.False(airport.IsGateway);
        Assert.Equal(Seen, airport.FirstSeenUtc);
        Assert.Equal(7, airport.SearchCount);
        Assert.Equal(2, airport.Version);
    }



    [Fact]
    public void AirportResponse_when_the_zone_is_not_known_has_none()
    {
        var airport = new AirportResponse(Id, "BBB", "Second Airport", null, IsDeparture: false, IsGateway: true, Seen, null, 1);

        Assert.Null(airport.TimeZoneId);
        Assert.False(airport.IsDeparture);
        Assert.True(airport.IsGateway);
        Assert.Null(airport.SearchCount);
    }



    [Fact]
    public void SetAirportTimeZoneRequest_when_built_keeps_the_zone()
    {
        Assert.Equal("America/St_Vincent", new SetAirportTimeZoneRequest("America/St_Vincent").TimeZoneId);
    }



    [Theory]
    [InlineData(VocabularyKind.RoomLevel, 0)]
    [InlineData(VocabularyKind.RoomView, 1)]
    [InlineData(VocabularyKind.RoomType, 2)]
    [InlineData(VocabularyKind.RoomFeature, 3)]
    public void VocabularyKind_when_cast_to_a_number_keeps_its_declared_value(VocabularyKind kind, int expected)
    {
        Assert.Equal(expected, (int)kind);
    }



    [Fact]
    public void VocabularyValueResponse_when_a_value_is_stale_keeps_each_value()
    {
        var value = new VocabularyValueResponse(Id, VocabularyKind.RoomFeature, "Soaking tub", Seen, 7, IsStale: true, 1);

        Assert.Equal(Id, value.Id);
        Assert.Equal(VocabularyKind.RoomFeature, value.Kind);
        Assert.Equal("Soaking tub", value.Value);
        Assert.Equal(Seen, value.LastSeenUtc);
        Assert.Equal(7, value.RunsSinceSeen);
        Assert.True(value.IsStale);
        Assert.Equal(1, value.SearchCount);
    }



    [Fact]
    public void VocabularyValueResponse_when_a_value_was_just_seen_and_read_by_an_editor_is_not_stale_and_has_no_count()
    {
        var value = new VocabularyValueResponse(Id, VocabularyKind.RoomLevel, "Club", Seen, 0, IsStale: false, null);

        Assert.Equal(0, value.RunsSinceSeen);
        Assert.False(value.IsStale);
        Assert.Null(value.SearchCount);
    }



    [Fact]
    public void CatalogStatusResponse_when_built_keeps_the_instant_or_none()
    {
        Assert.Equal(Seen, new CatalogStatusResponse(Seen).LastRefreshedUtc);
        Assert.Null(new CatalogStatusResponse(null).LastRefreshedUtc);
    }
}
