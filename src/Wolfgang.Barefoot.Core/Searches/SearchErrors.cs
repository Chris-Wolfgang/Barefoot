using Wolfgang.Barefoot.Core.Keys;

namespace Wolfgang.Barefoot.Core.Searches;

/// <summary>
/// The error codes of the rules a search definition must meet. Each is a <c>422</c>: the request is
/// well-formed and the rules refuse it (ADR 0012 §2).
/// </summary>
public static class SearchErrors
{
    private const int Refused = 422;



    /// <summary>The name is empty.</summary>
    public static readonly ErrorCode NameRequired =
        new("search.name_required", Refused, "Give the search a name.", ErrorSeverity.Warning);

    /// <summary>The brand is not one this version knows.</summary>
    public static readonly ErrorCode BrandUnknown =
        new("search.brand_unknown", Refused, "'{0}' is not a brand.", ErrorSeverity.Warning);

    /// <summary>No resort is chosen.</summary>
    public static readonly ErrorCode ResortsRequired =
        new("search.resorts_required", Refused, "Choose at least one resort.", ErrorSeverity.Warning);

    /// <summary>The stay is shorter than one night.</summary>
    public static readonly ErrorCode NightsNotPositive =
        new("search.nights_not_positive", Refused, "A stay is at least 1 night; {0} was given.", ErrorSeverity.Warning);

    /// <summary>Fewer than one adult.</summary>
    public static readonly ErrorCode AdultsNotPositive =
        new("search.adults_not_positive", Refused, "A search needs at least 1 adult; {0} was given.", ErrorSeverity.Warning);

    /// <summary>A count of children or infants is below zero.</summary>
    public static readonly ErrorCode TravellersNegative =
        new("search.travellers_negative", Refused, "A number of travellers cannot be below 0; {0} was given.", ErrorSeverity.Warning);

    /// <summary>Children or infants on a search for an adults-only brand.</summary>
    public static readonly ErrorCode ChildrenNeedBeaches =
        new("search.children_need_beaches", Refused, "{0} resorts are adults-only. Children and infants can travel only on a Beaches search.", ErrorSeverity.Warning);

    /// <summary>The window kind is not one this version knows.</summary>
    public static readonly ErrorCode WindowKindUnknown =
        new("search.window_kind_unknown", Refused, "'{0}' is not a kind of check-in window.", ErrorSeverity.Warning);

    /// <summary>A field the chosen window kind needs is missing.</summary>
    public static readonly ErrorCode WindowIncomplete =
        new("search.window_incomplete", Refused, "The check-in window is missing a value.", ErrorSeverity.Warning);

    /// <summary>A field of the other window kind is set.</summary>
    public static readonly ErrorCode WindowModeMixed =
        new("search.window_mode_mixed", Refused, "A check-in window is either recurring or one-time. This value belongs to the other kind.", ErrorSeverity.Warning);

    /// <summary>A recurring window names a day that does not exist.</summary>
    public static readonly ErrorCode WindowDayInvalid =
        new("search.window_day_invalid", Refused, "There is no day {1} in month {0}.", ErrorSeverity.Warning);

    /// <summary>A one-time window ends before it starts.</summary>
    public static readonly ErrorCode WindowDatesReversed =
        new("search.window_dates_reversed", Refused, "The last check-in date, {1:yyyy-MM-dd}, is before the first, {0:yyyy-MM-dd}.", ErrorSeverity.Warning);

    /// <summary>Flights are included and no departure airport is chosen.</summary>
    public static readonly ErrorCode AirportsRequired =
        new("search.airports_required", Refused, "Choose at least one departure airport, or turn flights off.", ErrorSeverity.Warning);

    /// <summary>Flights are included and no cabin is chosen.</summary>
    public static readonly ErrorCode CabinRequired =
        new("search.cabin_required", Refused, "Choose a cabin, or turn flights off.", ErrorSeverity.Warning);

    /// <summary>The cabin is not one this version knows.</summary>
    public static readonly ErrorCode CabinUnknown =
        new("search.cabin_unknown", Refused, "'{0}' is not a cabin.", ErrorSeverity.Warning);

    /// <summary>A value that only matters for flights is set while flights are off.</summary>
    public static readonly ErrorCode FlightFieldWithoutFlights =
        new("search.flight_field_without_flights", Refused, "This applies to flights, and the search does not include flights.", ErrorSeverity.Warning);

    /// <summary>A limit that must be greater than zero is not.</summary>
    public static readonly ErrorCode LimitNotPositive =
        new("search.limit_not_positive", Refused, "This limit must be greater than 0; {0} was given.", ErrorSeverity.Warning);

    /// <summary>The most stops is below zero.</summary>
    public static readonly ErrorCode MaxStopsNegative =
        new("search.max_stops_negative", Refused, "The most stops cannot be below 0; {0} was given.", ErrorSeverity.Warning);

    /// <summary>A recipient is chosen more than once.</summary>
    public static readonly ErrorCode RecipientRepeated =
        new("search.recipient_repeated", Refused, "A recipient can be chosen once per search; {0} is chosen more than once.", ErrorSeverity.Warning);

    /// <summary>The run order is outside its range.</summary>
    public static readonly ErrorCode RunOrderOutOfRange =
        new("search.run_order_out_of_range", Refused, "The run order is between {1} and {2}; {0} was given.", ErrorSeverity.Warning);
}
