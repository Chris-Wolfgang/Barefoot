using Wolfgang.Barefoot.Contracts.Searches;

namespace Wolfgang.Barefoot.Contracts.Tests.Unit.Searches;

public sealed class SearchDefinitionTests
{
    [Fact]
    public void Constructor_when_only_the_required_values_are_given_leaves_the_rest_empty_and_the_run_order_normal()
    {
        var definition = new SearchDefinition
        {
            Name = "Anniversary",
            Brand = Brand.Sandals,
            Nights = 7,
            Adults = 2,
            WindowKind = WindowKind.Recurring,
        };

        Assert.Equal("Anniversary", definition.Name);
        Assert.Equal(Brand.Sandals, definition.Brand);
        Assert.Equal(7, definition.Nights);
        Assert.Equal(2, definition.Adults);
        Assert.Equal(WindowKind.Recurring, definition.WindowKind);
        Assert.Empty(definition.ResortIds);
        Assert.Empty(definition.DepartureAirportIds);
        Assert.Equal(0, definition.Children);
        Assert.Equal(0, definition.Infants);
        Assert.False(definition.IncludeFlights);
        Assert.Null(definition.Cabin);
        Assert.Null(definition.RecurringFirstMonth);
        Assert.Null(definition.RecurringFirstDay);
        Assert.Null(definition.RecurringLastMonth);
        Assert.Null(definition.RecurringLastDay);
        Assert.Null(definition.FirstCheckIn);
        Assert.Null(definition.LastCheckIn);
        Assert.Null(definition.MaxTotalPrice);
        Assert.Null(definition.MaxDriveMinutes);
        Assert.Null(definition.MaxFlightMinutes);
        Assert.Null(definition.MaxStops);
        Assert.Equal(50, definition.RunOrder);
        Assert.Equal(50, SearchDefinition.NormalRunOrder);
    }



    [Fact]
    public void Constructor_when_every_value_is_given_keeps_each_one()
    {
        var resort = Guid.Parse("5d0c6f0e-3a51-4c1d-8a43-1b2c3d4e5f60");
        var airport = Guid.Parse("0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d");
        var definition = new SearchDefinition
        {
            Name = "Family week",
            Brand = Brand.Beaches,
            ResortIds = [resort],
            Nights = 5,
            Adults = 2,
            Children = 2,
            Infants = 1,
            WindowKind = WindowKind.OneTime,
            RecurringFirstMonth = 3,
            RecurringFirstDay = 1,
            RecurringLastMonth = 4,
            RecurringLastDay = 15,
            FirstCheckIn = new DateOnly(2027, 3, 1),
            LastCheckIn = new DateOnly(2027, 3, 8),
            IncludeFlights = true,
            Cabin = Cabin.PremiumEconomy,
            DepartureAirportIds = [airport],
            MaxTotalPrice = 9000m,
            MaxDriveMinutes = 90,
            MaxFlightMinutes = 600,
            MaxStops = 1,
            RunOrder = 75,
        };

        Assert.Equal(Brand.Beaches, definition.Brand);
        Assert.Equal([resort], definition.ResortIds);
        Assert.Equal(2, definition.Children);
        Assert.Equal(1, definition.Infants);
        Assert.Equal(WindowKind.OneTime, definition.WindowKind);
        Assert.Equal(3, definition.RecurringFirstMonth);
        Assert.Equal(1, definition.RecurringFirstDay);
        Assert.Equal(4, definition.RecurringLastMonth);
        Assert.Equal(15, definition.RecurringLastDay);
        Assert.Equal(new DateOnly(2027, 3, 1), definition.FirstCheckIn);
        Assert.Equal(new DateOnly(2027, 3, 8), definition.LastCheckIn);
        Assert.True(definition.IncludeFlights);
        Assert.Equal(Cabin.PremiumEconomy, definition.Cabin);
        Assert.Equal([airport], definition.DepartureAirportIds);
        Assert.Equal(9000m, definition.MaxTotalPrice);
        Assert.Equal(90, definition.MaxDriveMinutes);
        Assert.Equal(600, definition.MaxFlightMinutes);
        Assert.Equal(1, definition.MaxStops);
        Assert.Equal(75, definition.RunOrder);
    }



    [Theory]
    [InlineData(Brand.Sandals, 0)]
    [InlineData(Brand.Beaches, 1)]
    public void Brand_when_cast_to_a_number_keeps_its_declared_value(Brand brand, int expected)
    {
        Assert.Equal(expected, (int)brand);
    }



    [Theory]
    [InlineData(Cabin.Economy, 0)]
    [InlineData(Cabin.PremiumEconomy, 1)]
    [InlineData(Cabin.Business, 2)]
    [InlineData(Cabin.First, 3)]
    public void Cabin_when_cast_to_a_number_keeps_its_declared_value(Cabin cabin, int expected)
    {
        Assert.Equal(expected, (int)cabin);
    }



    [Theory]
    [InlineData(WindowKind.Recurring, 0)]
    [InlineData(WindowKind.OneTime, 1)]
    public void WindowKind_when_cast_to_a_number_keeps_its_declared_value(WindowKind kind, int expected)
    {
        Assert.Equal(expected, (int)kind);
    }
}
