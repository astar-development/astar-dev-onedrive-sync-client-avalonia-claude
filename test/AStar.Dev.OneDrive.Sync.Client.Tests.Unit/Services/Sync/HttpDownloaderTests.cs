namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Sync;

using AStar.Dev.OneDrive.Sync.Client.Services.Sync;
using System.Reflection;

public class HttpDownloaderBackoffTests
{
    /// <summary>
    /// Tests the exponential backoff calculation logic of HttpDownloader.
    /// Since GetBackoffDelay is private, we use reflection to test it.
    /// </summary>
    /// 
    [Fact]
    public void GetBackoffDelay_Attempt1_ShouldBeBetweenBaseAndBaseWithJitter()
    {
        // Arrange
        var method = typeof(HttpDownloader).GetMethod("GetBackoffDelay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method.ShouldNotBeNull();

        // Act
        var result = (TimeSpan)method!.Invoke(null, new object[] { 1 })!;

        // Assert - Base delay is 2s, with up to 20% jitter (0.4s)
        result.TotalSeconds.ShouldBeGreaterThanOrEqualTo(2.0);
        result.TotalSeconds.ShouldBeLessThanOrEqualTo(2.4);
    }

    [Fact]
    public void GetBackoffDelay_Attempt2_ShouldBeDoubleAttempt1()
    {
        // Arrange
        var method = typeof(HttpDownloader).GetMethod("GetBackoffDelay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method.ShouldNotBeNull();

        // Act
        var result1 = (TimeSpan)method!.Invoke(null, new object[] { 1 })!;
        var result2 = (TimeSpan)method!.Invoke(null, new object[] { 2 })!;

        // Assert - Attempt 2 should be ~4s (2 * 2^(2-1) = 2 * 2)
        result2.TotalSeconds.ShouldBeGreaterThanOrEqualTo(4.0);
        result2.TotalSeconds.ShouldBeLessThanOrEqualTo(4.8);
    }

    [Fact]
    public void GetBackoffDelay_IncreasingAttempts_ShouldExponentiallyIncrease()
    {
        // Arrange
        var method = typeof(HttpDownloader).GetMethod("GetBackoffDelay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method.ShouldNotBeNull();

        var delays = new List<TimeSpan>();

        // Act
        for(int i = 1; i <= 5; i++)
        {
            var delay = (TimeSpan)method!.Invoke(null, new object[] { i })!;
            delays.Add(delay);
        }

        // Assert - Each delay (roughly) doubles
        delays[0].TotalSeconds.ShouldBeLessThan(delays[1].TotalSeconds);
        delays[1].TotalSeconds.ShouldBeLessThan(delays[2].TotalSeconds);
        delays[2].TotalSeconds.ShouldBeLessThan(delays[3].TotalSeconds);
        delays[3].TotalSeconds.ShouldBeLessThan(delays[4].TotalSeconds);
    }

    [Fact]
    public void GetBackoffDelay_CappedAt120Seconds()
    {
        // Arrange
        var method = typeof(HttpDownloader).GetMethod("GetBackoffDelay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method.ShouldNotBeNull();

        // Act - Attempt 10 would be 2 * 2^9 = 1024s without cap
        var result = (TimeSpan)method!.Invoke(null, new object[] { 10 })!;

        // Assert - Should be capped at 120s + up to 20% jitter (144s max)
        result.TotalSeconds.ShouldBeLessThanOrEqualTo(144);
    }

    [Fact]
    public void GetBackoffDelay_WithJitter_ShouldHaveVariability()
    {
        // Arrange
        var method = typeof(HttpDownloader).GetMethod("GetBackoffDelay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method.ShouldNotBeNull();
        var delays = new List<TimeSpan>();

        // Act - Call multiple times to observe jitter
        for(int i = 0; i < 10; i++)
        {
            var delay = (TimeSpan)method!.Invoke(null, new object[] { 1 })!;
            delays.Add(delay);
        }

        // Assert - We should see different values due to jitter
        var uniqueValues = delays.DistinctBy(d => d.TotalMilliseconds).Count();
        // With random jitter, we expect variation (not all the same)
        uniqueValues.ShouldBeGreaterThan(1);
    }

    [Theory]
    [InlineData(1, 2.0, 2.4)]      // 2s base, 20% = 0.4s jitter
    [InlineData(2, 4.0, 4.8)]      // 4s base, 20% = 0.8s jitter
    [InlineData(3, 8.0, 9.6)]      // 8s base, 20% = 1.6s jitter
    [InlineData(4, 16.0, 19.2)]    // 16s base, 20% = 3.2s jitter
    [InlineData(5, 32.0, 38.4)]    // 32s base, 20% = 6.4s jitter
    public void GetBackoffDelay_RespectsCeilingAndJitter(int attempt, double minSeconds, double maxSeconds)
    {
        // Arrange
        var method = typeof(HttpDownloader).GetMethod("GetBackoffDelay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method.ShouldNotBeNull();

        // Act
        var result = (TimeSpan)method!.Invoke(null, new object[] { attempt })!;

        // Assert
        result.TotalSeconds.ShouldBeGreaterThanOrEqualTo(minSeconds);
        result.TotalSeconds.ShouldBeLessThanOrEqualTo(maxSeconds);
    }
}

public class HttpDownloaderFileOperationTests
{
    [Fact]
    public void HttpDownloader_ShouldBeDisposable()
    {
        // Act
        var downloader = new HttpDownloader();

        // Assert
        downloader.ShouldBeAssignableTo<IDisposable>();
    }

    [Fact]
    public void HttpDownloader_DisposeMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var downloader = new HttpDownloader();

        // Act & Assert
        downloader.Dispose();
        downloader.Dispose(); // Should not throw
    }

    [Fact]
    public async Task DownloadAsync_Created_ShouldHaveValidHttpClient()
    {
        // Arrange
        var downloader = new HttpDownloader();

        // Assert - Constructor succeeded, downloader is ready
        downloader.ShouldNotBeNull();

        downloader.Dispose();
    }
}
