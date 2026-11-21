using Wolfgang.Barefoot.Contracts.Settings;

namespace Wolfgang.Barefoot.Contracts.Tests.Unit.Settings;

public sealed class SettingRecordsTests
{
    [Fact]
    public void SettingResponse_when_a_value_is_stored_keeps_each_value()
    {
        var updated = new DateTimeOffset(2026, 11, 21, 9, 0, 0, TimeSpan.Zero);
        var setting = new SettingResponse("run.lease_seconds", "Int32", "300", "How long a lease lasts.", "600", 3, RequiresRestart: true, updated, "Admin Person");

        Assert.Equal("run.lease_seconds", setting.Key);
        Assert.Equal("Int32", setting.Type);
        Assert.Equal("300", setting.Default);
        Assert.Equal("How long a lease lasts.", setting.Description);
        Assert.Equal("600", setting.Value);
        Assert.Equal(3, setting.Version);
        Assert.True(setting.RequiresRestart);
        Assert.Equal(updated, setting.UpdatedUtc);
        Assert.Equal("Admin Person", setting.UpdatedBy);
    }



    [Fact]
    public void SettingResponse_when_no_value_is_stored_reports_version_zero_and_nobody()
    {
        var setting = new SettingResponse("run.lease_seconds", "Int32", "300", "How long a lease lasts.", "300", 0, RequiresRestart: false, null, null);

        Assert.Equal(0, setting.Version);
        Assert.False(setting.RequiresRestart);
        Assert.Null(setting.UpdatedUtc);
        Assert.Null(setting.UpdatedBy);
    }



    [Fact]
    public void SetSettingRequest_when_built_keeps_the_value()
    {
        Assert.Equal("600", new SetSettingRequest("600").Value);
    }
}
