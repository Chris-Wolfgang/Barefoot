using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Configurations;

/// <summary>
/// The <c>recipient</c> table: one address in one person's address book (ADR 0006).
/// </summary>
internal sealed class RecipientConfiguration : IEntityTypeConfiguration<Recipient>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Recipient> builder)
    {
        // The key is Id, which EF finds by its name.
        builder
            .Property(recipient => recipient.Id)
            .ValueGeneratedNever();

        builder
            .Property(recipient => recipient.Address)
            .HasMaxLength(ColumnLengths.EmailAddress);

        builder
            .Property(recipient => recipient.DisplayName)
            .HasMaxLength(ColumnLengths.DisplayName);

        builder
            .Property(recipient => recipient.Version)
            .IsConcurrencyToken();

        builder
            .HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(recipient => recipient.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves: listing one owner's address book, and the rule that an owner has an address once
        // (a duplicate is a 409). It leads with the owner, so it is also the foreign key's index.
        builder
            .HasIndex(recipient => new { recipient.OwnerId, recipient.Address })
            .IsUnique();
    }
}
