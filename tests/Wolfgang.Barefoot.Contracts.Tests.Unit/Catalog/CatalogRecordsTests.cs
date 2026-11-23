using Wolfgang.Barefoot.Contracts.Catalog;
using Wolfgang.Barefoot.Contracts.Searches;

namespace Wolfgang.Barefoot.Contracts.Tests.Unit.Catalog;

public sealed class CatalogRecordsTests
{
    [Fact]
    public void AirportSnapshot_when_built_keeps_each_airport_and_its_values()
    {
        var entry = new AirportSnapshotEntry("UVF", "Hewanorra Intl", "Vieux Fort", null, "St. Lucia");
        var snapshot = new AirportSnapshot([entry]);

        Assert.Same(entry, Assert.Single(snapshot.Airports));
        Assert.Equal("UVF", entry.Code);
        Assert.Equal("Hewanorra Intl", entry.Name);
        Assert.Equal("Vieux Fort", entry.City);
        Assert.Null(entry.State);
        Assert.Equal("St. Lucia", entry.Country);
    }



    [Fact]
    public void ResortSnapshot_when_built_keeps_each_resort_and_its_values()
    {
        var entry = new ResortSnapshotEntry("SGL", "Sandals Grande St. Lucian", Brand.Sandals, "St. Lucia", "UVF");
        var snapshot = new ResortSnapshot([entry]);

        Assert.Same(entry, Assert.Single(snapshot.Resorts));
        Assert.Equal("SGL", entry.Code);
        Assert.Equal("Sandals Grande St. Lucian", entry.Name);
        Assert.Equal(Brand.Sandals, entry.Brand);
        Assert.Equal("St. Lucia", entry.Island);
        Assert.Equal("UVF", entry.GatewayAirportCode);
    }



    [Fact]
    public void CatalogRefreshResponse_when_built_keeps_the_counts_and_the_warnings()
    {
        var response = new CatalogRefreshResponse(2, 3, 1, ["SGL appeared twice with different names."]);

        Assert.Equal(2, response.AddedCount);
        Assert.Equal(3, response.UpdatedCount);
        Assert.Equal(1, response.DeactivatedCount);
        Assert.Equal(["SGL appeared twice with different names."], response.Warnings);
    }
}
