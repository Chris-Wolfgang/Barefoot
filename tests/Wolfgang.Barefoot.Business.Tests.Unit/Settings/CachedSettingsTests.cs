using Microsoft.Extensions.Time.Testing;
using Wolfgang.Barefoot.Business.Settings;
using Wolfgang.Barefoot.Core.Keys;
using Wolfgang.Barefoot.TestKit;

namespace Wolfgang.Barefoot.Business.Tests.Unit.Settings;

/// <summary>
/// Reading a setting through the instance's cache (ADR 0011 §4; docs/SETTINGS.md, How code reads a
/// setting). The clock is a fake one, so the 30 seconds pass when a test says so.
/// </summary>
public sealed class CachedSettingsTests : IDisposable
{
    private static readonly SettingKey<int> Lease = new
    (
        "sample.lease_seconds",
        300,
        "How long a sample lease lasts.",
        value => value >= 60 ? null : "A lease lasts at least 60 seconds."
    );

    private static readonly SettingKey<string> Sender = new("sample.sender", "Barefoot", "Who a sample is from.");

    private readonly InMemorySettingStore _store = new();

    private readonly FakeTimeProvider _clock = TestClock.Create();

    private readonly SettingCache _cache = new();

    private readonly CachedSettings _settings;



    public CachedSettingsTests()
    {
        _settings = new CachedSettings(_cache, _store, _clock);
    }



    /// <inheritdoc />
    public void Dispose()
    {
        _cache.Dispose();
    }



    [Fact]
    public async Task GetAsync_when_nothing_is_stored_gives_the_default()
    {
        Assert.Equal(300, await GetAsync(Lease));
        Assert.Equal("Barefoot", await GetAsync(Sender));
    }



    [Fact]
    public async Task GetAsync_when_a_value_is_stored_gives_it()
    {
        Store(Lease, "120");
        Store(Sender, "Sample Sender");

        Assert.Equal(120, await GetAsync(Lease));
        Assert.Equal("Sample Sender", await GetAsync(Sender));
    }



    [Fact]
    public async Task GetAsync_when_the_stored_text_is_not_a_value_of_the_type_gives_the_default()
    {
        Store(Lease, "soon");

        Assert.Equal(300, await GetAsync(Lease));
    }



    [Fact]
    public async Task GetAsync_when_the_validator_rejects_the_stored_value_gives_the_default()
    {
        Store(Lease, "59");

        Assert.Equal(300, await GetAsync(Lease));
    }



    [Fact]
    public async Task GetAsync_when_read_first_asks_the_store_once_for_the_generation_and_once_for_the_values()
    {
        await GetAsync(Lease);

        Assert.Equal(1, _store.ProbeCount);
        Assert.Equal(1, _store.ReadCount);
    }



    [Fact]
    public async Task GetAsync_when_read_again_within_the_interval_does_not_ask_the_store()
    {
        await GetAsync(Lease);
        _clock.Advance(SettingCache.ProbeInterval - TimeSpan.FromTicks(1));

        await GetAsync(Lease);
        await GetAsync(Sender);

        Assert.Equal(1, _store.ProbeCount);
        Assert.Equal(1, _store.ReadCount);
    }



    [Fact]
    public async Task GetAsync_when_the_interval_has_passed_and_nothing_changed_asks_only_for_the_generation()
    {
        Store(Lease, "120");
        await GetAsync(Lease);
        _clock.Advance(SettingCache.ProbeInterval);

        Assert.Equal(120, await GetAsync(Lease));

        Assert.Equal(2, _store.ProbeCount);
        Assert.Equal(1, _store.ReadCount);
    }



    [Fact]
    public async Task GetAsync_when_a_value_changes_within_the_interval_still_gives_the_old_one()
    {
        Store(Lease, "120");
        await GetAsync(Lease);

        Store(Lease, "180", TimeSpan.FromSeconds(1));
        _clock.Advance(TimeSpan.FromSeconds(29));

        Assert.Equal(120, await GetAsync(Lease));
    }



    [Fact]
    public async Task GetAsync_when_a_value_changes_gives_the_new_one_once_the_interval_has_passed()
    {
        Store(Lease, "120");
        await GetAsync(Lease);

        Store(Lease, "180", TimeSpan.FromSeconds(1));
        _clock.Advance(SettingCache.ProbeInterval);

        Assert.Equal(180, await GetAsync(Lease));
        Assert.Equal(2, _store.ReadCount);
    }



    [Fact]
    public async Task GetAsync_when_a_value_is_reset_gives_the_default_once_the_interval_has_passed()
    {
        Store(Lease, "120");
        await GetAsync(Lease);

        _store.Reset(Lease.Name);
        _clock.Advance(SettingCache.ProbeInterval);

        Assert.Equal(300, await GetAsync(Lease));
    }



    [Fact]
    public async Task GetAsync_when_the_interval_passes_twice_asks_for_the_generation_each_time()
    {
        await GetAsync(Lease);
        _clock.Advance(SettingCache.ProbeInterval);
        await GetAsync(Lease);
        _clock.Advance(SettingCache.ProbeInterval - TimeSpan.FromTicks(1));
        await GetAsync(Lease);

        Assert.Equal(2, _store.ProbeCount);

        _clock.Advance(TimeSpan.FromTicks(1));
        await GetAsync(Lease);

        Assert.Equal(3, _store.ProbeCount);
    }



    [Fact]
    public async Task Invalidate_when_called_makes_the_next_read_see_a_change_at_once()
    {
        Store(Lease, "120");
        await GetAsync(Lease);

        Store(Lease, "180", TimeSpan.FromSeconds(1));
        _cache.Invalidate();

        Assert.Equal(180, await GetAsync(Lease));
    }



    [Fact]
    public async Task Invalidate_when_the_generation_did_not_change_still_reads_the_values_again()
    {
        Store(Lease, "120");
        await GetAsync(Lease);

        _cache.Invalidate();
        await GetAsync(Lease);

        Assert.Equal(2, _store.ReadCount);
    }



    [Fact]
    public async Task GetAsync_when_two_readers_share_a_cache_reads_the_store_once()
    {
        var other = new CachedSettings(_cache, _store, _clock);
        Store(Lease, "120");

        Assert.Equal(120, await GetAsync(Lease));
        Assert.Equal(120, await other.GetAsync(Lease, CancellationToken.None));

        Assert.Equal(1, _store.ReadCount);
    }



    [Fact]
    public async Task GetAsync_when_cancelled_before_the_store_is_read_throws_and_reads_nothing()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>
        (
            async () => await _settings.GetAsync(Lease, cancellation.Token)
        );

        Assert.Equal(0, _store.ProbeCount);
        Assert.Equal(300, await GetAsync(Lease));
    }



    [Fact]
    public async Task GetAsync_when_the_type_is_not_one_a_setting_may_have_throws()
    {
        var price = new SettingKey<decimal>("sample.price", 1m, "A sample price.");

        await Assert.ThrowsAsync<NotSupportedException>(async () => await _settings.GetAsync(price, CancellationToken.None));
    }



    [Fact]
    public async Task GetAsync_when_given_no_key_throws()
    {
        var exception = await Assert.ThrowsAsync<ArgumentNullException>
        (
            async () => await _settings.GetAsync<int>(null!, CancellationToken.None)
        );

        Assert.Equal("key", exception.ParamName);
    }



    [Fact]
    public void Constructor_when_given_no_cache_throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new CachedSettings(null!, _store, _clock));

        Assert.Equal("cache", exception.ParamName);
    }



    [Fact]
    public void Constructor_when_given_no_store_throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new CachedSettings(_cache, null!, _clock));

        Assert.Equal("store", exception.ParamName);
    }



    [Fact]
    public void Constructor_when_given_no_clock_throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new CachedSettings(_cache, _store, null!));

        Assert.Equal("clock", exception.ParamName);
    }



    [Fact]
    public async Task Dispose_when_called_makes_a_cache_that_holds_nothing_unusable()
    {
        var cache = new SettingCache();
        cache.Dispose();

        var settings = new CachedSettings(cache, _store, _clock);

        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await settings.GetAsync(Lease, CancellationToken.None));
    }



    [Fact]
    public void ProbeInterval_when_read_is_thirty_seconds()
    {
        Assert.Equal(TimeSpan.FromSeconds(30), SettingCache.ProbeInterval);
    }



    private async Task<T> GetAsync<T>(SettingKey<T> key)
        where T : notnull
    {
        return await _settings.GetAsync(key, CancellationToken.None);
    }



    /// <summary>
    /// Stores a value as changed at the clock's start plus <paramref name="after"/>.
    /// </summary>
    private void Store<T>(SettingKey<T> key, string text, TimeSpan after = default)
        where T : notnull
    {
        _store.Set(key.Name, text, TestClock.DefaultStart + after);
    }
}
