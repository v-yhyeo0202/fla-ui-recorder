using RecorderUI.Component;
using RecorderUI.Core;
using System.Windows.Input;

namespace RecorderUI.ViewModel;

public class ConfigViewModel : BaseViewModel
{
    private Utility utility;
    private RecorderConfigForm _recorderConfigForm;
    public RecorderConfigForm recorderConfigForm { get { return _recorderConfigForm; } set { _recorderConfigForm = value; OnPropertyChanged(nameof(recorderConfigForm)); } }

    public ICommand Navigate2RecordCommand { get; }

    public void Navigate2Record(object parameter)
    {
        ((App)System.Windows.Application.Current).mainViewModel.Navigate(new RecordViewModel(), this);

        return;
    }

    public ConfigViewModel()
    {
        utility = new Utility();
        Navigate2RecordCommand = new RelayCommand(Navigate2Record);

        try
        {
            recorderConfigForm = new RecorderConfigForm(_formDependency: new RecorderConfigFormDependency(Navigate2RecordCommand));
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
    public ICommand Navigate2RecordCommand;

    public RecorderConfigFormDependency(ICommand _Navigate2RecordCommand)
    {
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
}