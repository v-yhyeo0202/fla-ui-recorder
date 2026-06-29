using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Recorder;

public class AttacherConfig
{
    public string mainWindowTitle { get; set; } = null;
    public bool bExactMatch { get; set; } = false;
}

public class RecorderConfig
{
    public string applicationPath { get; set; } = null;
    public string stepDirectoryPath { get; set; } = null;
    public string stepName { get; set; } = null;
    public List<AttacherConfig> listAttacherConfig { get; set; } = null;
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