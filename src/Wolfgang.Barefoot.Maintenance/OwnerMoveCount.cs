namespace Wolfgang.Barefoot.Maintenance;

/// <summary>
/// How many rows <see cref="OwnerMove.RunAsync"/> moved to the new owner.
/// </summary>
/// <param name="Searches">The searches moved.</param>
/// <param name="Recipients">The recipients moved.</param>
/// <param name="Issues">The open issues moved.</param>
/// <param name="Deliveries">The digest deliveries moved.</param>
internal sealed record OwnerMoveCount(int Searches, int Recipients, int Issues, int Deliveries);
