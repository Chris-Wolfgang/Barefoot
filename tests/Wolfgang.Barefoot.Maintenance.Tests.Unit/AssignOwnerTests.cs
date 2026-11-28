using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Wolfgang.Barefoot.Contracts.Issues;
using Wolfgang.Barefoot.Contracts.Runs;
using Wolfgang.Barefoot.Contracts.Searches;
using Wolfgang.Barefoot.Core.Entities;
using Wolfgang.Barefoot.Core.Hosting;
using Wolfgang.Barefoot.Data;

namespace Wolfgang.Barefoot.Maintenance.Tests.Unit;

/// <summary>
/// <c>bf-maint assign-owner</c>, run in-process with captured streams on SQLite (docs/MIGRATE.md,
/// ADR 0016). The database holds every table of the model and the seeded "Unassigned" user.
/// </summary>
public sealed class AssignOwnerTests
{
    private static readonly Guid Target = new("11111111-1111-1111-1111-111111111111");

    private static readonly Guid Other = new("22222222-2222-2222-2222-222222222222");

    private static readonly DateTimeOffset Instant = new(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);

    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .AddInMemoryCollection([new KeyValuePair<string, string?>("ConnectionStrings:Barefoot", "Server=db.example.com;Database=barefoot")])
        .Build();



    [Fact]
    public async Task RunAsync_when_the_unassigned_user_owns_rows_moves_them_all_and_counts_each_kind()
    {
        await using var schema = await SeededAsync();
        var searchId = await AddSearchAsync(schema, AppUser.UnassignedOid, "Winter week");
        await AddSearchAsync(schema, AppUser.UnassignedOid, "Spring break");
        await AddRecipientAsync(schema, AppUser.UnassignedOid, "pat@example.com");
        await AddIssueAsync(schema, AppUser.UnassignedOid);
        await AddDeliveryAsync(schema, searchId, AppUser.UnassignedOid);
        await AddDeliveryAsync(schema, searchId, AppUser.UnassignedOid);
        await AddDeliveryAsync(schema, searchId, AppUser.UnassignedOid);

        var run = await RunAsync(schema, Target.ToString());

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal($"searches: 2\nrecipients: 1\nissues: 1\ndeliveries: 3{Environment.NewLine}", run.Output);
        Assert.Empty(run.Error);

        await using var context = schema.NewContext();
        Assert.Equal(2, await context.SearchMatrices.CountAsync(search => search.OwnerId == Target));
        Assert.Equal(1, await context.Recipients.CountAsync(recipient => recipient.OwnerId == Target));
        Assert.Equal(1, await context.OpenIssues.CountAsync(issue => issue.UserId == Target));
        Assert.Equal(3, await context.DigestDeliveries.CountAsync(delivery => delivery.RecipientUserId == Target));
        Assert.Equal(0, await OwnedByAsync(context, AppUser.UnassignedOid));
    }



    [Fact]
    public async Task RunAsync_when_a_search_and_a_recipient_move_gives_each_its_next_version()
    {
        await using var schema = await SeededAsync();
        await AddSearchAsync(schema, AppUser.UnassignedOid, "Winter week");
        await AddRecipientAsync(schema, AppUser.UnassignedOid, "pat@example.com");

        await RunAsync(schema, Target.ToString());

        await using var context = schema.NewContext();
        Assert.Equal(2, (await context.SearchMatrices.SingleAsync()).Version);
        Assert.Equal(2, (await context.Recipients.SingleAsync()).Version);
    }



    [Fact]
    public async Task RunAsync_when_other_users_own_rows_leaves_those_alone()
    {
        await using var schema = await SeededAsync();
        var searchId = await AddSearchAsync(schema, Other, "Winter week");
        await AddRecipientAsync(schema, Other, "pat@example.com");
        await AddIssueAsync(schema, Other);
        await AddDeliveryAsync(schema, searchId, Other);
        await AddDeliveryAsync(schema, searchId, recipient: null);

        var run = await RunAsync(schema, Target.ToString());

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal($"searches: 0\nrecipients: 0\nissues: 0\ndeliveries: 0{Environment.NewLine}", run.Output);

        await using var context = schema.NewContext();
        Assert.Equal(4, await OwnedByAsync(context, Other));
        Assert.Equal(0, await OwnedByAsync(context, Target));
        Assert.Equal(1, await context.DigestDeliveries.CountAsync(delivery => delivery.RecipientUserId == null));
        Assert.Equal(1, (await context.SearchMatrices.SingleAsync()).Version);
    }



    [Fact]
    public async Task RunAsync_when_run_a_second_time_moves_nothing_and_exits_Ok()
    {
        await using var schema = await SeededAsync();
        await AddSearchAsync(schema, AppUser.UnassignedOid, "Winter week");
        await RunAsync(schema, Target.ToString());

        var run = await RunAsync(schema, Target.ToString());

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal($"searches: 0\nrecipients: 0\nissues: 0\ndeliveries: 0{Environment.NewLine}", run.Output);

        await using var context = schema.NewContext();
        Assert.Equal(2, (await context.SearchMatrices.SingleAsync()).Version);
    }



    [Fact]
    public async Task RunAsync_when_the_target_has_a_search_of_the_same_name_moves_nothing_and_exits_Refused()
    {
        await using var schema = await SeededAsync();
        await AddSearchAsync(schema, AppUser.UnassignedOid, "Winter week");
        await AddSearchAsync(schema, AppUser.UnassignedOid, "Spring break");
        await AddRecipientAsync(schema, AppUser.UnassignedOid, "pat@example.com");
        await AddSearchAsync(schema, Target, "WINTER WEEK");

        var run = await RunAsync(schema, Target.ToString());

        Assert.Equal(ExitCode.Refused, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Equal
        (
            $"User {Target} already has what would be moved:\n  search 'Winter week'\nNothing was moved. Rename or delete one of each pair, then run it again.{Environment.NewLine}",
            run.Error
        );

        await using var context = schema.NewContext();
        Assert.Equal(3, await OwnedByAsync(context, AppUser.UnassignedOid));
        Assert.Equal(1, await OwnedByAsync(context, Target));
    }



    [Fact]
    public async Task RunAsync_when_the_target_has_a_recipient_of_the_same_address_in_another_case_moves_nothing_and_exits_Refused()
    {
        await using var schema = await SeededAsync();
        await AddRecipientAsync(schema, AppUser.UnassignedOid, "pat@example.com");
        await AddRecipientAsync(schema, Target, "Pat@Example.com");

        var run = await RunAsync(schema, Target.ToString());

        Assert.Equal(ExitCode.Refused, run.ExitCode);
        Assert.Equal
        (
            $"User {Target} already has what would be moved:\n  recipient pat@example.com\nNothing was moved. Rename or delete one of each pair, then run it again.{Environment.NewLine}",
            run.Error
        );

        await using var context = schema.NewContext();
        Assert.Equal(1, await OwnedByAsync(context, AppUser.UnassignedOid));
    }



    [Fact]
    public async Task RunAsync_when_several_rows_collide_lists_searches_first_and_each_kind_in_alphabetical_order()
    {
        await using var schema = await SeededAsync();
        foreach (var owner in new[] { AppUser.UnassignedOid, Target })
        {
            await AddSearchAsync(schema, owner, "Winter week");
            await AddSearchAsync(schema, owner, "Autumn days");
            await AddRecipientAsync(schema, owner, "pat@example.com");
            await AddRecipientAsync(schema, owner, "lee@example.com");
        }

        var run = await RunAsync(schema, Target.ToString());

        Assert.Equal(ExitCode.Refused, run.ExitCode);
        Assert.Contains
        (
            ":\n  search 'Autumn days'\n  search 'Winter week'\n  recipient lee@example.com\n  recipient pat@example.com\nNothing was moved.",
            run.Error,
            StringComparison.Ordinal
        );
    }



    [Fact]
    public async Task RunAsync_when_another_user_has_the_same_name_and_address_moves_the_rows_all_the_same()
    {
        await using var schema = await SeededAsync();
        await AddSearchAsync(schema, AppUser.UnassignedOid, "Winter week");
        await AddRecipientAsync(schema, AppUser.UnassignedOid, "pat@example.com");
        await AddSearchAsync(schema, Other, "Winter week");
        await AddRecipientAsync(schema, Other, "pat@example.com");

        var run = await RunAsync(schema, Target.ToString());

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Equal($"searches: 1\nrecipients: 1\nissues: 0\ndeliveries: 0{Environment.NewLine}", run.Output);
    }



    [Fact]
    public async Task RunAsync_when_the_user_has_never_signed_in_moves_nothing_and_exits_Refused()
    {
        await using var schema = await SeededAsync();
        await AddSearchAsync(schema, AppUser.UnassignedOid, "Winter week");
        var stranger = new Guid("44444444-4444-4444-4444-444444444444");

        var run = await RunAsync(schema, stranger.ToString());

        Assert.Equal(ExitCode.Refused, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Equal
        (
            $"No user {stranger} has signed in. Nothing was moved. The user has to sign in once first.{Environment.NewLine}",
            run.Error
        );

        await using var context = schema.NewContext();
        Assert.Equal(1, await OwnedByAsync(context, AppUser.UnassignedOid));
    }



    [Fact]
    public async Task RunAsync_when_the_user_named_is_the_unassigned_user_exits_Refused()
    {
        await using var schema = await SeededAsync();
        await AddSearchAsync(schema, AppUser.UnassignedOid, "Winter week");

        var run = await RunAsync(schema, Guid.Empty.ToString());

        Assert.Equal(ExitCode.Refused, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Equal($"That is the \"Unassigned\" user itself. Nothing was moved.{Environment.NewLine}", run.Error);

        await using var context = schema.NewContext();
        Assert.Equal(1, (await context.SearchMatrices.SingleAsync()).Version);
    }



    [Fact]
    public async Task RunAsync_when_the_argument_is_not_a_GUID_exits_Usage_and_opens_nothing()
    {
        await using var schema = await SeededAsync();

        var run = await RunAsync(schema, "pat");

        Assert.Equal(ExitCode.Usage, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Equal($"'pat' is not an object id. Give a GUID.{Environment.NewLine}", run.Error);
        Assert.Empty(schema.ConnectionStrings);
    }



    [Fact]
    public async Task RunAsync_when_asked_for_help_describes_the_argument()
    {
        await using var schema = await SeededAsync();

        var run = await HostRun.StartAsync(["assign-owner", "--help"], Configuration, schema.Open);

        Assert.Equal(ExitCode.Ok, run.ExitCode);
        Assert.Contains("The object id of the user who gets the rows: a GUID.", run.Output, StringComparison.Ordinal);
        Assert.Empty(schema.ConnectionStrings);
    }



    [Fact]
    public async Task RunAsync_when_no_user_is_named_exits_Usage_and_opens_nothing()
    {
        await using var schema = await SeededAsync();

        var run = await HostRun.StartAsync(["assign-owner"], Configuration, schema.Open);

        Assert.Equal(ExitCode.Usage, run.ExitCode);
        Assert.Empty(schema.ConnectionStrings);
    }



    [Fact]
    public async Task RunAsync_when_no_connection_string_is_set_exits_Usage_and_opens_nothing()
    {
        await using var schema = await SeededAsync();

        var run = await HostRun.StartAsync(["assign-owner", Target.ToString()], new ConfigurationBuilder().Build(), schema.Open);

        Assert.Equal(ExitCode.Usage, run.ExitCode);
        Assert.Empty(schema.ConnectionStrings);
    }



    [Fact]
    public async Task RunAsync_when_the_database_is_behind_the_build_exits_MissingMigration_before_reading_a_row()
    {
        await using var schema = await TestSchema.CreateAsync();

        var run = await RunAsync(schema, Target.ToString());

        Assert.Equal(ExitCode.MissingMigration, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Equal($"Missing migration: {TestSchema.Shipped}. Run `bf-maint migrate`.{Environment.NewLine}", run.Error);
    }



    [Fact]
    public async Task RunAsync_when_the_database_is_newer_than_the_build_exits_Refused_before_reading_a_row()
    {
        await using var schema = await TestSchema.CreateAsync(TestSchema.Shipped, "99990101000000_Later");

        var run = await RunAsync(schema, Target.ToString());

        Assert.Equal(ExitCode.Refused, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Equal($"The database is newer than this build. Deploy the matching build.{Environment.NewLine}", run.Error);
    }



    [Fact]
    public async Task CollisionsAsync_when_context_is_null_throws_ArgumentNullException()
    {
        var thrown = await Assert.ThrowsAsync<ArgumentNullException>(() => OwnerMove.CollisionsAsync(null!, Target));

        Assert.Equal("context", thrown.ParamName);
    }



    [Fact]
    public async Task RunAsync_when_context_is_null_throws_ArgumentNullException()
    {
        var thrown = await Assert.ThrowsAsync<ArgumentNullException>(() => OwnerMove.RunAsync(null!, Target));

        Assert.Equal("context", thrown.ParamName);
    }



    /// <summary>A database with the model's tables and two users who have signed in.</summary>
    private static async Task<TestSchema> SeededAsync()
    {
        var schema = await TestSchema.CreateModelAsync();

        await using var context = schema.NewContext();
        context.AppUsers.Add(new AppUser { Oid = Target, DisplayName = "Pat Example", Email = "pat@example.com", LastSeenUtc = Instant });
        context.AppUsers.Add(new AppUser { Oid = Other, DisplayName = "Lee Example", Email = "lee@example.com", LastSeenUtc = Instant });
        await context.SaveChangesAsync();

        return schema;
    }



    private static async Task<Guid> AddSearchAsync(TestSchema schema, Guid owner, string name)
    {
        var id = Guid.NewGuid();

        await using var context = schema.NewContext();
        context.SearchMatrices.Add
        (
            new SearchMatrix
            {
                Id = id,
                OwnerId = owner,
                Name = name,
                NormalizedName = SearchMatrix.NormalizeName(name),
                Brand = Brand.Sandals,
                NightCount = 7,
                AdultCount = 2,
                WindowKind = WindowKind.Recurring,
                CreatedUtc = Instant,
            }
        );
        await context.SaveChangesAsync();

        return id;
    }



    private static async Task AddRecipientAsync(TestSchema schema, Guid owner, string address)
    {
        await using var context = schema.NewContext();
        context.Recipients.Add(new Recipient { Id = Guid.NewGuid(), OwnerId = owner, Address = address, DisplayName = "Pat Example" });
        await context.SaveChangesAsync();
    }



    private static async Task AddIssueAsync(TestSchema schema, Guid user)
    {
        await using var context = schema.NewContext();
        context.OpenIssues.Add
        (
            new OpenIssue
            {
                Id = Guid.NewGuid(),
                UserId = user,
                Kind = IssueKind.WindowPassed,
                Subject = "The window of Winter week has passed.",
                ResolvesWhen = "Goes away when the window is changed.",
                RaisedUtc = Instant,
            }
        );
        await context.SaveChangesAsync();
    }



    /// <summary>Stores a run and one digest of it for the search <paramref name="searchId"/>, addressed to <paramref name="recipient"/>.</summary>
    private static async Task AddDeliveryAsync(TestSchema schema, Guid searchId, Guid? recipient)
    {
        var runId = Guid.NewGuid();
        var matrixRunId = Guid.NewGuid();

        // A search has one row for each run, so every digest here gets a run of its own.
        await using var context = schema.NewContext();
        context.Runs.Add(new Run { Id = runId, Trigger = "schedule", Status = RunStatus.Complete, StartedUtc = Instant });
        context.MatrixRuns.Add
        (
            new MatrixRun
            {
                Id = matrixRunId,
                RunId = runId,
                SearchMatrixId = searchId,
                FirstCheckIn = new DateOnly(2027, 1, 9),
                LastCheckIn = new DateOnly(2027, 2, 8),
                Stage = SearchRunStage.Exploded,
            }
        );
        context.DigestDeliveries.Add
        (
            new DigestDelivery
            {
                Id = Guid.NewGuid(),
                MatrixRunId = matrixRunId,
                RecipientUserId = recipient,
                RecipientAddress = "pat@example.com",
                Subject = "Winter week: 3 price drops",
                HtmlBody = "<p>Three prices dropped.</p>",
                TextBody = "Three prices dropped.",
                CreatedUtc = Instant,
            }
        );
        await context.SaveChangesAsync();
    }



    /// <summary>How many searches, recipients, open issues and digest deliveries point at <paramref name="user"/>.</summary>
    private static async Task<int> OwnedByAsync(BarefootDbContext context, Guid user)
    {
        return await context.SearchMatrices.CountAsync(search => search.OwnerId == user)
            + await context.Recipients.CountAsync(recipient => recipient.OwnerId == user)
            + await context.OpenIssues.CountAsync(issue => issue.UserId == user)
            + await context.DigestDeliveries.CountAsync(delivery => delivery.RecipientUserId == user);
    }



    private static async Task<HostRun> RunAsync(TestSchema schema, string oid)
    {
        return await HostRun.StartAsync(["assign-owner", oid], Configuration, schema.Open);
    }
}
