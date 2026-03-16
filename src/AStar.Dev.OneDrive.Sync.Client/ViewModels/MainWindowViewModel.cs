using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Auth;
using AStar.Dev.OneDrive.Sync.Client.Services.Graph;
using AStar.Dev.OneDrive.Sync.Client.Services.Startup;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;
using AStar.Dev.OneDrive.Sync.Client.Views;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.ViewModels;

public sealed partial class MainWindowViewModel(
    IAuthService    authService,
    IGraphService   graphService,
    IStartupService startupService,
    ISyncService    syncService,
    SyncScheduler   scheduler,
    ISyncRepository syncRepository) : ObservableObject
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
        NavSection.Dashboard => _dashboardView  ??= new DashboardView(),
        NavSection.Files     => FilesViewInstance,
        NavSection.Activity  => ActivityViewInstance,
        NavSection.Accounts  => AccountsViewInstance,
        NavSection.Settings  => _settingsView   ??= new SettingsView(),
        _                    => null
    };

    private FilesView    FilesViewInstance    =>
        _filesView    ??= new FilesView    { DataContext = Files };
    private ActivityView ActivityViewInstance =>
        _activityView ??= new ActivityView { DataContext = Activity };
    private AccountsView AccountsViewInstance =>
        _accountsView ??= new AccountsView { DataContext = this };

    private DashboardView? _dashboardView;
    private FilesView?     _filesView;
    private ActivityView?  _activityView;
    private AccountsView?  _accountsView;
    private SettingsView?  _settingsView;

    // ── Child view models ─────────────────────────────────────────────────

    public AccountsViewModel  Accounts  { get; } =
        new(authService, graphService, App.Repository);

    public FilesViewModel     Files     { get; } =
        new(authService, graphService, App.Repository);

    public ActivityViewModel  Activity  { get; } =
        new(syncService, syncRepository);

    public StatusBarViewModel StatusBar { get; } = new();

    // ── Startup ───────────────────────────────────────────────────────────

    public async Task InitialiseAsync()
    {
        // Wire sync events
        syncService.SyncProgressChanged += OnSyncProgressChanged;
        syncService.JobCompleted        += OnJobCompleted;
        syncService.ConflictDetected    += OnConflictDetected;

        // Wire account events
        Accounts.AccountSelected += OnAccountSelected;
        Accounts.AccountAdded    += OnAccountAdded;
        Accounts.AccountRemoved  += OnAccountRemoved;
        Accounts.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AccountsViewModel.ActiveAccount))
                SyncStatusBarToActiveAccount();
        };

        var restored = await startupService.RestoreAccountsAsync();
        Accounts.RestoreAccounts(restored);

        foreach (var account in restored)
            Files.AddAccount(account);

        var active = restored.FirstOrDefault(a => a.IsActive);
        if (active is not null)
        {
            await Files.ActivateAccountAsync(active.Id);
            await Activity.SetActiveAccountAsync(active.Id, active.Email);
        }

        SyncStatusBarToActiveAccount();
    }

    // ── Sync commands ─────────────────────────────────────────────────────

    [RelayCommand]
    private async Task SyncNowAsync()
    {
        var active = Accounts.ActiveAccount;
        if (active is null) return;

        var account = new OneDriveAccount
        {
            Id                = active.Id,
            DisplayName       = active.DisplayName,
            Email             = active.Email,
            LocalSyncPath     = string.Empty, // populated from DB in scheduler
            ConflictPolicy    = ConflictPolicy.Ignore,
            SelectedFolderIds = []
        };

        await scheduler.TriggerAccountAsync(account);
    }

    // ── Add account ───────────────────────────────────────────────────────

    [RelayCommand]
    private void AddAccount()
    {
        ActiveSection = NavSection.Accounts;
        Accounts.AddAccount();
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private async void OnAccountSelected(object? sender, AccountCardViewModel card)
    {
        ActiveSection = NavSection.Files;
        await Files.ActivateAccountAsync(card.Id);
        await Activity.SetActiveAccountAsync(card.Id, card.Email);
        SyncStatusBarToActiveAccount();
    }

    private async void OnAccountAdded(object? sender, OneDriveAccount account)
    {
        Files.AddAccount(account);
        ActiveSection = NavSection.Files;
        await Files.ActivateAccountAsync(account.Id);
        await Activity.SetActiveAccountAsync(account.Id, account.Email);
    }

    private void OnAccountRemoved(object? sender, string accountId) =>
        Files.RemoveAccount(accountId);

    private void OnSyncProgressChanged(object? sender, SyncProgressEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var card = Accounts.Accounts.FirstOrDefault(a => a.Id == e.AccountId);
            if (card is null) return;

            card.SyncState = e.IsComplete ? SyncState.Idle : SyncState.Syncing;

            if (card.Id == Accounts.ActiveAccount?.Id)
                SyncStatusBarToActiveAccount();
        });
    }

    private void OnJobCompleted(object? sender, JobCompletedEventArgs e)
    {
        var card = Accounts.Accounts
            .FirstOrDefault(a => a.Id == e.Job.AccountId);

        var item = ActivityItemViewModel.FromJob(
            e.Job,
            accountEmail: card?.Email ?? e.Job.AccountId,
            folderName:   string.Empty);

        Activity.AddActivityItem(item);
    }

    private void OnConflictDetected(object? sender, SyncConflict conflict)
    {
        Activity.AddConflictItem(conflict);

        // Update conflict count on the card
        Dispatcher.UIThread.Post(() =>
        {
            var card = Accounts.Accounts
                .FirstOrDefault(a => a.Id == conflict.AccountId);
            if (card is not null)
                card.ConflictCount++;

            SyncStatusBarToActiveAccount();
        });
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
