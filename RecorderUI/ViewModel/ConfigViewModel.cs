using RecorderUI.Component;
using RecorderUI.Core;
using System.Diagnostics;
using System.Printing;
using System.Windows;
using System.Windows.Input;

namespace RecorderUI.ViewModel;

public class ConfigViewModel : BaseViewModel
{
    private Utility utility;
    private RecorderConfigForm _recorderConfigForm;
    public RecorderConfigForm recorderConfigForm { get { return _recorderConfigForm; } set { _recorderConfigForm = value; OnPropertyChanged(nameof(recorderConfigForm)); } }

    private Visibility _addAttacherVisible = Visibility.Visible;
    public Visibility addAttacherVisible { get { return _addAttacherVisible; } set { _addAttacherVisible = value; OnPropertyChanged(nameof(addAttacherVisible)); } }

    private Visibility _attacherConfigFormVisible = Visibility.Collapsed;
    public Visibility attacherConfigFormVisible { get { return _attacherConfigFormVisible; } set { _attacherConfigFormVisible = value; OnPropertyChanged(nameof(attacherConfigFormVisible)); } }

    private Visibility _removeAttacherVisible = Visibility.Collapsed;
    public Visibility removeAttacherVisible { get { return _removeAttacherVisible; } set { _removeAttacherVisible = value; OnPropertyChanged(nameof(removeAttacherVisible)); } }

    private AttacherConfigForm _attacherConfigForm;
    public AttacherConfigForm attacherConfigForm { get { return _attacherConfigForm; } set { _attacherConfigForm = value; OnPropertyChanged(nameof(attacherConfigForm)); } }

    public ICommand Navigate2RecordCommand { get; }
    public ICommand AddAttacherCommand { get; }
    public ICommand RemoveAttacherCommand { get; }

    public void SetAttacherConfig(string configPath)
    {
        attacherConfigForm.LoadInput(configPath);
        addAttacherVisible = Visibility.Collapsed;
        attacherConfigFormVisible = Visibility.Visible;
        removeAttacherVisible = Visibility.Visible;

        return;
    }

    public void SetRecorderAttacherConfigPath(string configPath)
    {
        List<RecorderConfig> listRecorderConfig = recorderConfigForm.GetInput();
        listRecorderConfig[0].attacherConfigPath = configPath;
        recorderConfigForm.SetInput(listRecorderConfig);

        return;
    }

    public void UpdateRecorderAttacherConfigPath()
    {
        SetRecorderAttacherConfigPath(attacherConfigForm.GetConfigPath());

        return;
    }

    public void Navigate2Record(object parameter)
    {
        ((App)Application.Current).mainViewModel.Navigate(new RecordViewModel(), this);

        return;
    }

    public void AddAttacher(object parameter)
    {
        addAttacherVisible = Visibility.Collapsed;
        attacherConfigFormVisible = Visibility.Visible;
        removeAttacherVisible = Visibility.Visible;

        return;
    }

    public void RemoveAttacher(object parameter)
    {
        attacherConfigForm.Reset(null);
        SetRecorderAttacherConfigPath(null);
        addAttacherVisible = Visibility.Visible;
        attacherConfigFormVisible = Visibility.Collapsed;
        removeAttacherVisible = Visibility.Collapsed;

        return;
    }

    public ConfigViewModel()
    {
        utility = new Utility();

        try
        {
            Navigate2RecordCommand = new RelayCommand(Navigate2Record);
            AddAttacherCommand = new RelayCommand(AddAttacher);
            RemoveAttacherCommand = new RelayCommand(RemoveAttacher);

            recorderConfigForm = new RecorderConfigForm(_formDependency: new RecorderConfigFormDependency(SetAttacherConfig, Navigate2RecordCommand));
            attacherConfigForm = new AttacherConfigForm(_formDependency: new AttacherConfigFormDependency(UpdateRecorderAttacherConfigPath));
            RecorderConfig recorderConfig = recorderConfigForm.GetInput()[0];

            if (!string.IsNullOrEmpty(recorderConfig.attacherConfigPath))
            {
                SetAttacherConfig(recorderConfig.attacherConfigPath);
            }
        }
        catch (Exception e)
        {
            utility.ShowExceptionMessage(e, $"{nameof(ConfigViewModel)}.{nameof(ConfigViewModel)}");
        }

        return;
    }
}

public class RecorderConfigFormDependency : IFormDependency
{
    private Action<string> setAttacherConfig;
    public ICommand Navigate2RecordCommand;

    public RecorderConfigFormDependency(Action<string> _setAttacherConfig, ICommand _Navigate2RecordCommand)
    {
        setAttacherConfig = _setAttacherConfig;
        Navigate2RecordCommand = _Navigate2RecordCommand;

        return;
    }

    public void SetComboBoxItem(object _listItem, string propertyName) { }
    public async Task SetComboBoxItemAsync(object _listItem, string propertyName) { }
    public async Task PostInsertAsync(object _listInput) { }
    public void PostApply(object _listInput)
    {
        Navigate2RecordCommand.Execute(null);

        return;
    }

    public void PostSearchPath(object input)
    {
        setAttacherConfig((string)input);

        return;
    }
}

public class AttacherConfigFormDependency : IFormDependency
{
    private Action updateRecorderAttacherConfigPath;

    public AttacherConfigFormDependency(Action _updateRecorderAttacherConfigPath)
    {
        updateRecorderAttacherConfigPath = _updateRecorderAttacherConfigPath;
        
        return;
    }

    public void SetComboBoxItem(object _listItem, string propertyName) { }
    public async Task SetComboBoxItemAsync(object _listItem, string propertyName) { }
    public async Task PostInsertAsync(object _listInput) { }
    public void PostApply(object _listInput)
    {
        updateRecorderAttacherConfigPath();

        return;
    }

    public void PostSearchPath(object input) { }
}