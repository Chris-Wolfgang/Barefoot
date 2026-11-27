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
    /// The name of the API's database user: the name of its user-assigned managed identity (ADR 0016).
    /// </summary>
    public const string ApiUserName = "barefoot-api";

    /// <summary>
    /// What a script for SQL Server starts with. The schema has filtered indexes, which SQL Server
    /// only makes with <c>QUOTED_IDENTIFIER</c> on, and <c>sqlcmd</c> leaves it off unless told
    /// otherwise: without this line the script fails there with error 1934.
    /// </summary>
    public const string ScriptPreamble = "SET QUOTED_IDENTIFIER ON;\nGO\n\n";

    /// <summary>
    /// Reads the schema of the database as the lines <see cref="SchemaLines"/> describes, one per
    /// table, column, key, index and foreign key, in a column named <c>Value</c>. The migrations
    /// history table is left out: it is not part of the model.
    /// </summary>
    public const string CatalogQuery =
        """
        SELECT CAST(N'table ' + t.name + N'|' AS nvarchar(max)) AS [Value]
        FROM sys.tables t
        WHERE t.is_ms_shipped = 0 AND t.name <> N'__EFMigrationsHistory'
        UNION ALL
        SELECT N'column ' + t.name + N'.' + c.name + N'|'
            + CASE
                WHEN ty.name IN (N'nvarchar', N'nchar') THEN ty.name + N'(' + CASE WHEN c.max_length = -1 THEN N'max' ELSE CAST(c.max_length / 2 AS nvarchar(10)) END + N')'
                WHEN ty.name IN (N'varchar', N'char', N'varbinary', N'binary') THEN ty.name + N'(' + CASE WHEN c.max_length = -1 THEN N'max' ELSE CAST(c.max_length AS nvarchar(10)) END + N')'
                WHEN ty.name IN (N'decimal', N'numeric') THEN ty.name + N'(' + CAST(c.precision AS nvarchar(10)) + N',' + CAST(c.scale AS nvarchar(10)) + N')'
                WHEN ty.name IN (N'datetime2', N'datetimeoffset', N'time') AND c.scale <> 7 THEN ty.name + N'(' + CAST(c.scale AS nvarchar(10)) + N')'
                ELSE ty.name
              END
            + CASE WHEN c.is_nullable = 1 THEN N' null' ELSE N' not null' END
        FROM sys.columns c
        JOIN sys.tables t ON t.object_id = c.object_id
        JOIN sys.types ty ON ty.user_type_id = c.user_type_id
        WHERE t.is_ms_shipped = 0 AND t.name <> N'__EFMigrationsHistory'
        UNION ALL
        SELECT CASE WHEN i.is_primary_key = 1 OR i.is_unique_constraint = 1 THEN N'key ' ELSE N'index ' END + t.name + N'.' + i.name + N'|'
            + CASE WHEN i.is_primary_key = 1 THEN N'primary' WHEN i.is_unique = 1 THEN N'unique' ELSE N'not unique' END
            + N' (' + (SELECT STRING_AGG(CAST(c.name AS nvarchar(max)), N',') WITHIN GROUP (ORDER BY ic.key_ordinal)
                       FROM sys.index_columns ic
                       JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                       WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0) + N')'
            + CASE WHEN i.has_filter = 1 THEN N' where ' + i.filter_definition COLLATE DATABASE_DEFAULT ELSE N'' END
        FROM sys.indexes i
        JOIN sys.tables t ON t.object_id = i.object_id
        WHERE t.is_ms_shipped = 0 AND t.name <> N'__EFMigrationsHistory' AND i.name IS NOT NULL
        UNION ALL
        SELECT N'foreign key ' + t.name + N'.' + fk.name + N'|('
            + (SELECT STRING_AGG(CAST(c.name AS nvarchar(max)), N',') WITHIN GROUP (ORDER BY fkc.constraint_column_id)
               FROM sys.foreign_key_columns fkc
               JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
               WHERE fkc.constraint_object_id = fk.object_id)
            + N') -> ' + rt.name + N' ('
            + (SELECT STRING_AGG(CAST(c.name AS nvarchar(max)), N',') WITHIN GROUP (ORDER BY fkc.constraint_column_id)
               FROM sys.foreign_key_columns fkc
               JOIN sys.columns c ON c.object_id = fkc.referenced_object_id AND c.column_id = fkc.referenced_column_id
               WHERE fkc.constraint_object_id = fk.object_id)
            + N') on delete ' + fk.delete_referential_action_desc COLLATE DATABASE_DEFAULT
        FROM sys.foreign_keys fk
        JOIN sys.tables t ON t.object_id = fk.parent_object_id
        JOIN sys.tables rt ON rt.object_id = fk.referenced_object_id
        WHERE t.is_ms_shipped = 0
        """;

    /// <summary>
    /// The SQL that makes <see cref="ApiUserName"/> from the identity of the same name and adds it to
    /// <c>db_datareader</c> and <c>db_datawriter</c> (ADR 0005). Only Azure SQL Database, engine
    /// edition 5, can make a user from Entra.
    /// </summary>
    public static readonly RoleScript Roles = new
    (
        "SELECT CASE WHEN CAST(SERVERPROPERTY('EngineEdition') AS int) = 5 THEN 1 ELSE 0 END AS [Value]",
        [
            $"IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'{ApiUserName}') CREATE USER [{ApiUserName}] FROM EXTERNAL PROVIDER;",
            $"IF IS_ROLEMEMBER(N'db_datareader', N'{ApiUserName}') = 0 ALTER ROLE [db_datareader] ADD MEMBER [{ApiUserName}];",
            $"IF IS_ROLEMEMBER(N'db_datawriter', N'{ApiUserName}') = 0 ALTER ROLE [db_datawriter] ADD MEMBER [{ApiUserName}];",
        ],
        "not Azure SQL Database"
    );



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
