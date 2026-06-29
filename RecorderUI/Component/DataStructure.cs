using RecorderUI.ViewModel;
using System.IO;

namespace RecorderUI.Component;

[ConfigFormSourceGen.RecorderConfig]
public partial class RecorderConfig : BaseViewModel
{
    private string _applicationPath = null;
    public string applicationPath { get { return _applicationPath; } set { _applicationPath = value; OnPropertyChanged(nameof(applicationPath)); } }

    private string _attacherConfigPath = null;
    public string attacherConfigPath { get { return _attacherConfigPath; } set { _attacherConfigPath = value; OnPropertyChanged(nameof(attacherConfigPath)); } }

    private string _stepDirectoryPath = Path.Join(Directory.GetCurrentDirectory(), "step");
    public string stepDirectoryPath { get { return _stepDirectoryPath; } set { _stepDirectoryPath = value; OnPropertyChanged(nameof(stepDirectoryPath)); } }

    private string _stepName = null;
    public string stepName { get { return _stepName; } set { _stepName = value; OnPropertyChanged(nameof(stepName)); } }
}

[ConfigFormSourceGen.AttacherConfig]
public partial class AttacherConfig : BaseViewModel
{
    private string _mainWindowTitle = null;
    public string mainWindowTitle { get { return _mainWindowTitle; } set { _mainWindowTitle = value; OnPropertyChanged(nameof(mainWindowTitle)); } }

    private bool _bExactMatch = true;
    public bool bExactMatch { get { return _bExactMatch; } set { _bExactMatch = value; OnPropertyChanged(nameof(bExactMatch)); } }
}