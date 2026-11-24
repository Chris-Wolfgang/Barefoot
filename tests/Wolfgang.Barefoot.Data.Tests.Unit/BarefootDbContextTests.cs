using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Wolfgang.Barefoot.Contracts.Issues;
using Wolfgang.Barefoot.Core.Entities;
using Wolfgang.Barefoot.Data.Conventions;

namespace Wolfgang.Barefoot.Data.Tests.Unit;

public sealed class BarefootDbContextTests
{
    private static readonly Guid UserOid = Guid.Parse("7a1c2f0e-5b3d-4c8a-9e6f-1d2c3b4a5968");

    private static readonly Guid OtherOid = Guid.Parse("0b9d8c7e-6f5a-4b3c-8d2e-1f0a9b8c7d6e");

    private static readonly Guid RowId = Guid.Parse("3e4f5a6b-7c8d-4e9f-a0b1-c2d3e4f5a6b7");

    private static readonly Guid OtherRowId = Guid.Parse("9f8e7d6c-5b4a-4392-8171-6f5e4d3c2b1a");

    private static readonly DateTimeOffset Instant = new(2026, 11, 4, 6, 40, 0, 123, TimeSpan.Zero);



    [Fact]
    public async Task Model_when_built_names_every_table_in_snake_case_and_singular()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();

        Assert.Equal
        (
            ["airport", "app_user", "open_issue", "recipient", "resort", "resort_span", "room_category", "setting", "vocabulary_value"],
            context.Model.GetEntityTypes().Select(entity => entity.GetTableName()).Order(StringComparer.Ordinal)
        );
    }



    [Fact]
    public async Task Model_when_built_names_every_key_foreign_key_and_index_with_its_prefix()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        var entities = context.Model.GetEntityTypes().ToList();

        Assert.Equal
        (
            [
                "pk_airport",
                "pk_app_user",
                "pk_open_issue",
                "pk_recipient",
                "pk_resort",
                "pk_resort_span",
                "pk_room_category",
                "pk_setting",
                "pk_vocabulary_value",
            ],
            entities.SelectMany(entity => entity.GetKeys()).Select(key => key.GetName()).Order(StringComparer.Ordinal)
        );
        Assert.Equal
        (
            [
                "fk_open_issue_user_id",
                "fk_recipient_owner_id",
                "fk_resort_gateway_airport_id",
                "fk_resort_span_resort_id",
                "fk_room_category_resort_id",
            ],
            entities.SelectMany(entity => entity.GetForeignKeys()).Select(key => key.GetConstraintName()).Order(StringComparer.Ordinal)
        );
        Assert.Equal
        (
            [
                "ix_open_issue_user_id_resolved_utc",
                "ix_resort_gateway_airport_id",
                "ix_resort_span_resort_id_eff_date",
                "ux_airport_code",
                "ux_recipient_owner_id_address",
                "ux_resort_code",
                "ux_room_category_resort_id_code",
                "ux_vocabulary_value_kind_value",
            ],
            entities.SelectMany(entity => entity.GetIndexes()).Select(index => index.GetDatabaseName()).Order(StringComparer.Ordinal)
        );
    }



    [Fact]
    public async Task Model_when_built_restricts_the_delete_of_every_foreign_key()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();

        Assert.All
        (
            context.Model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys()),
            foreignKey => Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior)
        );
    }



    [Fact]
    public async Task Model_when_built_stores_every_instant_as_a_utc_date_time_to_the_millisecond()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        var instants = context
            .Model
            .GetEntityTypes()
            .SelectMany(entity => entity.GetProperties())
            .Where(property => (Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType) == typeof(DateTimeOffset))
            .ToList();

        Assert.Equal(13, instants.Count);
        Assert.All
        (
            instants,
            property =>
            {
                Assert.IsType<UtcInstantConverter>(property.GetValueConverter());
                Assert.Equal(BarefootModel.InstantPrecision, property.GetPrecision());
            }
        );
    }



    [Fact]
    public async Task Model_when_built_makes_Version_a_concurrency_token_wherever_it_appears()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        var versions = context
            .Model
            .GetEntityTypes()
            .Select(entity => entity.FindProperty("Version"))
            .OfType<IProperty>()
            .ToList();

        Assert.Equal(5, versions.Count);
        Assert.All(versions, property => Assert.True(property.IsConcurrencyToken));
    }



    [Fact]
    public async Task SaveChangesAsync_when_an_app_user_is_added_reads_back_every_value()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            var user = User(UserOid, "pat@example.com");
            user.DisclaimerVersionAccepted = 2;
            user.DisclaimerAcceptedUtc = Instant.AddDays(-30);
            user.TimeZoneId = "America/New_York";
            user.DefaultAdultCount = 2;
            user.DefaultChildCount = 1;
            user.DefaultInfantCount = 0;
            user.SessionsValidAfterUtc = Instant.AddDays(-1);
            user.Version = 4;
            context.AppUsers.Add(user);
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.AppUsers.SingleAsync();

        Assert.Equal(UserOid, read.Oid);
        Assert.Equal("Pat Example", read.DisplayName);
        Assert.Equal("pat@example.com", read.Email);
        Assert.Equal(Instant, read.LastSeenUtc);
        Assert.Equal(TimeSpan.Zero, read.LastSeenUtc.Offset);
        Assert.Equal(2, read.DisclaimerVersionAccepted);
        Assert.Equal(Instant.AddDays(-30), read.DisclaimerAcceptedUtc);
        Assert.Equal("America/New_York", read.TimeZoneId);
        Assert.Equal(2, read.DefaultAdultCount);
        Assert.Equal(1, read.DefaultChildCount);
        Assert.Equal(0, read.DefaultInfantCount);
        Assert.Equal(Instant.AddDays(-1), read.SessionsValidAfterUtc);
        Assert.Equal(4, read.Version);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_setting_is_added_reads_it_back_by_its_key()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            context.Settings.Add(new Setting { Key = "digest.max_recipients_per_owner", Value = "5", UpdatedUtc = Instant, Version = 1 });
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.Settings.FindAsync("digest.max_recipients_per_owner");

        Assert.NotNull(read);
        Assert.Equal("5", read.Value);
        Assert.Equal(Instant, read.UpdatedUtc);
        Assert.Equal(1, read.Version);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_recipient_is_added_reads_back_every_value()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            context.AppUsers.Add(User(UserOid, "pat@example.com"));
            context.Recipients.Add(Recipient(RowId, UserOid, "sam@example.com"));
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.Recipients.SingleAsync();

        Assert.Equal(RowId, read.Id);
        Assert.Equal(UserOid, read.OwnerId);
        Assert.Equal("sam@example.com", read.Address);
        Assert.Equal("Sam Example", read.DisplayName);
        Assert.Equal(1, read.Version);
    }



    [Fact]
    public async Task SaveChangesAsync_when_an_open_issue_is_added_reads_back_every_value()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            context.AppUsers.Add(User(UserOid, "pat@example.com"));
            context.OpenIssues.Add
            (
                new OpenIssue
                {
                    Id = RowId,
                    UserId = UserOid,
                    Kind = IssueKind.WindowPassed,
                    Subject = "Winter week",
                    ResolvesWhen = "You give the search a window that has not passed.",
                    TargetId = OtherRowId,
                    RaisedUtc = Instant,
                    AcknowledgedUtc = Instant.AddHours(1),
                    ResolvedUtc = Instant.AddHours(2),
                }
            );
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.OpenIssues.SingleAsync();

        Assert.Equal(RowId, read.Id);
        Assert.Equal(UserOid, read.UserId);
        Assert.Equal(IssueKind.WindowPassed, read.Kind);
        Assert.Equal("Winter week", read.Subject);
        Assert.Equal("You give the search a window that has not passed.", read.ResolvesWhen);
        Assert.Equal(OtherRowId, read.TargetId);
        Assert.Equal(Instant, read.RaisedUtc);
        Assert.Equal(Instant.AddHours(1), read.AcknowledgedUtc);
        Assert.Equal(Instant.AddHours(2), read.ResolvedUtc);
    }



    [Fact]
    public async Task SaveChangesAsync_when_an_owner_has_the_same_address_twice_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        context.AppUsers.Add(User(UserOid, "pat@example.com"));
        context.Recipients.Add(Recipient(RowId, UserOid, "sam@example.com"));
        context.Recipients.Add(Recipient(OtherRowId, UserOid, "sam@example.com"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_two_owners_have_the_same_address_saves_both()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        context.AppUsers.Add(User(UserOid, "pat@example.com"));
        context.AppUsers.Add(User(OtherOid, "lee@example.com"));
        context.Recipients.Add(Recipient(RowId, UserOid, "sam@example.com"));
        context.Recipients.Add(Recipient(OtherRowId, OtherOid, "sam@example.com"));

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.Recipients.CountAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_recipient_names_an_owner_that_does_not_exist_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        context.Recipients.Add(Recipient(RowId, UserOid, "sam@example.com"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_user_who_owns_a_recipient_is_deleted_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            context.AppUsers.Add(User(UserOid, "pat@example.com"));
            context.Recipients.Add(Recipient(RowId, UserOid, "sam@example.com"));
            await context.SaveChangesAsync();
        }

        await using var deleter = database.NewContext();
        deleter.AppUsers.Remove(await deleter.AppUsers.SingleAsync());

        await Assert.ThrowsAsync<DbUpdateException>(() => deleter.SaveChangesAsync());
    }



    [Fact]
    public async Task ToListAsync_when_ordered_and_filtered_by_an_instant_sorts_and_compares_in_the_database()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            var late = User(UserOid, "pat@example.com");
            late.LastSeenUtc = Instant.AddMilliseconds(2);
            var early = User(OtherOid, "lee@example.com");
            early.LastSeenUtc = Instant.AddMilliseconds(1);
            var before = User(RowId, "kim@example.com");
            before.LastSeenUtc = Instant.AddDays(-1);
            context.AppUsers.AddRange(late, early, before);
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var seen = await reader
            .AppUsers
            .Where(user => user.LastSeenUtc > Instant)
            .OrderBy(user => user.LastSeenUtc)
            .Select(user => user.Email)
            .ToListAsync();

        Assert.Equal(["lee@example.com", "pat@example.com"], seen);
    }



    private static AppUser User(Guid oid, string email)
    {
        return new AppUser { Oid = oid, DisplayName = "Pat Example", Email = email, LastSeenUtc = Instant };
    }



    private static Recipient Recipient(Guid id, Guid ownerId, string address)
    {
        return new Recipient { Id = id, OwnerId = ownerId, Address = address, DisplayName = "Sam Example", Version = 1 };
    }
}
