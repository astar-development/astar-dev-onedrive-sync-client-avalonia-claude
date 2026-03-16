using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Auth;
using AStar.Dev.OneDrive.Sync.Client.Services.Graph;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace AStar.Dev.OneDrive.Sync.Client.ViewModels;

public sealed partial class AccountsViewModel(
    IAuthService       authService,
    IGraphService      graphService,
    IAccountRepository repository) : ObservableObject
{
    // ── Account list ──────────────────────────────────────────────────────

    public ObservableCollection<AccountCardViewModel> Accounts { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAccounts))]
    private AccountCardViewModel? _activeAccount;

    public bool HasAccounts => Accounts.Count > 0;

    // ── Wizard ────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWizardVisible))]
    private AddAccountWizardViewModel? _wizard;

    public bool IsWizardVisible => Wizard is not null;

    // ── Events ────────────────────────────────────────────────────────────

    public event EventHandler<AccountCardViewModel>? AccountSelected;

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>Called by MainWindowViewModel to open the add-account wizard.</summary>
    public void AddAccount()
    {
        var wizard = new AddAccountWizardViewModel(authService, graphService);
        wizard.Completed += OnWizardCompleted;
        wizard.Cancelled += OnWizardCancelled;
        Wizard = wizard;
    }

    /// <summary>
    /// Restores accounts from the database on startup.
    /// Called once by MainWindowViewModel after construction.
    /// </summary>
    public void RestoreAccounts(IEnumerable<OneDriveAccount> accounts)
    {
        foreach (var account in accounts)
        {
            var card = BuildCard(account);
            Accounts.Add(card);

            if (account.IsActive)
                ActiveAccount = card;
        }

        OnPropertyChanged(nameof(HasAccounts));
    }

    // ── Commands ──────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task RemoveAccountAsync(AccountCardViewModel card)
    {
        await authService.SignOutAsync(card.Id);
        await repository.DeleteAsync(card.Id);

        Accounts.Remove(card);

        if (ActiveAccount == card)
            ActiveAccount = Accounts.FirstOrDefault();

        OnPropertyChanged(nameof(HasAccounts));
    }

    // ── Wizard events ─────────────────────────────────────────────────────

    private async void OnWizardCompleted(object? sender, OneDriveAccount account)
    {
        CloseWizard();

        account.AccentIndex = Accounts.Count % 6;
        account.IsActive    = Accounts.Count == 0;

        // Persist to database
        var entity = ToEntity(account);
        await repository.UpsertAsync(entity);

        if (account.IsActive)
            await repository.SetActiveAccountAsync(account.Id);

        var card = BuildCard(account);
        Accounts.Add(card);
        OnPropertyChanged(nameof(HasAccounts));

        if (account.IsActive)
            ActiveAccount = card;
    }

    private void OnWizardCancelled(object? sender, EventArgs e) => CloseWizard();

    private void CloseWizard()
    {
        if (Wizard is not null)
        {
            Wizard.Completed -= OnWizardCompleted;
            Wizard.Cancelled -= OnWizardCancelled;
        }
        Wizard = null;
    }

    // ── Card selection ────────────────────────────────────────────────────

    private void OnCardSelected(object? sender, AccountCardViewModel card)
    {
        foreach (var c in Accounts)
            c.IsActive = c == card;

        ActiveAccount = card;
        AccountSelected?.Invoke(this, card);

        // Persist active state
        _ = repository.SetActiveAccountAsync(card.Id);
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private AccountCardViewModel BuildCard(OneDriveAccount account)
    {
        var card = new AccountCardViewModel(account);
        card.Selected        += OnCardSelected;
        card.RemoveRequested += (_, c) => RemoveAccountCommand.Execute(c);
        return card;
    }

    private static AccountEntity ToEntity(OneDriveAccount a) => new()
    {
        Id          = a.Id,
        DisplayName = a.DisplayName,
        Email       = a.Email,
        AccentIndex = a.AccentIndex,
        IsActive    = a.IsActive,
        DeltaLink   = a.DeltaLink,
        LastSyncedAt = a.LastSyncedAt,
        QuotaTotal  = a.QuotaTotal,
        QuotaUsed   = a.QuotaUsed,
        SyncFolders = [.. a.SelectedFolderIds.Select(id => new SyncFolderEntity
        {
            FolderId  = id,
            AccountId = a.Id
        })]
    };
}
