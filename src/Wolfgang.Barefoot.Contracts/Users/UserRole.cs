namespace Wolfgang.Barefoot.Contracts.Users;

/// <summary>
/// The role a person holds (DESIGN §10). Roles are assigned in Entra, never here; the API only
/// reports the one the person last signed in with.
/// </summary>
public enum UserRole
{
    /// <summary>Reads their own results and digests. An Admin creates their searches.</summary>
    Viewer = 0,

    /// <summary>Creates and edits their own searches.</summary>
    Editor = 1,

    /// <summary>Everything, for every user.</summary>
    Admin = 2,
}
