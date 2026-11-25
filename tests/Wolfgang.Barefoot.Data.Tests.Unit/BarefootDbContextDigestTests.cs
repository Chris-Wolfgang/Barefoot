using Microsoft.EntityFrameworkCore;
using Wolfgang.Barefoot.Contracts.Digests;
using Wolfgang.Barefoot.Contracts.Runs;
using Wolfgang.Barefoot.Contracts.Searches;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Tests.Unit;

/// <summary>
/// The <c>digest_delivery</c> table of <see cref="BarefootDbContext"/>: one stored digest for one
/// recipient of one search's run.
/// </summary>
public sealed class BarefootDbContextDigestTests
{
    private static readonly Guid OwnerOid = Guid.Parse("c3d4e5f6-0001-4c7d-8e9f-90a1b2c3d4e5");

    private static readonly Guid SearchId = Guid.Parse("c3d4e5f6-0002-4c7d-8e9f-90a1b2c3d4e5");

    private static readonly Guid RunId = Guid.Parse("c3d4e5f6-0003-4c7d-8e9f-90a1b2c3d4e5");

    private static readonly Guid MatrixRunId = Guid.Parse("c3d4e5f6-0004-4c7d-8e9f-90a1b2c3d4e5");

    private static readonly Guid RowId = Guid.Parse("c3d4e5f6-0005-4c7d-8e9f-90a1b2c3d4e5");

    private static readonly Guid OtherRowId = Guid.Parse("c3d4e5f6-0006-4c7d-8e9f-90a1b2c3d4e5");

    private static readonly Guid ThirdRowId = Guid.Parse("c3d4e5f6-0007-4c7d-8e9f-90a1b2c3d4e5");

    private static readonly Guid MissingId = Guid.Parse("c3d4e5f6-0008-4c7d-8e9f-90a1b2c3d4e5");

    private static readonly DateTimeOffset Instant = new(2026, 11, 4, 6, 40, 0, 123, TimeSpan.Zero);



    [Fact]
    public async Task SaveChangesAsync_when_a_digest_delivery_is_added_reads_back_every_value()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            AddSearchRun(context);
            var delivery = Delivery(RowId, "pat@example.com");
            delivery.RecipientUserId = OwnerOid;
            delivery.Status = DeliveryStatus.Failed;
            delivery.AttemptCount = 3;
            delivery.LastError = "The mail service answered 429.";
            delivery.SentUtc = Instant.AddMinutes(4);
            context.DigestDeliveries.Add(delivery);
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.DigestDeliveries.SingleAsync();

        Assert.Equal(RowId, read.Id);
        Assert.Equal(MatrixRunId, read.MatrixRunId);
        Assert.Equal(OwnerOid, read.RecipientUserId);
        Assert.Equal("pat@example.com", read.RecipientAddress);
        Assert.Equal("Winter week: 3 price drops", read.Subject);
        Assert.Equal("<p>Three prices dropped.</p>", read.HtmlBody);
        Assert.Equal("Three prices dropped.", read.TextBody);
        Assert.Equal(DeliveryStatus.Failed, read.Status);
        Assert.Equal(3, read.AttemptCount);
        Assert.Equal("The mail service answered 429.", read.LastError);
        Assert.Equal(Instant, read.CreatedUtc);
        Assert.Equal(Instant.AddMinutes(4), read.SentUtc);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_digest_for_an_extra_recipient_has_just_been_built_reads_back_a_digest_that_waits()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            AddSearchRun(context);
            context.DigestDeliveries.Add(Delivery(RowId, "sam@example.com"));
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.DigestDeliveries.SingleAsync();

        Assert.Null(read.RecipientUserId);
        Assert.Equal(DeliveryStatus.Pending, read.Status);
        Assert.Equal(0, read.AttemptCount);
        Assert.Null(read.LastError);
        Assert.Null(read.SentUtc);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_digest_body_is_far_longer_than_any_other_text_reads_it_back_whole()
    {
        var body = new string('x', 60_000);
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            AddSearchRun(context);
            var delivery = Delivery(RowId, "pat@example.com");
            delivery.HtmlBody = body;
            delivery.TextBody = body;
            context.DigestDeliveries.Add(delivery);
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.DigestDeliveries.SingleAsync();

        Assert.Equal(body, read.HtmlBody);
        Assert.Equal(body, read.TextBody);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_run_has_a_digest_for_each_of_two_recipients_saves_both()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddSearchRun(context);
        context.DigestDeliveries.Add(Delivery(RowId, "pat@example.com"));
        context.DigestDeliveries.Add(Delivery(OtherRowId, "sam@example.com"));

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.DigestDeliveries.CountAsync());
    }



    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SaveChangesAsync_when_a_digest_delivery_names_a_row_that_does_not_exist_throws_DbUpdateException(bool missingRun)
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddSearchRun(context);
        var delivery = Delivery(RowId, "pat@example.com");

        if (missingRun)
        {
            delivery.MatrixRunId = MissingId;
        }
        else
        {
            delivery.RecipientUserId = MissingId;
        }

        context.DigestDeliveries.Add(delivery);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_search_run_that_has_digests_is_deleted_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            AddSearchRun(context);
            context.DigestDeliveries.Add(Delivery(RowId, "pat@example.com"));
            await context.SaveChangesAsync();
        }

        await using var deleter = database.NewContext();
        deleter.MatrixRuns.Remove(await deleter.MatrixRuns.SingleAsync());

        await Assert.ThrowsAsync<DbUpdateException>(() => deleter.SaveChangesAsync());
    }



    [Fact]
    public async Task ToListAsync_when_filtered_to_the_digests_that_wait_reads_them_oldest_first_in_the_database()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            AddSearchRun(context);
            var newer = Delivery(RowId, "pat@example.com");
            newer.CreatedUtc = Instant.AddMilliseconds(2);
            var older = Delivery(OtherRowId, "sam@example.com");
            older.CreatedUtc = Instant.AddMilliseconds(1);
            var sent = Delivery(ThirdRowId, "lee@example.com");
            sent.Status = DeliveryStatus.Sent;
            context.DigestDeliveries.AddRange(newer, older, sent);
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var waiting = await reader
            .DigestDeliveries
            .Where(delivery => delivery.Status == DeliveryStatus.Pending)
            .OrderBy(delivery => delivery.CreatedUtc)
            .Select(delivery => delivery.Id)
            .ToListAsync();

        Assert.Equal([OtherRowId, RowId], waiting);
    }



    private static void AddSearchRun(BarefootDbContext context)
    {
        context.AppUsers.Add(new AppUser { Oid = OwnerOid, DisplayName = "Pat Example", Email = "pat@example.com", LastSeenUtc = Instant });
        context.SearchMatrices.Add
        (
            new SearchMatrix
            {
                Id = SearchId,
                OwnerId = OwnerOid,
                Name = "Winter week",
                NormalizedName = SearchMatrix.NormalizeName("Winter week"),
                Brand = Brand.Sandals,
                NightCount = 7,
                AdultCount = 2,
                WindowKind = WindowKind.Recurring,
                CreatedUtc = Instant,
            }
        );
        context.Runs.Add(new Run { Id = RunId, Trigger = "schedule", Status = RunStatus.Complete, StartedUtc = Instant });
        context.MatrixRuns.Add
        (
            new MatrixRun
            {
                Id = MatrixRunId,
                RunId = RunId,
                SearchMatrixId = SearchId,
                FirstCheckIn = new DateOnly(2027, 1, 9),
                LastCheckIn = new DateOnly(2027, 2, 8),
                Stage = SearchRunStage.Aggregated,
            }
        );
    }



    private static DigestDelivery Delivery(Guid id, string address)
    {
        return new DigestDelivery
        {
            Id = id,
            MatrixRunId = MatrixRunId,
            RecipientAddress = address,
            Subject = "Winter week: 3 price drops",
            HtmlBody = "<p>Three prices dropped.</p>",
            TextBody = "Three prices dropped.",
            CreatedUtc = Instant,
        };
    }
}
