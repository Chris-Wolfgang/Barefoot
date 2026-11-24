using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Configurations;

/// <summary>
/// The <c>app_user</c> table. Its key is the Entra object id, which is given and never generated.
/// </summary>
internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
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
    }
}
