using System.Runtime.InteropServices;

namespace Wolfgang.Barefoot.Core.Runs;

/// <summary>
/// A resort as the expander needs it: its id and the airport that serves it.
/// </summary>
/// <param name="ResortId">The resort.</param>
/// <param name="GatewayAirportId">The resort's gateway airport.</param>
[StructLayout(LayoutKind.Auto)]
public readonly record struct ExpandResort(Guid ResortId, Guid GatewayAirportId);
