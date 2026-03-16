using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.X11;
using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Services;
using AStar.Dev.OneDrive.Sync.Client.Services.Auth;
using AStar.Dev.OneDrive.Sync.Client.Services.Graph;
using AStar.Dev.OneDrive.Sync.Client.Services.Localization;
using AStar.Dev.OneDrive.Sync.Client.Services.Settings;
using AStar.Dev.OneDrive.Sync.Client.Services.Startup;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;
using AStar.Dev.OneDrive.Sync.Client.Views;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AStar.Dev.OneDrive.Sync.Client;

public partial class App : Application
{
    public static ILocalizationService Localisation  { get; private set; } = null!;
    public static IThemeService        Theme         { get; private set; } = null!;
    public static IAuthService         Auth          { get; private set; } = null!;
    public static IAccountRepository   Repository    { get; private set; } = null!;
    public static ISyncService         SyncService   { get; private set; } = null!;
    public static SyncScheduler        Scheduler     { get; private set; } = null!;
    public static ISettingsService     AppSettings   { get; private set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override async void OnFrameworkInitializationCompleted()
    {
        // ── Localisation ─────────────────────────────────────────────────
        var locService = new LocalizationService();
        await locService.InitialiseAsync(new CultureInfo("en-GB"));
        Localisation = locService;

        // ── Settings ─────────────────────────────────────────────────────
        var settingsService = await SettingsService.LoadAsync();
        AppSettings = settingsService;

        // ── Theme (apply saved preference) ───────────────────────────────
        var themeService = new ThemeService();
        themeService.Apply(settingsService.Current.Theme);
        Theme = themeService;

        // ── Database ─────────────────────────────────────────────────────
        var db = DbContextFactory.Create();
        await db.Database.MigrateAsync();
        var accountRepository = new AccountRepository(db);
        var syncRepository    = new SyncRepository(db);
        Repository = accountRepository;

        // ── Auth ─────────────────────────────────────────────────────────
        var tokenCache  = new TokenCacheService();
        var authService = new AuthService(tokenCache);
        Auth = authService;

        // ── Graph ─────────────────────────────────────────────────────────
        var graphService = new GraphService();

        // ── Sync ──────────────────────────────────────────────────────────
        var syncService = new SyncService(
            authService, graphService, accountRepository, syncRepository);
        var scheduler = new SyncScheduler(syncService, accountRepository);
        SyncService = syncService;
        Scheduler   = scheduler;

        // ── Startup ───────────────────────────────────────────────────────
        var startupService = new StartupService(accountRepository, authService);

        // ── Main window ──────────────────────────────────────────────────
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow(
                authService, graphService, startupService,
                syncService, scheduler, syncRepository,
                settingsService, accountRepository);

            desktop.MainWindow.Opened += (_, _) =>
                scheduler.Start(
                    TimeSpan.FromMinutes(settingsService.Current.SyncIntervalMinutes));

            desktop.Exit += async (_, _) =>
                await scheduler.DisposeAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
