namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Models;

using AStar.Dev.OneDrive.Sync.Client.Models;

public class SyncJobTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var syncJob = new SyncJob();

        // Assert
        syncJob.Id.ShouldNotBe(Guid.Empty);
        syncJob.AccountId.ShouldBe(string.Empty);
        syncJob.FolderId.ShouldBe(string.Empty);
        syncJob.RemoteItemId.ShouldBe(string.Empty);
        syncJob.RelativePath.ShouldBe(string.Empty);
        syncJob.LocalPath.ShouldBe(string.Empty);
        syncJob.Direction.ShouldBe(default(SyncDirection));
        syncJob.State.ShouldBe(SyncJobState.Queued);
        syncJob.ErrorMessage.ShouldBeNull();
        syncJob.DownloadUrl.ShouldBeNull();
        syncJob.FileSize.ShouldBe(0L);
        syncJob.RemoteModified.ShouldBe(default);
        syncJob.QueuedAt.ShouldNotBe(default);
        syncJob.CompletedAt.ShouldBeNull();
    }

    [Fact]
    public void Id_ShouldBeUnique()
    {
        // Act
        var job1 = new SyncJob();
        var job2 = new SyncJob();

        // Assert
        job1.Id.ShouldNotBe(job2.Id);
    }

    [Fact]
    public void CanCreateWithInitProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var accountId = "account-123";
        var folderId = "folder-456";
        var remoteItemId = "item-789";
        var relativePath = "Documents/report.pdf";
        var localPath = "/home/jason/Documents/report.pdf";
        var direction = SyncDirection.Download;
        var fileSize = 1024L;
        var remoteModified = DateTimeOffset.UtcNow.AddHours(-1);

        // Act
        var syncJob = new SyncJob
        {
            Id = id,
            AccountId = accountId,
            FolderId = folderId,
            RemoteItemId = remoteItemId,
            RelativePath = relativePath,
            LocalPath = localPath,
            Direction = direction,
            FileSize = fileSize,
            RemoteModified = remoteModified
        };

        // Assert
        syncJob.Id.ShouldBe(id);
        syncJob.AccountId.ShouldBe(accountId);
        syncJob.FolderId.ShouldBe(folderId);
        syncJob.RemoteItemId.ShouldBe(remoteItemId);
        syncJob.RelativePath.ShouldBe(relativePath);
        syncJob.LocalPath.ShouldBe(localPath);
        syncJob.Direction.ShouldBe(direction);
        syncJob.FileSize.ShouldBe(fileSize);
        syncJob.RemoteModified.ShouldBe(remoteModified);
    }

    [Fact]
    public void State_ShouldBeSettable()
    {
        // Arrange
        var syncJob = new SyncJob();

        // Act
        syncJob.State = SyncJobState.InProgress;

        // Assert
        syncJob.State.ShouldBe(SyncJobState.InProgress);
    }

    [Theory]
    [InlineData(SyncJobState.Queued)]
    [InlineData(SyncJobState.InProgress)]
    [InlineData(SyncJobState.Completed)]
    [InlineData(SyncJobState.Failed)]
    [InlineData(SyncJobState.Skipped)]
    public void State_ShouldSupportAllStates(SyncJobState state)
    {
        // Arrange
        var syncJob = new SyncJob();

        // Act
        syncJob.State = state;

        // Assert
        syncJob.State.ShouldBe(state);
    }

    [Fact]
    public void ErrorMessage_ShouldBeSettable()
    {
        // Arrange
        var syncJob = new SyncJob();
        var errorMessage = "File is locked by another process";

        // Act
        syncJob.ErrorMessage = errorMessage;

        // Assert
        syncJob.ErrorMessage.ShouldBe(errorMessage);
    }

    [Fact]
    public void DownloadUrl_ShouldBeSettable()
    {
        // Arrange
        var syncJob = new SyncJob();
        var downloadUrl = "https://graph.microsoft.com/v1.0/drives/abc123/items/xyz789/content";

        // Act
        syncJob.DownloadUrl = downloadUrl;

        // Assert
        syncJob.DownloadUrl.ShouldBe(downloadUrl);
    }

    [Fact]
    public void CompletedAt_ShouldBeSettable()
    {
        // Arrange
        var syncJob = new SyncJob();
        var completedAt = DateTimeOffset.UtcNow;

        // Act
        syncJob.CompletedAt = completedAt;

        // Assert
        syncJob.CompletedAt.ShouldBe(completedAt);
    }

    [Theory]
    [InlineData(SyncDirection.Download)]
    [InlineData(SyncDirection.Upload)]
    [InlineData(SyncDirection.Delete)]
    public void Direction_ShouldSupportAllDirections(SyncDirection direction)
    {
        // Arrange
        var syncJob = new SyncJob { Direction = direction };

        // Assert
        syncJob.Direction.ShouldBe(direction);
    }

    [Fact]
    public void QueuedAt_ShouldBeSetToCurrentUtcTimeByDefault()
    {
        // Arrange
        var beforeCreation = DateTimeOffset.UtcNow;

        // Act
        var syncJob = new SyncJob();

        // Assert
        syncJob.QueuedAt.ShouldBeGreaterThanOrEqualTo(beforeCreation);
        syncJob.QueuedAt.ShouldBeLessThanOrEqualTo(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void IsRecord_ShouldAllowValueEquality()
    {
        // Arrange
        var id = Guid.NewGuid();
        var queuedAt = DateTimeOffset.UtcNow;
        var job1 = new SyncJob { Id = id, AccountId = "account-123", QueuedAt = queuedAt };
        var job2 = new SyncJob { Id = id, AccountId = "account-123", QueuedAt = queuedAt };

        // Act & Assert
        job1.ShouldBe(job2);
    }

    [Fact]
    public void IsRecord_ShouldDifferOnPropertyChange()
    {
        // Arrange
        var id = Guid.NewGuid();
        var job1 = new SyncJob { Id = id, AccountId = "account-123" };
        var job2 = new SyncJob { Id = id, AccountId = "account-456" };

        // Act & Assert
        job1.ShouldNotBe(job2);
    }

    [Fact]
    public void DownloadJob_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var downloadJob = new SyncJob
        {
            AccountId = "account-123",
            FolderId = "folder-456",
            RemoteItemId = "item-789",
            Direction = SyncDirection.Download,
            State = SyncJobState.Queued
        };

        // Assert
        downloadJob.Direction.ShouldBe(SyncDirection.Download);
        downloadJob.State.ShouldBe(SyncJobState.Queued);
    }

    [Fact]
    public void UploadJob_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var uploadJob = new SyncJob
        {
            AccountId = "account-123",
            FolderId = "folder-456",
            RemoteItemId = "item-789",
            Direction = SyncDirection.Upload,
            State = SyncJobState.Queued
        };

        // Assert
        uploadJob.Direction.ShouldBe(SyncDirection.Upload);
        uploadJob.State.ShouldBe(SyncJobState.Queued);
    }

    [Fact]
    public void DeleteJob_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var deleteJob = new SyncJob
        {
            AccountId = "account-123",
            FolderId = "folder-456",
            RemoteItemId = "item-789",
            Direction = SyncDirection.Delete,
            State = SyncJobState.Queued
        };

        // Assert
        deleteJob.Direction.ShouldBe(SyncDirection.Delete);
        deleteJob.State.ShouldBe(SyncJobState.Queued);
    }
}
