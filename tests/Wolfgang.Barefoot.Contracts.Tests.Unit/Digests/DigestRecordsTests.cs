using Wolfgang.Barefoot.Contracts.Digests;

namespace Wolfgang.Barefoot.Contracts.Tests.Unit.Digests;

public sealed class DigestRecordsTests
{
    private static readonly Guid Id = Guid.Parse("11111111-2222-4333-8444-555555555555");

    private static readonly Guid Other = Guid.Parse("3f2a1b0c-9d8e-4f7a-b6c5-d4e3f2a1b0c9");

    private static readonly DateTimeOffset Created = new(2026, 11, 21, 9, 0, 0, TimeSpan.Zero);



    [Fact]
    public void DigestDeliveryResponse_when_built_keeps_each_value()
    {
        var run = Guid.Parse("a9b8c7d6-e5f4-4a3b-9c2d-1e0f9a8b7c6d");
        var delivery = new DigestDeliveryResponse
        (
            Id,
            Other,
            run,
            "someone@example.com",
            "Anniversary",
            DeliveryStatus.Sent,
            2,
            Created,
            Created.AddMinutes(5)
        );

        Assert.Equal(Id, delivery.Id);
        Assert.Equal(Other, delivery.SearchId);
        Assert.Equal(run, delivery.RunId);
        Assert.Equal("someone@example.com", delivery.RecipientAddress);
        Assert.Equal("Anniversary", delivery.Subject);
        Assert.Equal(DeliveryStatus.Sent, delivery.Status);
        Assert.Equal(2, delivery.AttemptCount);
        Assert.Equal(Created, delivery.CreatedUtc);
        Assert.Equal(Created.AddMinutes(5), delivery.SentUtc);
    }



    [Fact]
    public void DigestContentResponse_when_built_keeps_the_subject_and_both_bodies()
    {
        var content = new DigestContentResponse(Id, "Anniversary", "<p>Hello</p>", "Hello");

        Assert.Equal(Id, content.Id);
        Assert.Equal("Anniversary", content.Subject);
        Assert.Equal("<p>Hello</p>", content.HtmlBody);
        Assert.Equal("Hello", content.TextBody);
    }



    [Fact]
    public void DigestFilter_when_given_nothing_narrows_nothing()
    {
        var filter = new DigestFilter();

        Assert.Null(filter.SearchId);
        Assert.Null(filter.From);
        Assert.Null(filter.To);
        Assert.Null(filter.Text);
        Assert.False(filter.UndeliveredOnly);
    }



    [Fact]
    public void DigestFilter_when_given_values_keeps_each_one()
    {
        var filter = new DigestFilter(Other, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30), "butler", UndeliveredOnly: true);

        Assert.Equal(Other, filter.SearchId);
        Assert.Equal(new DateOnly(2026, 11, 1), filter.From);
        Assert.Equal(new DateOnly(2026, 11, 30), filter.To);
        Assert.Equal("butler", filter.Text);
        Assert.True(filter.UndeliveredOnly);
    }



    [Fact]
    public void DigestSendResponse_when_built_keeps_both_counts()
    {
        var response = new DigestSendResponse(11, 1);

        Assert.Equal(11, response.SentCount);
        Assert.Equal(1, response.FailedCount);
    }



    [Fact]
    public void Recipient_when_built_as_a_request_and_a_response_keeps_each_value()
    {
        var request = new RecipientRequest("spouse@example.com", "Spouse");
        var response = new RecipientResponse(Id, Other, "spouse@example.com", "Spouse", 2);

        Assert.Equal("spouse@example.com", request.Address);
        Assert.Equal("Spouse", request.DisplayName);
        Assert.Equal(Id, response.Id);
        Assert.Equal(Other, response.OwnerId);
        Assert.Equal("spouse@example.com", response.Address);
        Assert.Equal("Spouse", response.DisplayName);
        Assert.Equal(2, response.Version);
    }



    [Theory]
    [InlineData(DeliveryStatus.Pending, 0)]
    [InlineData(DeliveryStatus.Sent, 1)]
    [InlineData(DeliveryStatus.Failed, 2)]
    public void DeliveryStatus_when_cast_to_a_number_keeps_its_declared_value(DeliveryStatus status, int expected)
    {
        Assert.Equal(expected, (int)status);
    }
}
