using Wolfgang.Barefoot.Data.Conventions;

namespace Wolfgang.Barefoot.Data.Tests.Unit.Conventions;

public sealed class SnakeCaseTests
{
    [Theory]
    [InlineData("Run", "run")]
    [InlineData("AppUser", "app_user")]
    [InlineData("SearchMatrix", "search_matrix")]
    [InlineData("OwnerId", "owner_id")]
    [InlineData("TimeZoneId", "time_zone_id")]
    [InlineData("LastSeenUtc", "last_seen_utc")]
    [InlineData("ResultSha256", "result_sha256")]
    [InlineData("IsSoldOut", "is_sold_out")]
    [InlineData("IANAZone", "iana_zone")]
    [InlineData("ZoneIANA", "zone_iana")]
    [InlineData("Sha256Hash", "sha256_hash")]
    [InlineData("oid", "oid")]
    public void Of_when_given_a_pascal_case_name_returns_it_in_snake_case(string name, string expected)
    {
        Assert.Equal(expected, SnakeCase.Of(name));
    }



    [Fact]
    public void Of_when_the_name_is_empty_throws_ArgumentException()
    {
        Assert.Throws<ArgumentException>(() => SnakeCase.Of(string.Empty));
    }



    [Fact]
    public void Of_when_the_name_is_null_throws_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SnakeCase.Of(null!));
    }
}
