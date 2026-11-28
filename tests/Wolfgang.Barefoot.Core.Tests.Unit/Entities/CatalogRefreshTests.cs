using Wolfgang.Barefoot.Contracts.Catalog;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Core.Tests.Unit.Entities;

public sealed class CatalogRefreshTests
{
    private static readonly Guid Id = Guid.Parse("5c6d7e8f-9a0b-4c1d-8e2f-3a4b5c6d7e8f");

    private static readonly DateTimeOffset Instant = new(2026, 11, 4, 6, 40, 0, TimeSpan.Zero);



    [Fact]
    public void CatalogRefresh_when_every_value_is_given_keeps_each_value()
    {
        var refresh = new CatalogRefresh
        {
            Id = Id,
            Kind = CatalogKind.Resorts,
            ReceivedUtc = Instant,
            AddedCount = 2,
            UpdatedCount = 5,
            DeactivatedCount = 1,
            WarningCount = 3,
            RefusalReason = "The snapshot was empty.",
        };

        Assert.Equal(Id, refresh.Id);
        Assert.Equal(CatalogKind.Resorts, refresh.Kind);
        Assert.Equal(Instant, refresh.ReceivedUtc);
        Assert.Equal(2, refresh.AddedCount);
        Assert.Equal(5, refresh.UpdatedCount);
        Assert.Equal(1, refresh.DeactivatedCount);
        Assert.Equal(3, refresh.WarningCount);
        Assert.Equal("The snapshot was empty.", refresh.RefusalReason);
    }



    [Fact]
    public void CatalogRefresh_when_only_the_key_and_kind_are_given_counts_nothing_and_was_not_refused()
    {
        var refresh = new CatalogRefresh { Id = Id, Kind = CatalogKind.Airports };

        Assert.Equal(0, refresh.AddedCount);
        Assert.Equal(0, refresh.UpdatedCount);
        Assert.Equal(0, refresh.DeactivatedCount);
        Assert.Equal(0, refresh.WarningCount);
        Assert.Null(refresh.RefusalReason);
    }
}
