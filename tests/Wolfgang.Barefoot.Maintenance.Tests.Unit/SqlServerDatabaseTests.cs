using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Wolfgang.Barefoot.Maintenance.Tests.Unit;

/// <summary>
/// The connection options of ADR 0001 §1. No test here opens a connection.
/// </summary>
public sealed class SqlServerDatabaseTests
{
    private const string ConnectionString = "Server=db.example.com;Database=barefoot;Connect Timeout=5";



    [Fact]
    public void Open_when_given_a_connection_string_gives_a_context_on_SQL_Server()
    {
        using var context = SqlServerDatabase.Open(ConnectionString);

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
    }



    [Fact]
    public void Open_when_given_a_connection_string_keeps_its_server_and_database_and_waits_90_seconds_to_connect()
    {
        using var context = SqlServerDatabase.Open(ConnectionString);

        var connection = new SqlConnectionStringBuilder(context.Database.GetConnectionString());

        Assert.Equal("db.example.com", connection.DataSource);
        Assert.Equal("barefoot", connection.InitialCatalog);
        Assert.Equal(90, connection.ConnectTimeout);
    }



    [Theory]
    [InlineData("")]
    [InlineData(";Encrypt=False")]
    [InlineData(";Encrypt=Optional")]
    public void Open_when_given_a_connection_string_always_encrypts_the_connection(string encryption)
    {
        using var context = SqlServerDatabase.Open(ConnectionString + encryption);

        var connection = new SqlConnectionStringBuilder(context.Database.GetConnectionString());

        Assert.Equal(SqlConnectionEncryptOption.Mandatory, connection.Encrypt);
    }



    [Fact]
    public void ScriptPreamble_when_read_turns_quoted_identifiers_on_in_a_batch_of_its_own()
    {
        Assert.Equal("SET QUOTED_IDENTIFIER ON;\nGO\n\n", SqlServerDatabase.ScriptPreamble);
    }



    [Fact]
    public void Roles_when_read_asks_whether_the_server_is_Azure_SQL_Database()
    {
        Assert.Equal
        (
            "SELECT CASE WHEN CAST(SERVERPROPERTY('EngineEdition') AS int) = 5 THEN 1 ELSE 0 END AS [Value]",
            SqlServerDatabase.Roles.CanGrantQuery
        );
        Assert.Equal("not Azure SQL Database", SqlServerDatabase.Roles.WhyNot);
    }



    [Fact]
    public void Roles_when_read_makes_the_user_barefoot_api_once_and_adds_it_to_the_reader_and_writer_roles_once()
    {
        Assert.Equal("barefoot-api", SqlServerDatabase.ApiUserName);
        Assert.Equal
        (
            [
                "IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'barefoot-api') CREATE USER [barefoot-api] FROM EXTERNAL PROVIDER;",
                "IF IS_ROLEMEMBER(N'db_datareader', N'barefoot-api') = 0 ALTER ROLE [db_datareader] ADD MEMBER [barefoot-api];",
                "IF IS_ROLEMEMBER(N'db_datawriter', N'barefoot-api') = 0 ALTER ROLE [db_datawriter] ADD MEMBER [barefoot-api];",
            ],
            SqlServerDatabase.Roles.Statements
        );
    }



    [Fact]
    public void Open_when_given_a_connection_string_gives_commands_60_seconds()
    {
        using var context = SqlServerDatabase.Open(ConnectionString);

        Assert.Equal(60, context.Database.GetCommandTimeout());
    }



    [Fact]
    public void Open_when_given_a_connection_string_retries_a_failed_command()
    {
        using var context = SqlServerDatabase.Open(ConnectionString);

        var strategy = Assert.IsType<SqlServerRetryingExecutionStrategy>(context.Database.CreateExecutionStrategy());

        Assert.True(strategy.RetriesOnFailure);
    }



    [Fact]
    public void Open_when_the_connection_string_is_not_one_throws_ArgumentException()
    {
        Assert.Throws<ArgumentException>(() => SqlServerDatabase.Open("NotAKeyword=1"));
    }



    [Fact]
    public void AdditionalTransientErrorNumbers_when_read_holds_4060_alone()
    {
        Assert.Equal([4060], SqlServerDatabase.AdditionalTransientErrorNumbers);
    }



    [Fact]
    public void Constants_when_read_are_the_defaults_of_ADR_0001()
    {
        Assert.Equal(90, SqlServerDatabase.ConnectTimeoutSeconds);
        Assert.Equal(60, SqlServerDatabase.CommandTimeoutSeconds);
        Assert.Equal(8, SqlServerDatabase.MaxRetryCount);
        Assert.Equal(20, SqlServerDatabase.MaxRetryDelaySeconds);
    }



    [Fact]
    public void Offline_when_called_gives_a_context_on_SQL_Server_that_names_no_database()
    {
        using var context = SqlServerDatabase.Offline();

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
        Assert.Null(context.Database.GetConnectionString());
    }
}
