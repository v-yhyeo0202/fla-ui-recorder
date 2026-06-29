using System.Diagnostics;
using System.IO;
using YamlDotNet.Serialization;

namespace Recorder;

public class ProcessManager
{
    private RecorderConfig recorderConfig;
    private int attacherIndex;
    private Process process;

    public ProcessManager()
    {
        recorderConfig = new DeserializerBuilder().Build().Deserialize<RecorderConfig>(File.ReadAllText("config.yml"));
        attacherIndex = 0;
        process = Process.Start(recorderConfig.applicationPath);

        return;
    }

    public void GetProcessByIndex(int index)
    {
        AttacherConfig attacherConfig = recorderConfig.listAttacherConfig[index];
        string processName = Path.GetFileNameWithoutExtension(recorderConfig.applicationPath);
        process = Process.GetProcessesByName(processName)
            .Where(i => !i.MainWindowTitle.Contains("fla-ui-recorder"))
            .Where(j =>
            {
                if (attacherConfig.bExactMatch)
                {
                    return j.MainWindowTitle == attacherConfig.mainWindowTitle;
                }

                return j.MainWindowTitle.Contains(attacherConfig.mainWindowTitle);
            })
            .OrderByDescending(k => k.StartTime)
            .FirstOrDefault();

        return;
    }

    public Process GetSequentialProcess()
    {
        process = null;

        while (process == null)
        {
            Thread.Sleep(200);

            if (recorderConfig.listAttacherConfig == null)
            {
                string processName = Path.GetFileNameWithoutExtension(recorderConfig.applicationPath);
                process = Process.GetProcessesByName(processName).FirstOrDefault();
            }
            else
            {
                GetProcessByIndex(attacherIndex);
            }
        }

        attacherIndex++;

        if (attacherIndex == recorderConfig.listAttacherConfig.Count)
        {
            attacherIndex--;
        }

        return process;
    }

    public void Kill()
    {
        for (int i = 0; i < recorderConfig.listAttacherConfig.Count; i++)
        {
            GetProcessByIndex(recorderConfig.listAttacherConfig.Count - i - 1);

            try
            {
                process.Kill();
                break;
            }
            catch { }
        }

        return;
    }
}