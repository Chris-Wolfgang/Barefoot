using Wolfgang.Barefoot.Tests.Architecture.Rules;
using Wolfgang.Barefoot.Tests.Architecture.Scanning;

namespace Wolfgang.Barefoot.Tests.Architecture;

/// <summary>
/// A test is named <c>MethodUnderTest_when_condition_expected_result</c>.
/// </summary>
public sealed class TestNamingTests
{
    [Fact]
    public async Task MisnamedTestsIn_when_given_the_repository_reports_nothing()
    {
        var sources = await RepositoryFiles.TestSourcesAsync();

        Assert.NotEmpty(sources);
        Assert.Empty(TestSourceRules.MisnamedTestsIn(sources));
    }



    [Fact]
    public void MisnamedTestsIn_when_names_break_the_pattern_reports_each()
    {
        var source = new SourceFile
        (
            "tests/Sample/SampleTests.cs",
            """
            public sealed class SampleTests
            {
                [Fact]
                public void Resolve_when_the_window_wraps_the_year_rolls_forward() { }

                [Fact]
                public void ResolvesTheWindow() { }

                [Xunit.Theory, InlineData(1)]
                public void Resolve_returns_a_date(int value) { }

                [FactAttribute]
                public void resolve_when_lower_case_fails() { }

                [SkippableFact]
                public async Task Resolve_when_() { }

                [Fact]
                public void Resolve_when_short() { }

                public void Helper() { }

                [Obsolete]
                public void AnotherHelper() { }
            }
            """
        );

        Assert.Equal
        (
            [
                "tests/Sample/SampleTests.cs: Resolve_returns_a_date",
                "tests/Sample/SampleTests.cs: Resolve_when_",
                "tests/Sample/SampleTests.cs: Resolve_when_short",
                "tests/Sample/SampleTests.cs: ResolvesTheWindow",
                "tests/Sample/SampleTests.cs: resolve_when_lower_case_fails",
            ],
            TestSourceRules.MisnamedTestsIn([source])
        );
    }
}
