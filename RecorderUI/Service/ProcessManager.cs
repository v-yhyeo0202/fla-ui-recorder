using RecorderUI.Component;
using System.Diagnostics;
using System.IO;

namespace RecorderUI.Service;

public class ProcessManager
{
    private RecorderConfig recorderConfig;
    private List<AttacherConfig> listAttacherConfig;
    private int attacherIndex;

    public Process GetTargetedProcess()
    {
        Process process = null;

        while (process == null)
        {
            Thread.Sleep(200);
            string processName = Path.GetFileNameWithoutExtension(recorderConfig.applicationPath);
            process = Process.GetProcessesByName(processName)
                .Where(i => !i.MainWindowTitle.Contains("fla-ui-recorder"))
                .Where(j =>
                {
                    AttacherConfig attacherConfig = listAttacherConfig[attacherIndex];
                    if (attacherConfig.bExactMatch)
                    {
                        return j.MainWindowTitle == attacherConfig.mainWindowTitle;
                    }
                    
                    return j.MainWindowTitle.Contains(attacherConfig.mainWindowTitle);
                })
                .OrderByDescending(k => k.StartTime)
                .FirstOrDefault();
        }

        attacherIndex = attacherIndex < listAttacherConfig.Count - 1 ? attacherIndex + 1 : attacherIndex;

        return process;
    }

    public ProcessManager(RecorderConfig _recorderConfig, List<AttacherConfig> _listAttacherConfig)
    {
        recorderConfig = _recorderConfig;
        listAttacherConfig = _listAttacherConfig;
        attacherIndex = 0;

        Process.Start(recorderConfig.applicationPath);

        return;
    }
}