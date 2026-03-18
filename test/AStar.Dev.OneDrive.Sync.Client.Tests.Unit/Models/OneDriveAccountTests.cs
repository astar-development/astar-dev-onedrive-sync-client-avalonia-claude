namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Models;

using AStar.Dev.OneDrive.Sync.Client.Models;

public class OneDriveAccountTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var account = new OneDriveAccount();

        // Assert
        account.Id.ShouldNotBeNullOrEmpty();
        account.DisplayName.ShouldBe(string.Empty);
        account.Email.ShouldBe(string.Empty);
        account.AccentIndex.ShouldBe(0);
        account.SelectedFolderIds.ShouldBeEmpty();
        account.DeltaLink.ShouldBeNull();
        account.LastSyncedAt.ShouldBeNull();
        account.QuotaTotal.ShouldBe(0L);
        account.QuotaUsed.ShouldBe(0L);
        account.IsActive.ShouldBeFalse();
        account.FolderNames.ShouldBeEmpty();
        account.LocalSyncPath.ShouldBe(string.Empty);
        account.ConflictPolicy.ShouldBe(ConflictPolicy.Ignore);
    }

    [Fact]
    public void Id_ShouldBeUnique()
    {
        // Act
        var account1 = new OneDriveAccount();
        var account2 = new OneDriveAccount();

        // Assert
        account1.Id.ShouldNotBe(account2.Id);
    }

    [Fact]
    public void DisplayName_ShouldBeSettable()
    {
        // Arrange
        var account = new OneDriveAccount();
        var displayName = "Jason Smith";

        // Act
        account.DisplayName = displayName;

        // Assert
        account.DisplayName.ShouldBe(displayName);
    }

    [Fact]
    public void Email_ShouldBeSettable()
    {
        // Arrange
        var account = new OneDriveAccount();
        var email = "jason@outlook.com";

        // Act
        account.Email = email;

        // Assert
        account.Email.ShouldBe(email);
    }

    [Fact]
    public void AccentIndex_ShouldBeSettable()
    {
        // Arrange
        var account = new OneDriveAccount();
        var accentIndex = 3;

        // Act
        account.AccentIndex = accentIndex;

        // Assert
        account.AccentIndex.ShouldBe(accentIndex);
    }

    [Fact]
    public void SelectedFolderIds_ShouldBeModifiable()
    {
        // Arrange
        var account = new OneDriveAccount();
        var folderId = "folder-123";

        // Act
        account.SelectedFolderIds.Add(folderId);

        // Assert
        account.SelectedFolderIds.ShouldContain(folderId);
    }

    [Fact]
    public void DeltaLink_ShouldBeSettable()
    {
        // Arrange
        var account = new OneDriveAccount();
        var deltaLink = "https://graph.microsoft.com/v1.0/drives/abc123/root/delta?token=abc";

        // Act
        account.DeltaLink = deltaLink;

        // Assert
        account.DeltaLink.ShouldBe(deltaLink);
    }

    [Fact]
    public void LastSyncedAt_ShouldBeSettable()
    {
        // Arrange
        var account = new OneDriveAccount();
        var lastSyncedAt = DateTimeOffset.UtcNow;

        // Act
        account.LastSyncedAt = lastSyncedAt;

        // Assert
        account.LastSyncedAt.ShouldBe(lastSyncedAt);
    }

    [Fact]
    public void QuotaTotal_ShouldBeSettable()
    {
        // Arrange
        var account = new OneDriveAccount();
        var quotaTotal = 1_099_511_627_776L; // 1 TB

        // Act
        account.QuotaTotal = quotaTotal;

        // Assert
        account.QuotaTotal.ShouldBe(quotaTotal);
    }

    [Fact]
    public void QuotaUsed_ShouldBeSettable()
    {
        // Arrange
        var account = new OneDriveAccount();
        var quotaUsed = 549_755_813_888L; // 512 GB

        // Act
        account.QuotaUsed = quotaUsed;

        // Assert
        account.QuotaUsed.ShouldBe(quotaUsed);
    }

    [Fact]
    public void IsActive_ShouldBeSettable()
    {
        // Arrange
        var account = new OneDriveAccount();

        // Act
        account.IsActive = true;

        // Assert
        account.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void FolderNames_ShouldBeModifiable()
    {
        // Arrange
        var account = new OneDriveAccount();
        var folderId = "folder-123";
        var folderName = "Documents";

        // Act
        account.FolderNames[folderId] = folderName;

        // Assert
        account.FolderNames[folderId].ShouldBe(folderName);
    }

    [Fact]
    public void LocalSyncPath_ShouldBeSettable()
    {
        // Arrange
        var account = new OneDriveAccount();
        var localSyncPath = "/home/jason/OneDrive";

        // Act
        account.LocalSyncPath = localSyncPath;

        // Assert
        account.LocalSyncPath.ShouldBe(localSyncPath);
    }

    [Fact]
    public void ConflictPolicy_ShouldBeSettable()
    {
        // Arrange
        var account = new OneDriveAccount();

        // Act
        account.ConflictPolicy = ConflictPolicy.LastWriteWins;

        // Assert
        account.ConflictPolicy.ShouldBe(ConflictPolicy.LastWriteWins);
    }

    [Theory]
    [InlineData(ConflictPolicy.Ignore)]
    [InlineData(ConflictPolicy.KeepBoth)]
    [InlineData(ConflictPolicy.LastWriteWins)]
    [InlineData(ConflictPolicy.LocalWins)]
    public void ConflictPolicy_ShouldSupportMultiplePolicies(ConflictPolicy policy)
    {
        // Arrange
        var account = new OneDriveAccount();

        // Act
        account.ConflictPolicy = policy;

        // Assert
        account.ConflictPolicy.ShouldBe(policy);
    }

    [Fact]
    public void MultipleProperties_ShouldMaintainState()
    {
        // Arrange
        var account = new OneDriveAccount();
        var displayName = "Jason Smith";
        var email = "jason@outlook.com";
        var accentIndex = 2;
        var quotaTotal = 1_099_511_627_776L;
        var quotaUsed = 549_755_813_888L;

        // Act
        account.DisplayName = displayName;
        account.Email = email;
        account.AccentIndex = accentIndex;
        account.QuotaTotal = quotaTotal;
        account.QuotaUsed = quotaUsed;
        account.IsActive = true;

        // Assert
        account.DisplayName.ShouldBe(displayName);
        account.Email.ShouldBe(email);
        account.AccentIndex.ShouldBe(accentIndex);
        account.QuotaTotal.ShouldBe(quotaTotal);
        account.QuotaUsed.ShouldBe(quotaUsed);
        account.IsActive.ShouldBeTrue();
    }
}
