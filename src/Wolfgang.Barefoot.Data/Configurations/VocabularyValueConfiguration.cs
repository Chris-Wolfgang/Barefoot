using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Configurations;

/// <summary>
/// The <c>vocabulary_value</c> table: one value of a room vocabulary, as results reveal it (DESIGN §7).
/// </summary>
internal sealed class VocabularyValueConfiguration : IEntityTypeConfiguration<VocabularyValue>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<VocabularyValue> builder)
    {
        // The key is Id, which EF finds by its name.
        builder
            .Property(value => value.Id)
            .ValueGeneratedNever();

        builder
            .Property(value => value.Value)
            .HasMaxLength(ColumnLengths.Line);

        // Serves: listing one vocabulary where a search is edited, finding a value when a result shows
        // it, and the rule that a vocabulary has a value once.
        builder
            .HasIndex(value => new { value.Kind, value.Value })
            .IsUnique();
    }
}
