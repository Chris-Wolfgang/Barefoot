using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Configurations;

/// <summary>
/// The <c>flight_option</c> table: one way to fly there and back that the site offered for a flight
/// lookup.
/// </summary>
internal sealed class FlightOptionConfiguration : IEntityTypeConfiguration<FlightOption>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FlightOption> builder)
    {
        // The key is Id, which EF finds by its name.
        builder
            .Property(option => option.Id)
            .ValueGeneratedNever();

        // EF gives this foreign key its index, ix_flight_option_flight_set_id.
        // Serves: reading the options of one flight lookup, which every result page and digest asks for.
        builder
            .HasOne<FlightSet>()
            .WithMany()
            .HasForeignKey(option => option.FlightSetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
