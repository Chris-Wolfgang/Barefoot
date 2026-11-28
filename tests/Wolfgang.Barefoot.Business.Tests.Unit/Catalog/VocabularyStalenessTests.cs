using Wolfgang.Barefoot.Business.Catalog;

namespace Wolfgang.Barefoot.Business.Tests.Unit.Catalog;

/// <summary>
/// A vocabulary value unseen for the configured number of runs is greyed (Barefoot#93).
/// </summary>
public sealed class VocabularyStalenessTests
{
    private static readonly DateTimeOffset Seen = new(2026, 1, 14, 12, 0, 0, TimeSpan.Zero);



    [Fact]
    public void Of_when_no_run_started_since_the_value_was_seen_counts_none_and_is_not_stale()
    {
        DateTimeOffset[] runs = [Seen.AddDays(-7), Seen.AddDays(-3)];

        Assert.Equal(new VocabularyAge(0, IsStale: false), VocabularyStaleness.Of(Seen, runs, 1));
    }



    [Fact]
    public void Of_when_there_are_no_runs_counts_none_and_is_not_stale()
    {
        Assert.Equal(new VocabularyAge(0, IsStale: false), VocabularyStaleness.Of(Seen, [], 1));
    }



    [Fact]
    public void Of_when_a_run_started_at_the_instant_the_value_was_seen_does_not_count_it()
    {
        DateTimeOffset[] runs = [Seen, Seen.AddTicks(1)];

        Assert.Equal(new VocabularyAge(1, IsStale: false), VocabularyStaleness.Of(Seen, runs, 2));
    }



    [Fact]
    public void Of_when_fewer_runs_than_the_limit_started_since_is_not_stale()
    {
        DateTimeOffset[] runs = [Seen.AddDays(10), Seen.AddDays(-3), Seen.AddDays(3), Seen.AddDays(7)];

        Assert.Equal(new VocabularyAge(3, IsStale: false), VocabularyStaleness.Of(Seen, runs, 4));
    }



    [Fact]
    public void Of_when_as_many_runs_as_the_limit_started_since_is_stale()
    {
        DateTimeOffset[] runs = [Seen.AddDays(3), Seen.AddDays(7), Seen.AddDays(10)];

        Assert.Equal(new VocabularyAge(3, IsStale: true), VocabularyStaleness.Of(Seen, runs, 3));
    }



    [Fact]
    public void Of_when_called_gives_the_count_and_the_verdict_as_two_values()
    {
        DateTimeOffset[] runs = [Seen.AddDays(3), Seen.AddDays(7)];

        var age = VocabularyStaleness.Of(Seen, runs, 2);

        Assert.Equal(2, age.RunsSinceSeenCount);
        Assert.True(age.IsStale);
    }



    [Fact]
    public void Of_when_more_runs_than_the_limit_started_since_is_stale()
    {
        DateTimeOffset[] runs = [Seen.AddDays(3), Seen.AddDays(7), Seen.AddDays(10)];

        Assert.Equal(new VocabularyAge(3, IsStale: true), VocabularyStaleness.Of(Seen, runs, 1));
    }



    [Fact]
    public void Of_when_a_run_started_in_another_offset_compares_the_instants()
    {
        DateTimeOffset[] runs = [new DateTimeOffset(2026, 1, 14, 13, 30, 0, TimeSpan.FromHours(2))];

        Assert.Equal(new VocabularyAge(0, IsStale: false), VocabularyStaleness.Of(Seen, runs, 1));
    }



    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Of_when_the_limit_is_less_than_one_throws(int staleAfterRunCount)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>
        (
            () => VocabularyStaleness.Of(Seen, [], staleAfterRunCount)
        );

        Assert.Equal("staleAfterRunCount", exception.ParamName);
    }



    [Fact]
    public void Of_when_given_no_runs_list_throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => VocabularyStaleness.Of(Seen, null!, 1));

        Assert.Equal("runStartsUtc", exception.ParamName);
    }
}
