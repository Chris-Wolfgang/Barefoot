using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Wolfgang.Barefoot.Contracts.Runs;
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
            [
                "airport: id",
                "app_user: oid",
                "digest_delivery: id",
                "flight_direction: id",
                "flight_leg: id",
                "flight_option: id",
                "flight_set: id",
                "matrix_run: id",
                "open_issue: id",
                "recipient: id",
                "resort: id",
                "resort_span: id",
                "room_category: id",
                "room_result: id",
                "room_result_feature: id",
                "run: id",
                "search_departure_airport: id",
                "search_item: id",
                "search_item_flight_set: id",
                "search_matrix: id",
                "search_recipient: id",
                "search_request: id",
                "search_resort: id",
                "search_room_filter: id",
                "setting: key",
                "vocabulary_value: id",
            ],
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

        Assert.Equal(26, keys.Count);
        Assert.All(keys, property => Assert.Equal(ValueGenerated.Never, property.ValueGenerated));
    }



    [Fact]
    public async Task Configure_when_applied_gives_every_text_column_its_length_and_only_a_digests_bodies_none()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();

        Assert.Equal
        (
            [
                "airport.city: 100",
                "airport.code: 3",
                "airport.country: 100",
                "airport.name: 200",
                "airport.state: 100",
                "airport.time_zone_id: 64",
                "app_user.display_name: 200",
                "app_user.email: 320",
                "app_user.time_zone_id: 64",
                "digest_delivery.html_body: ",
                "digest_delivery.last_error: 2000",
                "digest_delivery.recipient_address: 320",
                "digest_delivery.subject: 200",
                "digest_delivery.text_body: ",
                "flight_leg.arrival_airport_code: 3",
                "flight_leg.cabin_name: 200",
                "flight_leg.carrier: 200",
                "flight_leg.departure_airport_code: 3",
                "flight_leg.fare_code: 16",
                "flight_leg.flight_number: 16",
                "flight_leg.operating_carrier: 200",
                "flight_set.last_error: 2000",
                "flight_set.leased_by: 200",
                "flight_set.result_sha256: 64",
                "matrix_run.skip_reason: 500",
                "open_issue.resolves_when: 500",
                "open_issue.subject: 200",
                "recipient.address: 320",
                "recipient.display_name: 200",
                "resort.code: 32",
                "resort.island: 100",
                "resort.name: 200",
                "resort.short_name: 200",
                "room_category.code: 32",
                "room_category.name: 200",
                "room_result.promotion: 500",
                "run.abort_reason: 500",
                "run.trigger: 200",
                "run.worker_build_version: 64",
                "run.worker_name: 200",
                "search_matrix.name: 200",
                "search_matrix.normalized_name: 200",
                "search_request.last_error: 2000",
                "search_request.leased_by: 200",
                "search_request.result_sha256: 64",
                "setting.key: 128",
                "setting.value: 4000",
                "vocabulary_value.value: 200",
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
    public async Task Configure_when_applied_points_each_foreign_key_at_the_table_it_names()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();

        Assert.Equal
        (
            [
                "digest_delivery.matrix_run_id -> matrix_run",
                "digest_delivery.recipient_user_id -> app_user",
                "flight_direction.flight_option_id -> flight_option",
                "flight_leg.flight_direction_id -> flight_direction",
                "flight_option.flight_set_id -> flight_set",
                "flight_set.gateway_airport_id -> airport",
                "flight_set.origin_airport_id -> airport",
                "flight_set.run_id -> run",
                "matrix_run.run_id -> run",
                "matrix_run.search_matrix_id -> search_matrix",
                "open_issue.user_id -> app_user",
                "recipient.owner_id -> app_user",
                "resort.gateway_airport_id -> airport",
                "resort_span.resort_id -> resort",
                "room_category.resort_id -> resort",
                "room_result.room_category_id -> room_category",
                "room_result.room_level_id -> vocabulary_value",
                "room_result.room_type_id -> vocabulary_value",
                "room_result.room_view_id -> vocabulary_value",
                "room_result.search_request_id -> search_request",
                "room_result_feature.room_result_id -> room_result",
                "room_result_feature.vocabulary_value_id -> vocabulary_value",
                "search_departure_airport.airport_id -> airport",
                "search_departure_airport.search_matrix_id -> search_matrix",
                "search_item.matrix_run_id -> matrix_run",
                "search_item.search_request_id -> search_request",
                "search_item_flight_set.flight_set_id -> flight_set",
                "search_item_flight_set.search_item_id -> search_item",
                "search_matrix.owner_id -> app_user",
                "search_recipient.recipient_id -> recipient",
                "search_recipient.search_matrix_id -> search_matrix",
                "search_request.resort_id -> resort",
                "search_request.run_id -> run",
                "search_resort.resort_id -> resort",
                "search_resort.search_matrix_id -> search_matrix",
                "search_room_filter.search_matrix_id -> search_matrix",
                "search_room_filter.vocabulary_value_id -> vocabulary_value",
                "setting.updated_by_user_id -> app_user",
            ],
            context
                .Model
                .GetEntityTypes()
                .SelectMany(entity => entity.GetForeignKeys())
                .Select(foreignKey => $"{foreignKey.DeclaringEntityType.GetTableName()}.{foreignKey.Properties.Single().GetColumnName()} -> {foreignKey.PrincipalEntityType.GetTableName()}")
                .Order(StringComparer.Ordinal)
        );
        Assert.Equal(typeof(AppUser), context.Model.FindEntityType(typeof(Recipient))!.GetForeignKeys().Single().PrincipalEntityType.ClrType);
        Assert.Equal(typeof(Airport), context.Model.FindEntityType(typeof(Resort))!.GetForeignKeys().Single().PrincipalEntityType.ClrType);
        Assert.Equal(typeof(Resort), context.Model.FindEntityType(typeof(ResortSpan))!.GetForeignKeys().Single().PrincipalEntityType.ClrType);
        Assert.Equal(typeof(Resort), context.Model.FindEntityType(typeof(RoomCategory))!.GetForeignKeys().Single().PrincipalEntityType.ClrType);
    }



    [Fact]
    public async Task Configure_when_applied_makes_each_unique_index_unique_and_no_other()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        var indexes = context.Model.GetEntityTypes().SelectMany(entity => entity.GetIndexes()).ToList();

        Assert.Equal(43, indexes.Count);
        Assert.All
        (
            indexes,
            index => Assert.Equal(index.GetDatabaseName()!.StartsWith("ux_", StringComparison.Ordinal), index.IsUnique)
        );
        Assert.Equal(19, indexes.Count(index => index.IsUnique));
    }



    [Fact]
    public async Task Configure_when_applied_filters_only_the_run_lock_to_runs_in_progress()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();

        Assert.Equal
        (
            ["ux_run_status: status = 0"],
            context
                .Model
                .GetEntityTypes()
                .SelectMany(entity => entity.GetIndexes())
                .Where(index => index.GetFilter() is not null)
                .Select(index => $"{index.GetDatabaseName()}: {index.GetFilter()}")
        );
        Assert.Equal(0, (int)RunStatus.InProgress);
    }



    [Fact]
    public async Task Configure_when_applied_gives_no_key_or_index_a_name_longer_than_sql_server_allows()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        var entities = context.Model.GetEntityTypes().ToList();
        var names = entities
            .SelectMany(entity => entity.GetIndexes())
            .Select(index => index.GetDatabaseName()!)
            .Concat(entities.SelectMany(entity => entity.GetForeignKeys()).Select(key => key.GetConstraintName()!))
            .Concat(entities.SelectMany(entity => entity.GetKeys()).Select(key => key.GetName()!))
            .ToList();

        Assert.Equal(107, names.Count);
        Assert.All(names, name => Assert.InRange(name.Length, 1, 128));
    }
}
