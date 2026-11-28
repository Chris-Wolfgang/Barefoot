namespace Wolfgang.Barefoot.Maintenance;

/// <summary>
/// The SQL that gives the API its database user and roles on one kind of server (ADR 0016).
/// <see cref="RoleGrant"/> runs it.
/// </summary>
/// <param name="CanGrantQuery">
/// A query with one row and one column called <c>Value</c>: 1 when this server can make the user,
/// 0 when it cannot.
/// </param>
/// <param name="Statements">
/// The statements that make the user and add it to its roles, in order. Each one changes nothing
/// when what it adds is already there.
/// </param>
/// <param name="WhyNot">Why a server cannot make the user, as the tool says it: <c>not Azure SQL Database</c>.</param>
internal sealed record RoleScript(string CanGrantQuery, IReadOnlyList<string> Statements, string WhyNot);
