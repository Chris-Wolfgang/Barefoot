using Wolfgang.Barefoot.Contracts.Searches;
using Wolfgang.Barefoot.Core.Keys;
using Wolfgang.Barefoot.Core.Validation;

namespace Wolfgang.Barefoot.Core.Searches;

/// <summary>
/// The rules a search definition must meet (DESIGN §4). The editor and the API both call this one
/// validator, so a value the editor accepts is a value the API accepts.
/// </summary>
/// <remarks>
/// Every broken rule is reported, each with the field it is about and a typed code from
/// <see cref="SearchErrors"/>. The rules that need the database — the name is unique per owner, the
/// resorts and airports exist, the recipients are the owner's, the owner's active-search limit — are not here; they arrive with the
/// layer that can check them.
/// </remarks>
public static class SearchValidator
{
    /// <summary>The lowest run order: the search runs last.</summary>
    public const int LowestRunOrder = 1;



    /// <summary>The run order a new search starts with.</summary>
    public const int NormalRunOrder = SearchDefinition.NormalRunOrder;



    /// <summary>The highest run order: the search runs first.</summary>
    public const int HighestRunOrder = 99;



    /// <summary>
    /// Checks a definition against every rule.
    /// </summary>
    /// <param name="definition">What was entered.</param>
    /// <returns>One failure per broken rule and field, in the order the fields are declared; empty when valid.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="definition"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<ValidationFailure> Validate(SearchDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var failures = new List<ValidationFailure>();

        CheckWhatToSearch(definition, failures);
        CheckWindow(definition, failures);
        CheckFlights(definition, failures);
        CheckFilters(definition, failures);
        CheckRecipients(definition, failures);

        return failures;
    }



    /// <summary>
    /// The check-in window a valid definition describes.
    /// </summary>
    /// <param name="definition">A definition <see cref="Validate"/> reports no failure for.</param>
    /// <exception cref="ArgumentNullException"><paramref name="definition"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The definition's window fields are not valid.</exception>
    public static CheckInWindow BuildWindow(SearchDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var failures = new List<ValidationFailure>();
        CheckWindow(definition, failures);

        if (failures.Count > 0)
        {
            throw new ArgumentException($"The definition's check-in window is not valid: {failures[0].Message}", nameof(definition));
        }

        return definition.WindowKind == WindowKind.Recurring
            ? new RecurringWindow
            (
                new MonthDay(definition.RecurringFirstMonth!.Value, definition.RecurringFirstDay!.Value),
                new MonthDay(definition.RecurringLastMonth!.Value, definition.RecurringLastDay!.Value)
            )
            : new OneTimeWindow(definition.FirstCheckIn!.Value, definition.LastCheckIn!.Value);
    }



    private static void CheckWhatToSearch(SearchDefinition definition, List<ValidationFailure> failures)
    {
        if (string.IsNullOrWhiteSpace(definition.Name))
        {
            failures.Add(ValidationFailure.Of(SearchErrors.NameRequired, nameof(definition.Name)));
        }

        if (!Enum.IsDefined(definition.Brand))
        {
            failures.Add(ValidationFailure.Of(SearchErrors.BrandUnknown, nameof(definition.Brand), (int)definition.Brand));
        }

        if (definition.ResortIds.Count == 0)
        {
            failures.Add(ValidationFailure.Of(SearchErrors.ResortsRequired, nameof(definition.ResortIds)));
        }

        if (definition.Nights < 1)
        {
            failures.Add(ValidationFailure.Of(SearchErrors.NightsNotPositive, nameof(definition.Nights), definition.Nights));
        }

        if (definition.Adults < 1)
        {
            failures.Add(ValidationFailure.Of(SearchErrors.AdultsNotPositive, nameof(definition.Adults), definition.Adults));
        }

        CheckYoungTravellers(definition.Children, nameof(definition.Children), definition.Brand, failures);
        CheckYoungTravellers(definition.Infants, nameof(definition.Infants), definition.Brand, failures);

        if (definition.RunOrder is < LowestRunOrder or > HighestRunOrder)
        {
            failures.Add
            (
                ValidationFailure.Of
                (
                    SearchErrors.RunOrderOutOfRange,
                    nameof(definition.RunOrder),
                    definition.RunOrder,
                    LowestRunOrder,
                    HighestRunOrder
                )
            );
        }
    }



    private static void CheckYoungTravellers
    (
        int count,
        string field,
        Brand brand,
        List<ValidationFailure> failures
    )
    {
        if (count < 0)
        {
            failures.Add(ValidationFailure.Of(SearchErrors.TravellersNegative, field, count));
        }
        else if (count > 0 && brand == Brand.Sandals)
        {
            failures.Add(ValidationFailure.Of(SearchErrors.ChildrenNeedBeaches, field, brand));
        }
    }



    private static void CheckWindow(SearchDefinition definition, List<ValidationFailure> failures)
    {
        switch (definition.WindowKind)
        {
            case WindowKind.Recurring:
                CheckRecurringWindow(definition, failures);
                break;

            case WindowKind.OneTime:
                CheckOneTimeWindow(definition, failures);
                break;

            default:
                failures.Add(ValidationFailure.Of(SearchErrors.WindowKindUnknown, nameof(definition.WindowKind), (int)definition.WindowKind));
                break;
        }
    }



    private static void CheckRecurringWindow(SearchDefinition definition, List<ValidationFailure> failures)
    {
        CheckMonthDay
        (
            definition.RecurringFirstMonth,
            nameof(definition.RecurringFirstMonth),
            definition.RecurringFirstDay,
            nameof(definition.RecurringFirstDay),
            failures
        );
        CheckMonthDay
        (
            definition.RecurringLastMonth,
            nameof(definition.RecurringLastMonth),
            definition.RecurringLastDay,
            nameof(definition.RecurringLastDay),
            failures
        );

        RefuseOtherKind(definition.FirstCheckIn.HasValue, nameof(definition.FirstCheckIn), failures);
        RefuseOtherKind(definition.LastCheckIn.HasValue, nameof(definition.LastCheckIn), failures);
    }



    private static void CheckMonthDay
    (
        int? month,
        string monthField,
        int? day,
        string dayField,
        List<ValidationFailure> failures
    )
    {
        if (month is null)
        {
            failures.Add(ValidationFailure.Of(SearchErrors.WindowIncomplete, monthField));
        }

        if (day is null)
        {
            failures.Add(ValidationFailure.Of(SearchErrors.WindowIncomplete, dayField));
        }

        if (month is { } m && day is { } d && !IsDayOfYear(m, d))
        {
            failures.Add(ValidationFailure.Of(SearchErrors.WindowDayInvalid, m is < 1 or > 12 ? monthField : dayField, m, d));
        }
    }



    private static bool IsDayOfYear(int month, int day)
    {
        // 2024 is a leap year: 29 February is a day a recurring window may name.
        return month is >= 1 and <= 12 && day >= 1 && day <= DateTime.DaysInMonth(2024, month);
    }



    private static void CheckOneTimeWindow(SearchDefinition definition, List<ValidationFailure> failures)
    {
        if (definition.FirstCheckIn is null)
        {
            failures.Add(ValidationFailure.Of(SearchErrors.WindowIncomplete, nameof(definition.FirstCheckIn)));
        }

        if (definition.LastCheckIn is null)
        {
            failures.Add(ValidationFailure.Of(SearchErrors.WindowIncomplete, nameof(definition.LastCheckIn)));
        }

        if (definition.FirstCheckIn is { } first && definition.LastCheckIn is { } last && last < first)
        {
            failures.Add(ValidationFailure.Of(SearchErrors.WindowDatesReversed, nameof(definition.LastCheckIn), first, last));
        }

        RefuseOtherKind(definition.RecurringFirstMonth.HasValue, nameof(definition.RecurringFirstMonth), failures);
        RefuseOtherKind(definition.RecurringFirstDay.HasValue, nameof(definition.RecurringFirstDay), failures);
        RefuseOtherKind(definition.RecurringLastMonth.HasValue, nameof(definition.RecurringLastMonth), failures);
        RefuseOtherKind(definition.RecurringLastDay.HasValue, nameof(definition.RecurringLastDay), failures);
    }



    private static void RefuseOtherKind(bool isSet, string field, List<ValidationFailure> failures)
    {
        if (isSet)
        {
            failures.Add(ValidationFailure.Of(SearchErrors.WindowModeMixed, field));
        }
    }



    private static void CheckFlights(SearchDefinition definition, List<ValidationFailure> failures)
    {
        if (definition.Cabin is { } cabin && !Enum.IsDefined(cabin))
        {
            failures.Add(ValidationFailure.Of(SearchErrors.CabinUnknown, nameof(definition.Cabin), (int)cabin));
        }

        if (definition.IncludeFlights)
        {
            if (definition.Cabin is null)
            {
                failures.Add(ValidationFailure.Of(SearchErrors.CabinRequired, nameof(definition.Cabin)));
            }

            if (definition.DepartureAirportIds.Count == 0)
            {
                failures.Add(ValidationFailure.Of(SearchErrors.AirportsRequired, nameof(definition.DepartureAirportIds)));
            }

            return;
        }

        RefuseWithoutFlights(definition.Cabin.HasValue, nameof(definition.Cabin), failures);
        RefuseWithoutFlights(definition.DepartureAirportIds.Count > 0, nameof(definition.DepartureAirportIds), failures);
        RefuseWithoutFlights(definition.MaxFlightMinutes.HasValue, nameof(definition.MaxFlightMinutes), failures);
        RefuseWithoutFlights(definition.MaxStops.HasValue, nameof(definition.MaxStops), failures);
    }



    private static void RefuseWithoutFlights(bool isSet, string field, List<ValidationFailure> failures)
    {
        if (isSet)
        {
            failures.Add(ValidationFailure.Of(SearchErrors.FlightFieldWithoutFlights, field));
        }
    }



    private static void CheckFilters(SearchDefinition definition, List<ValidationFailure> failures)
    {
        if (definition.MaxTotalPrice is <= 0)
        {
            failures.Add(ValidationFailure.Of(SearchErrors.LimitNotPositive, nameof(definition.MaxTotalPrice), definition.MaxTotalPrice));
        }

        if (definition.MaxDriveMinutes is <= 0)
        {
            failures.Add(ValidationFailure.Of(SearchErrors.LimitNotPositive, nameof(definition.MaxDriveMinutes), definition.MaxDriveMinutes));
        }

        if (definition.MaxFlightMinutes is <= 0)
        {
            failures.Add(ValidationFailure.Of(SearchErrors.LimitNotPositive, nameof(definition.MaxFlightMinutes), definition.MaxFlightMinutes));
        }

        if (definition.MaxStops is < 0)
        {
            failures.Add(ValidationFailure.Of(SearchErrors.MaxStopsNegative, nameof(definition.MaxStops), definition.MaxStops));
        }

        RefuseRepeats(definition.RoomLevelIds, SearchErrors.FilterValueRepeated, nameof(definition.RoomLevelIds), failures);
        RefuseRepeats(definition.RoomViewIds, SearchErrors.FilterValueRepeated, nameof(definition.RoomViewIds), failures);
        RefuseRepeats(definition.RoomTypeIds, SearchErrors.FilterValueRepeated, nameof(definition.RoomTypeIds), failures);
        RefuseRepeats(definition.MustHaveFeatureIds, SearchErrors.FilterValueRepeated, nameof(definition.MustHaveFeatureIds), failures);
    }


    private static void CheckRecipients(SearchDefinition definition, List<ValidationFailure> failures)
    {
        RefuseRepeats(definition.RecipientIds, SearchErrors.RecipientRepeated, nameof(definition.RecipientIds), failures);
    }



    /// <summary>Reports each id that is in the list more than once, once however often it repeats.</summary>
    private static void RefuseRepeats
    (
        IReadOnlyList<Guid> ids,
        ErrorCode code,
        string field,
        List<ValidationFailure> failures
    )
    {
        failures.AddRange
        (
            ids
                .GroupBy(id => id)
                .Where(group => group.Skip(1).Any())
                .Select(group => ValidationFailure.Of(code, field, group.Key))
        );
    }
}
