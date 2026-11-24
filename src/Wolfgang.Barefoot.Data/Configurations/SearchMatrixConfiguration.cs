using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Configurations;

/// <summary>
/// The <c>search_matrix</c> table: a stored search (DESIGN §4, ADR 0004).
/// </summary>
internal sealed class SearchMatrixConfiguration : IEntityTypeConfiguration<SearchMatrix>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SearchMatrix> builder)
    {
        // The key is Id, which EF finds by its name.
        builder
            .Property(search => search.Id)
            .ValueGeneratedNever();

        builder
            .Property(search => search.Name)
            .HasMaxLength(ColumnLengths.Line);

        builder
            .Property(search => search.NormalizedName)
            .HasMaxLength(ColumnLengths.Line);

        builder
            .Property(search => search.Version)
            .IsConcurrencyToken();

        builder
            .HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(search => search.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves: listing one owner's searches, and the rule that an owner has a name once (a
        // duplicate is a 409). It leads with the owner, so it is also the foreign key's index.
        builder
            .HasIndex(search => new { search.OwnerId, search.NormalizedName })
            .IsUnique();
    }
}
