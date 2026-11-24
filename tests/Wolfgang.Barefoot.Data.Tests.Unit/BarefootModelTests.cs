using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Tests.Unit;

public sealed class BarefootModelTests
{
    [Fact]
    public void ConfigureConventions_when_the_builder_is_null_throws_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => BarefootModel.ConfigureConventions(null!));
    }



    [Fact]
    public void Configure_when_the_builder_is_null_throws_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => BarefootModel.Configure(null!));
    }



    [Fact]
    public async Task Configure_when_applied_gives_every_table_its_key()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();

        Assert.Equal
        (
            ["app_user: oid", "open_issue: id", "recipient: id", "setting: key"],
            context
                .Model
                .GetEntityTypes()
                .Select(entity => $"{entity.GetTableName()}: {string.Join(", ", entity.FindPrimaryKey()!.Properties.Select(property => property.GetColumnName()))}")
                .Order(StringComparer.Ordinal)
        );
    }



    [Fact]
    public async Task Configure_when_applied_never_generates_a_key_in_the_database()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        var keys = context
            .Model
            .GetEntityTypes()
            .SelectMany(entity => entity.FindPrimaryKey()!.Properties)
            .ToList();

        Assert.Equal(4, keys.Count);
        Assert.All(keys, property => Assert.Equal(ValueGenerated.Never, property.ValueGenerated));
    }



    [Fact]
    public async Task Configure_when_applied_gives_every_text_column_its_length()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();

        Assert.Equal
        (
            [
                "app_user.display_name: 200",
                "app_user.email: 320",
                "app_user.time_zone_id: 64",
                "open_issue.resolves_when: 500",
                "open_issue.subject: 200",
                "recipient.address: 320",
                "recipient.display_name: 200",
                "setting.key: 128",
                "setting.value: 4000",
            ],
            context
                .Model
                .GetEntityTypes()
                .SelectMany(entity => entity.GetProperties())
                .Where(property => property.ClrType == typeof(string))
                .Select(property => $"{property.DeclaringType.GetTableName()}.{property.GetColumnName()}: {property.GetMaxLength()}")
                .Order(StringComparer.Ordinal)
        );
    }



    [Fact]
    public async Task Configure_when_applied_points_each_foreign_key_at_the_user_it_names()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();

        Assert.Equal
        (
            ["open_issue.user_id -> app_user", "recipient.owner_id -> app_user"],
            context
                .Model
                .GetEntityTypes()
                .SelectMany(entity => entity.GetForeignKeys())
                .Select(foreignKey => $"{foreignKey.DeclaringEntityType.GetTableName()}.{foreignKey.Properties.Single().GetColumnName()} -> {foreignKey.PrincipalEntityType.GetTableName()}")
                .Order(StringComparer.Ordinal)
        );
        Assert.Equal(typeof(AppUser), context.Model.FindEntityType(typeof(Recipient))!.GetForeignKeys().Single().PrincipalEntityType.ClrType);
    }
}
