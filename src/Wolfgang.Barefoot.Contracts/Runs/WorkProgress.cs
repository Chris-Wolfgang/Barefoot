namespace Wolfgang.Barefoot.Contracts.Runs;

/// <summary>
/// How far one kind of a run's work has come: its room lookups or its flight sets. A run knows all
/// its work when it is created, so the total does not change.
/// </summary>
/// <param name="Done">How many have a result or an error.</param>
/// <param name="Total">How many there are.</param>
/// <param name="Failed">How many of the done ones ended in an error.</param>
public sealed record WorkProgress(int Done, int Total, int Failed);
