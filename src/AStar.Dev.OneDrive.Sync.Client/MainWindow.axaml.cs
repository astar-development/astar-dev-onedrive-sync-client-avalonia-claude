using AStar.Dev.OneDrive.Sync.Client.ViewModels;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}
