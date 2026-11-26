using Microsoft.EntityFrameworkCore;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data;

/// <summary>
/// Barefoot's database: the unit of work every Data service uses (ADR 0002 §3). The model names no
/// provider; production runs it on SQL Server and the tests on SQLite.
/// </summary>
/// <param name="options">The provider and the connection, chosen by the host.</param>
public sealed class BarefootDbContext(DbContextOptions<BarefootDbContext> options) : DbContext(options)
{
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
        BarefootModel.ConfigureConventions(configurationBuilder);
    }



    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        BarefootModel.Configure(modelBuilder);
    }
}
