using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Services.Graph;

/// <summary>
/// Abstracts Microsoft Graph API calls for OneDrive operations.
/// All methods accept a pre-acquired access token — token refresh
/// is handled by the caller (AuthService) before invoking these.
/// </summary>
public interface IGraphService
{
    /// <summary>Returns the root-level folders in the user's OneDrive.</summary>
    Task<List<DriveFolder>> GetRootFoldersAsync(
        string accessToken,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the user's OneDrive storage quota.
    /// </summary>
    Task<(long Total, long Used)> GetQuotaAsync(
        string accessToken,
        CancellationToken ct = default);

    /// <summary>
    /// Executes a delta query for the given folder.
    /// Returns changed items and the next delta link to store.
    /// Pass null deltaLink for a full sync (first run).
    /// </summary>
    Task<DeltaResult> GetDeltaAsync(
        string  accessToken,
        string  folderId,
        string? deltaLink,
        CancellationToken ct = default);
}
