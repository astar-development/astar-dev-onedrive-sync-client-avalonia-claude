namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services;

using AStar.Dev.OneDrive.Sync.Client.Services;
using System.Globalization;

public class ThemeServiceTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithSystemTheme()
    {
        // Act
        var service = new ThemeService();

        // Assert
        service.CurrentTheme.ShouldBe(AppTheme.System);
    }

    [Fact]
    public void Apply_WithLightTheme_ShouldChangeCurrentTheme()
    {
        // Arrange
        var service = new ThemeService();

        // Act
        service.Apply(AppTheme.Light);

        // Assert
        service.CurrentTheme.ShouldBe(AppTheme.Light);
    }

    [Fact]
    public void Apply_WithDarkTheme_ShouldChangeCurrentTheme()
    {
        // Arrange
        var service = new ThemeService();

        // Act
        service.Apply(AppTheme.Dark);

        // Assert
        service.CurrentTheme.ShouldBe(AppTheme.Dark);
    }

    [Fact]
    public void Apply_WithSystemTheme_ShouldChangeCurrentTheme()
    {
        // Arrange
        var service = new ThemeService();

        // Act
        service.Apply(AppTheme.System);

        // Assert
        service.CurrentTheme.ShouldBe(AppTheme.System);
    }

    [Fact]
    public void Apply_ShouldRaiseThemeChangedEvent()
    {
        // Arrange
        var service = new ThemeService();
        var eventRaised = false;
        AppTheme? raisedTheme = null;

        service.ThemeChanged += (s, theme) =>
        {
            eventRaised = true;
            raisedTheme = theme;
        };

        // Act
        service.Apply(AppTheme.Dark);

        // Assert
        eventRaised.ShouldBeTrue();
        raisedTheme.ShouldBe(AppTheme.Dark);
    }

    [Theory]
    [InlineData(AppTheme.Light)]
    [InlineData(AppTheme.Dark)]
    [InlineData(AppTheme.System)]
    public void Apply_WithAnyTheme_ShouldRaiseEvent(AppTheme theme)
    {
        // Arrange
        var service = new ThemeService();
        var eventRaised = false;

        service.ThemeChanged += (s, t) => eventRaised = true;

        // Act
        service.Apply(theme);

        // Assert
        eventRaised.ShouldBeTrue();
    }

    [Fact]
    public void Apply_MultipleTimesWithSameTheme_ShouldRaiseEventEachTime()
    {
        // Arrange
        var service = new ThemeService();
        var eventCount = 0;

        service.ThemeChanged += (s, t) => eventCount++;

        // Act
        service.Apply(AppTheme.Dark);
        service.Apply(AppTheme.Dark);
        service.Apply(AppTheme.Dark);

        // Assert
        eventCount.ShouldBe(3);
    }

    [Fact]
    public void Apply_AlternatingThemes_ShouldUpdateCurrentThemeCorrectly()
    {
        // Arrange
        var service = new ThemeService();

        // Act
        service.Apply(AppTheme.Light);
        service.CurrentTheme.ShouldBe(AppTheme.Light);

        service.Apply(AppTheme.Dark);
        service.CurrentTheme.ShouldBe(AppTheme.Dark);

        service.Apply(AppTheme.Light);

        // Assert
        service.CurrentTheme.ShouldBe(AppTheme.Light);
    }

    [Fact]
    public void ThemeService_ShouldBeDisposable()
    {
        // Act
        var service = new ThemeService();

        // Assert
        service.ShouldBeAssignableTo<IDisposable>();
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var service = new ThemeService();

        // Act & Assert
        service.Dispose(); // Should not throw
    }

    [Fact]
    public void Apply_AfterDispose_BehaviorIsUndefined()
    {
        // Arrange
        var service = new ThemeService();
        service.Dispose();

        // Act & Assert - After dispose, Apply may or may not work, just ensure no crash
        try
        {
            service.Apply(AppTheme.Light);
        }
        catch(ObjectDisposedException)
        {
            // Expected
        }
    }
}
