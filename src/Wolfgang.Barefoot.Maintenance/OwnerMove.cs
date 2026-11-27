using Microsoft.EntityFrameworkCore;
using Wolfgang.Barefoot.Core.Entities;
using Wolfgang.Barefoot.Data;

namespace Wolfgang.Barefoot.Maintenance;

/// <summary>
/// Moves what the seeded "Unassigned" user owns to another user (ADR 0004 §8, ADR 0016): searches,
/// recipients, open issues and digest deliveries.
/// </summary>
internal static class OwnerMove
{
    /// <summary>
    /// Finds what stands in the way of a move to <paramref name="target"/>: a search of the
    /// "Unassigned" user whose name the target already has, or a recipient whose address the target
    /// already has. An owner has a name once and an address once.
    /// </summary>
    /// <param name="context">A context on the database.</param>
    /// <param name="target">The user the rows would move to.</param>
    /// <param name="cancellationToken">Cancels the reads.</param>
    /// <returns>One line per collision, searches first, each list in alphabetical order; none when the move can go ahead.</returns>
    public static async Task<IReadOnlyList<string>> CollisionsAsync(BarefootDbContext context, Guid target, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var searches = await context
            .SearchMatrices
            .Where(search => search.OwnerId == AppUser.UnassignedOid || search.OwnerId == target)
            .Select(search => new Owned(search.OwnerId, search.NormalizedName, search.Name))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // An address is stored lower-cased and the column compares by code unit, so two addresses
        // that differ only in case are two rows to the database. They are one address to a person,
        // so the check here ignores case.
        var recipients = await context
            .Recipients
            .Where(recipient => recipient.OwnerId == AppUser.UnassignedOid || recipient.OwnerId == target)
            .Select(recipient => new Owned(recipient.OwnerId, recipient.Address.ToUpperInvariant(), recipient.Address))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return
        [
            .. Shared(searches, target).Select(name => $"search '{name}'"),
            .. Shared(recipients, target).Select(address => $"recipient {address}"),
        ];
    }



    /// <summary>
    /// Moves every row the "Unassigned" user owns to <paramref name="target"/> in one transaction.
    /// A search and a recipient get their next <c>Version</c>.
    /// </summary>
    /// <param name="context">A context on the database, with nothing tracked.</param>
    /// <param name="target">The user the rows move to. Their <see cref="AppUser"/> row must exist.</param>
    /// <param name="cancellationToken">Cancels the work before it is saved.</param>
    /// <returns>How many rows of each kind moved.</returns>
    public static async Task<OwnerMoveCount> RunAsync(BarefootDbContext context, Guid target, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var searches = await context
            .SearchMatrices
            .Where(search => search.OwnerId == AppUser.UnassignedOid)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var recipients = await context
            .Recipients
            .Where(recipient => recipient.OwnerId == AppUser.UnassignedOid)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var issues = await context
            .OpenIssues
            .Where(issue => issue.UserId == AppUser.UnassignedOid)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var deliveries = await context
            .DigestDeliveries
            .Where(delivery => delivery.RecipientUserId == AppUser.UnassignedOid)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        searches.ForEach(search => search.OwnerId = target);
        recipients.ForEach(recipient => recipient.OwnerId = target);
        issues.ForEach(issue => issue.UserId = target);
        deliveries.ForEach(delivery => delivery.RecipientUserId = target);

        // One save is one transaction: every row moves or none does.
        await context
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return new OwnerMoveCount(searches.Count, recipients.Count, issues.Count, deliveries.Count);
    }



    /// <summary>The labels of the rows of "Unassigned" whose key <paramref name="target"/> also has.</summary>
    private static IEnumerable<string> Shared(List<Owned> rows, Guid target)
    {
        var taken = rows
            .Where(row => row.OwnerId == target)
            .Select(row => row.Key)
            .ToHashSet(StringComparer.Ordinal);

        return rows
            .Where(row => row.OwnerId == AppUser.UnassignedOid && taken.Contains(row.Key))
            .Select(row => row.Label)
            .Order(StringComparer.Ordinal);
    }



    /// <summary>A row by its owner, the key the owner has once, and what to call it in a message.</summary>
    private sealed record Owned(Guid OwnerId, string Key, string Label);
}
