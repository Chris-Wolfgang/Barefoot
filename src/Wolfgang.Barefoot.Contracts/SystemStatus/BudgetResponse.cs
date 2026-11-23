namespace Wolfgang.Barefoot.Contracts.SystemStatus;

/// <summary>
/// How much of the free allowances the installation has used: the budget cards of the Admin's
/// System page (DESIGN §12, Budget and health). Every figure comes from the hosting platform and is
/// <see langword="null"/> until the API is allowed to read it there.
/// </summary>
/// <param name="DatabaseStatus">Whether the database is awake, or <see cref="DatabaseStatus.NotSet"/> when it is not known.</param>
/// <param name="DatabaseAllowanceUsedPercent">How much of this month's free database allowance is used, 0 to 100.</param>
/// <param name="DatabaseAllowanceResetsOn">The day that allowance starts again.</param>
/// <param name="StorageUsedBytes">How much the database stores.</param>
/// <param name="StorageLimitBytes">How much it may store.</param>
/// <param name="ComputeUsedPercent">How much of the free compute grant the API and the web site have used, 0 to 100.</param>
/// <param name="ComputeAsOfUtc">
/// When the compute figure was true. The platform reports it hours late, so the card says "as of".
/// </param>
public sealed record BudgetResponse
(
    DatabaseStatus DatabaseStatus,
    int? DatabaseAllowanceUsedPercent,
    DateOnly? DatabaseAllowanceResetsOn,
    long? StorageUsedBytes,
    long? StorageLimitBytes,
    int? ComputeUsedPercent,
    DateTimeOffset? ComputeAsOfUtc
);
