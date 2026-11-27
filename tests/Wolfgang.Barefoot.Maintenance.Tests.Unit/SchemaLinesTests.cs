using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Wolfgang.Barefoot.Maintenance.Tests.Unit;

/// <summary>
/// The lines that describe the model, here on SQLite, whose column types are <c>TEXT</c> and
/// <c>INTEGER</c> where SQL Server's are <c>uniqueidentifier</c> and <c>bigint</c>.
/// </summary>
public sealed class SchemaLinesTests
{
    [Fact]
    public void Expected_when_given_a_context_describes_tables_columns_keys_indexes_and_foreign_keys()
    {
        using var context = TestSchema.Offline();

        var lines = SchemaLines.Expected(context);

        Assert.Contains("table app_user|", lines);
        Assert.Contains("column app_user.oid|TEXT not null", lines);
        Assert.Contains("column app_user.version|INTEGER not null", lines);
        Assert.Contains("key app_user.pk_app_user|primary (oid)", lines);
        Assert.Contains("index airport.ux_airport_code|unique (code)", lines);
        Assert.Contains("index run.ux_run_status|unique (status) where status=0", lines);
        Assert.Contains
        (
            "foreign key recipient.fk_recipient_owner_id|(owner_id) -> app_user (oid) on delete NO_ACTION",
            lines
        );
    }



    [Fact]
    public void Expected_when_given_a_context_describes_each_item_once()
    {
        using var context = TestSchema.Offline();

        var items = SchemaLines
            .Expected(context)
            .Select(line => line[..line.IndexOf('|', StringComparison.Ordinal)])
            .ToList();

        Assert.Equal(items.Count, items.Distinct(StringComparer.Ordinal).Count());
        Assert.DoesNotContain(items, item => item.Contains("__EFMigrationsHistory", StringComparison.Ordinal));
    }



    [Fact]
    public void Expected_when_a_column_may_be_empty_says_null()
    {
        using var context = TestSchema.Offline();

        var nullable = SchemaLines
            .Expected(context)
            .Where(line => line.StartsWith("column ", StringComparison.Ordinal) && !line.EndsWith(" not null", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(nullable);
        Assert.All(nullable, line => Assert.EndsWith(" null", line, StringComparison.Ordinal));
    }



    [Fact]
    public void Expected_when_the_model_has_a_unique_key_a_plain_index_and_a_cascade_names_each()
    {
        using var context = new SampleContext();

        var lines = SchemaLines.Expected(context);

        Assert.Equal
        (
            [
                "table Child|",
                "column Child.Id|INTEGER not null",
                "column Child.Note|TEXT null",
                "column Child.ParentCode|TEXT not null",
                "key Child.PK_Child|primary (Id)",
                "index Child.IX_Child_ParentCode|not unique (ParentCode)",
                "foreign key Child.FK_Child_Parent_ParentCode|(ParentCode) -> Parent (Code) on delete CASCADE",
                "table Parent|",
                "column Parent.Id|INTEGER not null",
                "column Parent.Code|TEXT not null",
                "key Parent.AK_Parent_Code|unique (Code)",
                "key Parent.PK_Parent|primary (Id)",
            ],
            lines
        );
    }



    [Fact]
    public void Expected_when_context_is_null_throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => SchemaLines.Expected(null!));

        Assert.Equal("context", exception.ParamName);
    }



    [Theory]
    [InlineData("table run|", "table run|")]
    [InlineData("index run.ix|unique (status)", "index run.ix|unique (status)")]
    [InlineData("index run.ix|unique (status) where status = 0", "index run.ix|unique (status) where status=0")]
    [InlineData("index run.ix|unique (status) where ([Status]=(0))", "index run.ix|unique (status) where status=0")]
    [InlineData("index Run.IX|Unique (Status) where [a] IS NOT NULL", "index Run.IX|Unique (Status) where aisnotnull")]
    [InlineData(" where [A] = 1", " where a=1")]
    public void Normalized_when_given_a_line_spells_only_the_filter_one_way(string line, string expected)
    {
        Assert.Equal(expected, SchemaLines.Normalized(line));
    }



    [Fact]
    public void Normalized_when_line_is_null_throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => SchemaLines.Normalized(null!));

        Assert.Equal("line", exception.ParamName);
    }



    [Theory]
    [InlineData(ReferentialAction.Restrict, "NO_ACTION")]
    [InlineData(ReferentialAction.NoAction, "NO_ACTION")]
    [InlineData(ReferentialAction.Cascade, "CASCADE")]
    [InlineData(ReferentialAction.SetNull, "SET_NULL")]
    [InlineData(ReferentialAction.SetDefault, "SET_DEFAULT")]
    public void OnDelete_when_given_a_rule_names_it_as_the_catalog_does(ReferentialAction action, string expected)
    {
        Assert.Equal(expected, SchemaLines.OnDelete(action));
    }



    /// <summary>
    /// A model with what Barefoot's own model does not have: a unique key that is not the primary
    /// key, an index that is not unique, and a foreign key that cascades.
    /// </summary>
    private sealed class SampleContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=:memory:");
        }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Parent>(parent => parent.HasAlternateKey(row => row.Code));

            modelBuilder.Entity<Child>
            (
                child => child
                    .HasOne<Parent>()
                    .WithMany()
                    .HasForeignKey(row => row.ParentCode)
                    .HasPrincipalKey(row => row.Code)
                    .OnDelete(DeleteBehavior.Cascade)
            );
        }
    }



    [ExcludeFromCodeCoverage(Justification = "A row type for a sample model; no test reads its properties.")]
    private sealed class Parent
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;
    }



    [ExcludeFromCodeCoverage(Justification = "A row type for a sample model; no test reads its properties.")]
    private sealed class Child
    {
        public int Id { get; set; }

        public string? Note { get; set; }

        public string ParentCode { get; set; } = string.Empty;
    }
}
