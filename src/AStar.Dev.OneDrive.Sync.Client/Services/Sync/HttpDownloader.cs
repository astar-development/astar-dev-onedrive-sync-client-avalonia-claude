namespace AStar.Dev.OneDrive.Sync.Client.Services.Sync;

/// <summary>
/// Handles file downloads with automatic retry on 429 Too Many Requests.
/// Uses exponential backoff respecting the Retry-After header when present.
/// A single shared HttpClient instance is used across all downloads.
/// </summary>
public sealed class HttpDownloader : IDisposable
{
    private readonly HttpClient _http;

    private const int    MaxRetries        = 5;
    private const double BaseDelaySeconds  = 2.0;
    private const double MaxDelaySeconds   = 120.0;

    public HttpDownloader()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add(
            "User-Agent", "AStar.Dev.OneDrive.Sync/1.0");
    }

    /// <summary>
    /// Downloads the file at <paramref name="url"/> to <paramref name="localPath"/>.
    /// Automatically retries on 429 with exponential backoff.
    /// Preserves the remote last-modified timestamp on the local file.
    /// </summary>
    public async Task DownloadAsync(
        string            url,
        string            localPath,
        DateTimeOffset    remoteModified,
        IProgress<long>?  progress      = null,
        CancellationToken ct            = default)
    {
        var attempt = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            attempt++;
            HttpResponseMessage? response = null;

            try
            {
                response = await _http.GetAsync(
                    url,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct);

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    if (attempt > MaxRetries)
                        throw new HttpRequestException(
                            $"Rate limited after {MaxRetries} retries.");

                    var delay = GetRetryDelay(response, attempt);
                    Serilog.Log.Warning(
                        "[HttpDownloader] 429 received, waiting {Delay:F1}s " +
                        "(attempt {Attempt}/{Max})",
                        delay.TotalSeconds, attempt, MaxRetries);

                    response.Dispose();
                    await Task.Delay(delay, ct);
                    continue;
                }

                response.EnsureSuccessStatusCode();

                // Ensure parent directories exist
                var dir = Path.GetDirectoryName(localPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                // Stream to disk
                await using var stream = await response.Content
                    .ReadAsStreamAsync(ct);
                await using var file = new FileStream(
                    localPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync:   true);

                var buffer    = new byte[81920];
                long written  = 0;
                int  read;

                while ((read = await stream.ReadAsync(buffer, ct)) > 0)
                {
                    await file.WriteAsync(buffer.AsMemory(0, read), ct);
                    written += read;
                    progress?.Report(written);
                }

                // Preserve remote timestamp
                File.SetLastWriteTimeUtc(localPath, remoteModified.UtcDateTime);
                return;
            }
            catch (HttpRequestException) when (attempt <= MaxRetries)
            {
                // Transient network error — retry with backoff
                var delay = GetBackoffDelay(attempt);
                Serilog.Log.Warning(
                    "[HttpDownloader] Network error, retrying in {Delay:F1}s " +
                    "(attempt {Attempt}/{Max})",
                    delay.TotalSeconds, attempt, MaxRetries);
                await Task.Delay(delay, ct);
            }
            finally
            {
                response?.Dispose();
            }
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private static TimeSpan GetRetryDelay(
        HttpResponseMessage response, int attempt)
    {
        // Honour Retry-After header if present
        if (response.Headers.RetryAfter?.Delta is { } delta)
            return delta + TimeSpan.FromSeconds(1); // +1s buffer

        if (response.Headers.RetryAfter?.Date is { } date)
        {
            var wait = date - DateTimeOffset.UtcNow;
            if (wait > TimeSpan.Zero) return wait + TimeSpan.FromSeconds(1);
        }

        return GetBackoffDelay(attempt);
    }

    private static TimeSpan GetBackoffDelay(int attempt)
    {
        // Exponential backoff with jitter: 2s, 4s, 8s, 16s, 32s (max 120s)
        var seconds = Math.Min(
            BaseDelaySeconds * Math.Pow(2, attempt - 1),
            MaxDelaySeconds);

        // Add up to 20% jitter to avoid thundering herd
        var jitter = seconds * 0.2 * Random.Shared.NextDouble();
        return TimeSpan.FromSeconds(seconds + jitter);
    }

    public void Dispose() => _http.Dispose();
}
