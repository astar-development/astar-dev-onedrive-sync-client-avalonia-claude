namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Models;

using AStar.Dev.OneDrive.Sync.Client.Models;

public class DeltaItemTests
{
    [Fact]
    public void DeltaItem_AllPropertiesCanBeSet()
    {
        // Arrange
        var id = "file-123";
        var driveId = "drive-456";
        var name = "document.docx";
        var parentId = "folder-789";
        var isFolder = false;
        var isDeleted = false;
        var size = 2048L;
        var lastModified = DateTimeOffset.UtcNow.AddHours(-1);
        var downloadUrl = "https://graph.microsoft.com/v1.0/drives/abc/items/xyz/content";
        var relativePath = "Documents/document.docx";

        // Act
        var item = new DeltaItem(
            id, driveId, name, parentId, isFolder, isDeleted, size,
            lastModified, downloadUrl, relativePath);

        // Assert
        item.Id.ShouldBe(id);
        item.DriveId.ShouldBe(driveId);
        item.Name.ShouldBe(name);
        item.ParentId.ShouldBe(parentId);
        item.IsFolder.ShouldBe(isFolder);
        item.IsDeleted.ShouldBe(isDeleted);
        item.Size.ShouldBe(size);
        item.LastModified.ShouldBe(lastModified);
        item.DownloadUrl.ShouldBe(downloadUrl);
        item.RelativePath.ShouldBe(relativePath);
    }

    [Fact]
    public void DeltaItem_CanBeCreated_WithoutRelativePath()
    {
        // Arrange
        var id = "file-123";
        var driveId = "drive-456";
        var name = "document.docx";
        var parentId = "folder-789";

        // Act
        var item = new DeltaItem(id, driveId, name, parentId, false, false, 2048, DateTimeOffset.UtcNow, "url");

        // Assert
        item.RelativePath.ShouldBeNull();
    }

    [Fact]
    public void DeltaItem_DeletedItem_ShouldHaveIsDeletedTrue()
    {
        // Arrange & Act
        var item = new DeltaItem("id", "drive", "name", "parent", false, true, 0, null, null);

        // Assert
        item.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public void DeltaItem_Folder_ShouldHaveIsFolderTrue()
    {
        // Arrange & Act
        var item = new DeltaItem("id", "drive", "Documents", "parent", true, false, 0, null, null);

        // Assert
        item.IsFolder.ShouldBeTrue();
    }

    [Fact]
    public void DeltaItem_File_ShouldHaveIsFolderFalse()
    {
        // Arrange & Act
        var item = new DeltaItem("id", "drive", "file.txt", "parent", false, false, 1024, DateTimeOffset.UtcNow, "url");

        // Assert
        item.IsFolder.ShouldBeFalse();
    }

    [Fact]
    public void DeltaItem_ZeroSize_IsValid()
    {
        // Arrange & Act
        var item = new DeltaItem("id", "drive", "empty.txt", "parent", false, false, 0, DateTimeOffset.UtcNow, "url");

        // Assert
        item.Size.ShouldBe(0);
    }

    [Fact]
    public void DeltaItem_LargeSize_IsValid()
    {
        // Arrange
        var largeSize = 1_099_511_627_776L; // 1TB

        // Act
        var item = new DeltaItem("id", "drive", "large.iso", "parent", false, false, largeSize, DateTimeOffset.UtcNow, "url");

        // Assert
        item.Size.ShouldBe(largeSize);
    }

    [Fact]
    public void DeltaItem_WithoutLastModified_ShouldBeNull()
    {
        // Arrange & Act
        var item = new DeltaItem("id", "drive", "name", "parent", false, false, 0, null, "url");

        // Assert
        item.LastModified.ShouldBeNull();
    }

    [Fact]
    public void DeltaItem_IsRecord_ShouldSupportValueEquality()
    {
        // Arrange
        var item1 = new DeltaItem("id", "drive", "name", "parent", false, false, 100, DateTimeOffset.UtcNow, "url");
        var item2 = new DeltaItem("id", "drive", "name", "parent", false, false, 100, item1.LastModified, "url");

        // Act & Assert
        item1.ShouldBe(item2);
    }

    [Fact]
    public void DeltaItem_DeletedFile_ShouldDifferFromNonDeleted()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var active = new DeltaItem("id", "drive", "name", "parent", false, false, 100, timestamp, "url");
        var deleted = new DeltaItem("id", "drive", "name", "parent", false, true, 100, timestamp, "url");

        // Act & Assert
        active.ShouldNotBe(deleted);
    }
}

public class FolderTreeNodeTests
{
    [Fact]
    public void FolderTreeNode_CanBeCreatedWithAllProperties()
    {
        // Arrange
        var id = "folder-123";
        var name = "Documents";
        var parentId = "folder-456";
        var accountId = "account-789";

        // Act
        var node = new FolderTreeNode(id, name, parentId, accountId);

        // Assert
        node.Id.ShouldBe(id);
        node.Name.ShouldBe(name);
        node.ParentId.ShouldBe(parentId);
        node.AccountId.ShouldBe(accountId);
    }

    [Fact]
    public void FolderTreeNode_DefaultSyncState_ShouldBeExcluded()
    {
        // Arrange & Act
        var node = new FolderTreeNode("id", "Docs", "parent", "account");

        // Assert
        node.SyncState.ShouldBe(FolderSyncState.Excluded);
    }

    [Fact]
    public void FolderTreeNode_DefaultHasChildren_ShouldBeTrue()
    {
        // Arrange & Act
        var node = new FolderTreeNode("id", "Docs", "parent", "account");

        // Assert
        node.HasChildren.ShouldBeTrue();
    }

    [Fact]
    public void FolderTreeNode_CanBeCreatedWithCustomState()
    {
        // Arrange & Act
        var node = new FolderTreeNode("id", "Docs", "parent", "account", FolderSyncState.Included);

        // Assert
        node.SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public void FolderTreeNode_CanBeCreatedWithNoChildren()
    {
        // Arrange & Act
        var node = new FolderTreeNode("id", "Docs", "parent", "account",
            FolderSyncState.Excluded, HasChildren: false);

        // Assert
        node.HasChildren.ShouldBeFalse();
    }

    [Theory]
    [InlineData(FolderSyncState.Excluded)]
    [InlineData(FolderSyncState.Included)]
    [InlineData(FolderSyncState.Partial)]
    [InlineData(FolderSyncState.Syncing)]
    [InlineData(FolderSyncState.Synced)]
    [InlineData(FolderSyncState.Conflict)]
    [InlineData(FolderSyncState.Error)]
    public void FolderTreeNode_ShouldSupportAllSyncStates(FolderSyncState state)
    {
        // Act
        var node = new FolderTreeNode("id", "Docs", "parent", "account", state);

        // Assert
        node.SyncState.ShouldBe(state);
    }

    [Fact]
    public void FolderTreeNode_RootFolder_CanHaveNullParentId()
    {
        // Arrange & Act
        var node = new FolderTreeNode("root-id", "OneDrive", null, "account");

        // Assert
        node.ParentId.ShouldBeNull();
    }

    [Fact]
    public void FolderTreeNode_IsRecord_ShouldSupportValueEquality()
    {
        // Arrange
        var node1 = new FolderTreeNode("id", "Docs", "parent", "account", FolderSyncState.Included);
        var node2 = new FolderTreeNode("id", "Docs", "parent", "account", FolderSyncState.Included);

        // Act & Assert
        node1.ShouldBe(node2);
    }

    [Fact]
    public void FolderTreeNode_DifferentStateShouldNotBeEqual()
    {
        // Arrange
        var node1 = new FolderTreeNode("id", "Docs", "parent", "account", FolderSyncState.Included);
        var node2 = new FolderTreeNode("id", "Docs", "parent", "account", FolderSyncState.Excluded);

        // Act & Assert
        node1.ShouldNotBe(node2);
    }

    [Fact]
    public void FolderTreeNode_NestedFolderStructure_ShouldMaintainHierarchy()
    {
        // Arrange
        var root = new FolderTreeNode("root-id", "OneDrive", null, "account");
        var child = new FolderTreeNode("child-id", "Documents", "root-id", "account");
        var grandchild = new FolderTreeNode("grandchild-id", "Projects", "child-id", "account");

        // Act & Assert
        root.ParentId.ShouldBeNull();
        child.ParentId.ShouldBe("root-id");
        grandchild.ParentId.ShouldBe("child-id");
    }
}

public class SyncConflictTests
{
    [Fact]
    public void SyncConflict_NewInstance_ShouldHaveUniqueId()
    {
        // Act
        var conflict1 = new SyncConflict();
        var conflict2 = new SyncConflict();

        // Assert
        conflict1.Id.ShouldNotBe(conflict2.Id);
    }

    [Fact]
    public void SyncConflict_NewInstance_ShouldHavePendingState()
    {
        // Act
        var conflict = new SyncConflict();

        // Assert
        conflict.State.ShouldBe(ConflictState.Pending);
    }

    [Fact]
    public void SyncConflict_NewInstance_ShouldHaveNoResolution()
    {
        // Act
        var conflict = new SyncConflict();

        // Assert
        conflict.Resolution.ShouldBeNull();
    }

    [Fact]
    public void SyncConflict_NewInstance_ShouldHaveCurrentDetectedTime()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow.AddMilliseconds(-100);

        // Act
        var conflict = new SyncConflict();

        // Assert
        conflict.DetectedAt.ShouldBeGreaterThanOrEqualTo(before);
        conflict.DetectedAt.ShouldBeLessThanOrEqualTo(DateTimeOffset.UtcNow.AddMilliseconds(100));
    }

    [Fact]
    public void SyncConflict_NewInstance_ShouldNotBeResolved()
    {
        // Act
        var conflict = new SyncConflict();

        // Assert
        conflict.ResolvedAt.ShouldBeNull();
    }

    [Fact]
    public void SyncConflict_CanSetPropertiesViaInit()
    {
        // Arrange
        var id = Guid.NewGuid();
        var accountId = "account-123";
        var folderId = "folder-456";
        var remoteItemId = "item-789";
        var relativePath = "Documents/report.pdf";
        var localPath = "/home/jason/Documents/report.pdf";
        var now = DateTimeOffset.UtcNow;

        // Act
        var conflict = new SyncConflict
        {
            Id = id,
            AccountId = accountId,
            FolderId = folderId,
            RemoteItemId = remoteItemId,
            RelativePath = relativePath,
            LocalPath = localPath,
            LocalModified = now.AddHours(-1),
            RemoteModified = now,
            LocalSize = 1024,
            RemoteSize = 2048,
            DetectedAt = now
        };

        // Assert
        conflict.Id.ShouldBe(id);
        conflict.AccountId.ShouldBe(accountId);
        conflict.FolderId.ShouldBe(folderId);
        conflict.RemoteItemId.ShouldBe(remoteItemId);
        conflict.RelativePath.ShouldBe(relativePath);
        conflict.LocalPath.ShouldBe(localPath);
        conflict.LocalSize.ShouldBe(1024);
        conflict.RemoteSize.ShouldBe(2048);
    }

    [Fact]
    public void SyncConflict_CanBeResolved()
    {
        // Arrange
        var conflict = new SyncConflict { State = ConflictState.Pending };

        // Act
        conflict.State = ConflictState.Resolved;
        conflict.Resolution = ConflictPolicy.LastWriteWins;
        conflict.ResolvedAt = DateTimeOffset.UtcNow;

        // Assert
        conflict.State.ShouldBe(ConflictState.Resolved);
        conflict.Resolution.ShouldBe(ConflictPolicy.LastWriteWins);
        conflict.ResolvedAt.ShouldNotBeNull();
    }

    [Fact]
    public void SyncConflict_CanBeSkipped()
    {
        // Arrange
        var conflict = new SyncConflict { State = ConflictState.Pending };

        // Act
        conflict.State = ConflictState.Skipped;

        // Assert
        conflict.State.ShouldBe(ConflictState.Skipped);
    }

    [Theory]
    [InlineData(ConflictState.Pending)]
    [InlineData(ConflictState.Resolved)]
    [InlineData(ConflictState.Skipped)]
    public void SyncConflict_ShouldSupportAllStates(ConflictState state)
    {
        // Act
        var conflict = new SyncConflict { State = state };

        // Assert
        conflict.State.ShouldBe(state);
    }

    [Fact]
    public void SyncConflict_LocalSizeCanBeLarge()
    {
        // Arrange
        var largeSize = 1_073_741_824L; // 1GB

        // Act
        var conflict = new SyncConflict { LocalSize = largeSize };

        // Assert
        conflict.LocalSize.ShouldBe(largeSize);
    }

    [Fact]
    public void SyncConflict_CanTrackVersionConflict()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;

        // Act
        var conflict = new SyncConflict
        {
            LocalModified = now.AddHours(-2),
            RemoteModified = now,
            LocalSize = 1024,
            RemoteSize = 2048
        };

        // Assert
        conflict.LocalModified.ShouldBeLessThan(conflict.RemoteModified);
        conflict.LocalSize.ShouldNotBe(conflict.RemoteSize);
    }

    [Fact]
    public void SyncConflict_WithoutResolution_ShouldHaveNullResolution()
    {
        // Arrange
        var conflict = new SyncConflict { State = ConflictState.Pending };

        // Assert
        conflict.Resolution.ShouldBeNull();
    }

    [Fact]
    public void SyncConflict_WithResolution_ShouldHaveNonNullResolution()
    {
        // Arrange
        var conflict = new SyncConflict
        {
            State = ConflictState.Resolved,
            Resolution = ConflictPolicy.LocalWins
        };

        // Assert
        conflict.Resolution.ShouldNotBeNull();
        conflict.Resolution.ShouldBe(ConflictPolicy.LocalWins);
    }
}
