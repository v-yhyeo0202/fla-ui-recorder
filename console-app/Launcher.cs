using System.Diagnostics;
using System.IO;
using YamlDotNet.Serialization;

namespace console_app;

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