using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Wolfgang.Barefoot.Data.Conventions;

/// <summary>
/// Stores every instant as a UTC date and time without an offset (docs/DATABASE-CONVENTIONS.md, Time).
/// The offset is always zero, and a plain date-time column sorts and compares in SQL on both SQL
/// Server and SQLite, which a <see cref="DateTimeOffset"/> column does not on SQLite.
/// </summary>
internal sealed class UtcInstantConverter : ValueConverter<DateTimeOffset, DateTime>
{
    /// <summary>Makes the converter.</summary>
    public UtcInstantConverter()
        : base
        (
            instant => instant.UtcDateTime,
            stored => new DateTimeOffset(DateTime.SpecifyKind(stored, DateTimeKind.Utc))
        )
    {
    }
}
