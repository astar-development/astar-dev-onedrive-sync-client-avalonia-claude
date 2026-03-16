using AStar.Dev.OneDrive.Sync.Client.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace AStar.Dev.OneDrive.Sync.Client.ViewModels;

/// <summary>
/// Drives the Accounts section view and the left-hand account panel.
/// Owns the canonical list of connected accounts.
/// </summary>
public sealed partial class AccountsViewModel : ObservableObject
{
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

    // ── Events (consumed by MainWindowViewModel to drive navigation) ──────

    /// <summary>Raised when the user clicks an account card — navigate to Files.</summary>
    public event EventHandler<AccountCardViewModel>? AccountSelected;

    // ── Commands ──────────────────────────────────────────────────────────

    [RelayCommand]
    private void AddAccount()
    {
        System.Diagnostics.Debug.WriteLine("AddAccount command fired");
        var wizard = new AddAccountWizardViewModel();
        wizard.Completed  += OnWizardCompleted;
        wizard.Cancelled  += OnWizardCancelled;
        Wizard = wizard;
    }

    [RelayCommand]
    private void RemoveAccount(AccountCardViewModel card)
    {
        Accounts.Remove(card);
        if (ActiveAccount == card)
            ActiveAccount = Accounts.FirstOrDefault();
        OnPropertyChanged(nameof(HasAccounts));
    }

    // ── Initialisation (stub data for now) ────────────────────────────────

    public AccountsViewModel()
    {
        // No stub accounts — start empty so the user goes through the wizard
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private void OnWizardCompleted(object? sender, OneDriveAccount account)
    {
        CloseWizard();

        account.AccentIndex = Accounts.Count % 6;
        account.IsActive    = Accounts.Count == 0; // first account is active by default

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
        // Update active state on all cards
        foreach (var c in Accounts)
            c.IsActive = c == card;

        ActiveAccount = card;
        AccountSelected?.Invoke(this, card);
    }
}
