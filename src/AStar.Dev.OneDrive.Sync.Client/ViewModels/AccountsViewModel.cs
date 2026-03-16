using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Auth;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace AStar.Dev.OneDrive.Sync.Client.ViewModels;

public sealed partial class AccountsViewModel(IAuthService authService) : ObservableObject
{
    private readonly IAuthService _authService = authService;

    // ── Account list ──────────────────────────────────────────────────────

    public ObservableCollection<AccountCardViewModel> Accounts { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAccounts))]
    private AccountCardViewModel? _activeAccount;

    public bool HasAccounts => Accounts.Count > 0;

    // ── Wizard state ──────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWizardVisible))]
    private AddAccountWizardViewModel? _wizard;

    public bool IsWizardVisible => Wizard is not null;

    // ── Events ────────────────────────────────────────────────────────────

    public event EventHandler<AccountCardViewModel>? AccountSelected;

    // ── Commands ──────────────────────────────────────────────────────────

    public void AddAccount()
    {
        var wizard = new AddAccountWizardViewModel(_authService);
        wizard.Completed += OnWizardCompleted;
        wizard.Cancelled += OnWizardCancelled;
        Wizard = wizard;
    }

    [RelayCommand]
    private async Task RemoveAccountAsync(AccountCardViewModel card)
    {
        // Sign out from MSAL cache
        await _authService.SignOutAsync(card.Id);

        Accounts.Remove(card);

        if (ActiveAccount == card)
            ActiveAccount = Accounts.FirstOrDefault();

        OnPropertyChanged(nameof(HasAccounts));
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private void OnWizardCompleted(object? sender, OneDriveAccount account)
    {
        CloseWizard();

        account.AccentIndex = Accounts.Count % 6;
        account.IsActive    = Accounts.Count == 0;

        var card = new AccountCardViewModel(account);
        card.Selected        += OnCardSelected;
        card.RemoveRequested += (_, c) => RemoveAccountCommand.Execute(c);

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

    private void OnCardSelected(object? sender, AccountCardViewModel card)
    {
        foreach (var c in Accounts)
            c.IsActive = c == card;

        ActiveAccount = card;
        AccountSelected?.Invoke(this, card);
    }
}
