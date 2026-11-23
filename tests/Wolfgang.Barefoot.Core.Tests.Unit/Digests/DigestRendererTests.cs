using Wolfgang.Barefoot.Core.Digests;

namespace Wolfgang.Barefoot.Core.Tests.Unit.Digests;

public sealed class DigestRendererTests
{
    private static readonly Uri Results = new("https://barefoot.example/results?search=1&run=2");

    private static readonly Uri GrandeResults = new("https://barefoot.example/results?search=1&run=2&resort=SGL");



    [Theory]
    [InlineData("full")]
    [InlineData("edge")]
    public async Task Render_when_given_a_digest_writes_exactly_the_pinned_html(string name)
    {
        var rendered = DigestRenderer.Render(Model(name));

        Assert.Equal(await FixtureAsync($"{name}.html"), rendered.HtmlBody);
    }



    [Theory]
    [InlineData("full")]
    [InlineData("edge")]
    public async Task Render_when_given_a_digest_writes_exactly_the_pinned_plain_text(string name)
    {
        var rendered = DigestRenderer.Render(Model(name));

        Assert.Equal(await FixtureAsync($"{name}.txt"), rendered.TextBody);
    }



    [Theory]
    [InlineData("full", "Feb–Mar St. Lucia <scouting>: 62 matches from $6,142")]
    [InlineData("edge", "One night: 1 match from $1,706.66")]
    [InlineData("empty", "Nothing yet: no matches")]
    public void Render_when_given_a_digest_writes_a_subject_with_the_count_and_the_cheapest_price(string name, string expected)
    {
        Assert.Equal(expected, DigestRenderer.Render(Model(name)).Subject);
    }



    [Fact]
    public void Render_when_the_cheapest_price_is_nothing_still_names_it_in_the_subject()
    {
        var free = Empty() with
        {
            MatchCount = 2,
            Resorts = [new DigestResort("Free Resort", 0m, null, IsOffline: false, [], 0, Results)],
        };

        Assert.Equal("Nothing yet: 2 matches from $0", DigestRenderer.Render(free).Subject);
    }



    [Fact]
    public void Render_when_a_value_holds_markup_encodes_it_in_the_html_and_leaves_the_text_alone()
    {
        var rendered = DigestRenderer.Render(Full());

        Assert.Contains("Feb–Mar St. Lucia &lt;scouting&gt;", rendered.HtmlBody, StringComparison.Ordinal);
        Assert.DoesNotContain("<scouting>", rendered.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("Tom &amp; Tina&#39;s &quot;offline&quot; resort", rendered.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("search=1&amp;run=2&amp;resort=SGL", rendered.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("Feb–Mar St. Lucia <scouting>", rendered.TextBody, StringComparison.Ordinal);
        Assert.Contains("search=1&run=2&resort=SGL", rendered.TextBody, StringComparison.Ordinal);
    }



    [Fact]
    public void Render_when_called_ends_every_line_with_a_line_feed_only()
    {
        var rendered = DigestRenderer.Render(Full());

        Assert.DoesNotContain('\r', rendered.HtmlBody);
        Assert.DoesNotContain('\r', rendered.TextBody);
        Assert.EndsWith("</html>\n", rendered.HtmlBody, StringComparison.Ordinal);
        Assert.EndsWith("Sandals Resorts International.\n", rendered.TextBody, StringComparison.Ordinal);
    }



    [Fact]
    public void Render_when_the_digest_is_null_throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => DigestRenderer.Render(null!));

        Assert.Equal("digest", exception.ParamName);
    }



    [Theory]
    [InlineData("6142", "$6,142")]
    [InlineData("0", "$0")]
    [InlineData("1706.66", "$1,706.66")]
    [InlineData("1706.6", "$1,706.60")]
    [InlineData("1234567.00", "$1,234,567")]
    public void Money_when_given_an_amount_shows_cents_only_when_there_are_any(string amount, string expected)
    {
        Assert.Equal(expected, DigestRenderer.Money(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));
    }



    [Theory]
    [InlineData("-486", "▼ $486")]
    [InlineData("120.5", "▲ $120.50")]
    [InlineData("0", "unchanged")]
    public void Change_when_given_a_movement_points_down_for_cheaper_and_up_for_dearer(string change, string expected)
    {
        Assert.Equal(expected, DigestRenderer.Change(decimal.Parse(change, System.Globalization.CultureInfo.InvariantCulture)));
    }



    [Fact]
    public void Change_when_there_is_nothing_to_compare_with_is_null()
    {
        Assert.Null(DigestRenderer.Change(null));
    }



    private static DigestModel Model(string name)
    {
        return name switch
        {
            "full" => Full(),
            "edge" => Edge(),
            _ => Empty(),
        };
    }



    private static DigestModel Full()
    {
        var club = new DigestRoom
        (
            6142m,
            "with flights from AAA",
            "Beachfront Club Level Room with Private Pool",
            ["Club level", "ocean view", "private pool", "soaking tub"],
            new DateOnly(2027, 2, 24),
            7,
            4918m,
            5404m,
            "7th night free",
            [new DigestFlightPrice("AAA", 1224m), new DigestFlightPrice("BBB", 1310m)]
        );

        var second = club with
        {
            TotalAmount = 6362m,
            CheckIn = new DateOnly(2027, 2, 23),
            RoomAmount = 5100m,
            Promotion = null,
            Flights = [new DigestFlightPrice("AAA", 1262m)],
        };

        var villa = new DigestRoom
        (
            6480m,
            "with flights from BBB",
            "Sunset Bluff Oceanfront Butler Villa Suite with Private Pool",
            ["Butler level", "ocean view"],
            new DateOnly(2027, 3, 1),
            7,
            5170m,
            null,
            null,
            [new DigestFlightPrice("BBB", 1310m), new DigestFlightPrice("AAA", 1344m)]
        );

        return new DigestModel
        (
            "Barefoot Vacation",
            "for Sandals & Beaches resorts",
            "Feb–Mar St. Lucia <scouting>",
            new DateOnly(2026, 11, 3),
            62,
            ["Tom & Tina's \"offline\" resort is offline on the booking site and was skipped. It stays in your search."],
            [
                "Check-in Feb 15 – Mar 15, 2027 · 7 nights · 2 adults · flights from AAA and BBB, economy.",
                "Showing: Club or Butler level · ocean view · up to $9,000 total · up to 1 stop.",
                "58 of 58 room lookups and 58 of 58 flight lookups completed.",
            ],
            [
                new DigestResort("Grande St. Lucian", 6142m, -486m, IsOffline: false, [club, second], 38, GrandeResults),
                new DigestResort("Regency La Toc", 6480m, 0m, IsOffline: false, [villa], 0, Results),
                new DigestResort("Tom & Tina's \"offline\" resort", null, null, IsOffline: true, [], 0, Results),
            ],
            Results
        );
    }



    private static DigestModel Edge()
    {
        var plain = new DigestRoom
        (
            1706.66m,
            null,
            "Garden Room",
            [],
            new DateOnly(2027, 1, 1),
            1,
            1706.66m,
            null,
            null,
            []
        );

        return new DigestModel
        (
            "Barefoot Vacation",
            "for Sandals & Beaches resorts",
            "One night",
            new DateOnly(2026, 12, 25),
            1,
            [],
            ["Check-in Jan 1, 2027 · 1 night · 2 adults · no flights."],
            [
                new DigestResort("Dearer Resort", 1706.66m, 120m, IsOffline: false, [plain], 1, Results),
                new DigestResort("New Resort", 2000m, null, IsOffline: false, [], 0, Results),
                new DigestResort("Unpriced Resort", null, null, IsOffline: false, [plain], 0, Results),
                new DigestResort("Empty Resort", null, null, IsOffline: false, [], 0, Results),
            ],
            Results
        );
    }



    private static DigestModel Empty()
    {
        return Edge() with
        {
            SearchName = "Nothing yet",
            MatchCount = 0,
            Resorts = [new DigestResort("Empty Resort", null, null, IsOffline: false, [], 0, Results)],
        };
    }



    private static async Task<string> FixtureAsync(string name)
    {
        var resource = $"Wolfgang.Barefoot.Core.Tests.Unit.Digests.Fixtures.{name}.fixture";
        var stream = typeof(DigestRendererTests).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"The fixture {resource} is not embedded in the test assembly.");

        await using (stream.ConfigureAwait(true))
        {
            using var reader = new StreamReader(stream);

            return await reader.ReadToEndAsync();
        }
    }
}
