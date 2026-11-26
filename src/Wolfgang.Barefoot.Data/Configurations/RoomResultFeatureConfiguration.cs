using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Configurations;

/// <summary>
/// The <c>room_result_feature</c> table: one feature a room result has.
/// </summary>
internal sealed class RoomResultFeatureConfiguration : IEntityTypeConfiguration<RoomResultFeature>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RoomResultFeature> builder)
    {
        // The key is Id, which EF finds by its name.
        builder
            .Property(feature => feature.Id)
            .ValueGeneratedNever();

        builder
            .HasOne<RoomResult>()
            .WithMany()
            .HasForeignKey(feature => feature.RoomResultId)
            .OnDelete(DeleteBehavior.Restrict);

        // EF gives this foreign key its index, ix_room_result_feature_vocabulary_value_id.
        // Serves: finding the results that have one feature, which deciding whether the feature is
        // still shown asks for.
        builder
            .HasOne<VocabularyValue>()
            .WithMany()
            .HasForeignKey(feature => feature.VocabularyValueId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves: reading the features of one result, and the rule that a result names a feature
        // once. It leads with the result, so it is also that foreign key's index.
        builder
            .HasIndex(feature => new { feature.RoomResultId, feature.VocabularyValueId })
            .IsUnique();
    }
}
