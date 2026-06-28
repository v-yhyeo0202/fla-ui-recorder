using RecorderUI.ViewModel;
using System.Windows;

namespace RecorderUI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public MainViewModel mainViewModel;
    public MainWindow mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        mainViewModel = new MainViewModel(new ConfigViewModel());
        mainWindow = new()
        {
            DataContext = mainViewModel
        };

        mainWindow.Show();
        base.OnStartup(e);

        return;
    }
}