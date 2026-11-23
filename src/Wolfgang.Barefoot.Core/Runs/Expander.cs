namespace Wolfgang.Barefoot.Core.Runs;

/// <summary>
/// Turns the active searches into the work list of a run: every resort on every check-in date is one
/// room lookup, and identical lookups wanted by several searches are performed once (DESIGN §5 and
/// §8; ADR 0004).
/// </summary>
/// <remarks>
/// A pure function. The caller resolves each search's window and supplies its resorts, so the result
/// depends on nothing but the argument. The same function gives the editor its cost figures: the
/// number of room and flight lookups one search adds.
/// </remarks>
public static class Expander
{
    /// <summary>
    /// The work list for <paramref name="searches"/>.
    /// </summary>
    /// <param name="searches">
    /// The active searches, in the order they should be considered: lookups are listed in the order
    /// they are first wanted.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="searches"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// A search has fewer than one night, or has a cabin without a departure airport, or a departure
    /// airport without a cabin.
    /// </exception>
    public static Expansion Expand(IReadOnlyList<SearchToExpand> searches)
    {
        ArgumentNullException.ThrowIfNull(searches);

        var runOrders = new Dictionary<LookupKey, int>();
        var requestOrder = new List<LookupKey>();
        var flightSets = new HashSet<FlightSetKey>();
        var flightSetOrder = new List<FlightSetKey>();
        var items = new List<PlannedItem>();

        foreach (var search in searches)
        {
            Require(search, nameof(searches));

            var airports = search
                .DepartureAirportIds
                .Distinct()
                .ToList();

            foreach (var key in LookupsOf(search))
            {
                if (runOrders.TryGetValue(key.Lookup, out var known))
                {
                    runOrders[key.Lookup] = Math.Max(known, search.RunOrder);
                }
                else
                {
                    runOrders[key.Lookup] = search.RunOrder;
                    requestOrder.Add(key.Lookup);
                }

                var wanted = airports
                    .Select
                    (
                        airport => new FlightSetKey
                        (
                            airport,
                            key.GatewayAirportId,
                            key.Lookup.CheckIn,
                            key.Lookup.CheckOut,
                            search.Cabin!.Value,
                            search.Adults,
                            search.Children,
                            search.Infants
                        )
                    )
                    .ToList();

                flightSetOrder.AddRange(wanted.Where(flightSets.Add));
                items.Add(new PlannedItem(search.SearchId, key.Lookup, search.Cabin, wanted));
            }
        }

        return new Expansion
        (
            requestOrder
                .Select(key => new PlannedRequest(key, runOrders[key]))
                .ToList(),
            flightSetOrder,
            items
        );
    }



    /// <summary>
    /// Every distinct room lookup one search wants, resort by resort and date by date, each with the
    /// gateway airport of its resort.
    /// </summary>
    private static IEnumerable<(LookupKey Lookup, Guid GatewayAirportId)> LookupsOf(SearchToExpand search)
    {
        return search
            .Resorts
            .DistinctBy(resort => resort.ResortId)
            .SelectMany
            (
                resort => search
                    .Window
                    .Dates()
                    .Select
                    (
                        checkIn =>
                        (
                            new LookupKey
                            (
                                resort.ResortId,
                                checkIn,
                                checkIn.AddDays(search.Nights),
                                search.Adults,
                                search.Children,
                                search.Infants
                            ),
                            resort.GatewayAirportId
                        )
                    )
            );
    }



    private static void Require(SearchToExpand search, string parameterName)
    {
        if (search.Nights < 1)
        {
            throw new ArgumentException($"Search {search.SearchId} has {search.Nights} night(s); a stay is at least one.", parameterName);
        }

        if (search.Cabin is null != (search.DepartureAirportIds.Count == 0))
        {
            throw new ArgumentException
            (
                $"Search {search.SearchId} must have both a cabin and a departure airport, or neither.",
                parameterName
            );
        }
    }
}
