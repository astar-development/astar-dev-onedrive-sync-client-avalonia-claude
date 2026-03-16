using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Auth;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AStar.Dev.OneDrive.Sync.Client.ViewModels;

public enum WizardStep { SignIn, SelectFolders, Confirm }

/// <summary>
/// Stub folder item shown in the wizard folder-selection step.
/// Replaced with real Graph API data when the Graph service is wired up.
/// </summary>
public sealed partial class WizardFolderItem : ObservableObject
{
    public string Id   { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;

    [ObservableProperty] private bool _isSelected = true;
}

/// <summary>
/// Drives the Add Account wizard — a three-step flow:
///   Step 1 — Sign in (real MSAL browser launch)
///   Step 2 — Select folders to sync (skippable)
///   Step 3 — Confirm and finish
/// </summary>
public sealed partial class AddAccountWizardViewModel(IAuthService authService) : ObservableObject
{
    private readonly IAuthService _authService = authService;
    private          string       _accountId   = string.Empty;
    private          string?      _accessToken;
    private CancellationTokenSource? _authCts;

    // ── Step state ────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSignInStep))]
    [NotifyPropertyChangedFor(nameof(IsSelectFoldersStep))]
    [NotifyPropertyChangedFor(nameof(IsConfirmStep))]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    [NotifyPropertyChangedFor(nameof(NextLabel))]
    private WizardStep _currentStep = WizardStep.SignIn;

    public bool IsSignInStep        => CurrentStep == WizardStep.SignIn;
    public bool IsSelectFoldersStep => CurrentStep == WizardStep.SelectFolders;
    public bool IsConfirmStep       => CurrentStep == WizardStep.Confirm;

    // ── Sign-in step ──────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    private bool _isSignedIn;

    [ObservableProperty] private bool   _isWaitingForAuth;
    [ObservableProperty] private string _signInStatusText  = string.Empty;
    [ObservableProperty] private bool   _signInHasError;

    // ── Folder selection step ─────────────────────────────────────────────

    public ObservableCollection<WizardFolderItem> Folders { get; } = [];

    [ObservableProperty] private bool _isLoadingFolders;

    // ── Confirm step ──────────────────────────────────────────────────────

    [ObservableProperty] private string _confirmedDisplayName = string.Empty;
    [ObservableProperty] private string _confirmedEmail       = string.Empty;
    [ObservableProperty] private int    _confirmedFolderCount;

    // ── Navigation ────────────────────────────────────────────────────────

    public bool CanGoBack => CurrentStep != WizardStep.SignIn;
    public bool CanGoNext => CurrentStep switch
    {
        WizardStep.SignIn        => IsSignedIn,
        WizardStep.SelectFolders => true,
        WizardStep.Confirm       => true,
        _                        => false
    };

    public string NextLabel => CurrentStep == WizardStep.Confirm ? "Finish" : "Next";

    [RelayCommand]
    private void Back()
    {
        if (CurrentStep == WizardStep.SelectFolders) CurrentStep = WizardStep.SignIn;
        else if (CurrentStep == WizardStep.Confirm)  CurrentStep = WizardStep.SelectFolders;
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void Next()
    {
        if (CurrentStep == WizardStep.SignIn)
        {
            LoadStubFolders();
            CurrentStep = WizardStep.SelectFolders;
        }
        else if (CurrentStep == WizardStep.SelectFolders)
        {
            BuildConfirmSummary();
            CurrentStep = WizardStep.Confirm;
        }
        else if (CurrentStep == WizardStep.Confirm)
        {
            Finish();
        }
    }

    [RelayCommand]
    private void SkipFolders()
    {
        foreach (var f in Folders) f.IsSelected = false;
        BuildConfirmSummary();
        CurrentStep = WizardStep.Confirm;
    }

    // ── Sign-in ───────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task OpenBrowserAsync()
    {
        if (IsWaitingForAuth) return;

        SignInHasError    = false;
        SignInStatusText  = "Waiting for sign-in\u2026";
        IsWaitingForAuth  = true;

        _authCts = new CancellationTokenSource();

        try
        {
            var result = await _authService.SignInInteractiveAsync(_authCts.Token);

            if (result.IsCancelled)
            {
                SignInStatusText = "Sign-in cancelled.";
                SignInHasError   = false;
            }
            else if (result.IsError)
            {
                SignInStatusText = result.ErrorMessage ?? "Sign-in failed.";
                SignInHasError   = true;
            }
            else
            {
                // Success
                _accountId           = result.AccountId!;
                _accessToken         = result.AccessToken;
                ConfirmedDisplayName = result.DisplayName ?? string.Empty;
                ConfirmedEmail       = result.Email       ?? string.Empty;
                IsSignedIn           = true;
                SignInStatusText     = $"Signed in as {ConfirmedEmail}";
                SignInHasError       = false;

                NextCommand.NotifyCanExecuteChanged();
            }
        }
        finally
        {
            IsWaitingForAuth = false;
            _authCts.Dispose();
            _authCts = null;
        }
    }

    // ── Events ────────────────────────────────────────────────────────────

    public event EventHandler<OneDriveAccount>? Completed;
    public event EventHandler?                  Cancelled;

    [RelayCommand]
    private async Task Cancel()
    {
        // Cancel any in-progress auth
        _authCts?.Cancel();
        await Task.CompletedTask;
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private void LoadStubFolders()
    {
        Folders.Clear();
        // Stub folders — replaced with real Graph /me/drive/root/children in step 5
        foreach (var name in new[] { "Documents", "Photos", "Desktop", "Music", "Videos" })
        {
            Folders.Add(new WizardFolderItem
            {
                Id         = Guid.NewGuid().ToString(),
                Name       = name,
                IsSelected = name is "Documents" or "Desktop"
            });
        }
    }

    private void BuildConfirmSummary()
    {
        ConfirmedFolderCount = Folders.Count(f => f.IsSelected);
    }

    private void Finish()
    {
        var account = new OneDriveAccount
        {
            Id                = _accountId,
            DisplayName       = ConfirmedDisplayName,
            Email             = ConfirmedEmail,
            SelectedFolderIds = [.. Folders
                .Where(f => f.IsSelected)
                .Select(f => f.Id)]
        };

        Completed?.Invoke(this, account);
    }
}
