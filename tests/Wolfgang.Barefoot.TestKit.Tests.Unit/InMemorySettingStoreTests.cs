using Wolfgang.Barefoot.Core.Settings;

namespace Wolfgang.Barefoot.TestKit.Tests.Unit;

public sealed class InMemorySettingStoreTests
{
    private static readonly DateTimeOffset Noon = TestClock.DefaultStart;



    [Fact]
    public async Task ProbeAsync_when_nothing_is_stored_reports_no_rows_and_no_change()
    {
        var store = new InMemorySettingStore();

        Assert.Equal(new SettingGeneration(0, null), await store.ProbeAsync(CancellationToken.None));
    }



    [Fact]
    public async Task ProbeAsync_when_values_are_stored_reports_their_number_and_the_newest_change()
    {
        var store = new InMemorySettingStore();
        store.Set("a.one", "1", Noon.AddMinutes(5));
        store.Set("a.two", "2", Noon);

        Assert.Equal(new SettingGeneration(2, Noon.AddMinutes(5)), await store.ProbeAsync(CancellationToken.None));
    }



    [Fact]
    public async Task ReadAllAsync_when_values_are_stored_gives_each_by_name()
    {
        var store = new InMemorySettingStore();
        store.Set("a.one", "1", Noon);
        store.Set("a.two", "2", Noon);
        store.Set("a.one", "changed", Noon.AddMinutes(1));

        var values = await store.ReadAllAsync(CancellationToken.None);

        Assert.Equal(2, values.Count);
        Assert.Equal("changed", values["a.one"]);
        Assert.Equal("2", values["a.two"]);
    }



    [Fact]
    public async Task ReadAllAsync_when_called_gives_a_copy_that_a_later_change_does_not_touch()
    {
        var store = new InMemorySettingStore();
        store.Set("a.one", "1", Noon);

        var values = await store.ReadAllAsync(CancellationToken.None);
        store.Set("a.one", "2", Noon);
        store.Set("a.two", "2", Noon);

        Assert.Equal("1", Assert.Single(values).Value);
    }



    [Fact]
    public async Task Reset_when_a_value_is_stored_removes_it()
    {
        var store = new InMemorySettingStore();
        store.Set("a.one", "1", Noon);
        store.Set("a.two", "2", Noon.AddMinutes(1));

        store.Reset("a.two");
        store.Reset("a.never");

        Assert.Equal(new SettingGeneration(1, Noon), await store.ProbeAsync(CancellationToken.None));
        Assert.Equal(["a.one"], (await store.ReadAllAsync(CancellationToken.None)).Keys);
    }



    [Fact]
    public async Task ProbeCount_when_the_store_is_asked_counts_each_call_and_so_does_ReadCount()
    {
        var store = new InMemorySettingStore();

        await store.ProbeAsync(CancellationToken.None);
        await store.ProbeAsync(CancellationToken.None);
        await store.ReadAllAsync(CancellationToken.None);

        Assert.Equal(2, store.ProbeCount);
        Assert.Equal(1, store.ReadCount);
    }



    [Fact]
    public async Task ProbeAsync_when_cancelled_throws_and_counts_nothing_and_so_does_ReadAllAsync()
    {
        var store = new InMemorySettingStore();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => store.ProbeAsync(cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => store.ReadAllAsync(cancellation.Token));

        Assert.Equal(0, store.ProbeCount);
        Assert.Equal(0, store.ReadCount);
    }



    [Fact]
    public void Set_when_given_no_name_throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new InMemorySettingStore().Set(null!, "1", Noon));

        Assert.Equal("name", exception.ParamName);
    }



    [Fact]
    public void Set_when_given_no_value_throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new InMemorySettingStore().Set("a.one", null!, Noon));

        Assert.Equal("value", exception.ParamName);
    }



    [Fact]
    public void Reset_when_given_no_name_throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new InMemorySettingStore().Reset(null!));

        Assert.Equal("name", exception.ParamName);
    }
}
