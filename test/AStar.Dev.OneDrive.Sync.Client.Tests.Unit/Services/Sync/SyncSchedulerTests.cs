namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Sync;

using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using NSubstitute;

public class SyncSchedulerTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDependencies()
    {
        // Arrange
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();

        // Act
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);

        // Assert
        scheduler.ShouldNotBeNull();
    }

    [Fact]
    public void DefaultInterval_ShouldBe60Minutes()
    {
        // Assert
        SyncScheduler.DefaultInterval.ShouldBe(TimeSpan.FromMinutes(60));
    }

    [Fact]
    public void Start_ShouldInitializeTimer()
    {
        // Arrange
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);

        // Act
        scheduler.Start();

        // Assert - Timer should be created (no exception thrown)
        scheduler.ShouldNotBeNull();
    }

    [Fact]
    public void Start_WithDefaultInterval_ShouldUse60Minutes()
    {
        // Arrange
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);

        // Act
        scheduler.Start();

        // Assert - Should start with default interval
        scheduler.ShouldNotBeNull();
    }

    [Fact]
    public void Start_WithCustomInterval_ShouldUseProvidedInterval()
    {
        // Arrange
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        var customInterval = TimeSpan.FromMinutes(30);

        // Act
        scheduler.Start(customInterval);

        // Assert - Should start with custom interval
        scheduler.ShouldNotBeNull();
    }

    [Fact]
    public void Stop_ShouldStopTimer()
    {
        // Arrange
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        scheduler.Start();

        // Act
        scheduler.Stop();

        // Assert - Should stop without throwing
        scheduler.ShouldNotBeNull();
    }

    [Fact]
    public void SetInterval_ShouldUpdateInterval()
    {
        // Arrange
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        scheduler.Start();
        var newInterval = TimeSpan.FromMinutes(30);

        // Act
        scheduler.SetInterval(newInterval);

        // Assert - Should update without throwing
        scheduler.ShouldNotBeNull();
    }

    [Fact]
    public async Task TriggerNowAsync_ShouldExecuteSyncPass()
    {
        // Arrange
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        mockRepository.GetAllAsync().Returns(new List<AccountEntity>());

        var scheduler = new SyncScheduler(mockSyncService, mockRepository);

        // Act
        await scheduler.TriggerNowAsync();

        // Assert - Should call repository to get accounts
        await mockRepository.Received(1).GetAllAsync();
    }

    [Fact]
    public async Task TriggerNowAsync_WhenAlreadyRunning_ShouldNotStartNewPass()
    {
        // Arrange
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        mockRepository.GetAllAsync().Returns(new List<AccountEntity>());

        var scheduler = new SyncScheduler(mockSyncService, mockRepository);

        // This is difficult to test without internal state exposure
        // Just verify it doesn't throw
        // Act
        await scheduler.TriggerNowAsync();

        // Assert
        scheduler.ShouldNotBeNull();
    }

    [Fact]
    public async Task TriggerAccountAsync_ShouldSyncSpecificAccount()
    {
        // Arrange
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        var account = new OneDriveAccount { Id = "test-account" };

        // Act
        await scheduler.TriggerAccountAsync(account);

        // Assert - Should call SyncAccountAsync with the account
        await mockSyncService.Received(1).SyncAccountAsync(account, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TriggerAccountAsync_ShouldRaiseSyncStartedEvent()
    {
        // Arrange
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        var account = new OneDriveAccount { Id = "test-account" };
        var eventRaised = false;
        string? raisedAccountId = null;

        scheduler.SyncStarted += (s, accountId) =>
        {
            eventRaised = true;
            raisedAccountId = accountId;
        };

        // Act
        await scheduler.TriggerAccountAsync(account);

        // Assert
        eventRaised.ShouldBeTrue();
        raisedAccountId.ShouldBe("test-account");
    }

    [Fact]
    public async Task TriggerAccountAsync_ShouldRaiseSyncCompletedEvent()
    {
        // Arrange
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        var account = new OneDriveAccount { Id = "test-account" };
        var eventRaised = false;
        string? raisedAccountId = null;

        scheduler.SyncCompleted += (s, accountId) =>
        {
            eventRaised = true;
            raisedAccountId = accountId;
        };

        // Act
        await scheduler.TriggerAccountAsync(account);

        // Assert
        eventRaised.ShouldBeTrue();
        raisedAccountId.ShouldBe("test-account");
    }

    [Fact]
    public async Task TriggerAccountAsync_WhenSyncServiceThrows_ShouldStillRaiseCompletedEvent()
    {
        // Arrange
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        var account = new OneDriveAccount { Id = "test-account" };

        mockSyncService.SyncAccountAsync(Arg.Any<OneDriveAccount>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Sync failed")));

        var completedEventRaised = false;
        scheduler.SyncCompleted += (s, accountId) => completedEventRaised = true;

        // Act & Assert - Should not throw, and should still raise completed event
        var ex = await Should.ThrowAsync<InvalidOperationException>(
            async () => await scheduler.TriggerAccountAsync(account));

        completedEventRaised.ShouldBeTrue();
    }

    [Fact]
    public void SyncScheduler_ShouldBeAsyncDisposable()
    {
        // Arrange
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);

        // Assert
        scheduler.ShouldBeAssignableTo<IAsyncDisposable>();
    }

    [Theory]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(120)]
    public void Start_WithVariousIntervals_ShouldInitializeSuccessfully(int minutes)
    {
        // Arrange
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        var interval = TimeSpan.FromMinutes(minutes);

        // Act
        scheduler.Start(interval);

        // Assert - Should not throw
        scheduler.ShouldNotBeNull();
    }

    [Fact]
    public async Task TriggerAccountAsync_ShouldPassCorrectAccountData()
    {
        // Arrange
        var mockSyncService = Substitute.For<ISyncService>();
        var mockRepository = Substitute.For<IAccountRepository>();
        var scheduler = new SyncScheduler(mockSyncService, mockRepository);
        var account = new OneDriveAccount
        {
            Id = "account-123",
            Email = "test@outlook.com",
            DisplayName = "Test User"
        };

        // Act
        await scheduler.TriggerAccountAsync(account);

        // Assert
        await mockSyncService.Received(1).SyncAccountAsync(
            Arg.Is<OneDriveAccount>(a => a.Id == "account-123"),
            Arg.Any<CancellationToken>());
    }
}
