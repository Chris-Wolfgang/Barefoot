using System.Runtime.InteropServices;

namespace Wolfgang.Barefoot.Core.Runs;

/// <summary>
/// What makes one room lookup the same as another within a run: the resort, the stay and who is
/// travelling (ADR 0004 §1). The check-out date is part of it, because a different number of nights
/// is a different price. The cabin is not, because it affects flights and not rooms.
/// </summary>
/// <param name="ResortId">The resort.</param>
/// <param name="CheckIn">The check-in date.</param>
/// <param name="CheckOut">The check-out date.</param>
/// <param name="Adults">The number of adults.</param>
/// <param name="Children">The number of children.</param>
/// <param name="Infants">The number of infants.</param>
[StructLayout(LayoutKind.Auto)]
public readonly record struct LookupKey
(
    Guid ResortId,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Adults,
    int Children,
    int Infants
);
