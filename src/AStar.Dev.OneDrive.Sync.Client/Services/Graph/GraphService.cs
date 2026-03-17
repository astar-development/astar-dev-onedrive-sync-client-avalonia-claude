using AStar.Dev.OneDrive.Sync.Client.Models;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;

namespace AStar.Dev.OneDrive.Sync.Client.Services.Graph;

public sealed class GraphService : IGraphService
{
    private readonly Dictionary<string, DriveContext> _cache = [];

    // ── IGraphService ─────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<string> GetDriveIdAsync(string accessToken, CancellationToken ct = default)
        => (await ResolveClientWithDriveContextAsync(accessToken, ct)).Ctx.DriveId;

    /// <inheritdoc />
    public async Task<List<DriveFolder>> GetRootFoldersAsync(string accessToken, CancellationToken ct = default)
    {
        var (client, driveContext) = await ResolveClientWithDriveContextAsync(accessToken, ct);

        var result = await client.Drives[driveContext.DriveId].Items[driveContext.RootId].Children
            .GetAsync(req =>
            {
                req.QueryParameters.Select = ["id", "name", "folder", "parentReference"];
                req.QueryParameters.Top    = 100;
            }, ct);

        List<DriveFolder> folders = [];

        var page = result;
        while (page?.Value is not null)
        {
            folders.AddRange(
                page.Value
                    .Where(i => i.Folder is not null)
                    .Select(i => new DriveFolder(
                        Id:       i.Id!,
                        Name:     i.Name!,
                        ParentId: i.ParentReference?.Id)));

            if (page.OdataNextLink is null) break;

            page = await client.Drives[driveContext.DriveId].Items[driveContext.RootId].Children
                .WithUrl(page.OdataNextLink)
                .GetAsync(cancellationToken: ct);
        }

        return [.. folders.OrderBy(f => f.Name)];
    }

    /// <inheritdoc />
    public async Task<List<DriveFolder>> GetChildFoldersAsync( string accessToken, string driveId, string parentFolderId, CancellationToken ct = default)
    {
        var client = BuildClient(accessToken);

        var result = await client.Drives[driveId].Items[parentFolderId].Children
            .GetAsync(req =>
            {
                req.QueryParameters.Select = ["id", "name", "folder", "parentReference"];
                req.QueryParameters.Top    = 100;
            }, ct);

        List<DriveFolder> folders = [];

        var page = result;
        while (page?.Value is not null)
        {
            folders.AddRange(
                page.Value
                    .Where(i => i.Folder is not null)
                    .Select(i => new DriveFolder(
                        Id:       i.Id!,
                        Name:     i.Name!,
                        ParentId: i.ParentReference?.Id)));

            if (page.OdataNextLink is null) break;

            page = await client.Drives[driveId].Items[parentFolderId].Children
                .WithUrl(page.OdataNextLink)
                .GetAsync(cancellationToken: ct);
        }

        return [.. folders.OrderBy(f => f.Name)];
    }

    /// <inheritdoc />
    public async Task<(long Total, long Used)> GetQuotaAsync(string accessToken, CancellationToken ct = default)
    {
        var (client, ctx) = await ResolveAsync(accessToken, ct);

        var drive = await client.Drives[ctx.DriveId]
            .GetAsync(req =>
                req.QueryParameters.Select = ["quota"], ct);

        return drive?.Quota is { Total: not null, Used: not null }
            ? (drive.Quota.Total.Value, drive.Quota.Used.Value)
            : (0L, 0L);
    }

    /// <inheritdoc />
    public async Task<DeltaResult> GetDeltaAsync(string accessToken, string folderId, string? deltaLink, CancellationToken ct = default)
    {
        var (client, ctx) = await ResolveAsync(accessToken, ct);

        List<DeltaItem> items = [];
        string? nextDeltaLink = null;
        bool hasMorePages     = false;

        var page = deltaLink is not null
            ? await client.Drives[ctx.DriveId].Items[folderId].Delta
                .WithUrl(deltaLink)
                .GetAsDeltaGetResponseAsync(cancellationToken: ct)
            : await client.Drives[ctx.DriveId].Items[folderId].Delta
                .GetAsDeltaGetResponseAsync(cancellationToken: ct);

        while (page?.Value is not null) // ToDo - could this be the issue? returns 0 items on the first call, but not sure which folder it is for
        {
            foreach (var item in page.Value)
            {
                items.Add(new DeltaItem(
                    Id:           item.Id!,
                    Name:         item.Name ?? string.Empty,
                    ParentId:     item.ParentReference?.Id,
                    IsFolder:     item.Folder is not null,
                    IsDeleted:    item.Deleted is not null,
                    Size:         item.Size ?? 0L,
                    LastModified: item.LastModifiedDateTime,
                    DownloadUrl:  item.AdditionalData
                        .TryGetValue("@microsoft.graph.downloadUrl", out var url)
                            ? url?.ToString()
                            : null));
            }

            if (page.OdataNextLink is not null)
            {
                hasMorePages = true;
                page = await client.Drives[ctx.DriveId].Items[folderId].Delta
                    .WithUrl(page.OdataNextLink)
                    .GetAsDeltaGetResponseAsync(cancellationToken: ct);
            }
            else
            {
                nextDeltaLink = page.OdataDeltaLink;
                break;
            }
        }

        return new DeltaResult(items, nextDeltaLink, hasMorePages);
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private async Task<(GraphServiceClient Client, DriveContext Ctx)> ResolveClientWithDriveContextAsync(string accessToken, CancellationToken ct)
    {
        var graphServiceClient = BuildClient(accessToken);

        if (_cache.TryGetValue(accessToken, out var cached))
            return (graphServiceClient, cached);

        var drive = await graphServiceClient.Me.Drive.GetAsync(cancellationToken: ct);

        var driveId = drive?.Id ?? throw new InvalidOperationException("Could not retrieve drive ID.");

        var root = await graphServiceClient.Drives[driveId].Root.GetAsync(cancellationToken: ct);

        var rootId = root?.Id ?? throw new InvalidOperationException("Could not retrieve root item ID.");

        var driveContext = new DriveContext(driveId, rootId);
        _cache[accessToken] = driveContext;

        return (graphServiceClient, driveContext);
    }

    private static GraphServiceClient BuildClient(string accessToken) =>
        new(new BaseBearerTokenAuthenticationProvider(new StaticAccessTokenProvider(accessToken)));

    private sealed record DriveContext(string DriveId, string RootId);

    private sealed class StaticAccessTokenProvider(string token) : IAccessTokenProvider
    {
        public Task<string> GetAuthorizationTokenAsync(
            Uri uri,
            Dictionary<string, object>? additionalAuthenticationContext = null,
            CancellationToken ct = default) => Task.FromResult(token);

        public AllowedHostsValidator AllowedHostsValidator { get; } = new(["graph.microsoft.com"]);
    }
}
