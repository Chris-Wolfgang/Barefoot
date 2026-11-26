using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Conventions;

/// <summary>
/// Sets the <c>Version</c> of the rows a save is about to write (ADR 0004 §10). The context owns the
/// number: whatever a caller put in the property of a new or changed row is replaced.
/// </summary>
internal static class VersionStamp
{
    /// <summary>The name every versioned entity gives its concurrency token.</summary>
    internal const string PropertyName = nameof(AppUser.Version);

    /// <summary>
    /// The version a new row is stored with. A row that does not exist yet reports 0
    /// (ADR 0012 §3), so its first write has to change the tag.
    /// </summary>
    internal const long FirstVersion = 1;



    /// <summary>
    /// Gives every new row <see cref="FirstVersion"/> and every changed row the version it was read
    /// with plus one. Rows that are unchanged or being deleted, and rows of entities without a
    /// <c>Version</c>, are left alone.
    /// </summary>
    /// <remarks>
    /// A changed row counts up from the version it was read with, not from the property's current
    /// value, so saving again after a failed save writes the same number and not one higher.
    /// </remarks>
    /// <param name="changeTracker">The tracker of the context that is about to save.</param>
    public static void Apply(ChangeTracker changeTracker)
    {
        foreach (var entry in changeTracker.Entries())
        {
            if (entry.Metadata.FindProperty(PropertyName) is null)
            {
                continue;
            }

            var version = entry.Property(PropertyName);

            if (entry.State == EntityState.Added)
            {
                version.CurrentValue = FirstVersion;
            }
            else if (entry.State == EntityState.Modified)
            {
                version.CurrentValue = (long)version.OriginalValue! + 1;
            }
        }
    }
}
