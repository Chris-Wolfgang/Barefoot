using Wolfgang.Barefoot.Contracts.Settings;

namespace Wolfgang.Barefoot.Core.Settings;

/// <summary>
/// The settings registry as an Admin reads and edits it, from the Settings page or <c>bf settings</c>
/// (ADR 0011). Only the Admin role may call it.
/// </summary>
/// <remarks>
/// This is the administration surface, where a setting is named by its text key because the name
/// comes from a path or a command line. Code that <em>uses</em> a setting reads it with its typed key.
/// </remarks>
public interface ISettingService
{
    /// <summary>
    /// Every setting in the registry, with its current value.
    /// </summary>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<IReadOnlyList<SettingResponse>> ListAsync(CancellationToken cancellationToken);



    /// <summary>
    /// One setting.
    /// </summary>
    /// <param name="key">The setting's name.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<SettingResponse> GetAsync(string key, CancellationToken cancellationToken);



    /// <summary>
    /// Stores a value for a setting.
    /// </summary>
    /// <param name="key">The setting's name.</param>
    /// <param name="version">The version the setting was read at; 0 for a setting with no stored value.</param>
    /// <param name="request">The new value.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<SettingResponse> SetAsync
    (
        string key,
        long version,
        SetSettingRequest request,
        CancellationToken cancellationToken
    );



    /// <summary>
    /// Removes the stored value, so that the setting returns to its default.
    /// </summary>
    /// <param name="key">The setting's name.</param>
    /// <param name="version">The version the setting was read at.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task ResetAsync
    (
        string key,
        long version,
        CancellationToken cancellationToken
    );
}
