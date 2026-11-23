using System.Runtime.InteropServices;
using Wolfgang.Barefoot.Contracts.Searches;

namespace Wolfgang.Barefoot.Core.Runs;

/// <summary>
/// What makes one flight lookup the same as another within a run (DESIGN §6): where from, where to,
/// when, in which cabin and for whom. It names the gateway airport, not the resort, because resorts
/// on one island share a gateway and so share their flights.
/// </summary>
/// <param name="OriginAirportId">The departure airport.</param>
/// <param name="GatewayAirportId">The airport that serves the resort.</param>
/// <param name="Depart">The outbound date: the check-in date.</param>
/// <param name="Return">The return date: the check-out date.</param>
/// <param name="Cabin">The cabin.</param>
/// <param name="AdultCount">The number of adults.</param>
/// <param name="ChildCount">The number of children.</param>
/// <param name="InfantCount">The number of infants.</param>
[StructLayout(LayoutKind.Auto)]
public readonly record struct FlightSetKey
(
    Guid OriginAirportId,
    Guid GatewayAirportId,
    DateOnly Depart,
    DateOnly Return,
    Cabin Cabin,
    int AdultCount,
    int ChildCount,
    int InfantCount
);
