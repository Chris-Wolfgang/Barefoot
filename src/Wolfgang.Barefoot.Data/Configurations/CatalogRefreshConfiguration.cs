using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Configurations;

/// <summary>
/// The <c>catalog_refresh</c> table: one row per catalog snapshot received (ADR 0017 §5).
/// </summary>
internal sealed class CatalogRefreshConfiguration : IEntityTypeConfiguration<CatalogRefresh>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CatalogRefresh> builder)
    {
        // The key is Id, which EF finds by its name.
        builder
            .Property(refresh => refresh.Id)
            .ValueGeneratedNever();

        builder
            .Property(refresh => refresh.RefusalReason)
            .HasMaxLength(ColumnLengths.Sentence);

        // Serves: the newest snapshot of one kind, which the Reference data page asks for.
        builder.HasIndex(refresh => new { refresh.Kind, refresh.ReceivedUtc });
    }
}
