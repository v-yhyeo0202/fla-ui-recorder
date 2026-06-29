using RecorderUI.Component;
using RecorderUI.Core;
using RecorderUI.Service;
using System.Diagnostics;
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

    private double _waitMessageHeight = SystemParameters.PrimaryScreenHeight;
    public double waitMessageHeight { get { return _waitMessageHeight; } set { _waitMessageHeight = value; OnPropertyChanged(nameof(waitMessageHeight)); } }

    private double _waitMessageWidth = SystemParameters.PrimaryScreenWidth;
    public double waitMessageWidth { get { return _waitMessageWidth; } set { _waitMessageWidth = value; OnPropertyChanged(nameof(waitMessageWidth)); } }

    private Visibility _waitMessageVisible = Visibility.Collapsed;
    public Visibility waitMessageVisible { get { return _waitMessageVisible; } set { _waitMessageVisible = value; OnPropertyChanged(nameof(waitMessageVisible)); } }

    public ICommand ShowRecorderPanelCommand { get; }
    public ICommand RecordAsyncCommand { get; }
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

    public async Task RecordAsync(object parameter)
    {
        await recorder.RecordAsync();
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
        recorder = new Recorder(recorderConfig, listAttacherConfig, this);
        mainWindow = ((App)Application.Current).mainWindow;
        mainWindow.WindowStyle = WindowStyle.None;
        mainWindow.ResizeMode = ResizeMode.NoResize;
        mainWindow.Topmost = true;
        mainWindow.Top = 0;
        mainWindow.Left = (SystemParameters.PrimaryScreenWidth - windowWidth) / 2;
        mainWindow.Height = collapsedWindowHeight;
        mainWindow.Width = windowWidth;

        ShowRecorderPanelCommand = new RelayCommand(ShowRecorderPanel);
        RecordAsyncCommand = new RelayCommandAsync(RecordAsync);
        PauseCommand = new RelayCommand(Pause);
        StopAsyncCommand = new RelayCommandAsync(StopAsync);

        return;
    }

    public void ShowWaitMessage(bool bShow = true)
    {
        try
        {
            if (bShow)
            {
                mainWindow.Left = 0;
                mainWindow.Height = SystemParameters.PrimaryScreenHeight;
                mainWindow.Width = SystemParameters.PrimaryScreenWidth;
                waitMessageVisible = Visibility.Visible;
                recorderPanelVisible = Visibility.Collapsed;
            }
            else
            {
                mainWindow.Left = (SystemParameters.PrimaryScreenWidth - windowWidth) / 2;
                mainWindow.Height = collapsedWindowHeight;
                mainWindow.Width = windowWidth;
                waitMessageVisible = Visibility.Collapsed;
                recorderPanelVisible = Visibility.Visible;
            }
        }
        catch(Exception e)
        {
            Trace.WriteLine(e.Message);
        }

        return;
    }
}
