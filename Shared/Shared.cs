using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Shared;

public class VisualStudioLauncher
{
    private UserConfig config;
    private List<ProcessFilter> listProcessFilter;
    private int processFilterIndex;

    public Process Attach()
    {
        Process process = null;

        while (process == null)
        {
            Thread.Sleep(200);
            process = Process.GetProcessesByName("devenv")
                .Where(i => !i.MainWindowTitle.Contains(config.solutionName))
                .Where(j =>
                {
                    if (listProcessFilter[processFilterIndex].bContains)
                    {
                        return j.MainWindowTitle.Contains(listProcessFilter[processFilterIndex].mainWindowTitle);
                    }

                    return j.MainWindowTitle == listProcessFilter[processFilterIndex].mainWindowTitle;
                })
                .OrderByDescending(k => k.StartTime)
                .FirstOrDefault();
        }

        processFilterIndex = processFilterIndex < listProcessFilter.Count - 1 ? processFilterIndex + 1 : processFilterIndex;

        return process;
    }

    public VisualStudioLauncher()
    {
        string configText = File.ReadAllText("config.yml");
        config = new DeserializerBuilder().Build().Deserialize<UserConfig>(configText);
        listProcessFilter = new()
        {
            new ProcessFilter
            {
                mainWindowTitle = "Microsoft Visual Studio"
            },
            new ProcessFilter
            {
                mainWindowTitle = "Microsoft Visual Studio",
                bContains = true
            },
        };
        processFilterIndex = 0;
        Process.Start(config.visualStudioPath);

        return;
    }
}

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

    [DefaultValue(false)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool bRightClick { get; set; } = false;
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