using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Wolfgang.Barefoot.Maintenance.Tests.Unit;

public sealed class DataLossScreenTests
{
    private const string Migration = "20260201000000_AddRuns";



    [Fact]
    public void Find_when_a_table_is_dropped_names_the_table()
    {
        var steps = DataLossScreen.Find(Migration, [new DropTableOperation { Name = "run" }]);

        Assert.Equal([new DataLossStep(Migration, "drops table run")], steps);
    }



    [Fact]
    public void Find_when_a_column_is_dropped_names_the_table_and_the_column()
    {
        var steps = DataLossScreen.Find(Migration, [new DropColumnOperation { Table = "run", Name = "status" }]);

        Assert.Equal([new DataLossStep(Migration, "drops column run.status")], steps);
    }



    [Fact]
    public void Find_when_raw_SQL_is_run_counts_it_without_reading_it()
    {
        var steps = DataLossScreen.Find(Migration, [new SqlOperation { Sql = "SELECT 1" }]);

        Assert.Equal([new DataLossStep(Migration, "runs raw SQL")], steps);
    }



    [Theory]
    [InlineData(200, 100)]
    [InlineData(null, 100)]
    public void Find_when_a_column_gets_a_shorter_or_a_first_maximum_length_counts_it(int? before, int after)
    {
        var steps = DataLossScreen.Find(Migration, [Alter(column => column.MaxLength = after, old => old.MaxLength = before)]);

        Assert.Equal([new DataLossStep(Migration, "narrows column run.status")], steps);
    }



    [Theory]
    [InlineData(100, 200)]
    [InlineData(100, 100)]
    [InlineData(100, null)]
    public void Find_when_a_column_keeps_or_gains_length_does_not_count_it(int before, int? after)
    {
        var steps = DataLossScreen.Find
        (
            Migration,
            [
                Alter
                (
                    column =>
                    {
                        column.MaxLength = after;
                        column.ColumnType = "nvarchar(new)";
                    },
                    old =>
                    {
                        old.MaxLength = before;
                        old.ColumnType = "nvarchar(old)";
                    }
                ),
            ]
        );

        Assert.Empty(steps);
    }



    [Theory]
    [InlineData(18, 10)]
    [InlineData(null, 10)]
    public void Find_when_a_column_loses_precision_counts_it(int? before, int after)
    {
        var steps = DataLossScreen.Find(Migration, [Alter(column => column.Precision = after, old => old.Precision = before)]);

        Assert.Single(steps);
    }



    [Theory]
    [InlineData(4, 2)]
    [InlineData(null, 2)]
    public void Find_when_a_column_loses_scale_counts_it(int? before, int after)
    {
        var steps = DataLossScreen.Find
        (
            Migration,
            [
                Alter
                (
                    column =>
                    {
                        column.Precision = 18;
                        column.Scale = after;
                    },
                    old =>
                    {
                        old.Precision = 18;
                        old.Scale = before;
                    }
                ),
            ]
        );

        Assert.Single(steps);
    }



    [Fact]
    public void Find_when_a_column_keeps_precision_and_gains_scale_does_not_count_it()
    {
        var steps = DataLossScreen.Find
        (
            Migration,
            [
                Alter
                (
                    column =>
                    {
                        column.Precision = 18;
                        column.Scale = 4;
                        column.ColumnType = "decimal(18,4)";
                    },
                    old =>
                    {
                        old.Precision = 18;
                        old.Scale = 2;
                        old.ColumnType = "decimal(18,2)";
                    }
                ),
            ]
        );

        Assert.Empty(steps);
    }



    [Fact]
    public void Find_when_a_column_gets_another_CLR_type_counts_it()
    {
        var steps = DataLossScreen.Find(Migration, [Alter(column => column.ClrType = typeof(int), old => old.ClrType = typeof(long))]);

        Assert.Single(steps);
    }



    [Fact]
    public void Find_when_a_column_gets_another_column_type_and_no_size_says_which_way_counts_it()
    {
        var steps = DataLossScreen.Find(Migration, [Alter(column => column.ColumnType = "date", old => old.ColumnType = "datetime2")]);

        Assert.Single(steps);
    }



    [Theory]
    [InlineData("datetime2", "datetime2")]
    [InlineData("datetime2", "DATETIME2")]
    [InlineData(null, null)]
    public void Find_when_a_column_keeps_its_type_and_only_something_else_changes_does_not_count_it(string? before, string? after)
    {
        var steps = DataLossScreen.Find
        (
            Migration,
            [
                Alter
                (
                    column =>
                    {
                        column.ColumnType = after;
                        column.IsNullable = false;
                    },
                    old =>
                    {
                        old.ColumnType = before;
                        old.IsNullable = true;
                    }
                ),
            ]
        );

        Assert.Empty(steps);
    }



    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Find_when_only_one_side_has_a_precision_and_the_type_text_differs_goes_by_the_precision(bool precisionIsNew)
    {
        var steps = DataLossScreen.Find
        (
            Migration,
            [
                Alter
                (
                    column =>
                    {
                        column.ColumnType = "decimal(new)";
                        column.Precision = precisionIsNew ? 18 : null;
                    },
                    old =>
                    {
                        old.ColumnType = "decimal(old)";
                        old.Precision = precisionIsNew ? null : 18;
                    }
                ),
            ]
        );

        Assert.Equal(precisionIsNew ? 1 : 0, steps.Count);
    }



    [Fact]
    public void Find_when_operations_add_or_only_drop_what_holds_no_rows_finds_nothing()
    {
        var steps = DataLossScreen.Find
        (
            Migration,
            [
                new CreateTableOperation { Name = "run" },
                new AddColumnOperation { Table = "run", Name = "status", ClrType = typeof(int) },
                new DropIndexOperation { Table = "run", Name = "ix_run_status" },
                new DropForeignKeyOperation { Table = "run", Name = "fk_run_app_user" },
            ]
        );

        Assert.Empty(steps);
    }



    [Fact]
    public void Find_when_several_steps_lose_data_lists_them_in_the_order_given()
    {
        var steps = DataLossScreen.Find
        (
            Migration,
            [
                new DropColumnOperation { Table = "run", Name = "status" },
                new CreateTableOperation { Name = "other" },
                new SqlOperation { Sql = "SELECT 1" },
                new DropTableOperation { Name = "run" },
            ]
        );

        Assert.Equal(["drops column run.status", "runs raw SQL", "drops table run"], steps.Select(step => step.Description));
        Assert.All(steps, step => Assert.Equal(Migration, step.MigrationId));
    }



    [Fact]
    public void Find_when_given_no_operations_finds_nothing()
    {
        Assert.Empty(DataLossScreen.Find(Migration, []));
    }



    [Fact]
    public void Find_when_migrationId_is_null_throws_ArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => DataLossScreen.Find(null!, []));

        Assert.Equal("migrationId", exception.ParamName);
    }



    [Fact]
    public void Find_when_operations_is_null_throws_ArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => DataLossScreen.Find(Migration, null!));

        Assert.Equal("operations", exception.ParamName);
    }



    private static AlterColumnOperation Alter(Action<AlterColumnOperation> change, Action<ColumnOperation> before)
    {
        var column = new AlterColumnOperation
        {
            Table = "run",
            Name = "status",
            ClrType = typeof(string),
            OldColumn = new AddColumnOperation { ClrType = typeof(string) },
        };

        before(column.OldColumn);
        change(column);

        return column;
    }
}
