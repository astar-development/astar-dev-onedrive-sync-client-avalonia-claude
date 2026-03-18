namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Sync;

using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;
public class ConflictResolverTests
{
    [Fact]
    public void Resolve_WithIgnorePolicy_ShouldReturnSkip()
    {
        // Arrange
        var localModified = DateTimeOffset.UtcNow;
        var remoteModified = DateTimeOffset.UtcNow.AddMinutes(-10);

        // Act
        var outcome = ConflictResolver.Resolve(ConflictPolicy.Ignore, localModified, remoteModified);

        // Assert
        outcome.ShouldBe(ConflictOutcome.Skip);
    }

    [Fact]
    public void Resolve_WithLocalWinsPolicy_ShouldReturnUseLocal()
    {
        // Arrange
        var localModified = DateTimeOffset.UtcNow;
        var remoteModified = DateTimeOffset.UtcNow.AddHours(-1);

        // Act
        var outcome = ConflictResolver.Resolve(ConflictPolicy.LocalWins, localModified, remoteModified);

        // Assert
        outcome.ShouldBe(ConflictOutcome.UseLocal);
    }

    [Fact]
    public void Resolve_WithRemoteWinsPolicy_ShouldReturnUseRemote()
    {
        // Arrange
        var localModified = DateTimeOffset.UtcNow;
        var remoteModified = DateTimeOffset.UtcNow.AddHours(-1);

        // Act
        var outcome = ConflictResolver.Resolve(ConflictPolicy.RemoteWins, localModified, remoteModified);

        // Assert
        outcome.ShouldBe(ConflictOutcome.UseRemote);
    }

    [Fact]
    public void Resolve_WithKeepBothPolicy_ShouldReturnKeepBoth()
    {
        // Arrange
        var localModified = DateTimeOffset.UtcNow;
        var remoteModified = DateTimeOffset.UtcNow.AddMinutes(-5);

        // Act
        var outcome = ConflictResolver.Resolve(ConflictPolicy.KeepBoth, localModified, remoteModified);

        // Assert
        outcome.ShouldBe(ConflictOutcome.KeepBoth);
    }

    [Fact]
    public void Resolve_WithLastWriteWins_LocalIsNewer_ShouldReturnUseLocal()
    {
        // Arrange
        var remoteModified = DateTimeOffset.UtcNow.AddMinutes(-10);
        var localModified = DateTimeOffset.UtcNow; // Newer

        // Act
        var outcome = ConflictResolver.Resolve(ConflictPolicy.LastWriteWins, localModified, remoteModified);

        // Assert
        outcome.ShouldBe(ConflictOutcome.UseLocal);
    }

    [Fact]
    public void Resolve_WithLastWriteWins_RemoteIsNewer_ShouldReturnUseRemote()
    {
        // Arrange
        var localModified = DateTimeOffset.UtcNow.AddMinutes(-10);
        var remoteModified = DateTimeOffset.UtcNow; // Newer

        // Act
        var outcome = ConflictResolver.Resolve(ConflictPolicy.LastWriteWins, localModified, remoteModified);

        // Assert
        outcome.ShouldBe(ConflictOutcome.UseRemote);
    }

    [Fact]
    public void Resolve_WithLastWriteWins_SameTimes_ShouldReturnUseLocal()
    {
        // Arrange - exactly the same timestamp
        var timestamp = DateTimeOffset.UtcNow;
        var localModified = timestamp;
        var remoteModified = timestamp;

        // Act
        var outcome = ConflictResolver.Resolve(ConflictPolicy.LastWriteWins, localModified, remoteModified);

        // Assert - when equal, >= returns true, so uses local
        outcome.ShouldBe(ConflictOutcome.UseLocal);
    }

    [Fact]
    public void Resolve_WithLastWriteWins_LocalJustOneSecondNewer_ShouldReturnUseLocal()
    {
        // Arrange
        var remoteModified = DateTimeOffset.UtcNow;
        var localModified = remoteModified.AddSeconds(1);

        // Act
        var outcome = ConflictResolver.Resolve(ConflictPolicy.LastWriteWins, localModified, remoteModified);

        // Assert
        outcome.ShouldBe(ConflictOutcome.UseLocal);
    }

    [Fact]
    public void Resolve_WithLastWriteWins_RemoteJustOneSecondNewer_ShouldReturnUseRemote()
    {
        // Arrange
        var localModified = DateTimeOffset.UtcNow;
        var remoteModified = localModified.AddSeconds(1);

        // Act
        var outcome = ConflictResolver.Resolve(ConflictPolicy.LastWriteWins, localModified, remoteModified);

        // Assert
        outcome.ShouldBe(ConflictOutcome.UseRemote);
    }

    [Fact]
    public void Resolve_WithLastWriteWins_OldTimestampsVsNewTimestamps_ShouldCompareCorrectly()
    {
        // Arrange - very old timestamps
        var remoteModified = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var localModified = new DateTimeOffset(2025, 12, 31, 23, 59, 59, TimeSpan.Zero);

        // Act
        var outcome = ConflictResolver.Resolve(ConflictPolicy.LastWriteWins, localModified, remoteModified);

        // Assert
        outcome.ShouldBe(ConflictOutcome.UseLocal);
    }

    [Theory]
    [InlineData(ConflictPolicy.Ignore)]
    [InlineData(ConflictPolicy.LocalWins)]
    [InlineData(ConflictPolicy.RemoteWins)]
    [InlineData(ConflictPolicy.KeepBoth)]
    [InlineData(ConflictPolicy.LastWriteWins)]
    public void Resolve_ShouldHandleAllPolicies(ConflictPolicy policy)
    {
        // Arrange
        var localModified = DateTimeOffset.UtcNow;
        var remoteModified = DateTimeOffset.UtcNow.AddMinutes(-5);

        // Act
        var outcome = ConflictResolver.Resolve(policy, localModified, remoteModified);

        // Assert
        outcome.ShouldNotBe((ConflictOutcome)999);
        outcome.ShouldBeOneOf(
            ConflictOutcome.Skip,
            ConflictOutcome.UseLocal,
            ConflictOutcome.UseRemote,
            ConflictOutcome.KeepBoth);
    }

    [Fact]
    public void MakeKeepBothName_ShouldGenerateValidFilename()
    {
        // Arrange
        var localPath = "/home/jason/Documents/report.docx";
        var localModified = new DateTimeOffset(2024, 1, 15, 14, 32, 0, TimeSpan.Zero);

        // Act
        var result = ConflictResolver.MakeKeepBothName(localPath, localModified);

        // Assert
        result.ShouldContain("report");
        result.ShouldContain("docx");
        result.ShouldContain("local");
        result.ShouldContain("2024-01-15");
        result.ShouldContain("14-32");
    }

    [Fact]
    public void MakeKeepBothName_WithPathContainingSpaces_ShouldPreserveExtension()
    {
        // Arrange
        var localPath = "/home/jason/My Documents/My Report.docx";
        var localModified = DateTimeOffset.UtcNow;

        // Act
        var result = ConflictResolver.MakeKeepBothName(localPath, localModified);

        // Assert
        result.ShouldContain("My Report");
        result.ShouldEndWith(".docx");
    }

    [Fact]
    public void MakeKeepBothName_WithFileHavingNoExtension_ShouldStillGenerateName()
    {
        // Arrange
        var localPath = "/home/jason/Documents/README";
        var localModified = DateTimeOffset.UtcNow;

        // Act
        var result = ConflictResolver.MakeKeepBothName(localPath, localModified);

        // Assert
        result.ShouldContain("README");
        result.ShouldContain("local");
    }

    [Fact]
    public void MakeKeepBothName_WithMultipleDots_ShouldOnlyRemoveLastExtension()
    {
        // Arrange
        var localPath = "/home/jason/file.tar.gz";
        var localModified = DateTimeOffset.UtcNow;

        // Act
        var result = ConflictResolver.MakeKeepBothName(localPath, localModified);

        // Assert
        result.ShouldContain("file.tar");
        result.ShouldEndWith(".gz");
    }

    [Fact]
    public void MakeKeepBothName_GeneratesIncreasinglyUniquePaths()
    {
        // Arrange
        var localPath = "/home/jason/Documents/report.docx";
        var time1 = new DateTimeOffset(2024, 1, 15, 14, 32, 0, TimeSpan.Zero);
        var time2 = new DateTimeOffset(2024, 1, 16, 14, 32, 0, TimeSpan.Zero);

        // Act
        var result1 = ConflictResolver.MakeKeepBothName(localPath, time1);
        var result2 = ConflictResolver.MakeKeepBothName(localPath, time2);

        // Assert
        result1.ShouldNotBe(result2);
        result1.ShouldContain("2024-01-15");
        result2.ShouldContain("2024-01-16");
    }

    [Fact]
    public void MakeKeepBothName_OutputPathIsOnSameDirectory()
    {
        // Arrange
        var localPath = "/home/jason/Documents/report.docx";
        var localModified = DateTimeOffset.UtcNow;

        // Act
        var result = ConflictResolver.MakeKeepBothName(localPath, localModified);

        // Assert
        var resultDir = Path.GetDirectoryName(result);
        var originalDir = Path.GetDirectoryName(localPath);
        resultDir.ShouldBe(originalDir);
    }
}
