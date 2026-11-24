using Microsoft.EntityFrameworkCore;
using Wolfgang.Barefoot.Contracts.Catalog;
using Wolfgang.Barefoot.Contracts.Searches;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Tests.Unit;

/// <summary>
/// The reference-data tables of <see cref="BarefootDbContext"/>: airports, resorts and their spans,
/// room categories and the room vocabularies.
/// </summary>
public sealed class BarefootDbContextReferenceDataTests
{
    private static readonly Guid AirportId = Guid.Parse("1b2c3d4e-5f60-4718-892a-3b4c5d6e7f80");

    private static readonly Guid ResortId = Guid.Parse("2c3d4e5f-6071-4829-93ab-4c5d6e7f8091");

    private static readonly Guid OtherResortId = Guid.Parse("3d4e5f60-7182-493a-a4bc-5d6e7f8091a2");

    private static readonly Guid RowId = Guid.Parse("4e5f6071-8293-4a4b-b5cd-6e7f8091a2b3");

    private static readonly Guid OtherRowId = Guid.Parse("5f607182-93a4-4b5c-86de-7f8091a2b3c4");

    private static readonly DateTimeOffset Instant = new(2026, 11, 4, 6, 40, 0, 123, TimeSpan.Zero);



    [Fact]
    public async Task SaveChangesAsync_when_an_airport_is_added_reads_back_every_value()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            var airport = Airport(AirportId, "AAA");
            airport.State = "Example State";
            airport.TimeZoneId = "America/New_York";
            airport.IsDeparture = true;
            airport.IsGateway = true;
            airport.Version = 3;
            context.Airports.Add(airport);
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.Airports.SingleAsync();

        Assert.Equal(AirportId, read.Id);
        Assert.Equal("AAA", read.Code);
        Assert.Equal("Example International", read.Name);
        Assert.Equal("Example City", read.City);
        Assert.Equal("Example State", read.State);
        Assert.Equal("Exampleland", read.Country);
        Assert.Equal("America/New_York", read.TimeZoneId);
        Assert.True(read.IsDeparture);
        Assert.True(read.IsGateway);
        Assert.Equal(Instant, read.FirstSeenUtc);
        Assert.Equal(3, read.Version);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_resort_is_added_reads_back_every_value()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            context.Airports.Add(Airport(AirportId, "AAA"));
            var resort = Resort(ResortId, "RES1");
            resort.ShortName = "Example Bay";
            resort.GatewayAirportId = AirportId;
            resort.DriveMinutes = 25;
            resort.Version = 2;
            context.Resorts.Add(resort);
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.Resorts.SingleAsync();

        Assert.Equal(ResortId, read.Id);
        Assert.Equal("RES1", read.Code);
        Assert.Equal("Example Bay Resort", read.Name);
        Assert.Equal("Example Bay", read.ShortName);
        Assert.Equal(Brand.Beaches, read.Brand);
        Assert.Equal("Example Island", read.Island);
        Assert.Equal(AirportId, read.GatewayAirportId);
        Assert.Equal(25, read.DriveMinutes);
        Assert.Equal(Instant, read.FirstSeenUtc);
        Assert.Equal(2, read.Version);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_resort_has_no_gateway_airport_saves_it()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            context.Resorts.Add(Resort(ResortId, "RES1"));
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.Resorts.SingleAsync();

        Assert.Null(read.GatewayAirportId);
        Assert.Null(read.ShortName);
        Assert.Null(read.DriveMinutes);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_resort_span_is_added_reads_back_every_value()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            context.Resorts.Add(Resort(ResortId, "RES1"));
            context.ResortSpans.Add
            (
                new ResortSpan
                {
                    Id = RowId,
                    ResortId = ResortId,
                    EffDate = new DateOnly(2026, 3, 1),
                    EndDate = new DateOnly(2026, 9, 30),
                    Status = ResortSpanStatus.Offline,
                }
            );
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.ResortSpans.SingleAsync();

        Assert.Equal(RowId, read.Id);
        Assert.Equal(ResortId, read.ResortId);
        Assert.Equal(new DateOnly(2026, 3, 1), read.EffDate);
        Assert.Equal(new DateOnly(2026, 9, 30), read.EndDate);
        Assert.Equal(ResortSpanStatus.Offline, read.Status);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_resort_span_is_open_at_both_ends_reads_back_the_sentinel_days()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            context.Resorts.Add(Resort(ResortId, "RES1"));
            context.ResortSpans.Add(new ResortSpan { Id = RowId, ResortId = ResortId, Status = ResortSpanStatus.Online });
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.ResortSpans.SingleAsync();

        Assert.Equal(ResortSpan.OpenStart, read.EffDate);
        Assert.Equal(ResortSpan.OpenEnd, read.EndDate);
    }



    [Fact]
    public async Task ToListAsync_when_filtered_by_a_day_finds_the_span_that_covers_it_in_the_database()
    {
        var day = new DateOnly(2026, 6, 15);
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            context.Resorts.Add(Resort(ResortId, "RES1"));
            context.ResortSpans.Add
            (
                new ResortSpan { Id = RowId, ResortId = ResortId, EndDate = new DateOnly(2026, 2, 28), Status = ResortSpanStatus.Online }
            );
            context.ResortSpans.Add
            (
                new ResortSpan { Id = OtherRowId, ResortId = ResortId, EffDate = new DateOnly(2026, 3, 1), Status = ResortSpanStatus.Offline }
            );
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var covering = await reader
            .ResortSpans
            .Where(span => span.ResortId == ResortId && span.EffDate <= day && span.EndDate >= day)
            .Select(span => span.Id)
            .ToListAsync();

        Assert.Equal([OtherRowId], covering);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_room_category_is_added_reads_back_every_value()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            context.Resorts.Add(Resort(ResortId, "RES1"));
            context.RoomCategories.Add(RoomCategory(RowId, ResortId, "OV1"));
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.RoomCategories.SingleAsync();

        Assert.Equal(RowId, read.Id);
        Assert.Equal(ResortId, read.ResortId);
        Assert.Equal("OV1", read.Code);
        Assert.Equal("Ocean View Room", read.Name);
        Assert.Equal(Instant, read.FirstSeenUtc);
        Assert.Equal(Instant.AddDays(7), read.LastSeenUtc);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_vocabulary_value_is_added_reads_back_every_value()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            context.VocabularyValues.Add(Vocabulary(RowId, VocabularyKind.RoomView, "Ocean View"));
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.VocabularyValues.SingleAsync();

        Assert.Equal(RowId, read.Id);
        Assert.Equal(VocabularyKind.RoomView, read.Kind);
        Assert.Equal("Ocean View", read.Value);
        Assert.Equal(Instant, read.FirstSeenUtc);
        Assert.Equal(Instant.AddDays(7), read.LastSeenUtc);
    }



    [Fact]
    public async Task SaveChangesAsync_when_two_airports_have_the_same_code_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        context.Airports.Add(Airport(AirportId, "AAA"));
        context.Airports.Add(Airport(RowId, "AAA"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_two_resorts_have_the_same_code_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        context.Resorts.Add(Resort(ResortId, "RES1"));
        context.Resorts.Add(Resort(OtherResortId, "RES1"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_resort_has_the_same_room_code_twice_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        context.Resorts.Add(Resort(ResortId, "RES1"));
        context.RoomCategories.Add(RoomCategory(RowId, ResortId, "OV1"));
        context.RoomCategories.Add(RoomCategory(OtherRowId, ResortId, "OV1"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_two_resorts_have_the_same_room_code_saves_both()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        context.Resorts.Add(Resort(ResortId, "RES1"));
        context.Resorts.Add(Resort(OtherResortId, "RES2"));
        context.RoomCategories.Add(RoomCategory(RowId, ResortId, "OV1"));
        context.RoomCategories.Add(RoomCategory(OtherRowId, OtherResortId, "OV1"));

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.RoomCategories.CountAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_vocabulary_has_the_same_value_twice_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        context.VocabularyValues.Add(Vocabulary(RowId, VocabularyKind.RoomView, "Ocean View"));
        context.VocabularyValues.Add(Vocabulary(OtherRowId, VocabularyKind.RoomView, "Ocean View"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_two_vocabularies_have_the_same_value_saves_both()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        context.VocabularyValues.Add(Vocabulary(RowId, VocabularyKind.RoomView, "Ocean View"));
        context.VocabularyValues.Add(Vocabulary(OtherRowId, VocabularyKind.RoomFeature, "Ocean View"));

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.VocabularyValues.CountAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_resort_names_an_airport_that_does_not_exist_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        var resort = Resort(ResortId, "RES1");
        resort.GatewayAirportId = AirportId;
        context.Resorts.Add(resort);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_resort_span_names_a_resort_that_does_not_exist_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        context.ResortSpans.Add(new ResortSpan { Id = RowId, ResortId = ResortId, Status = ResortSpanStatus.Online });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_room_category_names_a_resort_that_does_not_exist_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        context.RoomCategories.Add(RoomCategory(RowId, ResortId, "OV1"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_an_airport_a_resort_is_reached_through_is_deleted_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            context.Airports.Add(Airport(AirportId, "AAA"));
            var resort = Resort(ResortId, "RES1");
            resort.GatewayAirportId = AirportId;
            context.Resorts.Add(resort);
            await context.SaveChangesAsync();
        }

        await using var deleter = database.NewContext();
        deleter.Airports.Remove(await deleter.Airports.SingleAsync());

        await Assert.ThrowsAsync<DbUpdateException>(() => deleter.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_resort_that_has_a_span_is_deleted_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            context.Resorts.Add(Resort(ResortId, "RES1"));
            context.ResortSpans.Add(new ResortSpan { Id = RowId, ResortId = ResortId, Status = ResortSpanStatus.Online });
            await context.SaveChangesAsync();
        }

        await using var deleter = database.NewContext();
        deleter.Resorts.Remove(await deleter.Resorts.SingleAsync());

        await Assert.ThrowsAsync<DbUpdateException>(() => deleter.SaveChangesAsync());
    }



    private static Airport Airport(Guid id, string code)
    {
        return new Airport
        {
            Id = id,
            Code = code,
            Name = "Example International",
            City = "Example City",
            Country = "Exampleland",
            FirstSeenUtc = Instant,
        };
    }



    private static Resort Resort(Guid id, string code)
    {
        return new Resort
        {
            Id = id,
            Code = code,
            Name = "Example Bay Resort",
            Brand = Brand.Beaches,
            Island = "Example Island",
            FirstSeenUtc = Instant,
        };
    }



    private static RoomCategory RoomCategory(Guid id, Guid resortId, string code)
    {
        return new RoomCategory
        {
            Id = id,
            ResortId = resortId,
            Code = code,
            Name = "Ocean View Room",
            FirstSeenUtc = Instant,
            LastSeenUtc = Instant.AddDays(7),
        };
    }



    private static VocabularyValue Vocabulary(Guid id, VocabularyKind kind, string value)
    {
        return new VocabularyValue
        {
            Id = id,
            Kind = kind,
            Value = value,
            FirstSeenUtc = Instant,
            LastSeenUtc = Instant.AddDays(7),
        };
    }
}
