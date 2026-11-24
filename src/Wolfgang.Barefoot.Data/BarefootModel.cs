using Microsoft.EntityFrameworkCore;
using Wolfgang.Barefoot.Data.Configurations;
using Wolfgang.Barefoot.Data.Conventions;

namespace Wolfgang.Barefoot.Data;

/// <summary>
/// What <see cref="BarefootDbContext"/>'s model is made of: the rules that hold for every column of a
/// type, the configuration of each table, and the names.
/// </summary>
internal static class BarefootModel
{
    /// <summary>
    /// How many digits of a second an instant keeps: milliseconds (docs/DATABASE-CONVENTIONS.md, Types).
    /// </summary>
    public const int InstantPrecision = 3;



    /// <summary>
    /// Sets how every instant in the model is stored: in UTC, without an offset, to the millisecond.
    /// </summary>
    /// <param name="configurationBuilder">The model's conventions.</param>
    public static void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        configurationBuilder
            .Properties<DateTimeOffset>()
            .HaveConversion<UtcInstantConverter>()
            .HavePrecision(InstantPrecision);
    }



    /// <summary>
    /// Applies the configuration of each table, then names everything.
    /// </summary>
    /// <param name="modelBuilder">The model being built.</param>
    public static void Configure(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // Every configuration is registered here by name. There is no assembly scanning (ADR 0002 §3).
        modelBuilder.ApplyConfiguration(new AppUserConfiguration());
        modelBuilder.ApplyConfiguration(new SettingConfiguration());
        modelBuilder.ApplyConfiguration(new RecipientConfiguration());
        modelBuilder.ApplyConfiguration(new OpenIssueConfiguration());
        modelBuilder.ApplyConfiguration(new AirportConfiguration());
        modelBuilder.ApplyConfiguration(new ResortConfiguration());
        modelBuilder.ApplyConfiguration(new ResortSpanConfiguration());
        modelBuilder.ApplyConfiguration(new RoomCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new VocabularyValueConfiguration());

        SchemaNames.Apply(modelBuilder);
    }
}
