using Wolfgang.Barefoot.Contracts.Searches;
using Wolfgang.Barefoot.Core.Runs;
using Wolfgang.Barefoot.Core.Searches;

namespace Wolfgang.Barefoot.Core.Tests.Unit.Runs;

public sealed class ExpanderTests
{
    private static readonly Guid MontegoBay = Id(1);

    private static readonly Guid Negril = Id(2);

    private static readonly Guid SaintLucia = Id(3);

    private static readonly Guid JamaicaGateway = Id(10);

    private static readonly Guid SaintLuciaGateway = Id(11);

    private static readonly Guid Philadelphia = Id(20);

    private static readonly Guid Newark = Id(21);

    private static readonly Guid Mine = Id(100);

    private static readonly Guid Theirs = Id(101);

    private static readonly DateOnly March10 = new(2027, 3, 10);



    [Fact]
    public void Expand_when_there_are_no_searches_returns_an_empty_work_list()
    {
        var expansion = Expander.Expand([]);

        Assert.Empty(expansion.Requests);
        Assert.Empty(expansion.FlightSets);
        Assert.Empty(expansion.Items);
    }



    [Fact]
    public void Expand_when_a_search_has_two_resorts_and_three_dates_plans_six_lookups_resort_by_resort()
    {
        var search = Search(Mine, days: 3) with
        {
            Resorts = [new ExpandResort(MontegoBay, JamaicaGateway), new ExpandResort(Negril, JamaicaGateway)],
        };

        var expansion = Expander.Expand([search]);

        Assert.Equal
        (
            [
                Lookup(MontegoBay, March10),
                Lookup(MontegoBay, March10.AddDays(1)),
                Lookup(MontegoBay, March10.AddDays(2)),
                Lookup(Negril, March10),
                Lookup(Negril, March10.AddDays(1)),
                Lookup(Negril, March10.AddDays(2)),
            ],
            expansion.Requests.Select(request => request.Key)
        );
        Assert.All(expansion.Requests, request => Assert.Equal(50, request.RunOrder));
        Assert.Equal(6, expansion.Items.Count);
        Assert.All
        (
            expansion.Items,
            item =>
            {
                Assert.Equal(Mine, item.SearchId);
                Assert.Null(item.Cabin);
                Assert.Empty(item.FlightSets);
            }
        );
        Assert.Empty(expansion.FlightSets);
    }



    [Fact]
    public void Expand_when_given_a_stay_sets_check_out_to_check_in_plus_the_nights()
    {
        var expansion = Expander.Expand([Search(Mine) with { NightCount = 5 }]);

        var key = Assert.Single(expansion.Requests).Key;
        Assert.Equal(March10, key.CheckIn);
        Assert.Equal(new DateOnly(2027, 3, 15), key.CheckOut);
        Assert.Equal(2, key.AdultCount);
    }



    [Fact]
    public void Expand_when_the_stay_is_one_night_plans_a_check_out_the_next_day()
    {
        var expansion = Expander.Expand([Search(Mine) with { NightCount = 1 }]);

        Assert.Equal(March10.AddDays(1), Assert.Single(expansion.Requests).Key.CheckOut);
    }



    [Fact]
    public void Expand_when_two_searches_want_the_same_lookup_plans_it_once_with_an_item_for_each()
    {
        var expansion = Expander.Expand([Search(Mine), Search(Theirs)]);

        Assert.Equal([Lookup(MontegoBay, March10)], expansion.Requests.Select(request => request.Key));
        Assert.Equal([Mine, Theirs], expansion.Items.Select(item => item.SearchId));
        Assert.All(expansion.Items, item => Assert.Equal(Lookup(MontegoBay, March10), item.Request));
    }



    [Theory]
    [InlineData(25, 75, 75)]
    [InlineData(75, 25, 75)]
    [InlineData(50, 50, 50)]
    public void Expand_when_searches_share_a_lookup_gives_it_the_highest_run_order(int mine, int theirs, int expected)
    {
        var expansion = Expander.Expand
        (
            [
                Search(Mine) with { RunOrder = mine },
                Search(Theirs) with { RunOrder = theirs },
            ]
        );

        Assert.Equal(expected, Assert.Single(expansion.Requests).RunOrder);
    }



    [Theory]
    [InlineData(8, 2, 0, 0)]
    [InlineData(7, 3, 0, 0)]
    [InlineData(7, 2, 1, 0)]
    [InlineData(7, 2, 0, 1)]
    public void Expand_when_the_stay_or_the_travellers_differ_plans_separate_lookups
    (
        int nights,
        int adults,
        int children,
        int infants
    )
    {
        var other = Search(Theirs) with
        {
            NightCount = nights,
            AdultCount = adults,
            ChildCount = children,
            InfantCount = infants,
        };

        var expansion = Expander.Expand([Search(Mine), other]);

        Assert.Equal(2, expansion.Requests.Count);
        Assert.Equal
        (
            new LookupKey(MontegoBay, March10, March10.AddDays(nights), adults, children, infants),
            expansion.Requests[1].Key
        );
    }



    [Fact]
    public void Expand_when_a_search_lists_a_resort_twice_plans_its_lookups_once()
    {
        var search = Search(Mine) with
        {
            Resorts = [new ExpandResort(MontegoBay, JamaicaGateway), new ExpandResort(MontegoBay, JamaicaGateway)],
        };

        var expansion = Expander.Expand([search]);

        Assert.Single(expansion.Requests);
        Assert.Single(expansion.Items);
    }



    [Fact]
    public void Expand_when_a_search_includes_flights_plans_one_flight_set_per_departure_airport()
    {
        var expansion = Expander.Expand([WithFlights(Search(Mine), Cabin.Economy, Philadelphia, Newark)]);

        var item = Assert.Single(expansion.Items);
        Assert.Equal(Cabin.Economy, item.Cabin);
        Assert.Equal
        (
            [
                Flights(Philadelphia, JamaicaGateway, March10, Cabin.Economy),
                Flights(Newark, JamaicaGateway, March10, Cabin.Economy),
            ],
            item.FlightSets
        );
        Assert.Equal(item.FlightSets, expansion.FlightSets);
    }



    [Fact]
    public void Expand_when_two_resorts_share_a_gateway_plans_their_flights_once()
    {
        var search = WithFlights(Search(Mine), Cabin.Economy, Philadelphia) with
        {
            Resorts =
            [
                new ExpandResort(MontegoBay, JamaicaGateway),
                new ExpandResort(Negril, JamaicaGateway),
                new ExpandResort(SaintLucia, SaintLuciaGateway),
            ],
        };

        var expansion = Expander.Expand([search]);

        Assert.Equal(3, expansion.Requests.Count);
        Assert.Equal
        (
            [
                Flights(Philadelphia, JamaicaGateway, March10, Cabin.Economy),
                Flights(Philadelphia, SaintLuciaGateway, March10, Cabin.Economy),
            ],
            expansion.FlightSets
        );
        Assert.Equal(expansion.Items[0].FlightSets, expansion.Items[1].FlightSets);
    }



    [Fact]
    public void Expand_when_two_searches_differ_only_in_cabin_shares_the_room_lookup_and_not_the_flights()
    {
        var expansion = Expander.Expand
        (
            [
                WithFlights(Search(Mine), Cabin.Economy, Philadelphia),
                WithFlights(Search(Theirs), Cabin.Business, Philadelphia),
            ]
        );

        Assert.Single(expansion.Requests);
        Assert.Equal([Cabin.Economy, Cabin.Business], expansion.Items.Select(item => item.Cabin));
        Assert.Equal
        (
            [
                Flights(Philadelphia, JamaicaGateway, March10, Cabin.Economy),
                Flights(Philadelphia, JamaicaGateway, March10, Cabin.Business),
            ],
            expansion.FlightSets
        );
    }



    [Fact]
    public void Expand_when_two_searches_want_the_same_flights_plans_them_once()
    {
        var expansion = Expander.Expand
        (
            [
                WithFlights(Search(Mine), Cabin.Economy, Philadelphia),
                WithFlights(Search(Theirs), Cabin.Economy, Philadelphia, Newark),
            ]
        );

        Assert.Equal
        (
            [
                Flights(Philadelphia, JamaicaGateway, March10, Cabin.Economy),
                Flights(Newark, JamaicaGateway, March10, Cabin.Economy),
            ],
            expansion.FlightSets
        );
        Assert.Single(expansion.Items[0].FlightSets);
        Assert.Equal(2, expansion.Items[1].FlightSets.Count);
    }



    [Fact]
    public void Expand_when_a_search_lists_an_airport_twice_plans_its_flights_once()
    {
        var expansion = Expander.Expand([WithFlights(Search(Mine), Cabin.Economy, Philadelphia, Philadelphia)]);

        Assert.Single(expansion.FlightSets);
        Assert.Single(Assert.Single(expansion.Items).FlightSets);
    }



    [Fact]
    public void Expand_when_flights_span_several_dates_departs_on_check_in_and_returns_on_check_out()
    {
        var expansion = Expander.Expand([WithFlights(Search(Mine, days: 2), Cabin.First, Philadelphia)]);

        Assert.Equal
        (
            [
                new FlightSetKey(Philadelphia, JamaicaGateway, March10, March10.AddDays(7), Cabin.First, 2, 0, 0),
                new FlightSetKey(Philadelphia, JamaicaGateway, March10.AddDays(1), March10.AddDays(8), Cabin.First, 2, 0, 0),
            ],
            expansion.FlightSets
        );
    }



    [Fact]
    public void Expand_when_the_travellers_differ_plans_separate_flight_sets()
    {
        var family = WithFlights(Search(Theirs), Cabin.Economy, Philadelphia) with { ChildCount = 2, InfantCount = 1 };

        var expansion = Expander.Expand([WithFlights(Search(Mine), Cabin.Economy, Philadelphia), family]);

        Assert.Equal(2, expansion.FlightSets.Count);
        Assert.Equal
        (
            new FlightSetKey(Philadelphia, JamaicaGateway, March10, March10.AddDays(7), Cabin.Economy, 2, 2, 1),
            expansion.FlightSets[1]
        );
    }



    [Fact]
    public void Expand_when_a_family_search_includes_flights_carries_every_part_into_both_keys()
    {
        var family = WithFlights(Search(Mine), Cabin.PremiumEconomy, Newark) with
        {
            NightCount = 4,
            AdultCount = 2,
            ChildCount = 3,
            InfantCount = 1,
        };

        var expansion = Expander.Expand([family]);

        var lookup = Assert.Single(expansion.Requests).Key;
        Assert.Equal(MontegoBay, lookup.ResortId);
        Assert.Equal(March10, lookup.CheckIn);
        Assert.Equal(March10.AddDays(4), lookup.CheckOut);
        Assert.Equal(2, lookup.AdultCount);
        Assert.Equal(3, lookup.ChildCount);
        Assert.Equal(1, lookup.InfantCount);

        var flights = Assert.Single(expansion.FlightSets);
        Assert.Equal(Newark, flights.OriginAirportId);
        Assert.Equal(JamaicaGateway, flights.GatewayAirportId);
        Assert.Equal(March10, flights.Depart);
        Assert.Equal(March10.AddDays(4), flights.Return);
        Assert.Equal(Cabin.PremiumEconomy, flights.Cabin);
        Assert.Equal(2, flights.AdultCount);
        Assert.Equal(3, flights.ChildCount);
        Assert.Equal(1, flights.InfantCount);
    }



    [Fact]
    public void Expand_when_the_searches_are_null_throws()
    {
        Assert.Throws<ArgumentNullException>(() => Expander.Expand(null!));
    }



    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Expand_when_a_search_has_no_nights_throws_naming_the_search(int nights)
    {
        var exception = Assert.Throws<ArgumentException>(() => Expander.Expand([Search(Mine) with { NightCount = nights }]));

        Assert.Equal("searches", exception.ParamName);
        Assert.Contains(Mine.ToString(), exception.Message, StringComparison.Ordinal);
        Assert.Contains("a stay is at least one", exception.Message, StringComparison.Ordinal);
    }



    [Fact]
    public void Expand_when_a_search_has_a_cabin_and_no_airport_throws()
    {
        var exception = Assert.Throws<ArgumentException>
        (
            () => Expander.Expand([Search(Mine) with { Cabin = Cabin.Economy }])
        );

        Assert.Equal("searches", exception.ParamName);
        Assert.Contains("both a cabin and a departure airport, or neither", exception.Message, StringComparison.Ordinal);
    }



    [Fact]
    public void Expand_when_a_search_has_an_airport_and_no_cabin_throws()
    {
        var exception = Assert.Throws<ArgumentException>
        (
            () => Expander.Expand([Search(Mine) with { DepartureAirportIds = [Philadelphia] }])
        );

        Assert.Contains(Mine.ToString(), exception.Message, StringComparison.Ordinal);
    }



    private static SearchToExpand Search(Guid id, int days = 1)
    {
        return new SearchToExpand
        {
            SearchId = id,
            Resorts = [new ExpandResort(MontegoBay, JamaicaGateway)],
            Window = new ResolvedWindow(March10, March10.AddDays(days - 1)),
            NightCount = 7,
            AdultCount = 2,
            RunOrder = 50,
        };
    }



    private static SearchToExpand WithFlights(SearchToExpand search, Cabin cabin, params Guid[] airports)
    {
        return search with { Cabin = cabin, DepartureAirportIds = airports };
    }



    private static LookupKey Lookup(Guid resort, DateOnly checkIn)
    {
        return new LookupKey(resort, checkIn, checkIn.AddDays(7), 2, 0, 0);
    }



    private static FlightSetKey Flights
    (
        Guid origin,
        Guid gateway,
        DateOnly depart,
        Cabin cabin
    )
    {
        return new FlightSetKey(origin, gateway, depart, depart.AddDays(7), cabin, 2, 0, 0);
    }



    private static Guid Id(int value)
    {
        return new Guid(value, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    }
}
