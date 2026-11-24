using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Configurations;

/// <summary>
/// The <c>resort</c> table: a resort's permanent identity (DESIGN §7).
/// </summary>
internal sealed class ResortConfiguration : IEntityTypeConfiguration<Resort>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Resort> builder)
    {
        // The key is Id, which EF finds by its name.
        builder
            .Property(resort => resort.Id)
            .ValueGeneratedNever();

        builder
            .Property(resort => resort.Code)
            .HasMaxLength(ColumnLengths.SiteCode);

        builder
            .Property(resort => resort.Name)
            .HasMaxLength(ColumnLengths.Line);

        builder
            .Property(resort => resort.ShortName)
            .HasMaxLength(ColumnLengths.Line);

        builder
            .Property(resort => resort.Island)
            .HasMaxLength(ColumnLengths.PlaceName);

        builder
            .Property(resort => resort.Version)
            .IsConcurrencyToken();

        builder
            .HasOne<Airport>()
            .WithMany()
            .HasForeignKey(resort => resort.GatewayAirportId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves: finding a resort by its code, which a catalog snapshot does for every resort it
        // lists, and the rule that a code is one resort.
        builder
            .HasIndex(resort => resort.Code)
            .IsUnique();

        // Serves: finding the resorts reached through one airport, which planning a run's flight
        // lookups asks for. It is also the foreign key's index.
        builder.HasIndex(resort => resort.GatewayAirportId);
    }
}
