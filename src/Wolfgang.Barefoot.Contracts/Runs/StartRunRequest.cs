namespace Wolfgang.Barefoot.Contracts.Runs;

/// <summary>
/// The body of <c>POST /runs</c> (DESIGN §5).
/// </summary>
/// <param name="Trigger">What started the run, for example the scheduled job or a person.</param>
public sealed record StartRunRequest(string Trigger);
