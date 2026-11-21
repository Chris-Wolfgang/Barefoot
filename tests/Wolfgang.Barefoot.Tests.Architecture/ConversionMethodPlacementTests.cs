using Wolfgang.Barefoot.Tests.Architecture.Rules;
using Wolfgang.Barefoot.Tests.Architecture.Scanning;

namespace Wolfgang.Barefoot.Tests.Architecture;

/// <summary>
/// ADR 0002 §3: <c>ToDto</c> and <c>FromDto</c> are defined only in <c>Api</c>, <c>ToEntity</c> and
/// <c>FromEntity</c> only in <c>Data</c>.
/// </summary>
public sealed class ConversionMethodPlacementTests
{
    private const string Conversions =
        """
        namespace Fixture;

        public sealed class Search { }

        public sealed class SearchDto { }

        public static class SearchConversions
        {
            public static SearchDto ToDto(this Search search) => new();

            internal static Search FromDto(SearchDto dto) => new();

            public static Search ToEntity(this SearchDto dto) => new();

            private static SearchDto FromEntity(Search search) => new();

            public static string ToText(this Search search) => string.Empty;
        }
        """;



    [Fact]
    public void MisplacedConversionsIn_when_given_the_source_assemblies_reports_nothing()
    {
        Assert.Empty(CompiledCodeRules.MisplacedConversionsIn(SourceAssemblies.All));
    }



    [Fact]
    public void MisplacedConversionsIn_when_Core_defines_conversions_reports_all_four()
    {
        var fixture = FixtureCompiler.Compile("Wolfgang.Barefoot.Core", Conversions);

        Assert.Equal
        (
            [
                "Wolfgang.Barefoot.Core: Fixture.SearchConversions.FromDto belongs in Wolfgang.Barefoot.Api",
                "Wolfgang.Barefoot.Core: Fixture.SearchConversions.FromEntity belongs in Wolfgang.Barefoot.Data",
                "Wolfgang.Barefoot.Core: Fixture.SearchConversions.ToDto belongs in Wolfgang.Barefoot.Api",
                "Wolfgang.Barefoot.Core: Fixture.SearchConversions.ToEntity belongs in Wolfgang.Barefoot.Data",
            ],
            CompiledCodeRules.MisplacedConversionsIn([fixture])
        );
    }



    [Fact]
    public void MisplacedConversionsIn_when_Api_defines_conversions_reports_only_the_entity_ones()
    {
        var fixture = FixtureCompiler.Compile("Wolfgang.Barefoot.Api", Conversions);

        Assert.Equal
        (
            [
                "Wolfgang.Barefoot.Api: Fixture.SearchConversions.FromEntity belongs in Wolfgang.Barefoot.Data",
                "Wolfgang.Barefoot.Api: Fixture.SearchConversions.ToEntity belongs in Wolfgang.Barefoot.Data",
            ],
            CompiledCodeRules.MisplacedConversionsIn([fixture])
        );
    }



    [Fact]
    public void MisplacedConversionsIn_when_Data_defines_conversions_reports_only_the_dto_ones()
    {
        var fixture = FixtureCompiler.Compile("Wolfgang.Barefoot.Data", Conversions);

        Assert.Equal
        (
            [
                "Wolfgang.Barefoot.Data: Fixture.SearchConversions.FromDto belongs in Wolfgang.Barefoot.Api",
                "Wolfgang.Barefoot.Data: Fixture.SearchConversions.ToDto belongs in Wolfgang.Barefoot.Api",
            ],
            CompiledCodeRules.MisplacedConversionsIn([fixture])
        );
    }
}
