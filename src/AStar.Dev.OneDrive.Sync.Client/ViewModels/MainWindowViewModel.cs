using AStar.Dev.OneDrive.Sync.Client.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    // ── Navigation ────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveView))]  // add to the existing list on _activeSection
    [NotifyPropertyChangedFor(nameof(IsDashboardActive))]
    [NotifyPropertyChangedFor(nameof(IsFilesActive))]
    [NotifyPropertyChangedFor(nameof(IsActivityActive))]
    [NotifyPropertyChangedFor(nameof(IsAccountsActive))]
    [NotifyPropertyChangedFor(nameof(IsSettingsActive))]
    private NavSection _activeSection = NavSection.Dashboard;

    public bool IsDashboardActive => ActiveSection == NavSection.Dashboard;
    public bool IsFilesActive      => ActiveSection == NavSection.Files;
    public bool IsActivityActive   => ActiveSection == NavSection.Activity;
    public bool IsAccountsActive   => ActiveSection == NavSection.Accounts;
    public bool IsSettingsActive   => ActiveSection == NavSection.Settings;

    [RelayCommand]
    private void Navigate(NavSection section) => ActiveSection = section;

    // ── Child view models ─────────────────────────────────────────────────

    public AccountsViewModel Accounts { get; } = new();
    public StatusBarViewModel StatusBar { get; } = new();

    // ── Construction ──────────────────────────────────────────────────────

    [RelayCommand]
    private void AddAccount()
    {
        //Accounts.AddAccountCommand.Execute(null);

    ActiveSection = NavSection.Accounts;
    Accounts.AddAccount();
    }

    public MainWindowViewModel()
    {
        Accounts.AccountSelected += OnAccountSelected;
        Accounts.AccountSelected += OnAccountSelected;
        Accounts.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AccountsViewModel.ActiveAccount))
                SyncStatusBarToActiveAccount();
        };
    }
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
    // ── Private helpers ───────────────────────────────────────────────────

    private void OnAccountSelected(object? sender, AccountCardViewModel card)
    {
        // Navigate to Files when an account card is clicked
        ActiveSection = NavSection.Files;
        SyncStatusBarToActiveAccount();
    }

    private void SyncStatusBarToActiveAccount()
    {
        var active = Accounts.ActiveAccount;
        if (active is null)
        {
            StatusBar.HasAccount        = false;
            StatusBar.AccountEmail      = string.Empty;
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
