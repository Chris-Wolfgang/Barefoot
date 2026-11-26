using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Configurations;

/// <summary>
/// The <c>search_request</c> table: one room lookup of a run (ADR 0004 §1).
/// </summary>
internal sealed class SearchRequestConfiguration : IEntityTypeConfiguration<SearchRequest>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SearchRequest> builder)
    {
        // The key is Id, which EF finds by its name.
        builder
            .Property(request => request.Id)
            .ValueGeneratedNever();

        builder
            .Property(request => request.LastError)
            .HasMaxLength(ColumnLengths.ErrorText);

        builder
            .Property(request => request.LeasedBy)
            .HasMaxLength(ColumnLengths.Line);

        builder
            .Property(request => request.ResultSha256)
            .HasMaxLength(ColumnLengths.Sha256Hex);

        builder
            .Property(request => request.Version)
            .IsConcurrencyToken();

        builder
            .HasOne<Run>()
            .WithMany()
            .HasForeignKey(request => request.RunId)
            .OnDelete(DeleteBehavior.Restrict);

        // EF gives this foreign key its index, ix_search_request_resort_id.
        // Serves: finding the lookups of one resort across runs, which following a price over time asks for.
        builder
            .HasOne<Resort>()
            .WithMany()
            .HasForeignKey(request => request.ResortId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves: reading the lookups of one run, and the rule that a run looks a stay up once
        // however many searches want it (dedupe at explode). The cabin is not part of it. It leads
        // with the run, so it is also that foreign key's index.
        builder
            .HasIndex
            (
                request => new
                {
                    request.RunId,
                    request.ResortId,
                    request.CheckIn,
                    request.CheckOut,
                    request.AdultCount,
                    request.ChildCount,
                    request.InfantCount,
                }
            )
            .IsUnique();
    }
}
