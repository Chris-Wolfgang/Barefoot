using Wolfgang.Barefoot.Tests.Architecture.Rules;
using Wolfgang.Barefoot.Tests.Architecture.Scanning;

namespace Wolfgang.Barefoot.Tests.Architecture;

/// <summary>
/// The <c>Data</c> project names its database provider, SQL Server, only in <c>Migrations/</c>. The
/// model, the configurations and the context stay free of it, so the tests can run them on SQLite.
/// </summary>
public sealed class ProviderPlacementTests
{
    [Fact]
    public async Task ProviderNamesOutsideMigrationsIn_when_given_the_repository_reports_nothing()
    {
        var sources = await RepositoryFiles.SourcesUnderAsync(DataSourceRules.DataDirectory);

        Assert.Contains
        (
            sources,
            source => source.Path.StartsWith(DataSourceRules.MigrationsDirectory, StringComparison.Ordinal)
        );
        Assert.Contains
        (
            sources,
            source => !source.Path.StartsWith(DataSourceRules.MigrationsDirectory, StringComparison.Ordinal)
        );
        Assert.Empty(DataSourceRules.ProviderNamesOutsideMigrationsIn(sources));
    }



    [Fact]
    public void ProviderNamesOutsideMigrationsIn_when_code_outside_Migrations_names_the_provider_reports_each_place()
    {
        var sources = new[]
        {
            new SourceFile
            (
                "src/Wolfgang.Barefoot.Data/Configurations/RoomConfiguration.cs",
                """
                using Microsoft.Data.SqlClient;
                using Microsoft.EntityFrameworkCore;

                // SqlServer in a comment is not code.
                internal sealed class RoomConfiguration
                {
                    public void Configure(EntityTypeBuilder<Room> builder)
                    {
                        builder.Property(room => room.Rate).HasAnnotation("SqlServer:Identity", "1, 1");
                    }
                }
                """
            ),
            new SourceFile
            (
                "src/Wolfgang.Barefoot.Data/BarefootDbContext.cs",
                """
                public sealed class BarefootDbContext
                {
                    protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlServer();
                }
                """
            ),
        };

        Assert.Equal
        (
            [
                "src/Wolfgang.Barefoot.Data/BarefootDbContext.cs(3): UseSqlServer",
                "src/Wolfgang.Barefoot.Data/Configurations/RoomConfiguration.cs(1): SqlClient",
                "src/Wolfgang.Barefoot.Data/Configurations/RoomConfiguration.cs(9): SqlServer:Identity",
            ],
            DataSourceRules.ProviderNamesOutsideMigrationsIn(sources)
        );
    }



    [Fact]
    public void ProviderNamesOutsideMigrationsIn_when_the_provider_is_named_in_Migrations_or_another_project_reports_nothing()
    {
        var sources = new[]
        {
            new SourceFile
            (
                "src/Wolfgang.Barefoot.Data/Migrations/DesignTimeContextFactory.cs",
                "internal sealed class Factory { void Build(DbContextOptionsBuilder options) => options.UseSqlServer(); }"
            ),
            new SourceFile
            (
                "src/Wolfgang.Barefoot.Maintenance/Program.cs",
                "internal static class Program { static void Build(DbContextOptionsBuilder options) => options.UseSqlServer(); }"
            ),
            new SourceFile
            (
                "src/Wolfgang.Barefoot.Data/Conventions/ModelConventions.cs",
                "/// <summary>The longest name SQL Server accepts.</summary>\ninternal static class ModelConventions;"
            ),
        };

        Assert.Empty(DataSourceRules.ProviderNamesOutsideMigrationsIn(sources));
    }
}
