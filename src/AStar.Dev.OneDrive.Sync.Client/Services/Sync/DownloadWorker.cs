using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Models;
using System.Threading.Channels;

namespace AStar.Dev.OneDrive.Sync.Client.Services.Sync;

/// <summary>
/// A single download worker that drains jobs from a
/// <see cref="ChannelReader{T}"/> and executes them.
/// Multiple workers run concurrently — one per degree of parallelism.
/// </summary>
public sealed class DownloadWorker(
    int              workerId,
    HttpDownloader   downloader,
    ISyncRepository  syncRepository)
{
    public async Task RunAsync(
        ChannelReader<SyncJob>                    reader,
        string                                    accessToken,
        Action<SyncJob, bool, string?>            onJobComplete,
        CancellationToken                         ct)
    {
        await foreach (SyncJob job in reader.ReadAllAsync(ct))
        {
            ct.ThrowIfCancellationRequested();

            Serilog.Log.Debug(
                "[Worker {Id}] Processing {Direction} {Path}",
                workerId, job.Direction, job.RelativePath);

            await syncRepository.UpdateJobStateAsync(
                job.Id, SyncJobState.InProgress);

            string? error = null;
            var     success = false;

            try
            {
                await ExecuteJobAsync(job, ct);
                success = true;

                await syncRepository.UpdateJobStateAsync(
                    job.Id, SyncJobState.Completed);
            }
            catch (OperationCanceledException)
            {
                await syncRepository.UpdateJobStateAsync(
                    job.Id, SyncJobState.Queued); // re-queue on cancel
                throw;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                Serilog.Log.Error(ex,
                    "[Worker {Id}] Failed: {Path} — {Error}",
                    workerId, job.RelativePath, ex.Message);

                await syncRepository.UpdateJobStateAsync(
                    job.Id, SyncJobState.Failed, ex.Message);
            }
            finally
            {
                onJobComplete(job, success, error);
            }
        }
    }

    // ── Private ───────────────────────────────────────────────────────────

    private async Task ExecuteJobAsync(SyncJob job, CancellationToken ct)
    {
        switch (job.Direction)
        {
            case SyncDirection.Download:
                if (job.DownloadUrl is null)
                    throw new InvalidOperationException(
                        $"No download URL for {job.RelativePath}");

                await downloader.DownloadAsync(
                    job.DownloadUrl,
                    job.LocalPath,
                    job.RemoteModified,
                    ct: ct);
                break;

            case SyncDirection.Delete:
                if (File.Exists(job.LocalPath))
                    File.Delete(job.LocalPath);
                break;

            case SyncDirection.Upload:
                // Upload implementation in a later step
                Serilog.Log.Warning(
                    "[Worker {Id}] Upload not yet implemented: {Path}",
                    workerId, job.RelativePath);
                break;
        }
    }
}
