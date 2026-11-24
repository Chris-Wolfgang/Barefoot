using Microsoft.EntityFrameworkCore;
using Wolfgang.Barefoot.Core.Entities;
using Wolfgang.Barefoot.Data.Configurations;
using Wolfgang.Barefoot.Data.Conventions;

namespace Wolfgang.Barefoot.Data;

/// <summary>
/// Barefoot's database: the unit of work every Data service uses (ADR 0002 §3). The model names no
/// provider; production runs it on SQL Server and the tests on SQLite.
/// </summary>
/// <param name="options">The provider and the connection, chosen by the host.</param>
public sealed class BarefootDbContext(DbContextOptions<BarefootDbContext> options) : DbContext(options)
{
    /// <summary>How many digits a money column holds in all (docs/DATABASE-CONVENTIONS.md, Types).</summary>
    public const int AmountPrecision = 10;

    /// <summary>How many of them follow the decimal point.</summary>
    public const int AmountScale = 2;

    /// <summary>How many digits of a second an instant keeps: milliseconds.</summary>
    public const int InstantPrecision = 3;



    /// <summary>The people who have signed in, and the seeded "Unassigned" user.</summary>
    public DbSet<AppUser> AppUsers => Set<AppUser>();



    /// <summary>The settings an Admin can change while the application runs.</summary>
    public DbSet<Setting> Settings => Set<Setting>();



    /// <summary>The addresses in each person's address book.</summary>
    public DbSet<Recipient> Recipients => Set<Recipient>();



    /// <summary>What each person has been told needs their attention.</summary>
    public DbSet<OpenIssue> OpenIssues => Set<OpenIssue>();



    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        configurationBuilder
            .Properties<DateTimeOffset>()
            .HaveConversion<UtcInstantConverter>()
            .HavePrecision(InstantPrecision);

        configurationBuilder
            .Properties<decimal>()
            .HavePrecision(AmountPrecision, AmountScale);
    }



    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // Every configuration is registered here by name. There is no assembly scanning (ADR 0002 §3).
        modelBuilder.ApplyConfiguration(new AppUserConfiguration());
        modelBuilder.ApplyConfiguration(new SettingConfiguration());
        modelBuilder.ApplyConfiguration(new RecipientConfiguration());
        modelBuilder.ApplyConfiguration(new OpenIssueConfiguration());

        SchemaNames.Apply(modelBuilder);
    }
}
