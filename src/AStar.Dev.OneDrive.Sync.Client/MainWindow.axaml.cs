using Avalonia.Controls;
using AStar.Dev.OneDrive.Sync.Client.Services.Auth;
using AStar.Dev.OneDrive.Sync.Client.ViewModels;

namespace AStar.Dev.OneDrive.Sync.Client;

public partial class MainWindow : Window
{
    public MainWindow(IAuthService authService)
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel(authService);
    }
}
