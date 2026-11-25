using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Configurations;

/// <summary>
/// The <c>room_result</c> table: one room the site offered for one lookup (DESIGN §8).
/// </summary>
internal sealed class RoomResultConfiguration : IEntityTypeConfiguration<RoomResult>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RoomResult> builder)
    {
        // The key is Id, which EF finds by its name.
        builder
            .Property(result => result.Id)
            .ValueGeneratedNever();

        builder
            .Property(result => result.Promotion)
            .HasMaxLength(ColumnLengths.Sentence);

        // EF gives every foreign key below its index, ix_room_result_<column>.

        // Serves: reading the rooms one lookup found, which every result page and digest asks for.
        builder
            .HasOne<SearchRequest>()
            .WithMany()
            .HasForeignKey(result => result.SearchRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves: following one room's price over time, which the price history asks for.
        builder
            .HasOne<RoomCategory>()
            .WithMany()
            .HasForeignKey(result => result.RoomCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves, with the two below: finding the results that have one level, view or type, which
        // deciding whether a vocabulary value is still shown asks for.
        builder
            .HasOne<VocabularyValue>()
            .WithMany()
            .HasForeignKey(result => result.RoomLevelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<VocabularyValue>()
            .WithMany()
            .HasForeignKey(result => result.RoomViewId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<VocabularyValue>()
            .WithMany()
            .HasForeignKey(result => result.RoomTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
