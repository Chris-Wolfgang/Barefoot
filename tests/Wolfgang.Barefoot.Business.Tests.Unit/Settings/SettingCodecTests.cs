using System.Globalization;
using Wolfgang.Barefoot.Business.Settings;

namespace Wolfgang.Barefoot.Business.Tests.Unit.Settings;

/// <summary>
/// The text a setting's value is stored as (ADR 0011 §3).
/// </summary>
public sealed class SettingCodecTests
{
    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void Boolean_when_given_a_value_writes_and_reads_it(bool value, string text)
    {
        Assert.Equal(text, SettingCodec.Boolean.Format(value));
        Assert.Equal((true, value), SettingCodec.Boolean.Parse(text));
    }



    [Theory]
    [InlineData("True")]
    [InlineData("1")]
    [InlineData("")]
    [InlineData(" true")]
    public void Boolean_when_the_text_is_not_true_or_false_reads_nothing(string text)
    {
        Assert.Equal((false, false), SettingCodec.Boolean.Parse(text));
    }



    [Theory]
    [InlineData(0, "0")]
    [InlineData(300, "300")]
    [InlineData(-12, "-12")]
    [InlineData(int.MaxValue, "2147483647")]
    public void Integer_when_given_a_value_writes_and_reads_it(int value, string text)
    {
        Assert.Equal(text, SettingCodec.Integer.Format(value));
        Assert.Equal((true, value), SettingCodec.Integer.Parse(text));
    }



    [Theory]
    [InlineData("")]
    [InlineData("1.5")]
    [InlineData("1,000")]
    [InlineData(" 5")]
    [InlineData("5 ")]
    [InlineData("2147483648")]
    [InlineData("five")]
    public void Integer_when_the_text_is_not_a_whole_number_reads_nothing(string text)
    {
        Assert.False(SettingCodec.Integer.Parse(text).Ok);
    }



    [Theory]
    [InlineData(0L, "0")]
    [InlineData(-9000000000L, "-9000000000")]
    [InlineData(long.MaxValue, "9223372036854775807")]
    public void Long_when_given_a_value_writes_and_reads_it(long value, string text)
    {
        Assert.Equal(text, SettingCodec.Long.Format(value));
        Assert.Equal((true, value), SettingCodec.Long.Parse(text));
    }



    [Theory]
    [InlineData("")]
    [InlineData("1.5")]
    [InlineData(" 5")]
    [InlineData("9223372036854775808")]
    public void Long_when_the_text_is_not_a_whole_number_reads_nothing(string text)
    {
        Assert.False(SettingCodec.Long.Parse(text).Ok);
    }



    [Theory]
    [InlineData(0d, "0")]
    [InlineData(2.5, "2.5")]
    [InlineData(-0.1, "-0.1")]
    [InlineData(1e21, "1E+21")]
    public void Number_when_given_a_value_writes_and_reads_it(double value, string text)
    {
        Assert.Equal(text, SettingCodec.Number.Format(value));
        Assert.Equal((true, value), SettingCodec.Number.Parse(text));
    }



    [Theory]
    [InlineData("")]
    [InlineData("2,5")]
    [InlineData(" 2.5")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("1E+400")]
    public void Number_when_the_text_is_not_a_finite_number_reads_nothing(string text)
    {
        Assert.False(SettingCodec.Number.Parse(text).Ok);
    }



    [Fact]
    public void Number_when_the_culture_uses_a_comma_still_writes_a_point()
    {
        var before = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
        try
        {
            Assert.Equal("2.5", SettingCodec.Number.Format(2.5));
            Assert.Equal((true, 2.5), SettingCodec.Number.Parse("2.5"));
            Assert.Equal("1000", SettingCodec.Integer.Format(1000));
            Assert.Equal("1000", SettingCodec.Long.Format(1000));
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }



    [Theory]
    [InlineData("")]
    [InlineData("Barefoot")]
    [InlineData("  spaces kept  ")]
    public void Text_when_given_a_value_writes_and_reads_it_unchanged(string value)
    {
        Assert.Equal(value, SettingCodec.Text.Format(value));
        Assert.Equal((true, value), SettingCodec.Text.Parse(value));
    }



    [Fact]
    public void Duration_when_given_a_value_writes_and_reads_it()
    {
        var value = new TimeSpan(1, 2, 3, 4, 500);

        Assert.Equal("1.02:03:04.5000000", SettingCodec.Duration.Format(value));
        Assert.Equal((true, value), SettingCodec.Duration.Parse("1.02:03:04.5000000"));
        Assert.Equal("00:05:00", SettingCodec.Duration.Format(TimeSpan.FromMinutes(5)));
        Assert.Equal((true, TimeSpan.FromMinutes(5)), SettingCodec.Duration.Parse("00:05:00"));
    }



    [Theory]
    [InlineData("")]
    [InlineData("5 minutes")]
    [InlineData("25:00:00")]
    public void Duration_when_the_text_is_not_a_length_of_time_reads_nothing(string text)
    {
        Assert.False(SettingCodec.Duration.Parse(text).Ok);
    }



    [Fact]
    public void Instant_when_given_a_value_writes_it_in_UTC_and_reads_it()
    {
        var value = new DateTimeOffset(2026, 11, 26, 20, 30, 0, TimeSpan.FromHours(-5));

        var text = SettingCodec.Instant.Format(value);
        var (ok, read) = SettingCodec.Instant.Parse(text);

        Assert.Equal("2026-11-27T01:30:00.0000000Z", text);
        Assert.True(ok);
        Assert.Equal(value, read);
        Assert.Equal(TimeSpan.Zero, read.Offset);
    }



    [Theory]
    [InlineData("")]
    [InlineData("2026-11-27")]
    [InlineData("2026-11-27T01:30:00Z")]
    [InlineData("2026-11-27T01:30:00.0000000+01:00")]
    [InlineData("27/11/2026 01:30")]
    public void Instant_when_the_text_is_not_in_the_stored_form_reads_nothing(string text)
    {
        Assert.False(SettingCodec.Instant.Parse(text).Ok);
    }



    [Fact]
    public void TextList_when_given_a_value_writes_a_JSON_array_and_reads_it()
    {
        IReadOnlyList<string> value = ["a", "b, c", "d e"];

        var text = SettingCodec.TextList.Format(value);
        var (ok, read) = SettingCodec.TextList.Parse(text);

        Assert.Equal("""["a","b, c","d e"]""", text);
        Assert.True(ok);
        Assert.Equal(value, read);
    }



    [Fact]
    public void TextList_when_the_list_is_empty_writes_an_empty_array_and_reads_it()
    {
        Assert.Equal("[]", SettingCodec.TextList.Format([]));

        var (ok, read) = SettingCodec.TextList.Parse("[]");

        Assert.True(ok);
        Assert.Empty(read);
    }



    [Theory]
    [InlineData("")]
    [InlineData("a,b")]
    [InlineData("null")]
    [InlineData("[1,2]")]
    [InlineData("[\"a\",null]")]
    [InlineData("{\"a\":1}")]
    [InlineData("[\"a\"")]
    public void TextList_when_the_text_is_not_an_array_of_strings_reads_nothing(string text)
    {
        var (ok, read) = SettingCodec.TextList.Parse(text);

        Assert.False(ok);
        Assert.Empty(read);
    }



    [Fact]
    public void For_when_asked_for_a_type_a_setting_may_have_gives_its_codec()
    {
        Assert.Same(SettingCodec.Boolean, SettingCodec.For<bool>());
        Assert.Same(SettingCodec.Integer, SettingCodec.For<int>());
        Assert.Same(SettingCodec.Long, SettingCodec.For<long>());
        Assert.Same(SettingCodec.Number, SettingCodec.For<double>());
        Assert.Same(SettingCodec.Text, SettingCodec.For<string>());
        Assert.Same(SettingCodec.Duration, SettingCodec.For<TimeSpan>());
        Assert.Same(SettingCodec.Instant, SettingCodec.For<DateTimeOffset>());
        Assert.Same(SettingCodec.TextList, SettingCodec.For<IReadOnlyList<string>>());
    }



    [Fact]
    public void For_when_asked_for_another_type_throws_and_names_it()
    {
        var exception = Assert.Throws<NotSupportedException>(() => SettingCodec.For<decimal>());

        Assert.Equal("A setting cannot be a Decimal.", exception.Message);
    }



    [Fact]
    public void TypeName_when_read_is_the_name_the_Settings_page_shows()
    {
        Assert.Equal
        (
            ["boolean", "integer", "long", "number", "string", "duration", "instant", "string-list"],
            [
                SettingCodec.Boolean.TypeName,
                SettingCodec.Integer.TypeName,
                SettingCodec.Long.TypeName,
                SettingCodec.Number.TypeName,
                SettingCodec.Text.TypeName,
                SettingCodec.Duration.TypeName,
                SettingCodec.Instant.TypeName,
                SettingCodec.TextList.TypeName,
            ]
        );
    }
}
