using Avalonia.Controls;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Services.Auth;
using AStar.Dev.OneDrive.Sync.Client.Services.Graph;
using AStar.Dev.OneDrive.Sync.Client.Services.Startup;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;
using AStar.Dev.OneDrive.Sync.Client.ViewModels;

namespace AStar.Dev.OneDrive.Sync.Client;

public partial class MainWindow : Window
{
    public MainWindow(
        IAuthService    authService,
        IGraphService   graphService,
        IStartupService startupService,
        ISyncService    syncService,
        SyncScheduler   scheduler,
        ISyncRepository syncRepository)
    {
        InitializeComponent();
        var vm = new MainWindowViewModel(
            authService, graphService, startupService,
            syncService, scheduler, syncRepository);
        DataContext = vm;
        Opened += async (_, _) => await vm.InitialiseAsync();
    }
}
