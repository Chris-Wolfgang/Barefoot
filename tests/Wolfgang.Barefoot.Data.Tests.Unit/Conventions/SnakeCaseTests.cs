using Wolfgang.Extensions.String;

namespace Wolfgang.Barefoot.Data.Tests.Unit.Conventions;

/// <summary>
/// The names <c>ToSnakeCase</c> of <c>Wolfgang.Extensions.String</c> gives, for the shapes of name the
/// schema has or may get. A new version of the package that splits words differently fails here.
/// </summary>
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
    public void ToSnakeCase_when_given_a_pascal_case_name_returns_the_name_the_schema_uses(string name, string expected)
    {
        Assert.Equal(expected, name.ToSnakeCase());
    }
}
