using Microsoft.EntityFrameworkCore;
using Wolfgang.Barefoot.Contracts.Catalog;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Tests.Unit;

/// <summary>
/// The <c>catalog_refresh</c> table: one row per catalog snapshot received (ADR 0017 §5).
/// </summary>
public sealed class BarefootDbContextCatalogRefreshTests
{
    private static readonly Guid RowId = Guid.Parse("5c6d7e8f-9a0b-4c1d-8e2f-3a4b5c6d7e8f");

    private static readonly Guid OtherRowId = Guid.Parse("6d7e8f9a-0b1c-4d2e-9f3a-4b5c6d7e8f9a");

    private static readonly Guid ThirdRowId = Guid.Parse("7e8f9a0b-1c2d-4e3f-8a4b-5c6d7e8f9a0b");

    private static readonly DateTimeOffset Instant = new(2026, 11, 4, 6, 40, 0, 123, TimeSpan.Zero);



    [Fact]
    public async Task SaveChangesAsync_when_an_applied_refresh_is_added_reads_back_every_value()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            context.CatalogRefreshes.Add
            (
                new CatalogRefresh
                {
                    Id = RowId,
                    Kind = CatalogKind.Resorts,
                    ReceivedUtc = Instant,
                    AddedCount = 2,
                    UpdatedCount = 5,
                    DeactivatedCount = 1,
                    WarningCount = 3,
                }
            );
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.CatalogRefreshes.SingleAsync();

        Assert.Equal(RowId, read.Id);
        Assert.Equal(CatalogKind.Resorts, read.Kind);
        Assert.Equal(Instant, read.ReceivedUtc);
        Assert.Equal(TimeSpan.Zero, read.ReceivedUtc.Offset);
        Assert.Equal(2, read.AddedCount);
        Assert.Equal(5, read.UpdatedCount);
        Assert.Equal(1, read.DeactivatedCount);
        Assert.Equal(3, read.WarningCount);
        Assert.Null(read.RefusalReason);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_refused_refresh_is_added_reads_back_the_reason()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            context.CatalogRefreshes.Add(Refresh(RowId, CatalogKind.Airports, Instant, "The snapshot was empty."));
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.CatalogRefreshes.SingleAsync();

        Assert.Equal(CatalogKind.Airports, read.Kind);
        Assert.Equal("The snapshot was empty.", read.RefusalReason);
    }



    [Fact]
    public async Task SaveChangesAsync_when_two_refreshes_of_one_kind_arrive_at_the_same_instant_saves_both()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        context.CatalogRefreshes.Add(Refresh(RowId, CatalogKind.Airports, Instant, refusalReason: null));
        context.CatalogRefreshes.Add(Refresh(OtherRowId, CatalogKind.Airports, Instant, refusalReason: null));

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.CatalogRefreshes.CountAsync());
    }



    [Fact]
    public async Task MaxAsync_when_asked_for_the_applied_refreshes_leaves_out_a_later_refused_one()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            context.CatalogRefreshes.Add(Refresh(RowId, CatalogKind.Airports, Instant, refusalReason: null));
            context.CatalogRefreshes.Add(Refresh(OtherRowId, CatalogKind.Resorts, Instant.AddHours(1), refusalReason: null));
            context.CatalogRefreshes.Add(Refresh(ThirdRowId, CatalogKind.Resorts, Instant.AddDays(1), "The snapshot was empty."));
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var lastApplied = await reader
            .CatalogRefreshes
            .Where(refresh => refresh.RefusalReason == null)
            .MaxAsync(refresh => (DateTimeOffset?)refresh.ReceivedUtc);

        Assert.Equal(Instant.AddHours(1), lastApplied);
    }



    private static CatalogRefresh Refresh(Guid id, CatalogKind kind, DateTimeOffset receivedUtc, string? refusalReason)
    {
        return new CatalogRefresh { Id = id, Kind = kind, ReceivedUtc = receivedUtc, RefusalReason = refusalReason };
    }
}
