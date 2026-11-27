using Wolfgang.Barefoot.Contracts.Catalog;

namespace Wolfgang.Barefoot.Contracts.Tests.Unit.Catalog;

public sealed class CatalogKindTests
{
    [Theory]
    [InlineData(CatalogKind.Airports, 0)]
    [InlineData(CatalogKind.Resorts, 1)]
    public void CatalogKind_when_cast_to_a_number_keeps_its_declared_value(CatalogKind kind, int expected)
    {
        Assert.Equal(expected, (int)kind);
    }



    [Fact]
    public void GetValues_when_asked_of_CatalogKind_lists_the_two_catalogs()
    {
        Assert.Equal([CatalogKind.Airports, CatalogKind.Resorts], Enum.GetValues<CatalogKind>());
    }
}
