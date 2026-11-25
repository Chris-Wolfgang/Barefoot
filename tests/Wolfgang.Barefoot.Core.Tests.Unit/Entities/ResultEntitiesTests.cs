using Wolfgang.Barefoot.Contracts.Digests;
using Wolfgang.Barefoot.Contracts.Searches;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Core.Tests.Unit.Entities;

public sealed class ResultEntitiesTests
{
    private static readonly Guid Id = Guid.Parse("5e6f7081-92a3-44b5-86d7-e8f9012a3b4c");

    private static readonly Guid Parent = Guid.Parse("4b3a2918-0716-4e5d-8c4b-3a29180716f5");

    private static readonly Guid Other = Guid.Parse("1e2d3c4b-5a69-4788-9a6b-5c4d3e2f1a0b");

    private static readonly Guid Third = Guid.Parse("b1c2d3e4-f5a6-4b78-9c8d-ef0123456789");

    private static readonly DateTimeOffset Instant = new(2026, 11, 4, 6, 40, 0, TimeSpan.Zero);



    [Fact]
    public void RoomResult_when_every_value_is_given_keeps_each_value()
    {
        var result = new RoomResult
        {
            Id = Id,
            SearchRequestId = Parent,
            RoomCategoryId = Other,
            RoomLevelId = Third,
            RoomViewId = Parent,
            RoomTypeId = Other,
            IsSoldOut = true,
            RemainingRoomCount = 2,
            TotalAmount = 8120.40m,
            RegularAmount = 9300m,
            Promotion = "Save up to 15 %",
        };

        Assert.Equal(Id, result.Id);
        Assert.Equal(Parent, result.SearchRequestId);
        Assert.Equal(Other, result.RoomCategoryId);
        Assert.Equal(Third, result.RoomLevelId);
        Assert.Equal(Parent, result.RoomViewId);
        Assert.Equal(Other, result.RoomTypeId);
        Assert.True(result.IsSoldOut);
        Assert.Equal(2, result.RemainingRoomCount);
        Assert.Equal(8120.40m, result.TotalAmount);
        Assert.Equal(9300m, result.RegularAmount);
        Assert.Equal("Save up to 15 %", result.Promotion);
    }



    [Fact]
    public void RoomResult_when_only_the_required_values_are_given_is_not_sold_out_and_has_nothing_else()
    {
        var result = new RoomResult { Id = Id, SearchRequestId = Parent, RoomCategoryId = Other };

        Assert.Null(result.RoomLevelId);
        Assert.Null(result.RoomViewId);
        Assert.Null(result.RoomTypeId);
        Assert.False(result.IsSoldOut);
        Assert.Null(result.RemainingRoomCount);
        Assert.Null(result.TotalAmount);
        Assert.Null(result.RegularAmount);
        Assert.Null(result.Promotion);
    }



    [Fact]
    public void RoomResultFeature_when_every_value_is_given_keeps_each_value()
    {
        var link = new RoomResultFeature { Id = Id, RoomResultId = Parent, VocabularyValueId = Other };

        Assert.Equal(Id, link.Id);
        Assert.Equal(Parent, link.RoomResultId);
        Assert.Equal(Other, link.VocabularyValueId);
    }



    [Fact]
    public void FlightSet_when_it_has_been_claimed_and_answered_keeps_each_value()
    {
        var set = new FlightSet
        {
            Id = Id,
            RunId = Parent,
            OriginAirportId = Other,
            GatewayAirportId = Third,
            DepartDate = new DateOnly(2027, 2, 13),
            ReturnDate = new DateOnly(2027, 2, 20),
            Cabin = Cabin.PremiumEconomy,
            AdultCount = 2,
            ChildCount = 1,
            InfantCount = 1,
            Status = WorkStatus.Succeeded,
            LastError = "The page did not load.",
            CapturedUtc = Instant,
            LeasedBy = "scraper-1",
            LeaseExpiresUtc = Instant.AddMinutes(5),
            Attempt = Third,
            ResultSha256 = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08",
            Version = 3,
        };

        Assert.Equal(Id, set.Id);
        Assert.Equal(Parent, set.RunId);
        Assert.Equal(Other, set.OriginAirportId);
        Assert.Equal(Third, set.GatewayAirportId);
        Assert.Equal(new DateOnly(2027, 2, 13), set.DepartDate);
        Assert.Equal(new DateOnly(2027, 2, 20), set.ReturnDate);
        Assert.Equal(Cabin.PremiumEconomy, set.Cabin);
        Assert.Equal(2, set.AdultCount);
        Assert.Equal(1, set.ChildCount);
        Assert.Equal(1, set.InfantCount);
        Assert.Equal(WorkStatus.Succeeded, set.Status);
        Assert.Equal("The page did not load.", set.LastError);
        Assert.Equal(Instant, set.CapturedUtc);
        Assert.Equal("scraper-1", set.LeasedBy);
        Assert.Equal(Instant.AddMinutes(5), set.LeaseExpiresUtc);
        Assert.Equal(Third, set.Attempt);
        Assert.Equal("9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08", set.ResultSha256);
        Assert.Equal(3, set.Version);
    }



    [Fact]
    public void FlightSet_when_it_has_just_been_planned_has_no_lease_no_attempt_and_no_result()
    {
        var set = new FlightSet
        {
            Id = Id,
            RunId = Parent,
            OriginAirportId = Other,
            GatewayAirportId = Third,
            DepartDate = new DateOnly(2027, 2, 13),
            ReturnDate = new DateOnly(2027, 2, 20),
            Cabin = Cabin.Economy,
            AdultCount = 2,
            Status = WorkStatus.Pending,
        };

        Assert.Equal(0, set.ChildCount);
        Assert.Equal(0, set.InfantCount);
        Assert.Null(set.LastError);
        Assert.Null(set.CapturedUtc);
        Assert.Null(set.LeasedBy);
        Assert.Null(set.LeaseExpiresUtc);
        Assert.Null(set.Attempt);
        Assert.Null(set.ResultSha256);
        Assert.Equal(0, set.Version);
    }



    [Fact]
    public void SearchItemFlightSet_when_every_value_is_given_keeps_each_value()
    {
        var link = new SearchItemFlightSet { Id = Id, SearchItemId = Parent, FlightSetId = Other };

        Assert.Equal(Id, link.Id);
        Assert.Equal(Parent, link.SearchItemId);
        Assert.Equal(Other, link.FlightSetId);
    }



    [Fact]
    public void FlightOption_when_every_value_is_given_keeps_each_value()
    {
        var option = new FlightOption { Id = Id, FlightSetId = Parent, FareAmount = 1840.25m };

        Assert.Equal(Id, option.Id);
        Assert.Equal(Parent, option.FlightSetId);
        Assert.Equal(1840.25m, option.FareAmount);
    }



    [Fact]
    public void FlightDirection_when_it_is_an_overnight_return_keeps_each_value()
    {
        var direction = new FlightDirection { Id = Id, FlightOptionId = Parent, IsReturn = true, IsOvernight = true };

        Assert.Equal(Id, direction.Id);
        Assert.Equal(Parent, direction.FlightOptionId);
        Assert.True(direction.IsReturn);
        Assert.True(direction.IsOvernight);
    }



    [Fact]
    public void FlightDirection_when_it_is_the_way_out_by_day_is_neither_a_return_nor_overnight()
    {
        var direction = new FlightDirection { Id = Id, FlightOptionId = Parent, IsReturn = false };

        Assert.False(direction.IsReturn);
        Assert.False(direction.IsOvernight);
    }



    [Fact]
    public void FlightLeg_when_every_value_is_given_keeps_each_value()
    {
        var leg = new FlightLeg
        {
            Id = Id,
            FlightDirectionId = Parent,
            Sequence = 2,
            Carrier = "XA",
            FlightNumber = "1234",
            OperatingCarrier = "Example Air",
            CabinName = "Main Cabin",
            FareCode = "Q",
            DepartureDate = new DateOnly(2027, 2, 13),
            DepartureTime = new TimeOnly(22, 15),
            DepartureAirportCode = "AAA",
            ArrivalTime = new TimeOnly(1, 40),
            ArrivalAirportCode = "BBB",
            IsOvernight = true,
        };

        Assert.Equal(Id, leg.Id);
        Assert.Equal(Parent, leg.FlightDirectionId);
        Assert.Equal(2, leg.Sequence);
        Assert.Equal("XA", leg.Carrier);
        Assert.Equal("1234", leg.FlightNumber);
        Assert.Equal("Example Air", leg.OperatingCarrier);
        Assert.Equal("Main Cabin", leg.CabinName);
        Assert.Equal("Q", leg.FareCode);
        Assert.Equal(new DateOnly(2027, 2, 13), leg.DepartureDate);
        Assert.Equal(new TimeOnly(22, 15), leg.DepartureTime);
        Assert.Equal("AAA", leg.DepartureAirportCode);
        Assert.Equal(new TimeOnly(1, 40), leg.ArrivalTime);
        Assert.Equal("BBB", leg.ArrivalAirportCode);
        Assert.True(leg.IsOvernight);
    }



    [Fact]
    public void DigestDelivery_when_it_failed_for_a_user_keeps_each_value()
    {
        var delivery = new DigestDelivery
        {
            Id = Id,
            MatrixRunId = Parent,
            RecipientUserId = Other,
            RecipientAddress = "alex@example.com",
            Subject = "February week: 3 matches from $8,120",
            HtmlBody = "<p>Three rooms match.</p>",
            TextBody = "Three rooms match.",
            Status = DeliveryStatus.Failed,
            AttemptCount = 2,
            LastError = "The mailbox is full.",
            CreatedUtc = Instant,
            SentUtc = Instant.AddMinutes(3),
        };

        Assert.Equal(Id, delivery.Id);
        Assert.Equal(Parent, delivery.MatrixRunId);
        Assert.Equal(Other, delivery.RecipientUserId);
        Assert.Equal("alex@example.com", delivery.RecipientAddress);
        Assert.Equal("February week: 3 matches from $8,120", delivery.Subject);
        Assert.Equal("<p>Three rooms match.</p>", delivery.HtmlBody);
        Assert.Equal("Three rooms match.", delivery.TextBody);
        Assert.Equal(DeliveryStatus.Failed, delivery.Status);
        Assert.Equal(2, delivery.AttemptCount);
        Assert.Equal("The mailbox is full.", delivery.LastError);
        Assert.Equal(Instant, delivery.CreatedUtc);
        Assert.Equal(Instant.AddMinutes(3), delivery.SentUtc);
    }



    [Fact]
    public void DigestDelivery_when_it_has_just_been_built_for_an_extra_recipient_waits_and_names_no_user()
    {
        var delivery = new DigestDelivery
        {
            Id = Id,
            MatrixRunId = Parent,
            RecipientAddress = "sam@example.com",
            Subject = "February week: 3 matches from $8,120",
            HtmlBody = "<p>Three rooms match.</p>",
            TextBody = "Three rooms match.",
        };

        Assert.Null(delivery.RecipientUserId);
        Assert.Equal(DeliveryStatus.Pending, delivery.Status);
        Assert.Equal(0, delivery.AttemptCount);
        Assert.Null(delivery.LastError);
        Assert.Null(delivery.SentUtc);
    }
}
