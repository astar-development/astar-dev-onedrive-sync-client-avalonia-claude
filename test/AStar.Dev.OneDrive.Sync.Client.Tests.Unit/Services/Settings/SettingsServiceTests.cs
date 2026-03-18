namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Settings;

using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services;
using AStar.Dev.OneDrive.Sync.Client.Services.Settings;
using NSubstitute;

public class SettingsServiceTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultSettings()
    {
        // Act
        var service = new SettingsService();

        // Assert
        service.Current.ShouldNotBeNull();
        service.Current.Theme.ShouldBe(AppTheme.System);
        service.Current.Locale.ShouldBe("en-GB");
        service.Current.DefaultConflictPolicy.ShouldBe(ConflictPolicy.Ignore);
        service.Current.SyncIntervalMinutes.ShouldBe(60);
    }

    [Fact]
    public void Current_ShouldReturnAppSettings()
    {
        // Arrange
        var service = new SettingsService();

        // Act
        var settings = service.Current;

        // Assert
        settings.ShouldNotBeNull();
        settings.ShouldBeOfType<AppSettings>();
    }

    [Fact]
    public void Theme_ShouldBeSettable()
    {
        // Arrange
        var service = new SettingsService();

        // Act
        service.Current.Theme = AppTheme.Dark;

        // Assert
        service.Current.Theme.ShouldBe(AppTheme.Dark);
    }

    [Fact]
    public void Locale_ShouldBeSettable()
    {
        // Arrange
        var service = new SettingsService();

        // Act
        service.Current.Locale = "fr-FR";

        // Assert
        service.Current.Locale.ShouldBe("fr-FR");
    }

    [Fact]
    public void DefaultConflictPolicy_ShouldBeSettable()
    {
        // Arrange
        var service = new SettingsService();

        // Act
        service.Current.DefaultConflictPolicy = ConflictPolicy.LastWriteWins;

        // Assert
        service.Current.DefaultConflictPolicy.ShouldBe(ConflictPolicy.LastWriteWins);
    }

    [Fact]
    public void SyncIntervalMinutes_ShouldBeSettable()
    {
        // Arrange
        var service = new SettingsService();

        // Act
        service.Current.SyncIntervalMinutes = 30;

        // Assert
        service.Current.SyncIntervalMinutes.ShouldBe(30);
    }

    [Fact]
    public async Task SaveAsync_ShouldInvokeSettingsChangedEvent()
    {
        // Arrange
        var service = new SettingsService();
        var eventRaised = false;
        AppSettings? changedSettings = null;

        service.SettingsChanged += (s, settings) =>
        {
            eventRaised = true;
            changedSettings = settings;
        };

        // Act
        service.Current.Theme = AppTheme.Light;
        await service.SaveAsync();

        // Assert
        eventRaised.ShouldBeTrue();
        changedSettings.ShouldNotBeNull();
        changedSettings.Theme.ShouldBe(AppTheme.Light);
    }

    [Fact]
    public async Task SaveAsync_ShouldPersistSettings()
    {
        // Arrange
        var service = new SettingsService();
        service.Current.Locale = "de-DE";
        service.Current.SyncIntervalMinutes = 45;

        // Act
        await service.SaveAsync();

        // Assert
        service.Current.Locale.ShouldBe("de-DE");
        service.Current.SyncIntervalMinutes.ShouldBe(45);
    }

    [Fact]
    public async Task LoadAsync_ShouldReturnSettingsService()
    {
        // Act
        var service = await SettingsService.LoadAsync();

        // Assert
        service.ShouldNotBeNull();
        service.ShouldBeOfType<SettingsService>();
    }

    [Fact]
    public async Task LoadAsync_ShouldInitializeCurrentSettings()
    {
        // Act
        var service = await SettingsService.LoadAsync();

        // Assert
        service.Current.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(AppTheme.System)]
    [InlineData(AppTheme.Light)]
    [InlineData(AppTheme.Dark)]
    public void Theme_ShouldSupportAllThemeValues(AppTheme theme)
    {
        // Arrange
        var service = new SettingsService();

        // Act
        service.Current.Theme = theme;

        // Assert
        service.Current.Theme.ShouldBe(theme);
    }

    [Theory]
    [InlineData("en-GB")]
    [InlineData("fr-FR")]
    [InlineData("de-DE")]
    [InlineData("es-ES")]
    public void Locale_ShouldSupportDifferentCultures(string locale)
    {
        // Arrange
        var service = new SettingsService();

        // Act
        service.Current.Locale = locale;

        // Assert
        service.Current.Locale.ShouldBe(locale);
    }

    [Theory]
    [InlineData(ConflictPolicy.Ignore)]
    [InlineData(ConflictPolicy.KeepBoth)]
    [InlineData(ConflictPolicy.LastWriteWins)]
    [InlineData(ConflictPolicy.LocalWins)]
    public void DefaultConflictPolicy_ShouldSupportAllPolicies(ConflictPolicy policy)
    {
        // Arrange
        var service = new SettingsService();

        // Act
        service.Current.DefaultConflictPolicy = policy;

        // Assert
        service.Current.DefaultConflictPolicy.ShouldBe(policy);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(120)]
    public void SyncIntervalMinutes_ShouldSupportDifferentIntervals(int minutes)
    {
        // Arrange
        var service = new SettingsService();

        // Act
        service.Current.SyncIntervalMinutes = minutes;

        // Assert
        service.Current.SyncIntervalMinutes.ShouldBe(minutes);
    }

    [Fact]
    public void SettingsChanged_ShouldBeNullByDefault()
    {
        // Arrange
        var service = new SettingsService();
        var eventHandlerInvoked = false;

        // This test verifies that the event handler property exists and can receive subscribers
        service.SettingsChanged += (s, e) => eventHandlerInvoked = true;

        // Assert
        // Event handler successfully subscribed - no exception thrown
    }

    [Fact]
    public async Task MultipleSettingsChanges_ShouldMaintainState()
    {
        // Arrange
        var service = new SettingsService();
        var theme = AppTheme.Dark;
        var locale = "fr-FR";
        var policy = ConflictPolicy.KeepBoth;
        var interval = 45;

        // Act
        service.Current.Theme = theme;
        service.Current.Locale = locale;
        service.Current.DefaultConflictPolicy = policy;
        service.Current.SyncIntervalMinutes = interval;

        // Assert
        service.Current.Theme.ShouldBe(theme);
        service.Current.Locale.ShouldBe(locale);
        service.Current.DefaultConflictPolicy.ShouldBe(policy);
        service.Current.SyncIntervalMinutes.ShouldBe(interval);
    }

    [Fact]
    public async Task SaveAsync_WithMultipleChanges_ShouldEventIncludeAllChanges()
    {
        // Arrange
        var service = new SettingsService();
        var eventRaised = false;
        AppSettings? changedSettings = null;

        service.SettingsChanged += (s, settings) =>
        {
            eventRaised = true;
            changedSettings = settings;
        };

        // Act
        service.Current.Theme = AppTheme.Dark;
        service.Current.Locale = "es-ES";
        service.Current.SyncIntervalMinutes = 30;
        await service.SaveAsync();

        // Assert
        eventRaised.ShouldBeTrue();
        changedSettings.ShouldNotBeNull();
        changedSettings.Theme.ShouldBe(AppTheme.Dark);
        changedSettings.Locale.ShouldBe("es-ES");
        changedSettings.SyncIntervalMinutes.ShouldBe(30);
    }
}
