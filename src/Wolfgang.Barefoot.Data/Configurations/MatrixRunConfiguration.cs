using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Configurations;

/// <summary>
/// The <c>matrix_run</c> table: one search's part in one run (DESIGN §8).
/// </summary>
internal sealed class MatrixRunConfiguration : IEntityTypeConfiguration<MatrixRun>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MatrixRun> builder)
    {
        // The key is Id, which EF finds by its name.
        builder
            .Property(matrixRun => matrixRun.Id)
            .ValueGeneratedNever();

        builder
            .Property(matrixRun => matrixRun.SkipReason)
            .HasMaxLength(ColumnLengths.Sentence);

        builder
            .HasOne<Run>()
            .WithMany()
            .HasForeignKey(matrixRun => matrixRun.RunId)
            .OnDelete(DeleteBehavior.Restrict);

        // EF gives this foreign key its index, ix_matrix_run_search_matrix_id.
        // Serves: finding the runs one search took part in, which its Dashboard row and its history ask for.
        builder
            .HasOne<SearchMatrix>()
            .WithMany()
            .HasForeignKey(matrixRun => matrixRun.SearchMatrixId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves: reading the searches of one run, and the rule that a search takes part in a run
        // once. It leads with the run, so it is also that foreign key's index.
        builder
            .HasIndex(matrixRun => new { matrixRun.RunId, matrixRun.SearchMatrixId })
            .IsUnique();
    }
}
