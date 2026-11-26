using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Configurations;

/// <summary>
/// The <c>room_category</c> table: one kind of room a resort sells (DESIGN §7).
/// </summary>
internal sealed class RoomCategoryConfiguration : IEntityTypeConfiguration<RoomCategory>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RoomCategory> builder)
    {
        // The key is Id, which EF finds by its name.
        builder
            .Property(category => category.Id)
            .ValueGeneratedNever();

        builder
            .Property(category => category.Code)
            .HasMaxLength(ColumnLengths.SiteCode);

        builder
            .Property(category => category.Name)
            .HasMaxLength(ColumnLengths.Line);

        builder
            .HasOne<Resort>()
            .WithMany()
            .HasForeignKey(category => category.ResortId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves: finding a resort's room by its code, which saving a lookup's results does for every
        // room, and the rule that a resort has a code once. It leads with the resort, so it is also
        // the foreign key's index.
        builder
            .HasIndex(category => new { category.ResortId, category.Code })
            .IsUnique();
    }
}
