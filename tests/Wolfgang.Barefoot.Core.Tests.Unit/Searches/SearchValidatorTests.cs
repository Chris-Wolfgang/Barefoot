using Wolfgang.Barefoot.Core.Keys;
using Wolfgang.Barefoot.Core.Searches;
using Wolfgang.Barefoot.Core.Validation;

namespace Wolfgang.Barefoot.Core.Tests.Unit.Searches;

public sealed class SearchValidatorTests
{
    private static readonly Guid Resort = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly Guid Airport = Guid.Parse("22222222-2222-2222-2222-222222222222");



    [Fact]
    public void Validate_when_a_recurring_search_without_flights_is_complete_reports_nothing()
    {
        Assert.Empty(SearchValidator.Validate(Recurring()));
    }



    [Fact]
    public void Validate_when_a_one_time_Beaches_search_with_flights_and_every_filter_is_complete_reports_nothing()
    {
        var definition = OneTime() with
        {
            Brand = Brand.Beaches,
            Children = 2,
            Infants = 1,
            IncludeFlights = true,
            Cabin = Cabin.PremiumEconomy,
            DepartureAirportIds = [Airport],
            MaxTotalPrice = 0.01m,
            MaxDriveMinutes = 1,
            MaxFlightMinutes = 1,
            MaxStops = 0,
            RunOrder = 99,
        };

        Assert.Empty(SearchValidator.Validate(definition));
    }



    [Fact]
    public void Validate_when_the_definition_is_null_throws()
    {
        Assert.Throws<ArgumentNullException>(() => SearchValidator.Validate(null!));
    }



    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Validate_when_the_name_is_empty_reports_name_required(string name)
    {
        AssertOnly(Recurring() with { Name = name }, SearchErrors.NameRequired, "Name", "Give the search a name.");
    }



    [Fact]
    public void Validate_when_the_brand_is_not_defined_reports_brand_unknown()
    {
        AssertOnly(Recurring() with { Brand = (Brand)7 }, SearchErrors.BrandUnknown, "Brand", "'7' is not a brand.");
    }



    [Fact]
    public void Validate_when_no_resort_is_chosen_reports_resorts_required()
    {
        AssertOnly(Recurring() with { ResortIds = [] }, SearchErrors.ResortsRequired, "ResortIds", "Choose at least one resort.");
    }



    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Validate_when_nights_is_not_positive_reports_it_with_the_value(int nights)
    {
        AssertOnly
        (
            Recurring() with { Nights = nights },
            SearchErrors.NightsNotPositive,
            "Nights",
            string.Create(System.Globalization.CultureInfo.InvariantCulture, $"A stay is at least 1 night; {nights} was given.")
        );
    }



    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void Validate_when_adults_is_not_positive_reports_it_with_the_value(int adults)
    {
        AssertOnly
        (
            Recurring() with { Adults = adults },
            SearchErrors.AdultsNotPositive,
            "Adults",
            string.Create(System.Globalization.CultureInfo.InvariantCulture, $"A search needs at least 1 adult; {adults} was given.")
        );
    }



    [Fact]
    public void Validate_when_children_or_infants_are_negative_reports_each_field()
    {
        var failures = SearchValidator.Validate(Recurring() with { Brand = Brand.Beaches, Children = -1, Infants = -3 });

        Assert.Equal
        (
            [
                (SearchErrors.TravellersNegative, "Children", "A number of travellers cannot be below 0; -1 was given."),
                (SearchErrors.TravellersNegative, "Infants", "A number of travellers cannot be below 0; -3 was given."),
            ],
            failures.Select(Shape)
        );
    }



    [Fact]
    public void Validate_when_a_Sandals_search_has_children_or_infants_reports_each_field()
    {
        var failures = SearchValidator.Validate(Recurring() with { Children = 1, Infants = 2 });

        const string Message = "Sandals resorts are adults-only. Children and infants can travel only on a Beaches search.";
        Assert.Equal
        (
            [
                (SearchErrors.ChildrenNeedBeaches, "Children", Message),
                (SearchErrors.ChildrenNeedBeaches, "Infants", Message),
            ],
            failures.Select(Shape)
        );
    }



    [Fact]
    public void Validate_when_a_Beaches_search_has_children_and_infants_reports_nothing()
    {
        Assert.Empty(SearchValidator.Validate(Recurring() with { Brand = Brand.Beaches, Children = 3, Infants = 1 }));
    }



    [Theory]
    [InlineData(1, true)]
    [InlineData(50, true)]
    [InlineData(99, true)]
    [InlineData(0, false)]
    [InlineData(100, false)]
    [InlineData(-5, false)]
    public void Validate_when_the_run_order_is_given_accepts_1_to_99_only(int runOrder, bool valid)
    {
        var failures = SearchValidator.Validate(Recurring() with { RunOrder = runOrder });

        if (valid)
        {
            Assert.Empty(failures);
        }
        else
        {
            var failure = Assert.Single(failures);
            Assert.Same(SearchErrors.RunOrderOutOfRange, failure.Code);
            Assert.Equal("RunOrder", failure.Field);
            Assert.Equal(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"The run order is between 1 and 99; {runOrder} was given."), failure.Message);
        }
    }



    [Fact]
    public void Validate_when_no_run_order_is_given_uses_normal()
    {
        Assert.Equal(50, Recurring().RunOrder);
        Assert.Equal(1, SearchValidator.LowestRunOrder);
        Assert.Equal(50, SearchValidator.NormalRunOrder);
        Assert.Equal(99, SearchValidator.HighestRunOrder);
    }



    [Fact]
    public void Validate_when_the_window_kind_is_not_defined_reports_window_kind_unknown()
    {
        AssertOnly
        (
            Recurring() with { WindowKind = (WindowKind)5 },
            SearchErrors.WindowKindUnknown,
            "WindowKind",
            "'5' is not a kind of check-in window."
        );
    }



    [Fact]
    public void Validate_when_a_recurring_window_has_no_values_reports_each_missing_field()
    {
        var definition = Recurring() with
        {
            RecurringFirstMonth = null,
            RecurringFirstDay = null,
            RecurringLastMonth = null,
            RecurringLastDay = null,
        };

        Assert.Equal
        (
            ["RecurringFirstMonth", "RecurringFirstDay", "RecurringLastMonth", "RecurringLastDay"],
            SearchValidator.Validate(definition).Select(failure => failure.Field)
        );
        Assert.All(SearchValidator.Validate(definition), failure => Assert.Same(SearchErrors.WindowIncomplete, failure.Code));
    }



    [Fact]
    public void Validate_when_a_recurring_window_has_a_month_but_no_day_reports_only_the_day()
    {
        AssertOnly
        (
            Recurring() with { RecurringLastDay = null },
            SearchErrors.WindowIncomplete,
            "RecurringLastDay",
            "The check-in window is missing a value."
        );
    }



    [Theory]
    [InlineData(2, 30, "RecurringFirstDay", "There is no day 30 in month 2.")]
    [InlineData(4, 31, "RecurringFirstDay", "There is no day 31 in month 4.")]
    [InlineData(6, 0, "RecurringFirstDay", "There is no day 0 in month 6.")]
    [InlineData(13, 1, "RecurringFirstMonth", "There is no day 1 in month 13.")]
    [InlineData(0, 1, "RecurringFirstMonth", "There is no day 1 in month 0.")]
    public void Validate_when_a_recurring_window_starts_on_a_day_that_does_not_exist_reports_the_field_at_fault
    (
        int month,
        int day,
        string field,
        string message
    )
    {
        AssertOnly
        (
            Recurring() with { RecurringFirstMonth = month, RecurringFirstDay = day },
            SearchErrors.WindowDayInvalid,
            field,
            message
        );
    }



    [Fact]
    public void Validate_when_a_recurring_window_ends_on_a_day_that_does_not_exist_reports_the_last_day()
    {
        AssertOnly
        (
            Recurring() with { RecurringLastMonth = 11, RecurringLastDay = 31 },
            SearchErrors.WindowDayInvalid,
            "RecurringLastDay",
            "There is no day 31 in month 11."
        );
    }



    [Theory]
    [InlineData(2, 29)]
    [InlineData(1, 1)]
    [InlineData(12, 31)]
    public void Validate_when_a_recurring_window_names_an_edge_day_of_the_year_accepts_it(int month, int day)
    {
        Assert.Empty(SearchValidator.Validate(Recurring() with { RecurringFirstMonth = month, RecurringFirstDay = day }));
    }



    [Fact]
    public void Validate_when_a_recurring_window_wraps_the_year_accepts_it()
    {
        var definition = Recurring() with
        {
            RecurringFirstMonth = 12,
            RecurringFirstDay = 20,
            RecurringLastMonth = 1,
            RecurringLastDay = 5,
        };

        Assert.Empty(SearchValidator.Validate(definition));
    }



    [Fact]
    public void Validate_when_a_recurring_window_also_has_one_time_dates_reports_window_mode_mixed_for_each()
    {
        var definition = Recurring() with
        {
            FirstCheckIn = new DateOnly(2027, 6, 1),
            LastCheckIn = new DateOnly(2027, 6, 14),
        };

        const string Message = "A check-in window is either recurring or one-time. This value belongs to the other kind.";
        Assert.Equal
        (
            [
                (SearchErrors.WindowModeMixed, "FirstCheckIn", Message),
                (SearchErrors.WindowModeMixed, "LastCheckIn", Message),
            ],
            SearchValidator.Validate(definition).Select(Shape)
        );
    }



    [Fact]
    public void Validate_when_a_one_time_window_has_no_dates_reports_each_missing_field()
    {
        var failures = SearchValidator.Validate(OneTime() with { FirstCheckIn = null, LastCheckIn = null });

        Assert.Equal(["FirstCheckIn", "LastCheckIn"], failures.Select(failure => failure.Field));
        Assert.All(failures, failure => Assert.Same(SearchErrors.WindowIncomplete, failure.Code));
    }



    [Fact]
    public void Validate_when_a_one_time_window_ends_before_it_starts_reports_both_dates()
    {
        AssertOnly
        (
            OneTime() with { FirstCheckIn = new DateOnly(2027, 6, 14), LastCheckIn = new DateOnly(2027, 6, 13) },
            SearchErrors.WindowDatesReversed,
            "LastCheckIn",
            "The last check-in date, 2027-06-13, is before the first, 2027-06-14."
        );
    }



    [Fact]
    public void Validate_when_a_one_time_window_is_a_single_date_accepts_it()
    {
        var date = new DateOnly(2027, 6, 14);

        Assert.Empty(SearchValidator.Validate(OneTime() with { FirstCheckIn = date, LastCheckIn = date }));
    }



    [Fact]
    public void Validate_when_a_one_time_window_also_has_recurring_values_reports_window_mode_mixed_for_each()
    {
        var definition = OneTime() with
        {
            RecurringFirstMonth = 3,
            RecurringFirstDay = 10,
            RecurringLastMonth = 3,
            RecurringLastDay = 20,
        };

        var failures = SearchValidator.Validate(definition);

        Assert.Equal
        (
            ["RecurringFirstMonth", "RecurringFirstDay", "RecurringLastMonth", "RecurringLastDay"],
            failures.Select(failure => failure.Field)
        );
        Assert.All(failures, failure => Assert.Same(SearchErrors.WindowModeMixed, failure.Code));
    }



    [Fact]
    public void Validate_when_flights_are_on_without_a_cabin_or_an_airport_reports_both()
    {
        var failures = SearchValidator.Validate(Recurring() with { IncludeFlights = true });

        Assert.Equal
        (
            [
                (SearchErrors.CabinRequired, "Cabin", "Choose a cabin, or turn flights off."),
                (SearchErrors.AirportsRequired, "DepartureAirportIds", "Choose at least one departure airport, or turn flights off."),
            ],
            failures.Select(Shape)
        );
    }



    [Fact]
    public void Validate_when_flights_are_on_with_flight_filters_accepts_them()
    {
        var definition = Recurring() with
        {
            IncludeFlights = true,
            Cabin = Cabin.Economy,
            DepartureAirportIds = [Airport],
            MaxFlightMinutes = 600,
            MaxStops = 1,
        };

        Assert.Empty(SearchValidator.Validate(definition));
    }



    [Fact]
    public void Validate_when_the_cabin_is_not_defined_reports_cabin_unknown()
    {
        var definition = Recurring() with
        {
            IncludeFlights = true,
            Cabin = (Cabin)9,
            DepartureAirportIds = [Airport],
        };

        AssertOnly(definition, SearchErrors.CabinUnknown, "Cabin", "'9' is not a cabin.");
    }



    [Fact]
    public void Validate_when_flights_are_off_reports_every_flight_only_value_that_is_set()
    {
        var definition = Recurring() with
        {
            Cabin = Cabin.Business,
            DepartureAirportIds = [Airport],
            MaxFlightMinutes = 600,
            MaxStops = 1,
        };

        var failures = SearchValidator.Validate(definition);

        Assert.Equal(["Cabin", "DepartureAirportIds", "MaxFlightMinutes", "MaxStops"], failures.Select(failure => failure.Field));
        Assert.All
        (
            failures,
            failure =>
            {
                Assert.Same(SearchErrors.FlightFieldWithoutFlights, failure.Code);
                Assert.Equal("This applies to flights, and the search does not include flights.", failure.Message);
            }
        );
    }



    [Theory]
    [InlineData("MaxFlightMinutes")]
    [InlineData("MaxStops")]
    public void Validate_when_flights_are_off_and_one_flight_filter_is_set_reports_only_that_one(string field)
    {
        var definition = string.Equals(field, "MaxStops", StringComparison.Ordinal)
            ? Recurring() with { MaxStops = 0 }
            : Recurring() with { MaxFlightMinutes = 300 };

        AssertOnly
        (
            definition,
            SearchErrors.FlightFieldWithoutFlights,
            field,
            "This applies to flights, and the search does not include flights."
        );
    }



    [Fact]
    public void Validate_when_limits_are_zero_or_negative_reports_each()
    {
        var definition = Recurring() with
        {
            IncludeFlights = true,
            Cabin = Cabin.Economy,
            DepartureAirportIds = [Airport],
            MaxTotalPrice = 0m,
            MaxDriveMinutes = -5,
            MaxFlightMinutes = 0,
            MaxStops = -1,
        };

        Assert.Equal
        (
            [
                (SearchErrors.LimitNotPositive, "MaxTotalPrice", "This limit must be greater than 0; 0 was given."),
                (SearchErrors.LimitNotPositive, "MaxDriveMinutes", "This limit must be greater than 0; -5 was given."),
                (SearchErrors.LimitNotPositive, "MaxFlightMinutes", "This limit must be greater than 0; 0 was given."),
                (SearchErrors.MaxStopsNegative, "MaxStops", "The most stops cannot be below 0; -1 was given."),
            ],
            SearchValidator.Validate(definition).Select(Shape)
        );
    }



    [Fact]
    public void Validate_when_a_price_limit_is_negative_reports_it_in_invariant_format()
    {
        AssertOnly
        (
            Recurring() with { MaxTotalPrice = -1234.5m },
            SearchErrors.LimitNotPositive,
            "MaxTotalPrice",
            "This limit must be greater than 0; -1234.5 was given."
        );
    }



    [Fact]
    public void Validate_when_several_rules_are_broken_reports_them_in_field_order()
    {
        var definition = Recurring() with
        {
            Name = " ",
            ResortIds = [],
            Nights = 0,
            RecurringLastDay = null,
            MaxDriveMinutes = 0,
        };

        Assert.Equal
        (
            ["Name", "ResortIds", "Nights", "RecurringLastDay", "MaxDriveMinutes"],
            SearchValidator.Validate(definition).Select(failure => failure.Field)
        );
    }



    [Fact]
    public void Validate_when_every_error_code_is_read_finds_them_all_422_warnings_with_unique_names()
    {
        var codes = typeof(SearchErrors)
            .GetFields()
            .Select(field => (ErrorCode)field.GetValue(null)!)
            .ToList();

        Assert.Equal(19, codes.Count);
        Assert.Equal(codes.Count, codes.Select(code => code.Name).Distinct(StringComparer.Ordinal).Count());
        Assert.All
        (
            codes,
            code =>
            {
                Assert.Equal(422, code.Status);
                Assert.Equal(ErrorSeverity.Warning, code.Severity);
                Assert.StartsWith("search.", code.Name, StringComparison.Ordinal);
            }
        );
    }



    [Fact]
    public void BuildWindow_when_the_definition_is_recurring_returns_a_recurring_window()
    {
        var window = SearchValidator.BuildWindow(Recurring());

        Assert.Equal(new RecurringWindow(new MonthDay(3, 10), new MonthDay(3, 20)), window);
    }



    [Fact]
    public void BuildWindow_when_the_definition_is_one_time_returns_a_one_time_window()
    {
        var window = Assert.IsType<OneTimeWindow>(SearchValidator.BuildWindow(OneTime()));

        Assert.Equal(new DateOnly(2027, 6, 1), window.First);
        Assert.Equal(new DateOnly(2027, 6, 14), window.Last);
    }



    [Fact]
    public void BuildWindow_when_the_window_is_not_valid_throws_with_the_first_reason()
    {
        var exception = Assert.Throws<ArgumentException>
        (
            () => SearchValidator.BuildWindow(Recurring() with { RecurringFirstMonth = 13 })
        );

        Assert.Equal("definition", exception.ParamName);
        Assert.Contains("There is no day 10 in month 13.", exception.Message, StringComparison.Ordinal);
    }



    [Fact]
    public void BuildWindow_when_only_other_fields_are_invalid_still_builds_the_window()
    {
        var window = SearchValidator.BuildWindow(Recurring() with { Name = string.Empty, Nights = 0 });

        Assert.IsType<RecurringWindow>(window);
    }



    [Fact]
    public void BuildWindow_when_the_definition_is_null_throws()
    {
        Assert.Throws<ArgumentNullException>(() => SearchValidator.BuildWindow(null!));
    }



    [Fact]
    public void Of_when_given_a_code_and_values_fills_the_message()
    {
        var failure = ValidationFailure.Of(SearchErrors.NightsNotPositive, "Nights", 0);

        Assert.Same(SearchErrors.NightsNotPositive, failure.Code);
        Assert.Equal("Nights", failure.Field);
        Assert.Equal("A stay is at least 1 night; 0 was given.", failure.Message);
    }



    [Fact]
    public void Of_when_the_code_is_null_throws()
    {
        Assert.Throws<ArgumentNullException>(() => ValidationFailure.Of(null!, "Nights"));
    }



    private static SearchDefinition Recurring()
    {
        return new SearchDefinition
        {
            Name = "Spring in Jamaica",
            Brand = Brand.Sandals,
            ResortIds = [Resort],
            Nights = 7,
            Adults = 2,
            WindowKind = WindowKind.Recurring,
            RecurringFirstMonth = 3,
            RecurringFirstDay = 10,
            RecurringLastMonth = 3,
            RecurringLastDay = 20,
        };
    }



    private static SearchDefinition OneTime()
    {
        return new SearchDefinition
        {
            Name = "Anniversary",
            Brand = Brand.Sandals,
            ResortIds = [Resort],
            Nights = 5,
            Adults = 2,
            WindowKind = WindowKind.OneTime,
            FirstCheckIn = new DateOnly(2027, 6, 1),
            LastCheckIn = new DateOnly(2027, 6, 14),
        };
    }



    private static (ErrorCode Code, string Field, string Message) Shape(ValidationFailure failure)
    {
        return (failure.Code, failure.Field, failure.Message);
    }



    private static void AssertOnly
    (
        SearchDefinition definition,
        ErrorCode code,
        string field,
        string message
    )
    {
        var failure = Assert.Single(SearchValidator.Validate(definition));

        Assert.Same(code, failure.Code);
        Assert.Equal(field, failure.Field);
        Assert.Equal(message, failure.Message);
    }
}
