using Wolfgang.Barefoot.Data.Conventions;

namespace Wolfgang.Barefoot.Data.Tests.Unit.Conventions;

public sealed class UtcInstantConverterTests
{
    private static readonly UtcInstantConverter Converter = new();



    [Fact]
    public void ConvertToProvider_when_the_instant_has_an_offset_stores_the_utc_date_and_time()
    {
        var instant = new DateTimeOffset(2026, 11, 4, 8, 40, 0, TimeSpan.FromHours(2));

        var stored = (DateTime)Converter.ConvertToProvider(instant)!;

        Assert.Equal(new DateTime(2026, 11, 4, 6, 40, 0, DateTimeKind.Utc), stored);
        Assert.Equal(DateTimeKind.Utc, stored.Kind);
    }



    [Fact]
    public void ConvertFromProvider_when_the_stored_value_has_no_kind_reads_it_as_utc()
    {
        var stored = new DateTime(2026, 11, 4, 6, 40, 0, DateTimeKind.Unspecified);

        var instant = (DateTimeOffset)Converter.ConvertFromProvider(stored)!;

        Assert.Equal(new DateTimeOffset(2026, 11, 4, 6, 40, 0, TimeSpan.Zero), instant);
        Assert.Equal(TimeSpan.Zero, instant.Offset);
    }
}
