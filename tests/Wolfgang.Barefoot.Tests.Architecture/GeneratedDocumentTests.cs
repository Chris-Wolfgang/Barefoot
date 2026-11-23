using Wolfgang.Barefoot.Core.Hosting;
using Wolfgang.Barefoot.Tests.Architecture.Scanning;

namespace Wolfgang.Barefoot.Tests.Architecture;

/// <summary>
/// A document generated from code is committed, and equals what the code generates now. When one of
/// these fails, run <c>scripts/Update-GeneratedDocs.ps1</c> and commit the result.
/// </summary>
public sealed class GeneratedDocumentTests
{
    [Fact]
    public async Task ToMarkdown_when_compared_with_the_committed_exit_code_reference_is_identical()
    {
        var committed = await RepositoryFiles.ReadAsync("docs/EXIT-CODES.md");

        Assert.Equal(ExitCodes.ToMarkdown(), WithLineFeeds(committed));
    }



    [Theory]
    [InlineData("one\r\ntwo\r\n", "one\ntwo\n")]
    [InlineData("one\ntwo\n", "one\ntwo\n")]
    public void WithLineFeeds_when_given_either_line_ending_returns_line_feeds(string text, string expected)
    {
        Assert.Equal(expected, WithLineFeeds(text));
    }



    /// <summary>
    /// The text with Windows line endings turned into line feeds, so a checkout that converted them
    /// still compares equal.
    /// </summary>
    private static string WithLineFeeds(string text)
    {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}
