using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Configurations;

/// <summary>
/// The <c>flight_set</c> table: one flight lookup of a run (DESIGN §6).
/// </summary>
internal sealed class FlightSetConfiguration : IEntityTypeConfiguration<FlightSet>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FlightSet> builder)
    {
        // The key is Id, which EF finds by its name.
        builder
            .Property(flightSet => flightSet.Id)
            .ValueGeneratedNever();

        builder
            .Property(flightSet => flightSet.LastError)
            .HasMaxLength(ColumnLengths.ErrorText);

        builder
            .Property(flightSet => flightSet.LeasedBy)
            .HasMaxLength(ColumnLengths.Line);

        builder
            .Property(flightSet => flightSet.ResultSha256)
            .HasMaxLength(ColumnLengths.Sha256Hex);

        builder
            .Property(flightSet => flightSet.Version)
            .IsConcurrencyToken();

        ConfigureRelationships(builder);
    }



    private static void ConfigureRelationships(EntityTypeBuilder<FlightSet> builder)
    {
        builder
            .HasOne<Run>()
            .WithMany()
            .HasForeignKey(flightSet => flightSet.RunId)
            .OnDelete(DeleteBehavior.Restrict);

        // EF gives the two airport foreign keys their indexes, ix_flight_set_origin_airport_id and
        // ix_flight_set_gateway_airport_id.
        // Serves: finding the flight lookups from or to one airport across runs, which following a
        // fare over time asks for.
        builder
            .HasOne<Airport>()
            .WithMany()
            .HasForeignKey(flightSet => flightSet.OriginAirportId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<Airport>()
            .WithMany()
            .HasForeignKey(flightSet => flightSet.GatewayAirportId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves: reading the flight lookups of one run, and the rule that a run looks a journey up
        // once however many searches want it (DESIGN §6). It leads with the run, so it is also that
        // foreign key's index.
        builder
            .HasIndex
            (
                flightSet => new
                {
                    flightSet.RunId,
                    flightSet.OriginAirportId,
                    flightSet.GatewayAirportId,
                    flightSet.DepartDate,
                    flightSet.ReturnDate,
                    flightSet.Cabin,
                    flightSet.AdultCount,
                    flightSet.ChildCount,
                    flightSet.InfantCount,
                }
            )
            .IsUnique();
    }
}
