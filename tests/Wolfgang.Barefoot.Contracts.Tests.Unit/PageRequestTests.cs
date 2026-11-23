namespace Wolfgang.Barefoot.Contracts.Tests.Unit;

public sealed class PageRequestTests
{
    [Fact]
    public void Constructor_when_given_nothing_asks_for_the_first_page_in_the_default_order_and_size()
    {
        var request = new PageRequest();

        Assert.Null(request.After);
        Assert.Null(request.Before);
        Assert.Null(request.Sort);
        Assert.Null(request.Size);
    }



    [Fact]
    public void Constructor_when_given_values_keeps_each_one()
    {
        var request = new PageRequest("after-d", "before-c", "-name", 50);

        Assert.Equal("after-d", request.After);
        Assert.Equal("before-c", request.Before);
        Assert.Equal("-name", request.Sort);
        Assert.Equal(50, request.Size);
    }
}
