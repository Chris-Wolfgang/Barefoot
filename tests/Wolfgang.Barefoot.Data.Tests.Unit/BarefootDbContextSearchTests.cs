using Microsoft.EntityFrameworkCore;
using Wolfgang.Barefoot.Contracts.Catalog;
using Wolfgang.Barefoot.Contracts.Searches;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Tests.Unit;

/// <summary>
/// The search tables of <see cref="BarefootDbContext"/>: a stored search and the resorts, departure
/// airports, room filters and recipients it names.
/// </summary>
public sealed class BarefootDbContextSearchTests
{
    private static readonly Guid OwnerOid = Guid.Parse("6a718293-a4b5-4c6d-97ef-8091a2b3c4d5");

    private static readonly Guid OtherOid = Guid.Parse("7b8293a4-b5c6-4d7e-a8f0-91a2b3c4d5e6");

    private static readonly Guid SearchId = Guid.Parse("8c93a4b5-c6d7-4e8f-b901-a2b3c4d5e6f7");

    private static readonly Guid OtherSearchId = Guid.Parse("9da4b5c6-d7e8-4f90-8a12-b3c4d5e6f708");

    private static readonly Guid TargetId = Guid.Parse("aeb5c6d7-e8f9-4091-9b23-c4d5e6f70819");

    private static readonly Guid RowId = Guid.Parse("bfc6d7e8-f901-41a2-ac34-d5e6f708192a");

    private static readonly Guid OtherRowId = Guid.Parse("c0d7e8f9-0112-42b3-bd45-e6f708192a3b");

    private static readonly DateTimeOffset Instant = new(2026, 11, 4, 6, 40, 0, 123, TimeSpan.Zero);



    [Fact]
    public async Task SaveChangesAsync_when_a_recurring_search_is_added_reads_back_every_value()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            context.AppUsers.Add(User(OwnerOid, "pat@example.com"));
            var search = Search(SearchId, OwnerOid, "Winter week");
            search.Status = SearchStatus.Active;
            search.RunOrder = 7;
            search.ChildCount = 1;
            search.InfantCount = 1;
            search.RecurringFirstMonth = 12;
            search.RecurringFirstDay = 15;
            search.RecurringLastMonth = 2;
            search.RecurringLastDay = 28;
            search.IncludeFlights = true;
            search.Cabin = Cabin.PremiumEconomy;
            search.MaxTotalAmount = 12345.67m;
            search.MaxDriveMinutes = 45;
            search.MaxFlightMinutes = 300;
            search.MaxStopCount = 1;
            context.SearchMatrices.Add(search);
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.SearchMatrices.SingleAsync();

        Assert.Equal(SearchId, read.Id);
        Assert.Equal(OwnerOid, read.OwnerId);
        Assert.Equal("Winter week", read.Name);
        Assert.Equal("WINTER WEEK", read.NormalizedName);
        Assert.Equal(Brand.Sandals, read.Brand);
        Assert.Equal(SearchStatus.Active, read.Status);
        Assert.Equal(7, read.RunOrder);
        Assert.Equal(7, read.NightCount);
        Assert.Equal(2, read.AdultCount);
        Assert.Equal(1, read.ChildCount);
        Assert.Equal(1, read.InfantCount);
        Assert.Equal(WindowKind.Recurring, read.WindowKind);
        Assert.Equal(12, read.RecurringFirstMonth);
        Assert.Equal(15, read.RecurringFirstDay);
        Assert.Equal(2, read.RecurringLastMonth);
        Assert.Equal(28, read.RecurringLastDay);
        Assert.Null(read.FirstCheckIn);
        Assert.Null(read.LastCheckIn);
        Assert.True(read.IncludeFlights);
        Assert.Equal(Cabin.PremiumEconomy, read.Cabin);
        Assert.Equal(12345.67m, read.MaxTotalAmount);
        Assert.Equal(45, read.MaxDriveMinutes);
        Assert.Equal(300, read.MaxFlightMinutes);
        Assert.Equal(1, read.MaxStopCount);
        Assert.Equal(Instant, read.CreatedUtc);
        Assert.Equal(1, read.Version);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_one_time_search_without_limits_is_added_reads_back_its_window_and_no_limits()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            context.AppUsers.Add(User(OwnerOid, "pat@example.com"));
            var search = Search(SearchId, OwnerOid, "Spring break");
            search.WindowKind = WindowKind.OneTime;
            search.FirstCheckIn = new DateOnly(2027, 3, 13);
            search.LastCheckIn = new DateOnly(2027, 3, 20);
            context.SearchMatrices.Add(search);
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.SearchMatrices.SingleAsync();

        Assert.Equal(WindowKind.OneTime, read.WindowKind);
        Assert.Equal(new DateOnly(2027, 3, 13), read.FirstCheckIn);
        Assert.Equal(new DateOnly(2027, 3, 20), read.LastCheckIn);
        Assert.Null(read.RecurringFirstMonth);
        Assert.Null(read.RecurringFirstDay);
        Assert.Null(read.RecurringLastMonth);
        Assert.Null(read.RecurringLastDay);
        Assert.Equal(SearchStatus.Draft, read.Status);
        Assert.Equal(SearchDefinition.NormalRunOrder, read.RunOrder);
        Assert.False(read.IncludeFlights);
        Assert.Null(read.Cabin);
        Assert.Null(read.MaxTotalAmount);
        Assert.Null(read.MaxDriveMinutes);
        Assert.Null(read.MaxFlightMinutes);
        Assert.Null(read.MaxStopCount);
    }



    [Fact]
    public async Task SaveChangesAsync_when_an_owner_has_the_same_normalized_name_twice_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        context.AppUsers.Add(User(OwnerOid, "pat@example.com"));
        context.SearchMatrices.Add(Search(SearchId, OwnerOid, "Winter week"));
        context.SearchMatrices.Add(Search(OtherSearchId, OwnerOid, " winter WEEK "));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_two_owners_have_the_same_search_name_saves_both()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        context.AppUsers.Add(User(OwnerOid, "pat@example.com"));
        context.AppUsers.Add(User(OtherOid, "lee@example.com"));
        context.SearchMatrices.Add(Search(SearchId, OwnerOid, "Winter week"));
        context.SearchMatrices.Add(Search(OtherSearchId, OtherOid, "Winter week"));

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.SearchMatrices.CountAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_search_names_an_owner_that_does_not_exist_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        context.SearchMatrices.Add(Search(SearchId, OwnerOid, "Winter week"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_search_resort_is_added_reads_back_every_value()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            AddOwnerAndSearch(context);
            context.Resorts.Add(Resort(TargetId));
            context.SearchResorts.Add(new SearchResort { Id = RowId, SearchMatrixId = SearchId, ResortId = TargetId });
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.SearchResorts.SingleAsync();

        Assert.Equal(RowId, read.Id);
        Assert.Equal(SearchId, read.SearchMatrixId);
        Assert.Equal(TargetId, read.ResortId);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_search_departure_airport_is_added_reads_back_every_value()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            AddOwnerAndSearch(context);
            context.Airports.Add(Airport(TargetId));
            context.SearchDepartureAirports.Add(new SearchDepartureAirport { Id = RowId, SearchMatrixId = SearchId, AirportId = TargetId });
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.SearchDepartureAirports.SingleAsync();

        Assert.Equal(RowId, read.Id);
        Assert.Equal(SearchId, read.SearchMatrixId);
        Assert.Equal(TargetId, read.AirportId);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_search_room_filter_is_added_reads_back_every_value()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            AddOwnerAndSearch(context);
            context.VocabularyValues.Add(Vocabulary(TargetId));
            context.SearchRoomFilters.Add(new SearchRoomFilter { Id = RowId, SearchMatrixId = SearchId, VocabularyValueId = TargetId });
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.SearchRoomFilters.SingleAsync();

        Assert.Equal(RowId, read.Id);
        Assert.Equal(SearchId, read.SearchMatrixId);
        Assert.Equal(TargetId, read.VocabularyValueId);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_search_recipient_is_added_reads_back_every_value()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            AddOwnerAndSearch(context);
            context.Recipients.Add(Recipient(TargetId));
            context.SearchRecipients.Add(new SearchRecipient { Id = RowId, SearchMatrixId = SearchId, RecipientId = TargetId });
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.SearchRecipients.SingleAsync();

        Assert.Equal(RowId, read.Id);
        Assert.Equal(SearchId, read.SearchMatrixId);
        Assert.Equal(TargetId, read.RecipientId);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_search_names_a_resort_twice_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddOwnerAndSearch(context);
        context.Resorts.Add(Resort(TargetId));
        context.SearchResorts.Add(new SearchResort { Id = RowId, SearchMatrixId = SearchId, ResortId = TargetId });
        context.SearchResorts.Add(new SearchResort { Id = OtherRowId, SearchMatrixId = SearchId, ResortId = TargetId });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_two_searches_name_the_same_resort_saves_both()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddOwnerAndSearch(context);
        context.SearchMatrices.Add(Search(OtherSearchId, OwnerOid, "Spring break"));
        context.Resorts.Add(Resort(TargetId));
        context.SearchResorts.Add(new SearchResort { Id = RowId, SearchMatrixId = SearchId, ResortId = TargetId });
        context.SearchResorts.Add(new SearchResort { Id = OtherRowId, SearchMatrixId = OtherSearchId, ResortId = TargetId });

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.SearchResorts.CountAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_search_names_a_departure_airport_twice_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddOwnerAndSearch(context);
        context.Airports.Add(Airport(TargetId));
        context.SearchDepartureAirports.Add(new SearchDepartureAirport { Id = RowId, SearchMatrixId = SearchId, AirportId = TargetId });
        context.SearchDepartureAirports.Add(new SearchDepartureAirport { Id = OtherRowId, SearchMatrixId = SearchId, AirportId = TargetId });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_search_filters_by_a_value_twice_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddOwnerAndSearch(context);
        context.VocabularyValues.Add(Vocabulary(TargetId));
        context.SearchRoomFilters.Add(new SearchRoomFilter { Id = RowId, SearchMatrixId = SearchId, VocabularyValueId = TargetId });
        context.SearchRoomFilters.Add(new SearchRoomFilter { Id = OtherRowId, SearchMatrixId = SearchId, VocabularyValueId = TargetId });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_search_mails_a_recipient_twice_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddOwnerAndSearch(context);
        context.Recipients.Add(Recipient(TargetId));
        context.SearchRecipients.Add(new SearchRecipient { Id = RowId, SearchMatrixId = SearchId, RecipientId = TargetId });
        context.SearchRecipients.Add(new SearchRecipient { Id = OtherRowId, SearchMatrixId = SearchId, RecipientId = TargetId });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_search_resort_names_a_search_that_does_not_exist_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        context.Resorts.Add(Resort(TargetId));
        context.SearchResorts.Add(new SearchResort { Id = RowId, SearchMatrixId = SearchId, ResortId = TargetId });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_search_resort_names_a_resort_that_does_not_exist_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddOwnerAndSearch(context);
        context.SearchResorts.Add(new SearchResort { Id = RowId, SearchMatrixId = SearchId, ResortId = TargetId });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_search_that_still_mails_a_recipient_is_deleted_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await AddSearchThatMailsARecipientAsync(database);

        await using var deleter = database.NewContext();
        deleter.SearchMatrices.Remove(await deleter.SearchMatrices.SingleAsync());

        await Assert.ThrowsAsync<DbUpdateException>(() => deleter.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_recipient_a_search_still_mails_is_deleted_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await AddSearchThatMailsARecipientAsync(database);

        await using var deleter = database.NewContext();
        deleter.Recipients.Remove(await deleter.Recipients.SingleAsync());

        await Assert.ThrowsAsync<DbUpdateException>(() => deleter.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_search_is_deleted_with_its_link_rows_deletes_them_all()
    {
        await using var database = await TestDatabase.CreateAsync();
        await AddSearchThatMailsARecipientAsync(database);

        await using var deleter = database.NewContext();
        deleter.SearchRecipients.Remove(await deleter.SearchRecipients.SingleAsync());
        deleter.SearchMatrices.Remove(await deleter.SearchMatrices.SingleAsync());
        await deleter.SaveChangesAsync();

        Assert.Equal(0, await deleter.SearchMatrices.CountAsync());
        Assert.Equal(0, await deleter.SearchRecipients.CountAsync());
        Assert.Equal(1, await deleter.Recipients.CountAsync());
    }



    private static async Task AddSearchThatMailsARecipientAsync(TestDatabase database)
    {
        await using var context = database.NewContext();
        AddOwnerAndSearch(context);
        context.Recipients.Add(Recipient(TargetId));
        context.SearchRecipients.Add(new SearchRecipient { Id = RowId, SearchMatrixId = SearchId, RecipientId = TargetId });
        await context.SaveChangesAsync();
    }



    private static void AddOwnerAndSearch(BarefootDbContext context)
    {
        context.AppUsers.Add(User(OwnerOid, "pat@example.com"));
        context.SearchMatrices.Add(Search(SearchId, OwnerOid, "Winter week"));
    }



    private static AppUser User(Guid oid, string email)
    {
        return new AppUser { Oid = oid, DisplayName = "Pat Example", Email = email, LastSeenUtc = Instant };
    }



    private static SearchMatrix Search(Guid id, Guid ownerId, string name)
    {
        return new SearchMatrix
        {
            Id = id,
            OwnerId = ownerId,
            Name = name,
            NormalizedName = SearchMatrix.NormalizeName(name),
            Brand = Brand.Sandals,
            NightCount = 7,
            AdultCount = 2,
            WindowKind = WindowKind.Recurring,
            CreatedUtc = Instant,
        };
    }



    private static Resort Resort(Guid id)
    {
        return new Resort
        {
            Id = id,
            Code = "RES1",
            Name = "Example Bay Resort",
            Brand = Brand.Sandals,
            Island = "Example Island",
            FirstSeenUtc = Instant,
        };
    }



    private static Airport Airport(Guid id)
    {
        return new Airport
        {
            Id = id,
            Code = "AAA",
            Name = "Example International",
            City = "Example City",
            Country = "Exampleland",
            FirstSeenUtc = Instant,
        };
    }



    private static VocabularyValue Vocabulary(Guid id)
    {
        return new VocabularyValue
        {
            Id = id,
            Kind = VocabularyKind.RoomView,
            Value = "Ocean View",
            FirstSeenUtc = Instant,
            LastSeenUtc = Instant,
        };
    }



    private static Recipient Recipient(Guid id)
    {
        return new Recipient { Id = id, OwnerId = OwnerOid, Address = "sam@example.com", DisplayName = "Sam Example" };
    }
}
