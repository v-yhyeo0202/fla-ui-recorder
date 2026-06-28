using System.ComponentModel;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace console_app;


public class ProcessFilter
{
    public string mainWindowTitle = null;
    public bool bContains = false;
}

public class StepConfig
{
    public string controlType { get; set; } = null;
    public string xPath { get; set; } = null;

    [DefaultValue(null)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string text { get; set; } = null;

    [DefaultValue(null)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string clickType { get; set; } = null;

    [DefaultValue(false)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool bEvaluation { get; set; } = false;
}

public class UserConfig
{
    [YamlMember(Alias = "solutionName")]
    public string solutionName { get; set; } = null;

    [YamlMember(Alias = "stepDirectoryPath")]
    public string stepDirectoryPath { get; set; } = null;

    [YamlMember(Alias = "visualStudioPath")]
    public string visualStudioPath { get; set; } = null;
}