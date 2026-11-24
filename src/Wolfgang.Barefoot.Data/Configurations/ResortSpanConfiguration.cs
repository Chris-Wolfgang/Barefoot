using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Configurations;

/// <summary>
/// The <c>resort_span</c> table: a stretch of days during which a resort had one status (DESIGN §7).
/// </summary>
internal sealed class ResortSpanConfiguration : IEntityTypeConfiguration<ResortSpan>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ResortSpan> builder)
    {
        // The key is Id, which EF finds by its name.
        builder
            .Property(span => span.Id)
            .ValueGeneratedNever();

        builder
            .HasOne<Resort>()
            .WithMany()
            .HasForeignKey(span => span.ResortId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves: reading one resort's spans in date order, which asking for its status on a day and
        // closing its open span both do. It leads with the resort, so it is also the foreign key's index.
        builder.HasIndex(span => new { span.ResortId, span.EffDate });
    }
}
