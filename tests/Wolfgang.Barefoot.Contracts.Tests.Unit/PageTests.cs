namespace Wolfgang.Barefoot.Contracts.Tests.Unit;

public sealed class PageTests
{
    [Fact]
    public void Constructor_when_given_a_middle_page_keeps_the_rows_both_cursors_and_the_position()
    {
        var page = new Page<string>(["c", "d"], "after-d", "before-c", 2, 6);

        Assert.Equal(["c", "d"], page.Items);
        Assert.Equal("after-d", page.NextCursor);
        Assert.Equal("before-c", page.PreviousCursor);
        Assert.Equal(2, page.FirstItemIndex);
        Assert.Equal(6, page.TotalCount);
    }



    [Fact]
    public void Constructor_when_given_the_only_page_has_no_cursors()
    {
        var page = new Page<int>([1], null, null, 0, 1);

        Assert.Null(page.NextCursor);
        Assert.Null(page.PreviousCursor);
    }
}
