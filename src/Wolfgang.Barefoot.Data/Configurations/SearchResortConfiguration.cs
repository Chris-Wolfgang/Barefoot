using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Configurations;

/// <summary>
/// The <c>search_resort</c> table: one resort a search looks up.
/// </summary>
internal sealed class SearchResortConfiguration : IEntityTypeConfiguration<SearchResort>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SearchResort> builder)
    {
        // The key is Id, which EF finds by its name.
        builder
            .Property(link => link.Id)
            .ValueGeneratedNever();

        builder
            .HasOne<SearchMatrix>()
            .WithMany()
            .HasForeignKey(link => link.SearchMatrixId)
            .OnDelete(DeleteBehavior.Restrict);

        // EF gives this foreign key its index, ix_search_resort_resort_id.
        // Serves: finding the searches that name one resort, which the catalog asks when a resort goes offline.
        builder
            .HasOne<Resort>()
            .WithMany()
            .HasForeignKey(link => link.ResortId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves: reading everything one search names, and the rule that a search names a row once.
        // It leads with the search, so it is also that foreign key's index.
        builder
            .HasIndex(link => new { link.SearchMatrixId, link.ResortId })
            .IsUnique();
    }
}
