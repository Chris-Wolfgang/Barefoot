using Wolfgang.Barefoot.Contracts;
using Wolfgang.Barefoot.Core.Versioning;

namespace Wolfgang.Barefoot.Core.Tests.Unit.Versioning;

public sealed class VersionCheckTests
{
    [Theory]
    [InlineData(3, 3, VersionCompatibility.Compatible)]
    [InlineData(2, 3, VersionCompatibility.ApiTooOld)]
    [InlineData(4, 3, VersionCompatibility.ApiTooNew)]
    public void Compare_when_the_contract_is_not_frozen_requires_equal_revisions
    (
        int apiRevision,
        int clientRevision,
        VersionCompatibility expected
    )
    {
        Assert.Equal(expected, VersionCheck.Compare(apiRevision, clientRevision, contractIsFrozen: false));
    }



    [Theory]
    [InlineData(3, 3, VersionCompatibility.Compatible)]
    [InlineData(2, 3, VersionCompatibility.ApiTooOld)]
    [InlineData(4, 3, VersionCompatibility.Compatible)]
    public void Compare_when_the_contract_is_frozen_fails_only_an_API_that_is_too_old
    (
        int apiRevision,
        int clientRevision,
        VersionCompatibility expected
    )
    {
        Assert.Equal(expected, VersionCheck.Compare(apiRevision, clientRevision, contractIsFrozen: true));
    }



    [Theory]
    [InlineData(0, VersionCompatibility.Compatible)]
    [InlineData(-1, VersionCompatibility.ApiTooOld)]
    [InlineData(1, VersionCompatibility.ApiTooNew)]
    public void Evaluate_when_given_the_API_answer_compares_it_with_the_revision_this_build_was_made_for
    (
        int offset,
        VersionCompatibility expected
    )
    {
        var api = new VersionResponse("1.0.0", ContractVersion.Name, ContractVersion.Revision + offset);

        Assert.Equal(expected, VersionCheck.Evaluate(api));
    }



    [Fact]
    public void Evaluate_when_the_answer_is_null_throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => VersionCheck.Evaluate(null!));

        Assert.Equal("api", exception.ParamName);
    }



    [Theory]
    [InlineData("v0", false)]
    [InlineData("v1", true)]
    [InlineData("V0", true)]
    [InlineData("", true)]
    public void IsFrozen_when_given_a_contract_version_is_false_only_for_v0(string contractVersion, bool expected)
    {
        Assert.Equal(expected, VersionCheck.IsFrozen(contractVersion));
    }



    [Theory]
    [InlineData(VersionCompatibility.ApiTooOld, "The API is older than this tool. Deploy the API.")]
    [InlineData(VersionCompatibility.ApiTooNew, "This tool is older than the API. Update it.")]
    public void MessageFor_when_the_versions_do_not_match_says_which_side_to_update
    (
        VersionCompatibility compatibility,
        string expected
    )
    {
        Assert.Equal(expected, VersionCheck.MessageFor(compatibility));
    }



    [Fact]
    public void MessageFor_when_the_versions_match_has_nothing_to_say()
    {
        Assert.Null(VersionCheck.MessageFor(VersionCompatibility.Compatible));
    }



    [Theory]
    [InlineData(VersionCompatibility.Compatible, 0)]
    [InlineData(VersionCompatibility.ApiTooOld, 1)]
    [InlineData(VersionCompatibility.ApiTooNew, 2)]
    public void VersionCompatibility_when_cast_to_a_number_keeps_its_declared_value(VersionCompatibility value, int expected)
    {
        Assert.Equal(expected, (int)value);
    }
}
