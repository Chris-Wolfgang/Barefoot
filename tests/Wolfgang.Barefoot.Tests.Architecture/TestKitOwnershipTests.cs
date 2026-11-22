using Wolfgang.Barefoot.Tests.Architecture.Rules;
using Wolfgang.Barefoot.Tests.Architecture.Scanning;

namespace Wolfgang.Barefoot.Tests.Architecture;

/// <summary>
/// Test doubles live in the test kit. No other test project defines a fake of its own, derives a clock,
/// or builds a fake clock without going through <c>TestClock</c>.
/// </summary>
public sealed class TestKitOwnershipTests
{
    private const string ClockPackage = "Microsoft.Extensions.TimeProvider.Testing";



    [Fact]
    public async Task DoublesOutsideTheKitIn_when_given_the_repository_reports_nothing()
    {
        var sources = await RepositoryFiles.TestSourcesAsync();

        Assert.NotEmpty(sources);
        Assert.Empty(TestSourceRules.DoublesOutsideTheKitIn(sources));
    }



    [Fact]
    public async Task ProjectsAsync_when_read_shows_only_the_test_kit_referencing_the_fake_clock_package()
    {
        var projects = await RepositoryFiles.ProjectsAsync();

        var owners = projects
            .Where(project => project.PackageReferences.Contains(ClockPackage, StringComparer.OrdinalIgnoreCase))
            .Select(project => project.Name);

        Assert.Equal([ProjectLayoutRules.TestKit], owners);
    }



    [Fact]
    public void DoublesOutsideTheKitIn_when_a_test_project_defines_its_own_reports_each()
    {
        var sources = new[]
        {
            new SourceFile
            (
                "tests/Wolfgang.Barefoot.Core.Tests.Unit/Doubles.cs",
                """
                internal sealed class FakeBookingClient : IBookingClient { }

                internal sealed class ClockStub { }

                internal sealed class FrozenClock : TimeProvider { }

                internal sealed class Fakery { }

                public sealed class WindowTests
                {
                    public void Run()
                    {
                        var clock = new FakeTimeProvider(start);
                        var other = new Microsoft.Extensions.Time.Testing.FakeTimeProvider();
                        var fine = TestClock.Create();
                    }
                }
                """
            ),
            new SourceFile
            (
                "tests/Wolfgang.Barefoot.TestKit/FakeBookingClient.cs",
                """
                public sealed class FakeBookingClient : IBookingClient
                {
                    private readonly FakeTimeProvider _clock = new FakeTimeProvider();
                }
                """
            ),
        };

        Assert.Equal
        (
            [
                "tests/Wolfgang.Barefoot.Core.Tests.Unit/Doubles.cs: ClockStub is named as a test double",
                "tests/Wolfgang.Barefoot.Core.Tests.Unit/Doubles.cs: FakeBookingClient is named as a test double",
                "tests/Wolfgang.Barefoot.Core.Tests.Unit/Doubles.cs: FrozenClock derives from a clock",
                "tests/Wolfgang.Barefoot.Core.Tests.Unit/Doubles.cs: constructs FakeTimeProvider instead of asking TestClock",
            ],
            TestSourceRules.DoublesOutsideTheKitIn(sources)
        );
    }
}
