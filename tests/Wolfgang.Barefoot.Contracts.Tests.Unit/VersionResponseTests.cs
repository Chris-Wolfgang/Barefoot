namespace Wolfgang.Barefoot.Contracts.Tests.Unit;

public sealed class VersionResponseTests
{
    [Fact]
    public void Constructor_when_given_values_keeps_each_one()
    {
        var response = new VersionResponse("1.4.0+abc123", "v0", 7);

        Assert.Equal("1.4.0+abc123", response.BuildVersion);
        Assert.Equal("v0", response.ContractVersion);
        Assert.Equal(7, response.Revision);
    }



    [Fact]
    public void Equals_when_two_responses_carry_the_same_values_is_true()
    {
        Assert.Equal
        (
            new VersionResponse("1.4.0", "v0", 7),
            new VersionResponse("1.4.0", "v0", 7)
        );
    }
}
