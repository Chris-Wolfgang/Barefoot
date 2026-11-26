using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.Barefoot.Core.Entities;
using Wolfgang.Barefoot.Data.Conventions;

namespace Wolfgang.Barefoot.Data.Configurations;

/// <summary>
/// The <c>app_user</c> table. Its key is the Entra object id, which is given and never generated.
/// The table is created with one row in it, the "Unassigned" user (ADR 0004 §8).
/// </summary>
internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    /// <summary>
    /// When the "Unassigned" user was last seen: never, so the earliest day the model uses. A seeded
    /// value has to be the same every time the model is built, so it cannot be the time of creation.
    /// </summary>
    internal static readonly DateTimeOffset UnassignedLastSeenUtc = new(1900, 1, 1, 0, 0, 0, TimeSpan.Zero);



    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.HasKey(user => user.Oid);

        builder
            .Property(user => user.Oid)
            .ValueGeneratedNever();

        builder
            .Property(user => user.DisplayName)
            .HasMaxLength(ColumnLengths.DisplayName);

        builder
            .Property(user => user.Email)
            .HasMaxLength(ColumnLengths.EmailAddress);

        builder
            .Property(user => user.TimeZoneId)
            .HasMaxLength(ColumnLengths.TimeZoneId);

        builder
            .Property(user => user.Version)
            .IsConcurrencyToken();

        // Seeded rows are not written by SaveChanges, so the row states its own first version.
        builder.HasData
        (
            new AppUser
            {
                Oid = AppUser.UnassignedOid,
                DisplayName = AppUser.UnassignedDisplayName,
                Email = string.Empty,
                LastSeenUtc = UnassignedLastSeenUtc,
                Version = VersionStamp.FirstVersion,
            }
        );
    }
}
