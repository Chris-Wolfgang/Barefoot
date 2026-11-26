using Microsoft.EntityFrameworkCore;
using Wolfgang.Barefoot.Contracts.Runs;
using Wolfgang.Barefoot.Contracts.Searches;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Tests.Unit;

/// <summary>
/// The flight tables of <see cref="BarefootDbContext"/>: a run's flight lookups, the links from
/// search items to them, and the options, directions and flights the site offered.
/// </summary>
public sealed class BarefootDbContextFlightTests
{
    private static readonly Guid OwnerOid = Guid.Parse("b2c3d4e5-0001-4b6c-9d7e-8f90a1b2c3d4");

    private static readonly Guid SearchId = Guid.Parse("b2c3d4e5-0002-4b6c-9d7e-8f90a1b2c3d4");

    private static readonly Guid ResortId = Guid.Parse("b2c3d4e5-0003-4b6c-9d7e-8f90a1b2c3d4");

    private static readonly Guid OriginId = Guid.Parse("b2c3d4e5-0004-4b6c-9d7e-8f90a1b2c3d4");

    private static readonly Guid GatewayId = Guid.Parse("b2c3d4e5-0005-4b6c-9d7e-8f90a1b2c3d4");

    private static readonly Guid RunId = Guid.Parse("b2c3d4e5-0006-4b6c-9d7e-8f90a1b2c3d4");

    private static readonly Guid OtherRunId = Guid.Parse("b2c3d4e5-0007-4b6c-9d7e-8f90a1b2c3d4");

    private static readonly Guid MatrixRunId = Guid.Parse("b2c3d4e5-0008-4b6c-9d7e-8f90a1b2c3d4");

    private static readonly Guid RequestId = Guid.Parse("b2c3d4e5-0009-4b6c-9d7e-8f90a1b2c3d4");

    private static readonly Guid ItemId = Guid.Parse("b2c3d4e5-000a-4b6c-9d7e-8f90a1b2c3d4");

    private static readonly Guid FlightSetId = Guid.Parse("b2c3d4e5-000b-4b6c-9d7e-8f90a1b2c3d4");

    private static readonly Guid OtherFlightSetId = Guid.Parse("b2c3d4e5-000c-4b6c-9d7e-8f90a1b2c3d4");

    private static readonly Guid OptionId = Guid.Parse("b2c3d4e5-000d-4b6c-9d7e-8f90a1b2c3d4");

    private static readonly Guid DirectionId = Guid.Parse("b2c3d4e5-000e-4b6c-9d7e-8f90a1b2c3d4");

    private static readonly Guid OtherDirectionId = Guid.Parse("b2c3d4e5-000f-4b6c-9d7e-8f90a1b2c3d4");

    private static readonly Guid RowId = Guid.Parse("b2c3d4e5-0010-4b6c-9d7e-8f90a1b2c3d4");

    private static readonly Guid OtherRowId = Guid.Parse("b2c3d4e5-0011-4b6c-9d7e-8f90a1b2c3d4");

    private static readonly Guid MissingId = Guid.Parse("b2c3d4e5-0012-4b6c-9d7e-8f90a1b2c3d4");

    private static readonly Guid AttemptToken = Guid.Parse("b2c3d4e5-0013-4b6c-9d7e-8f90a1b2c3d4");

    private static readonly DateTimeOffset Instant = new(2026, 11, 4, 6, 40, 0, 123, TimeSpan.Zero);

    private static readonly DateOnly DepartDate = new(2027, 1, 9);



    [Fact]
    public async Task SaveChangesAsync_when_a_flight_set_is_added_reads_back_every_value()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            AddRunAndAirports(context);
            var flightSet = FlightSet(FlightSetId, RunId);
            flightSet.ChildCount = 1;
            flightSet.InfantCount = 1;
            flightSet.Status = WorkStatus.Failed;
            flightSet.LastError = "The site answered 503.";
            flightSet.CapturedUtc = Instant.AddMinutes(5);
            flightSet.LeasedBy = "worker-1";
            flightSet.LeaseExpiresUtc = Instant.AddMinutes(10);
            flightSet.Attempt = AttemptToken;
            flightSet.ResultSha256 = new string('b', 64);
            context.FlightSets.Add(flightSet);
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.FlightSets.SingleAsync();

        Assert.Equal(FlightSetId, read.Id);
        Assert.Equal(RunId, read.RunId);
        Assert.Equal(OriginId, read.OriginAirportId);
        Assert.Equal(GatewayId, read.GatewayAirportId);
        Assert.Equal(DepartDate, read.DepartDate);
        Assert.Equal(DepartDate.AddDays(7), read.ReturnDate);
        Assert.Equal(Cabin.PremiumEconomy, read.Cabin);
        Assert.Equal(2, read.AdultCount);
        Assert.Equal(1, read.ChildCount);
        Assert.Equal(1, read.InfantCount);
        Assert.Equal(WorkStatus.Failed, read.Status);
        Assert.Equal("The site answered 503.", read.LastError);
        Assert.Equal(Instant.AddMinutes(5), read.CapturedUtc);
        Assert.Equal("worker-1", read.LeasedBy);
        Assert.Equal(Instant.AddMinutes(10), read.LeaseExpiresUtc);
        Assert.Equal(AttemptToken, read.Attempt);
        Assert.Equal(new string('b', 64), read.ResultSha256);
        Assert.Equal(1, read.Version);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_flight_set_has_not_been_claimed_reads_back_no_lease_and_no_result()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            AddRunAndAirports(context);
            context.FlightSets.Add(FlightSet(FlightSetId, RunId));
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.FlightSets.SingleAsync();

        Assert.Equal(WorkStatus.Pending, read.Status);
        Assert.Null(read.LastError);
        Assert.Null(read.CapturedUtc);
        Assert.Null(read.LeasedBy);
        Assert.Null(read.LeaseExpiresUtc);
        Assert.Null(read.Attempt);
        Assert.Null(read.ResultSha256);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_run_has_the_same_flight_lookup_twice_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddRunAndAirports(context);
        context.FlightSets.Add(FlightSet(FlightSetId, RunId));
        context.FlightSets.Add(FlightSet(OtherFlightSetId, RunId));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Theory]
    [InlineData("airports")]
    [InlineData("depart")]
    [InlineData("return")]
    [InlineData("cabin")]
    [InlineData("adults")]
    [InlineData("children")]
    [InlineData("infants")]
    public async Task SaveChangesAsync_when_two_flight_lookups_of_a_run_differ_in_one_part_of_the_journey_saves_both(string part)
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddRunAndAirports(context);
        context.FlightSets.Add(FlightSet(FlightSetId, RunId));
        var other = FlightSet(OtherFlightSetId, RunId);

        switch (part)
        {
            case "airports":
                other.OriginAirportId = GatewayId;
                other.GatewayAirportId = OriginId;
                break;
            case "depart":
                other.DepartDate = DepartDate.AddDays(-1);
                break;
            case "return":
                other.ReturnDate = DepartDate.AddDays(8);
                break;
            case "cabin":
                other.Cabin = Cabin.Economy;
                break;
            case "adults":
                other.AdultCount = 3;
                break;
            case "children":
                other.ChildCount = 1;
                break;
            default:
                other.InfantCount = 1;
                break;
        }

        context.FlightSets.Add(other);

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.FlightSets.CountAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_two_runs_have_the_same_flight_lookup_saves_both()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddRunAndAirports(context);
        context.Runs.Add(new Run { Id = OtherRunId, Trigger = "schedule", Status = RunStatus.Complete, StartedUtc = Instant });
        context.FlightSets.Add(FlightSet(FlightSetId, RunId));
        context.FlightSets.Add(FlightSet(OtherFlightSetId, OtherRunId));

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.FlightSets.CountAsync());
    }



    [Theory]
    [InlineData("run")]
    [InlineData("origin")]
    [InlineData("gateway")]
    public async Task SaveChangesAsync_when_a_flight_set_names_a_row_that_does_not_exist_throws_DbUpdateException(string which)
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddRunAndAirports(context);
        var flightSet = FlightSet(FlightSetId, RunId);

        switch (which)
        {
            case "run":
                flightSet.RunId = MissingId;
                break;
            case "origin":
                flightSet.OriginAirportId = MissingId;
                break;
            default:
                flightSet.GatewayAirportId = MissingId;
                break;
        }

        context.FlightSets.Add(flightSet);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_search_item_flight_set_is_added_reads_back_every_value()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            AddRunAndAirports(context);
            AddSearchItem(context);
            context.FlightSets.Add(FlightSet(FlightSetId, RunId));
            context.SearchItemFlightSets.Add(new SearchItemFlightSet { Id = RowId, SearchItemId = ItemId, FlightSetId = FlightSetId });
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.SearchItemFlightSets.SingleAsync();

        Assert.Equal(RowId, read.Id);
        Assert.Equal(ItemId, read.SearchItemId);
        Assert.Equal(FlightSetId, read.FlightSetId);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_search_item_links_to_a_flight_lookup_twice_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddRunAndAirports(context);
        AddSearchItem(context);
        context.FlightSets.Add(FlightSet(FlightSetId, RunId));
        context.SearchItemFlightSets.Add(new SearchItemFlightSet { Id = RowId, SearchItemId = ItemId, FlightSetId = FlightSetId });
        context.SearchItemFlightSets.Add(new SearchItemFlightSet { Id = OtherRowId, SearchItemId = ItemId, FlightSetId = FlightSetId });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_search_item_links_to_two_flight_lookups_saves_both()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddRunAndAirports(context);
        AddSearchItem(context);
        context.FlightSets.Add(FlightSet(FlightSetId, RunId));
        var fromTheGateway = FlightSet(OtherFlightSetId, RunId);
        fromTheGateway.OriginAirportId = GatewayId;
        fromTheGateway.GatewayAirportId = OriginId;
        context.FlightSets.Add(fromTheGateway);
        context.SearchItemFlightSets.Add(new SearchItemFlightSet { Id = RowId, SearchItemId = ItemId, FlightSetId = FlightSetId });
        context.SearchItemFlightSets.Add(new SearchItemFlightSet { Id = OtherRowId, SearchItemId = ItemId, FlightSetId = OtherFlightSetId });

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.SearchItemFlightSets.CountAsync());
    }



    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SaveChangesAsync_when_a_search_item_flight_set_names_a_row_that_does_not_exist_throws_DbUpdateException(bool missingItem)
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddRunAndAirports(context);
        AddSearchItem(context);
        context.FlightSets.Add(FlightSet(FlightSetId, RunId));
        context.SearchItemFlightSets.Add
        (
            new SearchItemFlightSet
            {
                Id = RowId,
                SearchItemId = missingItem ? MissingId : ItemId,
                FlightSetId = missingItem ? FlightSetId : MissingId,
            }
        );

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_flight_option_with_both_directions_and_a_leg_is_added_reads_back_every_value()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            AddRunAndAirports(context);
            context.FlightSets.Add(FlightSet(FlightSetId, RunId));
            context.FlightOptions.Add(new FlightOption { Id = OptionId, FlightSetId = FlightSetId, FareAmount = 1234.56m });
            context.FlightDirections.Add(new FlightDirection { Id = DirectionId, FlightOptionId = OptionId, IsReturn = false });
            context.FlightDirections.Add(new FlightDirection { Id = OtherDirectionId, FlightOptionId = OptionId, IsReturn = true, IsOvernight = true });
            var leg = Leg(RowId, OtherDirectionId, 2);
            leg.IsOvernight = true;
            context.FlightLegs.Add(leg);
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var option = await reader.FlightOptions.SingleAsync();
        var outbound = await reader.FlightDirections.SingleAsync(direction => !direction.IsReturn);
        var back = await reader.FlightDirections.SingleAsync(direction => direction.IsReturn);
        var read = await reader.FlightLegs.SingleAsync();

        Assert.Equal(OptionId, option.Id);
        Assert.Equal(FlightSetId, option.FlightSetId);
        Assert.Equal(1234.56m, option.FareAmount);
        Assert.Equal(DirectionId, outbound.Id);
        Assert.Equal(OptionId, outbound.FlightOptionId);
        Assert.False(outbound.IsOvernight);
        Assert.Equal(OtherDirectionId, back.Id);
        Assert.True(back.IsOvernight);
        Assert.Equal(RowId, read.Id);
        Assert.Equal(OtherDirectionId, read.FlightDirectionId);
        Assert.Equal(2, read.Sequence);
        Assert.Equal("Example Air", read.Carrier);
        Assert.Equal("EX123", read.FlightNumber);
        Assert.Equal("Example Regional", read.OperatingCarrier);
        Assert.Equal("Main Cabin", read.CabinName);
        Assert.Equal("Y", read.FareCode);
        Assert.Equal(DepartDate, read.DepartureDate);
        Assert.Equal(new TimeOnly(22, 15), read.DepartureTime);
        Assert.Equal("AAA", read.DepartureAirportCode);
        Assert.Equal(new TimeOnly(1, 40), read.ArrivalTime);
        Assert.Equal("BBB", read.ArrivalAirportCode);
        Assert.True(read.IsOvernight);
    }



    [Fact]
    public async Task SaveChangesAsync_when_an_option_has_two_directions_the_same_way_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddRunAndAirports(context);
        context.FlightSets.Add(FlightSet(FlightSetId, RunId));
        context.FlightOptions.Add(new FlightOption { Id = OptionId, FlightSetId = FlightSetId, FareAmount = 1234.56m });
        context.FlightDirections.Add(new FlightDirection { Id = DirectionId, FlightOptionId = OptionId, IsReturn = true });
        context.FlightDirections.Add(new FlightDirection { Id = OtherDirectionId, FlightOptionId = OptionId, IsReturn = true });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_direction_has_two_flights_in_the_same_place_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddOptionWithOutboundDirection(context);
        context.FlightLegs.Add(Leg(RowId, DirectionId, 1));
        context.FlightLegs.Add(Leg(OtherRowId, DirectionId, 1));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task ToListAsync_when_a_direction_has_two_flights_reads_them_in_their_order_in_the_database()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            AddOptionWithOutboundDirection(context);
            context.FlightLegs.Add(Leg(RowId, DirectionId, 2));
            context.FlightLegs.Add(Leg(OtherRowId, DirectionId, 1));
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var legs = await reader
            .FlightLegs
            .Where(leg => leg.FlightDirectionId == DirectionId)
            .OrderBy(leg => leg.Sequence)
            .Select(leg => leg.Id)
            .ToListAsync();

        Assert.Equal([OtherRowId, RowId], legs);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_flight_option_names_a_flight_set_that_does_not_exist_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        context.FlightOptions.Add(new FlightOption { Id = OptionId, FlightSetId = MissingId, FareAmount = 1m });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_flight_direction_names_an_option_that_does_not_exist_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        context.FlightDirections.Add(new FlightDirection { Id = DirectionId, FlightOptionId = MissingId, IsReturn = false });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_flight_leg_names_a_direction_that_does_not_exist_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        context.FlightLegs.Add(Leg(RowId, MissingId, 1));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_flight_set_that_has_options_is_deleted_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            AddOptionWithOutboundDirection(context);
            await context.SaveChangesAsync();
        }

        await using var deleter = database.NewContext();
        deleter.FlightSets.Remove(await deleter.FlightSets.SingleAsync());

        await Assert.ThrowsAsync<DbUpdateException>(() => deleter.SaveChangesAsync());
    }



    private static void AddRunAndAirports(BarefootDbContext context)
    {
        context.Airports.Add(Airport(OriginId, "AAA"));
        context.Airports.Add(Airport(GatewayId, "BBB"));
        context.Runs.Add(new Run { Id = RunId, Trigger = "schedule", Status = RunStatus.InProgress, StartedUtc = Instant });
    }



    private static void AddOptionWithOutboundDirection(BarefootDbContext context)
    {
        AddRunAndAirports(context);
        context.FlightSets.Add(FlightSet(FlightSetId, RunId));
        context.FlightOptions.Add(new FlightOption { Id = OptionId, FlightSetId = FlightSetId, FareAmount = 1234.56m });
        context.FlightDirections.Add(new FlightDirection { Id = DirectionId, FlightOptionId = OptionId, IsReturn = false });
    }



    private static void AddSearchItem(BarefootDbContext context)
    {
        context.AppUsers.Add(new AppUser { Oid = OwnerOid, DisplayName = "Pat Example", Email = "pat@example.com", LastSeenUtc = Instant });
        context.SearchMatrices.Add
        (
            new SearchMatrix
            {
                Id = SearchId,
                OwnerId = OwnerOid,
                Name = "Winter week",
                NormalizedName = SearchMatrix.NormalizeName("Winter week"),
                Brand = Brand.Sandals,
                NightCount = 7,
                AdultCount = 2,
                WindowKind = WindowKind.Recurring,
                CreatedUtc = Instant,
            }
        );
        context.Resorts.Add
        (
            new Resort
            {
                Id = ResortId,
                Code = "RES1",
                Name = "Example Bay Resort",
                Brand = Brand.Sandals,
                Island = "Example Island",
                FirstSeenUtc = Instant,
            }
        );
        context.MatrixRuns.Add
        (
            new MatrixRun
            {
                Id = MatrixRunId,
                RunId = RunId,
                SearchMatrixId = SearchId,
                FirstCheckIn = DepartDate,
                LastCheckIn = DepartDate.AddDays(30),
                Stage = SearchRunStage.Exploded,
            }
        );
        context.SearchRequests.Add
        (
            new SearchRequest
            {
                Id = RequestId,
                RunId = RunId,
                ResortId = ResortId,
                CheckIn = DepartDate,
                CheckOut = DepartDate.AddDays(7),
                AdultCount = 2,
                RunOrder = 5,
                Status = WorkStatus.Pending,
            }
        );
        context.SearchItems.Add(new SearchItem { Id = ItemId, MatrixRunId = MatrixRunId, SearchRequestId = RequestId, Cabin = Cabin.PremiumEconomy });
    }



    private static Airport Airport(Guid id, string code)
    {
        return new Airport
        {
            Id = id,
            Code = code,
            Name = "Example International",
            City = "Example City",
            Country = "Exampleland",
            FirstSeenUtc = Instant,
        };
    }



    private static FlightSet FlightSet(Guid id, Guid runId)
    {
        return new FlightSet
        {
            Id = id,
            RunId = runId,
            OriginAirportId = OriginId,
            GatewayAirportId = GatewayId,
            DepartDate = DepartDate,
            ReturnDate = DepartDate.AddDays(7),
            Cabin = Cabin.PremiumEconomy,
            AdultCount = 2,
            Status = WorkStatus.Pending,
        };
    }



    private static FlightLeg Leg(Guid id, Guid directionId, int sequence)
    {
        return new FlightLeg
        {
            Id = id,
            FlightDirectionId = directionId,
            Sequence = sequence,
            Carrier = "Example Air",
            FlightNumber = "EX123",
            OperatingCarrier = "Example Regional",
            CabinName = "Main Cabin",
            FareCode = "Y",
            DepartureDate = DepartDate,
            DepartureTime = new TimeOnly(22, 15),
            DepartureAirportCode = "AAA",
            ArrivalTime = new TimeOnly(1, 40),
            ArrivalAirportCode = "BBB",
        };
    }
}
