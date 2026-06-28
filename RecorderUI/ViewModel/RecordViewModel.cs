using RecorderUI.Component;
using RecorderUI.Core;
using RecorderUI.Service;
using System.Windows;
using System.Windows.Input;

namespace RecorderUI.ViewModel;

public class RecordViewModel : BaseViewModel
{
    private Recorder recorder;
    private MainWindow mainWindow;
    private double collapsedWindowHeight = 5;
    private double expandedWindowHeight = 30;
    private double windowWidth = 160;

    private Visibility _indicatorVisible = Visibility.Visible;
    public Visibility indicatorVisible { get { return _indicatorVisible; } set { _indicatorVisible = value; OnPropertyChanged(nameof(indicatorVisible)); } }

    private Visibility _recorderPanelVisible = Visibility.Collapsed;
    public Visibility recorderPanelVisible { get { return _recorderPanelVisible; } set { _recorderPanelVisible = value; OnPropertyChanged(nameof(recorderPanelVisible)); } }

    private Visibility _recordVisible = Visibility.Collapsed;
    public Visibility recordVisible { get { return _recordVisible; } set { _recordVisible = value; OnPropertyChanged(nameof(recordVisible)); } }

    private Visibility _pauseVisible = Visibility.Visible;
    public Visibility pauseVisible { get { return _pauseVisible; } set { _pauseVisible = value; OnPropertyChanged(nameof(pauseVisible)); } }

    public ICommand ShowRecorderPanelCommand { get; }
    public ICommand RecordCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand StopAsyncCommand { get; }

    public void ShowRecorderPanel(object parameter)
    {
        if((bool) parameter)
        {
            indicatorVisible = Visibility.Collapsed;
            recorderPanelVisible = Visibility.Visible;
            mainWindow.Height = expandedWindowHeight;
            mainWindow.Width = windowWidth;
        }
        else
        {
            indicatorVisible = Visibility.Visible;
            recorderPanelVisible = Visibility.Collapsed;
            mainWindow.Height = collapsedWindowHeight;
            mainWindow.Width = windowWidth;
        }

        return;
    }

    public void Record(object parameter)
    {
        recorder.Record();
        recordVisible = Visibility.Collapsed;
        pauseVisible = Visibility.Visible;

        return;
    }
    public void Pause(object parameter)
    {
        recorder.Pause();
        recordVisible = Visibility.Visible;
        pauseVisible = Visibility.Collapsed;

        return;
    }

    public async Task StopAsync(object parameter)
    {
        await recorder.StopAsync();
        mainWindow.WindowStyle = WindowStyle.SingleBorderWindow;
        mainWindow.ResizeMode = ResizeMode.CanResize;
        mainWindow.Topmost = false;
        mainWindow.Top = (SystemParameters.PrimaryScreenHeight - 450) / 2;
        mainWindow.Left = (SystemParameters.PrimaryScreenWidth - 800) / 2;
        mainWindow.Height = 450;
        mainWindow.Width = 800;

        ((App)Application.Current).mainViewModel.NavigateBack();

        return;
    }

    public RecordViewModel(RecorderConfig recorderConfig, List<AttacherConfig> listAttacherConfig)
    {
        recorder = new Recorder(recorderConfig, listAttacherConfig);

        mainWindow = ((App)Application.Current).mainWindow;
        mainWindow.WindowStyle = WindowStyle.None;
        mainWindow.ResizeMode = ResizeMode.NoResize;
        mainWindow.Topmost = true;
        mainWindow.Top = 0;
        mainWindow.Left = (SystemParameters.PrimaryScreenWidth - windowWidth) / 2;
        mainWindow.Height = collapsedWindowHeight;
        mainWindow.Width = windowWidth;

        ShowRecorderPanelCommand = new RelayCommand(ShowRecorderPanel);
        RecordCommand = new RelayCommand(Record);
        PauseCommand = new RelayCommand(Pause);
        StopAsyncCommand = new RelayCommandAsync(StopAsync);

        return;
    }
}
