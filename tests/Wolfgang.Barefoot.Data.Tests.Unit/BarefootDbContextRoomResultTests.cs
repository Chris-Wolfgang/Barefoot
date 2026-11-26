using Microsoft.EntityFrameworkCore;
using Wolfgang.Barefoot.Contracts.Catalog;
using Wolfgang.Barefoot.Contracts.Runs;
using Wolfgang.Barefoot.Contracts.Searches;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Tests.Unit;

/// <summary>
/// The room-result tables of <see cref="BarefootDbContext"/>: the rooms the site offered for a
/// lookup, and the features of each room.
/// </summary>
public sealed class BarefootDbContextRoomResultTests
{
    private static readonly Guid ResortId = Guid.Parse("a1b2c3d4-0001-4a5b-8c6d-7e8f90a1b2c3");

    private static readonly Guid RunId = Guid.Parse("a1b2c3d4-0002-4a5b-8c6d-7e8f90a1b2c3");

    private static readonly Guid RequestId = Guid.Parse("a1b2c3d4-0003-4a5b-8c6d-7e8f90a1b2c3");

    private static readonly Guid CategoryId = Guid.Parse("a1b2c3d4-0004-4a5b-8c6d-7e8f90a1b2c3");

    private static readonly Guid LevelId = Guid.Parse("a1b2c3d4-0005-4a5b-8c6d-7e8f90a1b2c3");

    private static readonly Guid ViewId = Guid.Parse("a1b2c3d4-0006-4a5b-8c6d-7e8f90a1b2c3");

    private static readonly Guid TypeId = Guid.Parse("a1b2c3d4-0007-4a5b-8c6d-7e8f90a1b2c3");

    private static readonly Guid FeatureId = Guid.Parse("a1b2c3d4-0008-4a5b-8c6d-7e8f90a1b2c3");

    private static readonly Guid ResultId = Guid.Parse("a1b2c3d4-0009-4a5b-8c6d-7e8f90a1b2c3");

    private static readonly Guid OtherResultId = Guid.Parse("a1b2c3d4-000a-4a5b-8c6d-7e8f90a1b2c3");

    private static readonly Guid RowId = Guid.Parse("a1b2c3d4-000b-4a5b-8c6d-7e8f90a1b2c3");

    private static readonly Guid OtherRowId = Guid.Parse("a1b2c3d4-000c-4a5b-8c6d-7e8f90a1b2c3");

    private static readonly Guid MissingId = Guid.Parse("a1b2c3d4-000d-4a5b-8c6d-7e8f90a1b2c3");

    private static readonly DateTimeOffset Instant = new(2026, 11, 4, 6, 40, 0, 123, TimeSpan.Zero);



    [Fact]
    public async Task SaveChangesAsync_when_a_room_result_is_added_reads_back_every_value()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            AddLookupAndCatalog(context);
            context.RoomResults.Add
            (
                new RoomResult
                {
                    Id = ResultId,
                    SearchRequestId = RequestId,
                    RoomCategoryId = CategoryId,
                    RoomLevelId = LevelId,
                    RoomViewId = ViewId,
                    RoomTypeId = TypeId,
                    RemainingRoomCount = 2,
                    TotalAmount = 8421.50m,
                    RegularAmount = 9900.00m,
                    Promotion = "Seven nights for the price of six.",
                }
            );
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.RoomResults.SingleAsync();

        Assert.Equal(ResultId, read.Id);
        Assert.Equal(RequestId, read.SearchRequestId);
        Assert.Equal(CategoryId, read.RoomCategoryId);
        Assert.Equal(LevelId, read.RoomLevelId);
        Assert.Equal(ViewId, read.RoomViewId);
        Assert.Equal(TypeId, read.RoomTypeId);
        Assert.False(read.IsSoldOut);
        Assert.Equal(2, read.RemainingRoomCount);
        Assert.Equal(8421.50m, read.TotalAmount);
        Assert.Equal(9900.00m, read.RegularAmount);
        Assert.Equal("Seven nights for the price of six.", read.Promotion);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_room_is_sold_out_reads_back_no_price_and_nothing_that_was_not_read()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            AddLookupAndCatalog(context);
            var result = Result(ResultId);
            result.IsSoldOut = true;
            context.RoomResults.Add(result);
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.RoomResults.SingleAsync();

        Assert.True(read.IsSoldOut);
        Assert.Null(read.RoomLevelId);
        Assert.Null(read.RoomViewId);
        Assert.Null(read.RoomTypeId);
        Assert.Null(read.RemainingRoomCount);
        Assert.Null(read.TotalAmount);
        Assert.Null(read.RegularAmount);
        Assert.Null(read.Promotion);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_lookup_finds_the_same_room_category_twice_saves_both()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddLookupAndCatalog(context);
        context.RoomResults.Add(Result(ResultId));
        context.RoomResults.Add(Result(OtherResultId));

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.RoomResults.CountAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_room_result_feature_is_added_reads_back_every_value()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            AddLookupAndCatalog(context);
            context.RoomResults.Add(Result(ResultId));
            context.RoomResultFeatures.Add(new RoomResultFeature { Id = RowId, RoomResultId = ResultId, VocabularyValueId = FeatureId });
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.RoomResultFeatures.SingleAsync();

        Assert.Equal(RowId, read.Id);
        Assert.Equal(ResultId, read.RoomResultId);
        Assert.Equal(FeatureId, read.VocabularyValueId);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_result_names_a_feature_twice_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddLookupAndCatalog(context);
        context.RoomResults.Add(Result(ResultId));
        context.RoomResultFeatures.Add(new RoomResultFeature { Id = RowId, RoomResultId = ResultId, VocabularyValueId = FeatureId });
        context.RoomResultFeatures.Add(new RoomResultFeature { Id = OtherRowId, RoomResultId = ResultId, VocabularyValueId = FeatureId });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_two_results_name_the_same_feature_saves_both()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddLookupAndCatalog(context);
        context.RoomResults.Add(Result(ResultId));
        context.RoomResults.Add(Result(OtherResultId));
        context.RoomResultFeatures.Add(new RoomResultFeature { Id = RowId, RoomResultId = ResultId, VocabularyValueId = FeatureId });
        context.RoomResultFeatures.Add(new RoomResultFeature { Id = OtherRowId, RoomResultId = OtherResultId, VocabularyValueId = FeatureId });

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.RoomResultFeatures.CountAsync());
    }



    [Theory]
    [InlineData("lookup")]
    [InlineData("category")]
    [InlineData("level")]
    [InlineData("view")]
    [InlineData("type")]
    public async Task SaveChangesAsync_when_a_room_result_names_a_row_that_does_not_exist_throws_DbUpdateException(string which)
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddLookupAndCatalog(context);
        var result = Result(ResultId);

        switch (which)
        {
            case "lookup":
                result.SearchRequestId = MissingId;
                break;
            case "category":
                result.RoomCategoryId = MissingId;
                break;
            case "level":
                result.RoomLevelId = MissingId;
                break;
            case "view":
                result.RoomViewId = MissingId;
                break;
            default:
                result.RoomTypeId = MissingId;
                break;
        }

        context.RoomResults.Add(result);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SaveChangesAsync_when_a_room_result_feature_names_a_row_that_does_not_exist_throws_DbUpdateException(bool missingResult)
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddLookupAndCatalog(context);
        context.RoomResults.Add(Result(ResultId));
        context.RoomResultFeatures.Add
        (
            new RoomResultFeature
            {
                Id = RowId,
                RoomResultId = missingResult ? MissingId : ResultId,
                VocabularyValueId = missingResult ? FeatureId : MissingId,
            }
        );

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_lookup_that_has_results_is_deleted_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            AddLookupAndCatalog(context);
            context.RoomResults.Add(Result(ResultId));
            await context.SaveChangesAsync();
        }

        await using var deleter = database.NewContext();
        deleter.SearchRequests.Remove(await deleter.SearchRequests.SingleAsync());

        await Assert.ThrowsAsync<DbUpdateException>(() => deleter.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_result_that_has_features_is_deleted_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            AddLookupAndCatalog(context);
            context.RoomResults.Add(Result(ResultId));
            context.RoomResultFeatures.Add(new RoomResultFeature { Id = RowId, RoomResultId = ResultId, VocabularyValueId = FeatureId });
            await context.SaveChangesAsync();
        }

        await using var deleter = database.NewContext();
        deleter.RoomResults.Remove(await deleter.RoomResults.SingleAsync());

        await Assert.ThrowsAsync<DbUpdateException>(() => deleter.SaveChangesAsync());
    }



    private static void AddLookupAndCatalog(BarefootDbContext context)
    {
        context.Resorts.Add
        (
            new Resort
            {
                Id = ResortId,
                Code = "RES1",
                Name = "Example Bay Resort",
                Brand = Brand.Sandals,
                Island = "Example Island",
                FirstSeenUtc = Instant,
            }
        );
        context.RoomCategories.Add
        (
            new RoomCategory
            {
                Id = CategoryId,
                ResortId = ResortId,
                Code = "OV1",
                Name = "Ocean View Room",
                FirstSeenUtc = Instant,
                LastSeenUtc = Instant,
            }
        );
        context.VocabularyValues.AddRange
        (
            Vocabulary(LevelId, VocabularyKind.RoomLevel, "Club"),
            Vocabulary(ViewId, VocabularyKind.RoomView, "Ocean View"),
            Vocabulary(TypeId, VocabularyKind.RoomType, "Suite"),
            Vocabulary(FeatureId, VocabularyKind.RoomFeature, "Balcony")
        );
        context.Runs.Add(new Run { Id = RunId, Trigger = "schedule", Status = RunStatus.InProgress, StartedUtc = Instant });
        context.SearchRequests.Add
        (
            new SearchRequest
            {
                Id = RequestId,
                RunId = RunId,
                ResortId = ResortId,
                CheckIn = new DateOnly(2027, 1, 9),
                CheckOut = new DateOnly(2027, 1, 16),
                AdultCount = 2,
                RunOrder = 5,
                Status = WorkStatus.Succeeded,
            }
        );
    }



    private static VocabularyValue Vocabulary(Guid id, VocabularyKind kind, string value)
    {
        return new VocabularyValue { Id = id, Kind = kind, Value = value, FirstSeenUtc = Instant, LastSeenUtc = Instant };
    }



    private static RoomResult Result(Guid id)
    {
        return new RoomResult { Id = id, SearchRequestId = RequestId, RoomCategoryId = CategoryId };
    }
}
