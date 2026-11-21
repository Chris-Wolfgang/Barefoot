namespace Wolfgang.Barefoot.Contracts.Tests.Unit;

public sealed class ContractVersionTests
{
    [Fact]
    public void Name_when_read_is_the_unstable_version_every_route_lives_under()
    {
        Assert.Equal("v0", ContractVersion.Name);
    }



    [Fact]
    public void Revision_when_read_is_a_positive_number()
    {
        Assert.True(ContractVersion.Revision >= 1);
    }
}
