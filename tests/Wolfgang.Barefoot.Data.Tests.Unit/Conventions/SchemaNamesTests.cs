using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Wolfgang.Barefoot.Data.Conventions;

namespace Wolfgang.Barefoot.Data.Tests.Unit.Conventions;

public sealed class SchemaNamesTests
{
    [Fact]
    public void Apply_when_the_model_has_tables_names_each_table_and_column_in_snake_case()
    {
        var parcel = Model().FindEntityType(typeof(ParcelLabel))!;

        Assert.Equal("parcel_label", parcel.GetTableName());
        Assert.Equal
        (
            ["id", "sender_id", "tracking_code", "weight_grams"],
            parcel.GetProperties().Select(property => property.GetColumnName()).Order(StringComparer.Ordinal)
        );
    }



    [Fact]
    public void Apply_when_a_table_has_a_primary_and_an_alternate_key_prefixes_them_pk_and_ak()
    {
        var parcel = Model().FindEntityType(typeof(ParcelLabel))!;

        Assert.Equal
        (
            ["ak_parcel_label_tracking_code", "pk_parcel_label"],
            parcel.GetKeys().Select(key => key.GetName()).Order(StringComparer.Ordinal)
        );
    }



    [Fact]
    public void Apply_when_a_table_has_a_foreign_key_names_it_fk_table_columns()
    {
        var parcel = Model().FindEntityType(typeof(ParcelLabel))!;

        Assert.Equal
        (
            ["fk_parcel_label_sender_id"],
            parcel.GetForeignKeys().Select(foreignKey => foreignKey.GetConstraintName())
        );
    }



    [Fact]
    public void Apply_when_a_table_has_indexes_prefixes_a_unique_one_ux_and_any_other_ix()
    {
        var parcel = Model().FindEntityType(typeof(ParcelLabel))!;

        Assert.Equal
        (
            ["ix_parcel_label_sender_id", "ux_parcel_label_sender_id_weight_grams"],
            parcel.GetIndexes().Select(index => index.GetDatabaseName()).Order(StringComparer.Ordinal)
        );
    }



    [Fact]
    public void Apply_when_the_model_builder_is_null_throws_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SchemaNames.Apply(null!));
    }



    private static IModel Model()
    {
        using var context = new SampleContext();

        return context.Model;
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
    }



    private sealed class SampleContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseSqlite("DataSource=:memory:")
                .UseFreshModel();
        }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Sender>();

            var parcel = modelBuilder.Entity<ParcelLabel>();

            parcel.HasAlternateKey(label => label.TrackingCode);
            parcel.HasOne<Sender>().WithMany().HasForeignKey(label => label.SenderId);
            parcel.HasIndex(label => label.SenderId);
            parcel.HasIndex(label => new { label.SenderId, label.WeightGrams }).IsUnique();

            SchemaNames.Apply(modelBuilder);
        }
    }
}
