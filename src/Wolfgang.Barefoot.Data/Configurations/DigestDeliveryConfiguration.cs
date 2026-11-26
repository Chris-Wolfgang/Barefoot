using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Configurations;

/// <summary>
/// The <c>digest_delivery</c> table: one digest for one recipient, stored when it is built and sent
/// later from the stored row (ADR 0006).
/// </summary>
internal sealed class DigestDeliveryConfiguration : IEntityTypeConfiguration<DigestDelivery>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DigestDelivery> builder)
    {
        // The key is Id, which EF finds by its name.
        builder
            .Property(delivery => delivery.Id)
            .ValueGeneratedNever();

        builder
            .Property(delivery => delivery.RecipientAddress)
            .HasMaxLength(ColumnLengths.EmailAddress);

        builder
            .Property(delivery => delivery.Subject)
            .HasMaxLength(ColumnLengths.Line);

        // HtmlBody and TextBody have no length: a digest is about 30 KB (ADR 0006).

        builder
            .Property(delivery => delivery.LastError)
            .HasMaxLength(ColumnLengths.ErrorText);

        // EF gives the two foreign keys their indexes, ix_digest_delivery_matrix_run_id and
        // ix_digest_delivery_recipient_user_id.
        // Serves: reading the digests of one search's run, which a resend and the run's page ask for.
        builder
            .HasOne<MatrixRun>()
            .WithMany()
            .HasForeignKey(delivery => delivery.MatrixRunId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves: reading the digests one person was sent, which their digest list asks for.
        builder
            .HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(delivery => delivery.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves: finding the digests that wait to be sent, oldest first, which bf send-digests asks
        // for on every run (ADR 0006: it only ever sends Pending rows).
        builder.HasIndex(delivery => new { delivery.Status, delivery.CreatedUtc });
    }
}
