namespace Wolfgang.Barefoot.Contracts.Users;

/// <summary>
/// Narrows the list of users. Every member is optional; with none set the list is not narrowed.
/// </summary>
/// <param name="Find">Only people whose name or address contains this text.</param>
/// <param name="CanOwnSearchOnly">
/// Only people who can own a search: the owner picker an Admin uses to create a search for someone
/// else (ADR 0004 §8).
/// </param>
public sealed record UserFilter(string? Find = null, bool CanOwnSearchOnly = false);
