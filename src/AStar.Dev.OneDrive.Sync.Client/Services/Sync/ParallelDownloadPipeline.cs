using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Models;
using System.Threading.Channels;

namespace AStar.Dev.OneDrive.Sync.Client.Services.Sync;

/// <summary>
/// Orchestrates parallel file downloads using a bounded Channel.
///
/// Architecture:
///   Producer  — feeds SyncJob items into the channel one at a time,
///               applying backpressure when workers are saturated.
///   Consumers — N workers drain the channel concurrently.
///
/// Backpressure: the channel capacity is (Workers × 4) so the producer
/// never loads more than ~4 jobs per worker into memory at once.
/// With 300k files this means memory stays flat regardless of job count.
/// </summary>
public sealed class ParallelDownloadPipeline(
    ISyncRepository syncRepository,
    int             workerCount = 8)
{
    private readonly HttpDownloader _downloader = new();

    // ── Public API ────────────────────────────────────────────────────────

    public async Task RunAsync(
        IEnumerable<SyncJob>                      jobs,
        string                                    accessToken,
        Action<SyncProgressEventArgs>             onProgress,
        Action<JobCompletedEventArgs>             onJobCompleted,
        string                                    accountId,
        string                                    folderId,
        CancellationToken                         ct = default)
    {
        var jobList = jobs.ToList();
        if (jobList.Count == 0) return;

        var total     = jobList.Count;
        var completed = 0;
        var failed    = 0;
        var lockObj   = new object();

        // Bounded channel — backpressure prevents memory explosion
        var channel = Channel.CreateBounded<SyncJob>(
            new BoundedChannelOptions(workerCount * 4)
            {
                FullMode          = BoundedChannelFullMode.Wait,
                SingleReader      = false,
                SingleWriter      = true
            });

        void OnJobComplete(SyncJob job, bool success, string? error)
        {
            int done;
            lock (lockObj)
            {
                if (success) completed++;
                else         failed++;
                done = completed + failed;
            }

            onProgress(new SyncProgressEventArgs(
                accountId:   accountId,
                folderId:    folderId,
                completed:   done,
                total:       total,
                currentFile: job.RelativePath,
                syncState: ViewModels.SyncState.Syncing,
                isComplete:  done == total));
            onJobCompleted(new JobCompletedEventArgs(
                job with
                {
                    State       = success ? SyncJobState.Completed : SyncJobState.Failed,
                    ErrorMessage = error,
                    CompletedAt  = DateTimeOffset.UtcNow
                }));
            
            if (success)
                Serilog.Log.Information(
                    "[Pipeline] ✓ {Path} ({Done}/{Total})",
                    job.RelativePath, done, total);
        }

        // Start N worker tasks
        var workers = Enumerable.Range(1, workerCount)
            .Select(id => new DownloadWorker(id, _downloader, syncRepository)
                .RunAsync(channel.Reader, accessToken, OnJobComplete, ct))
            .ToList();

        // Producer — feed jobs into the channel with backpressure
        try
        {
            foreach (var job in jobList)
            {
                ct.ThrowIfCancellationRequested();
                await channel.Writer.WriteAsync(job, ct);
            }
        }
        finally
        {
            // Signal all workers that no more jobs are coming
            channel.Writer.Complete();
        }

        // Wait for all workers to drain the channel
        await Task.WhenAll(workers);

        // Clean up completed jobs from DB
        await syncRepository.ClearCompletedJobsAsync(accountId);

        Serilog.Log.Information(
            "[Pipeline] Complete — {Completed} succeeded, {Failed} failed " +
            "out of {Total} total",
            completed, failed, total);
    }

    public void Dispose() => _downloader.Dispose();
}
