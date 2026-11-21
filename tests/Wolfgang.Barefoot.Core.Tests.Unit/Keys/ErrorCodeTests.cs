using System.Globalization;
using Wolfgang.Barefoot.Core.Keys;

namespace Wolfgang.Barefoot.Core.Tests.Unit.Keys;

public sealed class ErrorCodeTests
{
    [Fact]
    public void Constructor_when_given_a_definition_keeps_every_part()
    {
        var code = new ErrorCode("search.name_taken", 409, "You already have a search named '{0}'.", ErrorSeverity.Warning);

        Assert.Equal("search.name_taken", code.Name);
        Assert.Equal(409, code.Status);
        Assert.Equal("You already have a search named '{0}'.", code.Template);
        Assert.Equal(ErrorSeverity.Warning, code.Severity);
        Assert.Equal(1, code.ArgumentCount);
        Assert.Equal("search-name_taken", code.Anchor);
        Assert.Equal("search.name_taken", code.ToString());
    }



    [Fact]
    public void Constructor_when_no_severity_is_given_uses_error()
    {
        var code = new ErrorCode("internal.error", 500, "Something went wrong. Trace {0}.");

        Assert.Equal(ErrorSeverity.Error, code.Severity);
    }



    [Theory]
    [InlineData("SearchNameTaken")]
    [InlineData("search")]
    [InlineData("")]
    public void Constructor_when_the_name_is_not_a_key_name_throws(string name)
    {
        var exception = Assert.Throws<ArgumentException>(() => new ErrorCode(name, 409, "Taken."));

        Assert.Equal("name", exception.ParamName);
    }



    [Theory]
    [InlineData(200)]
    [InlineData(304)]
    [InlineData(399)]
    [InlineData(600)]
    [InlineData(-1)]
    public void Constructor_when_the_status_is_not_an_error_status_throws(int status)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new ErrorCode("search.name_taken", status, "Taken."));

        Assert.Equal("status", exception.ParamName);
        Assert.Contains("between 400 and 599", exception.Message, StringComparison.Ordinal);
    }



    [Theory]
    [InlineData(400)]
    [InlineData(599)]
    public void Constructor_when_the_status_is_at_an_edge_of_the_range_accepts_it(int status)
    {
        Assert.Equal(status, new ErrorCode("search.name_taken", status, "Taken.").Status);
    }



    [Fact]
    public void Constructor_when_the_severity_is_not_defined_throws()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>
        (
            () => new ErrorCode("search.name_taken", 409, "Taken.", (ErrorSeverity)7)
        );

        Assert.Equal("severity", exception.ParamName);
        Assert.Contains("not a defined value", exception.Message, StringComparison.Ordinal);
    }



    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_when_the_message_is_empty_throws(string template)
    {
        var exception = Assert.Throws<ArgumentException>(() => new ErrorCode("search.name_taken", 409, template));

        Assert.Equal("messageTemplate", exception.ParamName);
        Assert.Contains("needs a message", exception.Message, StringComparison.Ordinal);
    }



    [Theory]
    [InlineData("A search named {name} exists.", "not positional")]
    [InlineData("A search named {} exists.", "not positional")]
    [InlineData("A search named {0 exists.", "never closed")]
    [InlineData("A search named 0} exists.", "single '}'")]
    [InlineData("{0} and {2}.", "no gap")]
    [InlineData("{1}.", "no gap")]
    [InlineData("{100}.", "not positional")]
    [InlineData("{0:{0}}.", "contains a '{'")]
    public void Constructor_when_the_template_is_malformed_throws_saying_what_is_wrong(string template, string expected)
    {
        var exception = Assert.Throws<ArgumentException>(() => new ErrorCode("search.name_taken", 409, template));

        Assert.Equal("messageTemplate", exception.ParamName);
        Assert.Contains("search.name_taken", exception.Message, StringComparison.Ordinal);
        Assert.Contains(expected, exception.Message, StringComparison.Ordinal);
        Assert.IsType<FormatException>(exception.InnerException);
    }



    [Theory]
    [InlineData("Nothing to fill in.", 0)]
    [InlineData("{0}", 1)]
    [InlineData("{0} of {1} lookups did not complete.", 2)]
    [InlineData("{1} then {0} then {1}.", 2)]
    [InlineData("Total {0,10:N2} for {1:yyyy-MM-dd}.", 2)]
    [InlineData("Literal {{braces}} and {0}.", 1)]
    [InlineData("{{0}}", 0)]
    [InlineData("Ends with a brace }}", 0)]
    [InlineData("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}", 11)]
    public void Constructor_when_the_template_is_well_formed_counts_its_arguments(string template, int expected)
    {
        Assert.Equal(expected, new ErrorCode("search.name_taken", 409, template).ArgumentCount);
    }



    [Fact]
    public void Format_when_given_the_right_number_of_values_fills_the_placeholders()
    {
        var code = new ErrorCode("run.lookups_incomplete", 422, "{0} of {1} lookups did not complete.");

        Assert.Equal("3 of 14 lookups did not complete.", code.Format(3, 14));
    }



    [Fact]
    public void Format_when_the_template_has_no_placeholder_returns_it_with_braces_unescaped()
    {
        var code = new ErrorCode("auth.forbidden", 403, "You may not do that {{here}}.");

        Assert.Equal("You may not do that {here}.", code.Format());
    }



    [Fact]
    public void Format_when_the_current_culture_uses_a_decimal_comma_still_formats_invariantly()
    {
        var code = new ErrorCode("search.price_too_high", 422, "The price {0:N2} is above the limit.");
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
        try
        {
            Assert.Equal("The price 1,234.50 is above the limit.", code.Format(1234.5m));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }



    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void Format_when_given_the_wrong_number_of_values_throws(int count)
    {
        var code = new ErrorCode("run.lookups_incomplete", 422, "{0} of {1} lookups did not complete.");

        var exception = Assert.Throws<ArgumentException>(() => code.Format(new object?[count]));

        Assert.Equal("arguments", exception.ParamName);
        Assert.Contains("takes 2 argument(s)", exception.Message, StringComparison.Ordinal);
    }



    [Fact]
    public void Format_when_given_null_instead_of_values_throws()
    {
        var code = new ErrorCode("auth.forbidden", 403, "You may not do that.");

        Assert.Throws<ArgumentNullException>(() => code.Format(null!));
    }
}
