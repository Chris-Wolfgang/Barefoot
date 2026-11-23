using Wolfgang.Barefoot.Contracts.Runs;

namespace Wolfgang.Barefoot.Contracts.Tests.Unit.Runs;

public sealed class SearchRunStageTests
{
    [Theory]
    [InlineData(SearchRunStage.NotSet, 0)]
    [InlineData(SearchRunStage.Exploded, 1)]
    [InlineData(SearchRunStage.Downloaded, 2)]
    [InlineData(SearchRunStage.Aggregated, 3)]
    [InlineData(SearchRunStage.Emailed, 4)]
    public void SearchRunStage_when_cast_to_a_number_keeps_its_declared_value(SearchRunStage stage, int expected)
    {
        Assert.Equal(expected, (int)stage);
    }
}
