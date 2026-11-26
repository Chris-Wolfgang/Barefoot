using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Configurations;

/// <summary>
/// The <c>flight_direction</c> table: the way out or the way back of a flight option.
/// </summary>
internal sealed class FlightDirectionConfiguration : IEntityTypeConfiguration<FlightDirection>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FlightDirection> builder)
    {
        // The key is Id, which EF finds by its name.
        builder
            .Property(direction => direction.Id)
            .ValueGeneratedNever();

        builder
            .HasOne<FlightOption>()
            .WithMany()
            .HasForeignKey(direction => direction.FlightOptionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves: reading the two directions of one option, and the rule that an option has one
        // direction each way. It leads with the option, so it is also the foreign key's index.
        builder
            .HasIndex(direction => new { direction.FlightOptionId, direction.IsReturn })
            .IsUnique();
    }
}
