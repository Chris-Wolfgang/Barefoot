using Microsoft.EntityFrameworkCore;
using Wolfgang.Barefoot.Core.Entities;
using Wolfgang.Barefoot.Core.Settings;
using Wolfgang.Barefoot.Data.Settings;

namespace Wolfgang.Barefoot.Data.Tests.Unit.Settings;

/// <summary>
/// The stored settings as the reader needs them (ADR 0011 §4), on SQLite.
/// </summary>
public sealed class SettingStoreTests
{
    private static readonly DateTimeOffset Noon = new(2026, 1, 14, 12, 0, 0, TimeSpan.Zero);



    [Fact]
    public async Task ProbeAsync_when_nothing_is_stored_reports_no_rows_and_no_change()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();

        var generation = await new SettingStore(context).ProbeAsync(CancellationToken.None);

        Assert.Equal(new SettingGeneration(0, null), generation);
    }



    [Fact]
    public async Task ProbeAsync_when_values_are_stored_reports_their_number_and_the_newest_change()
    {
        await using var database = await TestDatabase.CreateAsync();
        await AddAsync(database, "sample.one", "1", Noon.AddMinutes(5));
        await AddAsync(database, "sample.two", "2", Noon);
        await AddAsync(database, "sample.three", "3", Noon.AddMinutes(-5));
        await using var context = database.NewContext();

        var generation = await new SettingStore(context).ProbeAsync(CancellationToken.None);

        Assert.Equal(new SettingGeneration(3, Noon.AddMinutes(5)), generation);
        Assert.Equal(TimeSpan.Zero, generation.LastUpdatedUtc!.Value.Offset);
    }



    [Fact]
    public async Task ProbeAsync_when_a_value_is_changed_or_removed_reports_another_generation()
    {
        await using var database = await TestDatabase.CreateAsync();
        await AddAsync(database, "sample.one", "1", Noon);
        await AddAsync(database, "sample.two", "2", Noon);
        await using var context = database.NewContext();
        var store = new SettingStore(context);
        var before = await store.ProbeAsync(CancellationToken.None);

        await using (var writer = database.NewContext())
        {
            var row = await writer.Settings.SingleAsync(setting => setting.Key == "sample.one");
            row.Value = "changed";
            row.UpdatedUtc = Noon.AddSeconds(1);
            await writer.SaveChangesAsync();
        }

        var changed = await store.ProbeAsync(CancellationToken.None);

        await using (var writer = database.NewContext())
        {
            writer.Settings.Remove(await writer.Settings.SingleAsync(setting => setting.Key == "sample.one"));
            await writer.SaveChangesAsync();
        }

        var removed = await store.ProbeAsync(CancellationToken.None);

        Assert.Equal(new SettingGeneration(2, Noon.AddSeconds(1)), changed);
        Assert.Equal(new SettingGeneration(1, Noon), removed);
        Assert.NotEqual(before, changed);
        Assert.NotEqual(changed, removed);
    }



    [Fact]
    public async Task ReadAllAsync_when_nothing_is_stored_gives_nothing()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();

        Assert.Empty(await new SettingStore(context).ReadAllAsync(CancellationToken.None));
    }



    [Fact]
    public async Task ReadAllAsync_when_values_are_stored_gives_each_by_name_and_tracks_nothing()
    {
        await using var database = await TestDatabase.CreateAsync();
        await AddAsync(database, "sample.one", "1", Noon);
        await AddAsync(database, "sample.two", "two words", Noon);
        await using var context = database.NewContext();

        var values = await new SettingStore(context).ReadAllAsync(CancellationToken.None);

        Assert.Equal(2, values.Count);
        Assert.Equal("1", values["sample.one"]);
        Assert.Equal("two words", values["sample.two"]);
        Assert.False(values.ContainsKey("SAMPLE.ONE"));
        Assert.Empty(context.ChangeTracker.Entries());
    }



    [Fact]
    public async Task ProbeAsync_when_cancelled_throws()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>
        (
            () => new SettingStore(context).ProbeAsync(cancellation.Token)
        );
    }



    [Fact]
    public async Task ReadAllAsync_when_cancelled_throws()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>
        (
            () => new SettingStore(context).ReadAllAsync(cancellation.Token)
        );
    }



    [Fact]
    public void Constructor_when_given_no_context_throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new SettingStore(null!));

        Assert.Equal("context", exception.ParamName);
    }



    private static async Task AddAsync
    (
        TestDatabase database,
        string key,
        string value,
        DateTimeOffset updatedUtc
    )
    {
        await using var context = database.NewContext();

        context.Settings.Add(new Setting { Key = key, Value = value, UpdatedUtc = updatedUtc });

        await context.SaveChangesAsync();
    }
}
