using RecorderUI.ViewModel;
using System.IO;

namespace RecorderUI.Component;

[ConfigFormSourceGen.RecorderConfig]
public partial class RecorderConfig : BaseViewModel
{
    private string _applicationPath = null;
    public string applicationPath { get { return _applicationPath; } set { _applicationPath = value; OnPropertyChanged(nameof(applicationPath)); } }

    private string _stepDirectoryPath = Path.Join(Directory.GetCurrentDirectory(), "step");
    public string stepDirectoryPath { get { return _stepDirectoryPath; } set { _stepDirectoryPath = value; OnPropertyChanged(nameof(stepDirectoryPath)); } }
}