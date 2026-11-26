using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Wolfgang.Barefoot.Data.Conventions;

namespace Wolfgang.Barefoot.Data.Tests.Unit.Conventions;

public sealed class ModelConventionsTests
{
    [Fact]
    public async Task Verify_when_given_the_model_of_BarefootDbContext_finds_nothing()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();

        Assert.Empty(ModelConventions.Verify(context.Model));
    }



    [Fact]
    public void Verify_when_the_model_follows_every_convention_finds_nothing()
    {
        Assert.Empty(Problems(_ => { }));
    }



    [Fact]
    public void Verify_when_the_model_is_null_throws_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ModelConventions.Verify(null!));
    }



    [Fact]
    public void Verify_when_a_table_is_named_by_hand_reports_the_name_it_should_have()
    {
        var problems = Problems(model => model.Entity<ParcelLabel>().ToTable("Parcels"));

        Assert.Equal(["ParcelLabel: the table is named 'Parcels', not 'parcel_label'."], problems);
    }



    [Fact]
    public void Verify_when_a_table_is_in_a_schema_reports_the_schema()
    {
        var problems = Problems(model => model.Entity<ParcelLabel>().ToTable("parcel_label", "post"));

        Assert.Equal(["ParcelLabel: the table is in the schema 'post', not the default one."], problems);
    }



    [Fact]
    public void Verify_when_a_column_is_named_by_hand_reports_the_name_it_should_have()
    {
        var problems = Problems
        (
            model => model.Entity<ParcelLabel>().Property(label => label.TrackingCode).HasColumnName("Code")
        );

        Assert.Equal(["ParcelLabel: the column of TrackingCode is named 'Code', not 'tracking_code'."], problems);
    }



    [Fact]
    public void Verify_when_a_primary_key_is_named_by_hand_reports_the_name_it_should_have()
    {
        var problems = Problems(model => model.Entity<ParcelLabel>().HasKey(label => label.Id).HasName("PK_Parcels"));

        Assert.Equal(["ParcelLabel: the key is named 'PK_Parcels', not 'pk_parcel_label'."], problems);
    }



    [Fact]
    public void Verify_when_an_alternate_key_is_named_by_hand_reports_the_name_it_should_have()
    {
        var problems = Problems
        (
            model => model.Entity<ParcelLabel>().HasAlternateKey(label => label.TrackingCode).HasName("AK_Code")
        );

        Assert.Equal(["ParcelLabel: the key is named 'AK_Code', not 'ak_parcel_label_tracking_code'."], problems);
    }



    [Fact]
    public void Verify_when_a_foreign_key_is_named_by_hand_reports_the_name_it_should_have()
    {
        var problems = Problems
        (
            model => model
                .Entity<ParcelLabel>()
                .HasOne<Sender>()
                .WithMany()
                .HasForeignKey(label => label.SenderId)
                .HasConstraintName("FK_Sender")
        );

        Assert.Equal(["ParcelLabel: the foreign key is named 'FK_Sender', not 'fk_parcel_label_sender_id'."], problems);
    }



    [Fact]
    public void Verify_when_an_index_is_named_by_hand_reports_the_name_it_should_have()
    {
        var problems = Problems
        (
            model => model
                .Entity<ParcelLabel>()
                .HasIndex(label => new { label.SenderId, label.WeightGrams })
                .HasDatabaseName("IX_Weight")
        );

        Assert.Equal
        (
            ["ParcelLabel: the index is named 'IX_Weight', not 'ux_parcel_label_sender_id_weight_grams'."],
            problems
        );
    }



    [Theory]
    [InlineData(58, 0)]
    [InlineData(59, 1)]
    public void Verify_when_a_name_is_longer_than_128_characters_reports_it(int secondColumnLength, int expectedCount)
    {
        // "ix_parcel_label_" is 16 characters and the first column 53, so with the "_" between the
        // columns a second column of 58 makes a name of exactly 128.
        var first = new string('a', 53);
        var second = new string('b', secondColumnLength);

        var problems = Problems
        (
            model =>
            {
                var parcel = model.Entity<ParcelLabel>();
                parcel.Property<int>(first);
                parcel.Property<int>(second);
                parcel.HasIndex(first, second);
                SchemaNames.Apply(model);
            }
        );

        Assert.Equal(expectedCount, problems.Count);
        Assert.All
        (
            problems,
            problem => Assert.Equal
            (
                $"ParcelLabel: the name 'ix_parcel_label_{first}_{second}' has 129 characters, more than 128.",
                problem
            )
        );
    }



    [Fact]
    public void Verify_when_a_primary_key_is_not_a_Guid_reports_it()
    {
        var problems = Problems
        (
            model =>
            {
                model.Entity<Crate>();
                SchemaNames.Apply(model);
            }
        );

        Assert.Equal(["Crate: the primary key is not one Guid column."], problems);
    }



    [Fact]
    public void Verify_when_a_primary_key_has_two_columns_reports_it()
    {
        var problems = Problems
        (
            model =>
            {
                model.Entity<Pallet>().HasKey(pallet => new { pallet.Id, pallet.WarehouseId });
                SchemaNames.Apply(model);
            }
        );

        Assert.Equal(["Pallet: the primary key is not one Guid column."], problems);
    }



    [Fact]
    public void Verify_when_a_table_has_no_primary_key_reports_it()
    {
        var problems = Problems
        (
            model =>
            {
                model.Entity<Pallet>().HasNoKey();
                SchemaNames.Apply(model);
            }
        );

        Assert.Equal(["Pallet: the primary key is not one Guid column."], problems);
    }



    [Theory]
    [InlineData(DeleteBehavior.Cascade)]
    [InlineData(DeleteBehavior.SetNull)]
    [InlineData(DeleteBehavior.NoAction)]
    public void Verify_when_a_foreign_key_does_not_restrict_its_delete_reports_what_it_does(DeleteBehavior behavior)
    {
        var problems = Problems
        (
            model => model
                .Entity<ParcelLabel>()
                .HasOne<Sender>()
                .WithMany()
                .HasForeignKey(label => label.SenderId)
                .OnDelete(behavior)
        );

        Assert.Equal([$"ParcelLabel: the foreign key on SenderId deletes with {behavior}, not Restrict."], problems);
    }



    [Fact]
    public void Verify_when_a_foreign_key_of_two_columns_does_not_restrict_its_delete_names_both_columns()
    {
        var problems = Problems
        (
            model =>
            {
                model
                    .Entity<Bin>()
                    .HasOne<Shelf>()
                    .WithMany()
                    .HasForeignKey(bin => new { bin.AisleId, bin.BayId })
                    .HasPrincipalKey(shelf => new { shelf.AisleId, shelf.BayId })
                    .OnDelete(DeleteBehavior.Cascade);
                SchemaNames.Apply(model);
            }
        );

        Assert.Equal(["Bin: the foreign key on AisleId, BayId deletes with Cascade, not Restrict."], problems);
    }



    [Fact]
    public void Verify_when_a_Version_is_not_a_concurrency_token_reports_it()
    {
        var problems = Problems
        (
            model => model.Entity<ParcelLabel>().Property(label => label.Version).IsConcurrencyToken(false)
        );

        Assert.Equal(["ParcelLabel: Version is not a long concurrency token."], problems);
    }



    [Fact]
    public void Verify_when_a_Version_is_not_a_long_reports_it()
    {
        var problems = Problems
        (
            model =>
            {
                model.Entity<Ledger>().Property(ledger => ledger.Version).IsConcurrencyToken();
                SchemaNames.Apply(model);
            }
        );

        Assert.Equal(["Ledger: Version is not a long concurrency token."], problems);
    }



    [Fact]
    public void Verify_when_a_column_is_a_DateTime_reports_it_whether_or_not_it_can_be_empty()
    {
        var problems = Problems
        (
            model =>
            {
                model.Entity<Stamp>();
                SchemaNames.Apply(model);
            }
        );

        Assert.Equal
        (
            [
                "Stamp: PrintedAt is a DateTime; an instant is a DateTimeOffset.",
                "Stamp: VoidedAt is a DateTime; an instant is a DateTimeOffset.",
            ],
            problems.Order(StringComparer.Ordinal)
        );
    }



    [Fact]
    public void Verify_when_an_instant_has_another_converter_reports_it()
    {
        var problems = Problems
        (
            model => model
                .Entity<ParcelLabel>()
                .Property(label => label.SentUtc)
                .HasConversion<DateTimeOffsetToBinaryConverter>()
        );

        Assert.Equal(["ParcelLabel: SentUtc is not stored as a UTC date and time to the millisecond."], problems);
    }



    [Fact]
    public void Verify_when_an_instant_that_can_be_empty_has_another_precision_reports_it()
    {
        var problems = Problems
        (
            model => model.Entity<ParcelLabel>().Property(label => label.ReturnedUtc).HasPrecision(7)
        );

        Assert.Equal(["ParcelLabel: ReturnedUtc is not stored as a UTC date and time to the millisecond."], problems);
    }



    [Fact]
    public void Verify_when_two_tables_break_a_convention_reports_both()
    {
        var problems = Problems
        (
            model =>
            {
                model.Entity<Crate>();
                model.Entity<Stamp>().Ignore(stamp => stamp.VoidedAt);
                SchemaNames.Apply(model);
            }
        );

        Assert.Equal
        (
            [
                "Crate: the primary key is not one Guid column.",
                "Stamp: PrintedAt is a DateTime; an instant is a DateTimeOffset.",
            ],
            problems.Order(StringComparer.Ordinal)
        );
    }



    private static IReadOnlyList<string> Problems(Action<ModelBuilder> change)
    {
        using var context = new SampleContext(change);

        return ModelConventions.Verify(context.Model);
    }



    [ExcludeFromCodeCoverage(Justification = "A row shape for the sample model. EF reads its fields, so no test calls its accessors.")]
    private sealed class Sender
    {
        public Guid Id { get; set; }
    }



    [ExcludeFromCodeCoverage(Justification = "A row shape for the sample model. EF reads its fields, so no test calls its accessors.")]
    private sealed class ParcelLabel
    {
        public Guid Id { get; set; }

        public Guid SenderId { get; set; }

        public string TrackingCode { get; set; } = string.Empty;

        public int WeightGrams { get; set; }

        public DateTimeOffset SentUtc { get; set; }

        public DateTimeOffset? ReturnedUtc { get; set; }

        public long Version { get; set; }
    }



    [ExcludeFromCodeCoverage(Justification = "A row shape for the sample model. EF reads its fields, so no test calls its accessors.")]
    private sealed class Crate
    {
        public int Id { get; set; }
    }



    [ExcludeFromCodeCoverage(Justification = "A row shape for the sample model. EF reads its fields, so no test calls its accessors.")]
    private sealed class Pallet
    {
        public Guid Id { get; set; }

        public Guid WarehouseId { get; set; }
    }



    [ExcludeFromCodeCoverage(Justification = "A row shape for the sample model. EF reads its fields, so no test calls its accessors.")]
    private sealed class Shelf
    {
        public Guid Id { get; set; }

        public Guid AisleId { get; set; }

        public Guid BayId { get; set; }
    }



    [ExcludeFromCodeCoverage(Justification = "A row shape for the sample model. EF reads its fields, so no test calls its accessors.")]
    private sealed class Bin
    {
        public Guid Id { get; set; }

        public Guid AisleId { get; set; }

        public Guid BayId { get; set; }
    }



    [ExcludeFromCodeCoverage(Justification = "A row shape for the sample model. EF reads its fields, so no test calls its accessors.")]
    private sealed class Ledger
    {
        public Guid Id { get; set; }

        public int Version { get; set; }
    }



    [ExcludeFromCodeCoverage(Justification = "A row shape for the sample model. EF reads its fields, so no test calls its accessors.")]
    private sealed class Stamp
    {
        public Guid Id { get; set; }

        public DateTime PrintedAt { get; set; }

        public DateTime? VoidedAt { get; set; }
    }



    /// <summary>
    /// A small model that follows every convention, then changed by one test to break one.
    /// </summary>
    private sealed class SampleContext(Action<ModelBuilder> change) : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseSqlite("DataSource=:memory:")
                .UseFreshModel();
        }



        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            BarefootModel.ConfigureConventions(configurationBuilder);
        }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Sender>();

            var parcel = modelBuilder.Entity<ParcelLabel>();

            parcel.Property(label => label.Version).IsConcurrencyToken();
            parcel.HasAlternateKey(label => label.TrackingCode);
            parcel.HasOne<Sender>().WithMany().HasForeignKey(label => label.SenderId).OnDelete(DeleteBehavior.Restrict);
            parcel.HasIndex(label => new { label.SenderId, label.WeightGrams }).IsUnique();

            SchemaNames.Apply(modelBuilder);

            change(modelBuilder);
        }
    }
}
