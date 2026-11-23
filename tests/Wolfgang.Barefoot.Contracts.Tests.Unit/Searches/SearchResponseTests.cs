using Wolfgang.Barefoot.Contracts.Searches;

namespace Wolfgang.Barefoot.Contracts.Tests.Unit.Searches;

public sealed class SearchResponseTests
{
    [Fact]
    public void Constructor_when_given_values_keeps_each_one()
    {
        var id = Guid.Parse("3f2a1b0c-9d8e-4f7a-b6c5-d4e3f2a1b0c9");
        var owner = Guid.Parse("a9b8c7d6-e5f4-4a3b-9c2d-1e0f9a8b7c6d");
        var definition = new SearchDefinition
        {
            Name = "Anniversary",
            Brand = Brand.Sandals,
            NightCount = 7,
            AdultCount = 2,
            WindowKind = WindowKind.Recurring,
        };

        var response = new SearchResponse(id, owner, SearchStatus.Active, 4, definition);

        Assert.Equal(id, response.Id);
        Assert.Equal(owner, response.OwnerId);
        Assert.Equal(SearchStatus.Active, response.Status);
        Assert.Equal(4, response.Version);
        Assert.Same(definition, response.Definition);
    }



    [Theory]
    [InlineData(SearchStatus.Draft, 0)]
    [InlineData(SearchStatus.Active, 1)]
    [InlineData(SearchStatus.Inactive, 2)]
    public void SearchStatus_when_cast_to_a_number_keeps_its_declared_value(SearchStatus status, int expected)
    {
        Assert.Equal(expected, (int)status);
    }
}
