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



    /// <summary>The airports the booking site knows.</summary>
    public DbSet<Airport> Airports => Set<Airport>();



    /// <summary>The resorts, each with its permanent identity.</summary>
    public DbSet<Resort> Resorts => Set<Resort>();



    /// <summary>The stretches of days during which a resort was online or offline.</summary>
    public DbSet<ResortSpan> ResortSpans => Set<ResortSpan>();



    /// <summary>The kinds of room each resort sells.</summary>
    public DbSet<RoomCategory> RoomCategories => Set<RoomCategory>();



    /// <summary>The values of the room vocabularies: levels, views, types and features.</summary>
    public DbSet<VocabularyValue> VocabularyValues => Set<VocabularyValue>();



    /// <summary>The stored searches.</summary>
    public DbSet<SearchMatrix> SearchMatrices => Set<SearchMatrix>();



    /// <summary>The resorts each search looks up.</summary>
    public DbSet<SearchResort> SearchResorts => Set<SearchResort>();



    /// <summary>The airports each search looks up flights from.</summary>
    public DbSet<SearchDepartureAirport> SearchDepartureAirports => Set<SearchDepartureAirport>();



    /// <summary>The vocabulary values each search filters rooms by.</summary>
    public DbSet<SearchRoomFilter> SearchRoomFilters => Set<SearchRoomFilter>();



    /// <summary>The recipients each search mails its digest to, besides its owner.</summary>
    public DbSet<SearchRecipient> SearchRecipients => Set<SearchRecipient>();



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
