using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Wolfgang.Barefoot.Data.Migrations;

/// <summary>
/// Gives <c>dotnet ef</c> a <see cref="BarefootDbContext"/> on SQL Server, the provider the
/// migrations are written for (ADR 0001). It names no database: adding a migration and rendering its
/// SQL need none, and only <c>bf-maint</c> holds a connection string.
/// </summary>
internal sealed class DesignTimeContextFactory : IDesignTimeDbContextFactory<BarefootDbContext>
{
    /// <inheritdoc />
    public BarefootDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BarefootDbContext>()
            .UseSqlServer()
            .Options;

        return new BarefootDbContext(options);
    }
}
