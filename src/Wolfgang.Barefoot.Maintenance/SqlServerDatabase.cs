using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Wolfgang.Barefoot.Data;

namespace Wolfgang.Barefoot.Maintenance;

/// <summary>
/// Makes a <see cref="BarefootDbContext"/> on SQL Server with the connection options of ADR 0001 §1,
/// which let a serverless database resume before a command gives up.
/// </summary>
internal static class SqlServerDatabase
{
    /// <summary>How long a connection attempt waits, in seconds. A resume can take a minute.</summary>
    public const int ConnectTimeoutSeconds = 90;

    /// <summary>How long a command waits, in seconds.</summary>
    public const int CommandTimeoutSeconds = 60;

    /// <summary>How many times a command that failed for a passing reason is tried again.</summary>
    public const int MaxRetryCount = 8;

    /// <summary>The longest wait between two tries, in seconds.</summary>
    public const int MaxRetryDelaySeconds = 20;

    /// <summary>
    /// SQL Server error numbers that are retried besides the ones EF Core already knows. 4060, "cannot
    /// open database", appears on some resume paths.
    /// </summary>
    public static readonly IReadOnlyList<int> AdditionalTransientErrorNumbers = [4060];



    /// <summary>
    /// Makes a context on the database <paramref name="connectionString"/> names. It opens no
    /// connection.
    /// </summary>
    /// <param name="connectionString">
    /// A SQL Server connection string. Its connect timeout is replaced, and the connection is always
    /// encrypted, whatever the string says.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="connectionString"/> is not a connection string.</exception>
    public static BarefootDbContext Open(string connectionString)
    {
        // The string is set first, so the two settings after it replace whatever it says.
        var connection = new SqlConnectionStringBuilder
        {
            ConnectionString = connectionString,
            ConnectTimeout = ConnectTimeoutSeconds,
            Encrypt = SqlConnectionEncryptOption.Mandatory,
        };

        var options = new DbContextOptionsBuilder<BarefootDbContext>()
            .UseSqlServer
            (
                connection.ConnectionString,
                sqlServer => sqlServer
                    .EnableRetryOnFailure(MaxRetryCount, TimeSpan.FromSeconds(MaxRetryDelaySeconds), AdditionalTransientErrorNumbers)
                    .CommandTimeout(CommandTimeoutSeconds)
            )
            .Options;

        return new BarefootDbContext(options);
    }



    /// <summary>
    /// Makes a context on SQL Server that names no database. It can render the SQL of the migrations
    /// and cannot open a connection.
    /// </summary>
    public static BarefootDbContext Offline()
    {
        var options = new DbContextOptionsBuilder<BarefootDbContext>()
            .UseSqlServer()
            .Options;

        return new BarefootDbContext(options);
    }
}
