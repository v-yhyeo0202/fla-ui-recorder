using RecorderUI.Component;
using System.Diagnostics;
using System.IO;

namespace RecorderUI.Service;

public class ProcessManager
{
    private RecorderConfig recorderConfig;
    private List<AttacherConfig> listAttacherConfig;
    private int attacherIndex;
    private bool bLastIndex;
    private Process process;

    public ProcessManager(RecorderConfig _recorderConfig, List<AttacherConfig> _listAttacherConfig)
    {
        recorderConfig = _recorderConfig;
        listAttacherConfig = _listAttacherConfig;
        attacherIndex = 0;
        bLastIndex = false;
        process = Process.Start(recorderConfig.applicationPath);

        return;
    }

    public Process GetTargetedProcess()
    {
        process = null;

        while (process == null)
        {
            Thread.Sleep(200);
            string processName = Path.GetFileNameWithoutExtension(recorderConfig.applicationPath);

            if (string.IsNullOrEmpty(listAttacherConfig[0].mainWindowTitle))
            {
                Trace.WriteLine("debug4");
                process = Process.GetProcessesByName(processName).FirstOrDefault();
            }
            else
            {
                foreach(AttacherConfig attacherConfig in listAttacherConfig)
                {
                    Trace.WriteLine($"debug3 {attacherConfig.mainWindowTitle} {attacherConfig.bExactMatch}");
                }

                Trace.WriteLine($"debug5 {attacherIndex}");
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
        }

        attacherIndex++;

        if(attacherIndex == listAttacherConfig.Count)
        {
            attacherIndex--;
            bLastIndex = true;
        }

        Trace.WriteLine($"debug2 {process.MainWindowTitle}");

        return process;
    }

    public void Kill()
    {
        if(!bLastIndex)
        {
            attacherIndex--;
        }
        
        process = GetTargetedProcess();
        process.Kill();

        return;
    }

    public void PrintProcessInfo()
    {
        Trace.WriteLine("debug5");
        try
        {
            Trace.WriteLine($"ProcessName: {process.ProcessName}");
            Trace.WriteLine($"MainWindowTitle: {process.MainWindowTitle}");
            Trace.WriteLine($"Id: {process.Id}");
            Trace.WriteLine($"StartTime: {process.StartTime}");
        }
        catch(Exception e)
        {
            Trace.WriteLine(e.Message);
        }

        return;
    }
}