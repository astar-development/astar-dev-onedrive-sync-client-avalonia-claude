using System.Threading.Tasks;
using AStar.Dev.OneDrive.Sync.Client.Services.Auth;
using AStar.Dev.OneDrive.Sync.Client.Services.Graph;
using AStar.Dev.OneDrive.Sync.Client.Services.Startup;
using AStar.Dev.OneDrive.Sync.Client.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.ViewModels;

public sealed partial class MainWindowViewModel(
    IAuthService    authService,
    IGraphService   graphService,
    IStartupService startupService) : ObservableObject
{
    // ── Navigation ────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDashboardActive))]
    [NotifyPropertyChangedFor(nameof(IsFilesActive))]
    [NotifyPropertyChangedFor(nameof(IsActivityActive))]
    [NotifyPropertyChangedFor(nameof(IsAccountsActive))]
    [NotifyPropertyChangedFor(nameof(IsSettingsActive))]
    [NotifyPropertyChangedFor(nameof(ActiveView))]
    private NavSection _activeSection = NavSection.Dashboard;

    public bool IsDashboardActive => ActiveSection == NavSection.Dashboard;
    public bool IsFilesActive      => ActiveSection == NavSection.Files;
    public bool IsActivityActive   => ActiveSection == NavSection.Activity;
    public bool IsAccountsActive   => ActiveSection == NavSection.Accounts;
    public bool IsSettingsActive   => ActiveSection == NavSection.Settings;

    [RelayCommand]
    private void Navigate(NavSection section) => ActiveSection = section;

    // ── Active view ───────────────────────────────────────────────────────

    public object? ActiveView => ActiveSection switch
    {
        NavSection.Dashboard => _dashboardView ??= new DashboardView(),
        NavSection.Files     => _filesView     ??= new FilesView(),
        NavSection.Activity  => _activityView  ??= new ActivityView(),
        NavSection.Accounts  => _accountsView  ??= new AccountsView(),
        NavSection.Settings  => _settingsView  ??= new SettingsView(),
        _                    => null
    };

    private DashboardView? _dashboardView;
    private FilesView?     _filesView;
    private ActivityView?  _activityView;
    private AccountsView?  _accountsView;
    private SettingsView?  _settingsView;

    // ── Child view models ─────────────────────────────────────────────────

    public AccountsViewModel  Accounts  { get; } = new(authService, graphService,
        // repository injected via App — resolved at construction time
        App.Repository);

    public StatusBarViewModel StatusBar { get; } = new();

    // ── Startup ───────────────────────────────────────────────────────────

    /// <summary>
    /// Called once from MainWindow after DataContext is set.
    /// Restores persisted accounts without blocking the UI thread.
    /// </summary>
    public async Task InitialiseAsync()
    {
        var restored = await startupService.RestoreAccountsAsync();
        Accounts.RestoreAccounts(restored);
        SyncStatusBarToActiveAccount();

        Accounts.AccountSelected += OnAccountSelected;
        Accounts.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AccountsViewModel.ActiveAccount))
                SyncStatusBarToActiveAccount();
        };
    }

    // ── Add account (entry point from left panel button) ──────────────────

    [RelayCommand]
    private void AddAccount()
    {
        ActiveSection = NavSection.Accounts;
        Accounts.AddAccount();
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private void OnAccountSelected(object? sender, AccountCardViewModel card)
    {
        ActiveSection = NavSection.Files;
        SyncStatusBarToActiveAccount();
    }

    private void SyncStatusBarToActiveAccount()
    {
        var active = Accounts.ActiveAccount;
        if (active is null)
        {
            StatusBar.HasAccount         = false;
            StatusBar.AccountEmail       = string.Empty;
            StatusBar.AccountDisplayName = string.Empty;
            return;
        }

        StatusBar.HasAccount         = true;
        StatusBar.AccountEmail       = active.Email;
        StatusBar.AccountDisplayName = active.DisplayName;
        StatusBar.SyncState          = active.SyncState;
        StatusBar.ConflictCount      = active.ConflictCount;
        StatusBar.LastSyncText       = active.LastSyncText;
        StatusBar.IsSyncing          = active.SyncState == SyncState.Syncing;
    }
}
