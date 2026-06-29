using Microsoft.Win32;
using RecorderUI.Core;
using RecorderUI.ViewModel;
using Shared;
using System.Collections.ObjectModel;
using System.Runtime.Intrinsics.Arm;
using System.Windows;
using System.Windows.Input;

namespace RecorderUI.Component;

public abstract class FormViewModel : BaseViewModel
{
    protected FormConfig formConfig;
    protected IFormDependency formDependency;
    private ObservableCollection<FormWrapper> _listFormWrapper = null;
    public ObservableCollection<FormWrapper> listFormWrapper { get { return _listFormWrapper; } set { _listFormWrapper = value; OnPropertyChanged(nameof(listFormWrapper)); } }

    private Visibility _addItemVisibility = Visibility.Collapsed;
    public Visibility addItemVisibility { get { return _addItemVisibility; } set { _addItemVisibility = value; OnPropertyChanged(nameof(addItemVisibility)); } }

    private Visibility _insertVisibility = Visibility.Visible;
    public Visibility insertVisibility { get { return _insertVisibility; } set { _insertVisibility = value; OnPropertyChanged(nameof(insertVisibility)); } }

    private Visibility _applyVisibility = Visibility.Collapsed;
    public Visibility applyVisibility { get { return _applyVisibility; } set { _applyVisibility = value; OnPropertyChanged(nameof(applyVisibility)); } }

    public ICommand addItemCommand { get; }
    public ICommand insertCommand { get; }
    public ICommand applyCommand { get; }
    public ICommand resetCommand { get; }

    public abstract void AddItem(object parameter);
    public abstract Task InsertAsync(object parameter);
    public abstract Task ApplyAsync(object parameter);
    public abstract void Reset(object parameter);

    public FormViewModel(FormConfig globalFormConfig, FormConfig localFormConfig = null, IFormDependency _formDependency = null)
    {
        formConfig = localFormConfig ?? globalFormConfig;
        formDependency = _formDependency;
        listFormWrapper = new ObservableCollection<FormWrapper>();
        addItemVisibility = formConfig.bAddItem ? Visibility.Visible : Visibility.Hidden;

        if (formConfig.bApply)
        {
            insertVisibility = Visibility.Collapsed;
            applyVisibility = Visibility.Visible;
        }

        addItemCommand = new RelayCommand(AddItem);
        insertCommand = new RelayCommandAsync(InsertAsync);
        applyCommand = new RelayCommandAsync(ApplyAsync);
        resetCommand = new RelayCommand(Reset);

        return;
    }
}

public class FormWrapper : BaseViewModel
{
    public Guid? primaryKey = null;
    private ObservableCollection<BaseForm> _listForm = null;
    public ObservableCollection<BaseForm> listForm { get { return _listForm; } set { _listForm = value; OnPropertyChanged(nameof(listForm)); } }

    public FormWrapper()
    {
        listForm = new ObservableCollection<BaseForm>();

        return;
    }
}

public abstract class BaseForm : BaseViewModel
{
    private string _displayName = null;
    public string displayName { get { return _displayName; } set { _displayName = value; OnPropertyChanged(nameof(displayName)); } }

    private int _labelWidth = 0;
    public int labelWidth { get { return _labelWidth; } set { _labelWidth = value; OnPropertyChanged(nameof(labelWidth)); } }
}

public class TextForm : BaseForm
{
    private string _input = null;
    public string input { get { return _input; } set { _input = value; OnPropertyChanged(nameof(input)); } }
}

public class ComboForm : BaseForm
{
    private ObservableCollection<string> _listItem = null;
    public ObservableCollection<string> listItem { get { return _listItem; } set { _listItem = value; OnPropertyChanged(nameof(listItem)); } }

    private string _input = null;
    public string input { get { return _input; } set { _input = value; OnPropertyChanged(nameof(input)); } }

    public ComboForm()
    {
        listItem = new ObservableCollection<string>();

        return;
    }
}

public class PathForm : BaseForm
{
    private Utility utility;
    public bool bFilePath;
    public bool bSave;
    public IFormDependency formDependency;
    private string _input = null;
    public string input { get { return _input; } set { _input = value; OnPropertyChanged(nameof(input)); } }

    public ICommand addPathCommand { get; }

    public void AddPath(object parameter)
    {
        bool? status = null;

        if (bFilePath && !bSave)
        {
            OpenFileDialog openFileDialog = new();
            status = openFileDialog.ShowDialog();

            if ((status != null) && (bool)status)
            {
                input = openFileDialog.FileName;
            }
        }
        else if (bFilePath && bSave)
        {
            SaveFileDialog saveFileDialog = new();
            status = saveFileDialog.ShowDialog();

            if ((status != null) && (bool)status)
            {
                input = saveFileDialog.FileName;
            }
        }
        else
        {
            OpenFolderDialog openFolderDialog = new();
            status = openFolderDialog.ShowDialog();

            if ((status != null) && (bool)status)
            {
                input = openFolderDialog.FolderName;
            }
        }

        if(formDependency != null)
        {
            try
            {
                formDependency.PostSearchPath(input);
            }
            catch (Exception e)
            {
                utility.ShowExceptionMessage(e, $"{nameof(ConfigViewModel)}.{nameof(ConfigViewModel)}");
            }
        }

        return;
    }

    public PathForm(IFormDependency _formDependency = null)
    {
        utility = new Utility();
        formDependency = _formDependency;
        addPathCommand = new RelayCommand(AddPath);

        return;
    }
}

public class IntForm : BaseForm
{
    private int _input = 0;
    public int input { get { return _input; } set { _input = value; OnPropertyChanged(nameof(input)); } }
}

public class DecimalForm : BaseForm
{
    private decimal _input = 0;
    public decimal input { get { return _input; } set { _input = value; OnPropertyChanged(nameof(input)); } }
}

public class DateForm : BaseForm
{
    private DateTime _input = DateTime.Today;
    public DateTime input { get { return _input; } set { _input = value; OnPropertyChanged(nameof(input)); } }
}

public class CheckBoxForm : BaseForm
{
    private bool _input = false;
    public bool input { get { return _input; } set { _input = value; OnPropertyChanged(nameof(input)); } }
}