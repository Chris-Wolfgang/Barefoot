using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Configurations;

/// <summary>
/// The <c>setting</c> table. A row is keyed by the setting's name (ADR 0011).
/// </summary>
internal sealed class SettingConfiguration : IEntityTypeConfiguration<Setting>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Setting> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(setting => setting.Key);

        builder
            .Property(setting => setting.Key)
            .HasMaxLength(ColumnLengths.SettingKey);

        builder
            .Property(setting => setting.Value)
            .HasMaxLength(ColumnLengths.SettingValue);

        builder
            .Property(setting => setting.Version)
            .IsConcurrencyToken();
    }
}
