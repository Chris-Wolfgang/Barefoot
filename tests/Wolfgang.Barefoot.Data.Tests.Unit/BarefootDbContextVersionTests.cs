using Microsoft.EntityFrameworkCore;
using Wolfgang.Barefoot.Contracts.Issues;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Tests.Unit;

/// <summary>
/// The <c>Version</c> that <see cref="BarefootDbContext"/> gives the rows it saves, and what the
/// database does with a save that was made from an older version.
/// </summary>
public sealed class BarefootDbContextVersionTests
{
    private const string Key = "digest.max_recipients_per_owner";

    private const string OtherKey = "run.max_parallel_requests";

    private static readonly Guid UserOid = Guid.Parse("6a718293-a4b5-4c6d-97ef-8091a2b3c4d5");

    private static readonly Guid RowId = Guid.Parse("7b8293a4-b5c6-4d7e-a8f0-91a2b3c4d5e6");

    private static readonly Guid OtherRowId = Guid.Parse("8c93a4b5-c6d7-4e8f-b901-a2b3c4d5e6f7");

    private static readonly DateTimeOffset Instant = new(2026, 11, 4, 6, 40, 0, 123, TimeSpan.Zero);



    [Fact]
    public async Task SaveChangesAsync_when_a_versioned_row_is_added_stores_version_1_whatever_the_caller_set()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            var setting = Setting(Key, "5");
            setting.Version = 9;
            context.Settings.Add(setting);
            await context.SaveChangesAsync();

            Assert.Equal(1, setting.Version);
        }

        Assert.Equal(1, await StoredVersionAsync(database, Key));
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_versioned_row_is_changed_adds_one_to_its_version_each_time()
    {
        await using var database = await TestDatabase.CreateAsync();
        await AddSettingAsync(database, Key);

        await ChangeSettingAsync(database, Key, "6");
        var afterFirstChange = await StoredVersionAsync(database, Key);
        await ChangeSettingAsync(database, Key, "7");
        var afterSecondChange = await StoredVersionAsync(database, Key);

        Assert.Equal(2, afterFirstChange);
        Assert.Equal(3, afterSecondChange);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_changed_row_was_given_a_version_by_the_caller_counts_from_the_version_it_was_read_with()
    {
        await using var database = await TestDatabase.CreateAsync();
        await AddSettingAsync(database, Key);

        await using (var context = database.NewContext())
        {
            var setting = await context.Settings.SingleAsync();
            setting.Value = "6";
            setting.Version = 40;
            await context.SaveChangesAsync();
        }

        Assert.Equal(2, await StoredVersionAsync(database, Key));
    }



    [Fact]
    public async Task SaveChangesAsync_when_one_row_is_changed_leaves_the_version_of_an_unchanged_row()
    {
        await using var database = await TestDatabase.CreateAsync();
        await AddSettingAsync(database, Key);
        await AddSettingAsync(database, OtherKey);

        await using (var context = database.NewContext())
        {
            var settings = await context.Settings.ToListAsync();
            settings.Single(setting => string.Equals(setting.Key, Key, StringComparison.Ordinal)).Value = "6";
            await context.SaveChangesAsync();
        }

        Assert.Equal(2, await StoredVersionAsync(database, Key));
        Assert.Equal(1, await StoredVersionAsync(database, OtherKey));
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_row_without_a_version_is_changed_saves_it()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            context.AppUsers.Add(User());
            context.OpenIssues.Add(Issue());
            await context.SaveChangesAsync();
        }

        await using (var context = database.NewContext())
        {
            var issue = await context.OpenIssues.SingleAsync();
            issue.AcknowledgedUtc = Instant.AddHours(1);
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.OpenIssues.SingleAsync();

        Assert.Equal(Instant.AddHours(1), read.AcknowledgedUtc);
    }



    [Fact]
    public async Task SaveChangesAsync_when_the_row_changed_since_it_was_read_throws_DbUpdateConcurrencyException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await AddSettingAsync(database, Key);
        await using var stale = database.NewContext();
        var staleSetting = await stale.Settings.SingleAsync();
        await ChangeSettingAsync(database, Key, "6");
        staleSetting.Value = "7";

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => stale.SaveChangesAsync());

        await using var reader = database.NewContext();
        var read = await reader.Settings.SingleAsync();

        Assert.Equal("6", read.Value);
        Assert.Equal(2, read.Version);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_row_to_delete_changed_since_it_was_read_throws_DbUpdateConcurrencyException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await AddSettingAsync(database, Key);
        await using var stale = database.NewContext();
        var staleSetting = await stale.Settings.SingleAsync();
        await ChangeSettingAsync(database, Key, "6");
        stale.Settings.Remove(staleSetting);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => stale.SaveChangesAsync());

        Assert.Equal(2, await StoredVersionAsync(database, Key));
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_row_is_deleted_at_the_version_it_was_read_with_deletes_it()
    {
        await using var database = await TestDatabase.CreateAsync();
        await AddSettingAsync(database, Key);

        await using (var context = database.NewContext())
        {
            var setting = await context.Settings.SingleAsync();
            context.Settings.Remove(setting);
            await context.SaveChangesAsync();

            Assert.Equal(1, setting.Version);
        }

        await using var reader = database.NewContext();

        Assert.Empty(await reader.Settings.ToListAsync());
    }



    [Theory]
    [InlineData(2, true)]
    [InlineData(1, false)]
    [InlineData(3, false)]
    public async Task SaveChangesAsync_when_the_caller_states_the_version_it_expects_saves_only_if_the_row_has_it
    (
        long expectedVersion,
        bool saves
    )
    {
        await using var database = await TestDatabase.CreateAsync();
        await AddSettingAsync(database, Key);
        await ChangeSettingAsync(database, Key, "6");
        await using var context = database.NewContext();
        var setting = await context.Settings.SingleAsync();
        context.Entry(setting).Property(row => row.Version).OriginalValue = expectedVersion;
        setting.Value = "7";

        var failure = await Record.ExceptionAsync(async () => await context.SaveChangesAsync());

        Assert.Equal(saves, failure is null);
        Assert.Equal(!saves, failure is DbUpdateConcurrencyException);
        Assert.Equal(saves ? 3 : 2, await StoredVersionAsync(database, Key));
    }



    [Fact]
    public async Task SaveChangesAsync_when_saved_again_after_a_failed_save_adds_one_and_not_two()
    {
        await using var database = await TestDatabase.CreateAsync();
        await AddSettingAsync(database, Key);

        await using (var context = database.NewContext())
        {
            var setting = await context.Settings.SingleAsync();
            setting.Value = "6";
            context.AppUsers.Add(User());
            context.Recipients.Add(Recipient(RowId));
            var duplicate = Recipient(OtherRowId);
            context.Recipients.Add(duplicate);
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

            context.Recipients.Remove(duplicate);
            await context.SaveChangesAsync();
        }

        Assert.Equal(2, await StoredVersionAsync(database, Key));
    }



    [Fact]
    public async Task SaveChanges_when_a_versioned_row_is_added_and_then_changed_stores_versions_1_and_2()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        var setting = Setting(Key, "5");
        context.Settings.Add(setting);

        var rowsAdded = Save(context);
        var versionWhenAdded = setting.Version;
        setting.Value = "6";
        var rowsChanged = Save(context);

        Assert.Equal(1, rowsAdded);
        Assert.Equal(1, versionWhenAdded);
        Assert.Equal(1, rowsChanged);
        Assert.Equal(2, await StoredVersionAsync(database, Key));
    }



    private static int Save(BarefootDbContext context)
    {
        return context.SaveChanges();
    }



    private static async Task AddSettingAsync(TestDatabase database, string key)
    {
        await using var context = database.NewContext();
        context.Settings.Add(Setting(key, "5"));
        await context.SaveChangesAsync();
    }



    private static async Task ChangeSettingAsync(TestDatabase database, string key, string value)
    {
        await using var context = database.NewContext();
        var setting = await context.Settings.FindAsync(key);
        Assert.NotNull(setting);
        setting.Value = value;
        await context.SaveChangesAsync();
    }



    private static async Task<long> StoredVersionAsync(TestDatabase database, string key)
    {
        await using var context = database.NewContext();

        var setting = await context.Settings.FindAsync(key);
        Assert.NotNull(setting);

        return setting.Version;
    }



    private static Setting Setting(string key, string value)
    {
        return new Setting { Key = key, Value = value, UpdatedUtc = Instant };
    }



    private static AppUser User()
    {
        return new AppUser { Oid = UserOid, DisplayName = "Pat Example", Email = "pat@example.com", LastSeenUtc = Instant };
    }



    private static Recipient Recipient(Guid id)
    {
        return new Recipient { Id = id, OwnerId = UserOid, Address = "sam@example.com", DisplayName = "Sam Example" };
    }



    private static OpenIssue Issue()
    {
        return new OpenIssue
        {
            Id = RowId,
            UserId = UserOid,
            Kind = IssueKind.WindowPassed,
            Subject = "Winter week",
            ResolvesWhen = "You give the search a window that has not passed.",
            RaisedUtc = Instant,
        };
    }
}
