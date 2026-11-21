using Wolfgang.Barefoot.Tests.Architecture.Rules;
using Wolfgang.Barefoot.Tests.Architecture.Scanning;

namespace Wolfgang.Barefoot.Tests.Architecture;

/// <summary>
/// A test that changes state the whole process shares is in a collection that does not run in parallel
/// with anything else.
/// </summary>
public sealed class TestIsolationTests
{
    [Fact]
    public async Task UnserializedStateChangesIn_when_given_the_repository_reports_nothing()
    {
        var sources = await RepositoryFiles.TestSourcesAsync();

        Assert.NotEmpty(sources);
        Assert.Empty(TestSourceRules.UnserializedStateChangesIn(sources));
    }



    [Fact]
    public void UnserializedStateChangesIn_when_a_class_changes_shared_state_outside_a_serial_collection_reports_it()
    {
        var sources = new[]
        {
            new SourceFile
            (
                "tests/Sample/Collections.cs",
                """
                [CollectionDefinition("process state", DisableParallelization = true)]
                public sealed class ProcessStateCollection;

                [CollectionDefinition("parallel")]
                public sealed class ParallelCollection;
                """
            ),
            new SourceFile
            (
                "tests/Sample/StateTests.cs",
                """
                public sealed class NoCollectionTests
                {
                    public void Run()
                    {
                        Environment.SetEnvironmentVariable("BF_API", "x");
                        System.Console.SetOut(writer);
                        Environment.ExitCode = 3;
                        var read = Environment.GetEnvironmentVariable("BF_API");
                        local = Environment.ExitCode;
                    }
                }

                [Collection("parallel")]
                public sealed class WrongCollectionTests
                {
                    public void Run() => Directory.SetCurrentDirectory(path);
                }

                [Collection("process state")]
                public sealed class SerialTests
                {
                    public void Run() => Environment.SetEnvironmentVariable("BF_API", "x");
                }

                public sealed class QuietTests
                {
                    public void Run() => CultureInfo.CurrentCulture = culture;
                }
                """
            ),
        };

        Assert.Equal
        (
            [
                "tests/Sample/StateTests.cs: NoCollectionTests changes Console.SetOut",
                "tests/Sample/StateTests.cs: NoCollectionTests changes Environment.ExitCode",
                "tests/Sample/StateTests.cs: NoCollectionTests changes Environment.SetEnvironmentVariable",
                "tests/Sample/StateTests.cs: WrongCollectionTests changes Directory.SetCurrentDirectory",
            ],
            TestSourceRules.UnserializedStateChangesIn(sources)
        );
    }
}
