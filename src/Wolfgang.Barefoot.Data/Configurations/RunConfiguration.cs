using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.Barefoot.Contracts.Runs;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Configurations;

/// <summary>
/// The <c>run</c> table: one pass over every active search (DESIGN §5).
/// </summary>
internal sealed class RunConfiguration : IEntityTypeConfiguration<Run>
{
    /// <summary>
    /// The condition of the run lock's index: only a run in progress is in it. It is the one SQL
    /// fragment in the model, written so that SQL Server and SQLite both read it: a column name
    /// that needs no quoting, compared with the number the status is stored as.
    /// </summary>
    internal static readonly string InProgressFilter =
        "status = " + ((int)RunStatus.InProgress).ToString(CultureInfo.InvariantCulture);



    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Run> builder)
    {
        // The key is Id, which EF finds by its name.
        builder
            .Property(run => run.Id)
            .ValueGeneratedNever();

        builder
            .Property(run => run.Trigger)
            .HasMaxLength(ColumnLengths.Line);

        builder
            .Property(run => run.AbortReason)
            .HasMaxLength(ColumnLengths.Sentence);

        builder
            .Property(run => run.WorkerName)
            .HasMaxLength(ColumnLengths.Line);

        builder
            .Property(run => run.WorkerBuildVersion)
            .HasMaxLength(ColumnLengths.BuildVersion);

        // Serves: the run lock. Only runs in progress are in the index and it is unique, so a second
        // run in progress cannot be stored (a race is a 409). It also finds the run in progress.
        builder
            .HasIndex(run => run.Status)
            .IsUnique()
            .HasFilter(InProgressFilter);
    }
}
