using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.EventHandlers;
using FlaUI.Core.Identifiers;
using FlaUI.UIA3;
using RecorderUI.Component;
using RecorderUI.ViewModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RecorderUI.Service;

public class Recorder
{
    private RecorderConfig recorderConfig;
    private RecordViewModel recordViewModel;
    private FlaUI.Core.Application app;
    private UIA3Automation automation;
    private Window window;
    private ProcessManager processManager;
    private LowLevelRecorder lowLevelRecorder;

    private List<ControlType> listKeyPressControlType = new()
    {
        ControlType.ComboBox,
        ControlType.Edit
    };
    private List<AutomationEventHandlerBase> listKeyPressEventHandler = new();
    private AutomationElement[] arrayPreviousKeyPressElement;

    private List<ControlType> listClickableControlType = new()
    {
        ControlType.Button,
        ControlType.ComboBox,
        ControlType.DataItem,
        ControlType.ListItem,
        ControlType.MenuItem,
        ControlType.TreeItem
    };
    private AutomationElement[] arrayPreviousClickableElement;

    private List<ControlType> listEvaluableControlType = new()
    {
        ControlType.Text
    };
    private AutomationElement[] arrayPreviousEvaluableElement;
    private List<StepConfig> listStep = new();
    private static readonly SemaphoreSlim stopLock = new SemaphoreSlim(1, 1);

    public async Task Attach2ProcessAsync()
    {
        Process process = processManager.GetSequentialProcess();
        app = FlaUI.Core.Application.Attach(process);
        window = app.GetMainWindow(automation);
        await Task.Delay(2000);

        return;
    }

    public string EscapeXPathValue(string value)
    {
        if (!value.Contains('\''))

            return $"'{value}'";
        else if (!value.Contains('"'))

            return $"\"{value}\"";

        return "concat('" + value.Replace("'", "',\"'\",' ") + "')";
    }

    public string GetXPath(AutomationElement element)
    {
        string xPath = "";
        AutomationElement recursingElement = element;

        do
        {
            if (!string.IsNullOrEmpty(xPath))
            {
                xPath = $"/{xPath}";
            }

            string automationId = null;

            try
            {
                automationId = $"@AutomationId={EscapeXPathValue(recursingElement.AutomationId)} and ";
            }
            catch
            {
                automationId = "";
            }

            string name = $"@Name={EscapeXPathValue(recursingElement.Name)}";
            xPath = $"{recursingElement.ControlType}[{automationId}{name}]{xPath}";
            recursingElement = recursingElement.Parent;
        } while (recursingElement.ControlType != ControlType.Window);

        AutomationElement[] arrayElement = window.FindAllByXPath(xPath);

        if (arrayElement.Length < 2)
        {
            return xPath;
        }

        HashSet<string> setSelectedIdNameType = new();

        foreach (AutomationElement descendentElement in element.FindAllDescendants())
        {
            setSelectedIdNameType.Add($"{descendentElement.AutomationId},{descendentElement.Name},{descendentElement.ControlType}");
        }

        HashSet<string> setIdNameType = new();
        HashSet<string> setUniqueIdNameType = null;

        foreach (AutomationElement similarElement in arrayElement)
        {
            setIdNameType.Clear();

            foreach (AutomationElement descendentElement in similarElement.FindAllDescendants())
            {
                setIdNameType.Add($"{descendentElement.AutomationId},{descendentElement.Name},{descendentElement.ControlType}");
            }

            HashSet<string> setExclusiveIdNameType = setSelectedIdNameType.Except(setIdNameType).ToHashSet();

            if (setExclusiveIdNameType.Count == 0)
            {
                continue;
            }
            else if (setUniqueIdNameType == null)
            {
                setUniqueIdNameType = setExclusiveIdNameType;
            }
            else
            {
                setUniqueIdNameType.IntersectWith(setExclusiveIdNameType);
            }
        }

        try
        {
            List<string> listIdNameType = setUniqueIdNameType.First()
                .Split(',')
                .ToList();
            xPath = $"{xPath[..^1]} and .//{listIdNameType[2]}[@AutomationId={EscapeXPathValue(listIdNameType[0])} and @Name={EscapeXPathValue(listIdNameType[1])}]]";
        }
        catch
        {
            xPath = $".//{element.ControlType}[@AutomationId={EscapeXPathValue(element.AutomationId)} and @Name={EscapeXPathValue(element.Name)}]";
        }

        return xPath;
    }
    
    public void ShowWaitMessage(bool bShow = true)
    {
        Task.Run(() =>
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                recordViewModel.ShowWaitMessage(bShow);
            });
        });

        return;
    }

    public void AddKeyPressStep(AutomationElement element, EventId eventId)
    {
        if (eventId == automation.EventLibrary.Text.TextChangedEvent &&
            element.Properties.IsKeyboardFocusable &&
            lowLevelRecorder.bKeyPress)
        {
            string xPath = GetXPath(element);

            if (listStep.Count > 0 && listStep.Last().xPath == xPath)
            {
                listStep.Last().text = element.AsTextBox().Text;
            }
            else
            {
                listStep.Add(new StepConfig
                {
                    controlType = element.ControlType.ToString(),
                    xPath = xPath,
                    text = element.AsTextBox().Text
                });
            }
        }

        Task.Run(() => RegisterAutomationEventAsync(100));
        
        foreach (StepConfig step in listStep)
        {
            Trace.WriteLine($"debug0 {step.controlType}; {step.xPath}; {step.text}; {step.clickType}");
        }

        return;
    }

    public AutomationElement GetMousePointedElement(AutomationElement[] arrayElement)
    {
        System.Drawing.Point mousePosition = lowLevelRecorder.mousePosition;
        int minArea = int.MaxValue;
        AutomationElement mousePointedElement = null;

        foreach (AutomationElement element in arrayElement)
        {
            if (element.BoundingRectangle.Contains(mousePosition))
            {
                int area = element.BoundingRectangle.Width * element.BoundingRectangle.Height;

                if (area < minArea)
                {
                    minArea = area;
                    mousePointedElement = element;
                }
            }
        }

        return mousePointedElement;
    }

    public async Task AddMouseClickStepAsync()
    {
        await stopLock.WaitAsync();

        try
        {
            lowLevelRecorder.bRecord = false;
            ShowWaitMessage();
            AutomationElement element = await Task.Run(() => GetMousePointedElement(arrayPreviousClickableElement));

            if (element != null)
            {
                string xPath = await Task.Run(() => GetXPath(element));
                ClickType clickType = lowLevelRecorder.clickType;
                listStep.Add(new StepConfig
                {
                    controlType = element.ControlType.ToString(),
                    xPath = xPath,
                    clickType = clickType == ClickType.Left ? null : clickType.ToString()
                });
            }

            await RegisterAutomationEventAsync();
            lowLevelRecorder.bRecord = true;
            ShowWaitMessage(false);

            foreach (StepConfig step in listStep)
            {
                Trace.WriteLine($"debug0 {step.controlType}; {step.xPath}; {step.text}; {step.clickType}");
            }
        }
        finally
        {
            stopLock.Release();
        }

        return;
    }

    public async Task AddEvaluationStepAsync()
    {
        lowLevelRecorder.bRecord = false;
        ShowWaitMessage();
        AutomationElement element = await Task.Run(() => GetMousePointedElement(arrayPreviousEvaluableElement));

        if (element != null)
        {
            string xPath = await Task.Run(() => GetXPath(element));
            listStep.Add(new StepConfig
            {
                controlType = element.ControlType.ToString(),
                xPath = xPath,
                text = element.AsLabel().Text,
                bEvaluation = true
            });
        }

        await Task.Delay(1000);
        lowLevelRecorder.bRecord = true;
        ShowWaitMessage(false);

        foreach (StepConfig step in listStep)
        {
            Trace.WriteLine($"debug0 {step.controlType}; {step.xPath}; {step.text}; {step.clickType}");
        }

        return;
    }

    public void SetMouseClickStep()
    {
        listStep.Last().clickType = lowLevelRecorder.clickType.ToString();

        return;
    }

    public AutomationElement[] GetPreviousDesktopElement(List<ControlType> listControlType)
    {
        AutomationElement[] arrayElement = automation.GetDesktop()
            .FindAllChildren()
            .Where(i =>
            {
                try
                {
                    return i.Name == window.Name || string.IsNullOrEmpty(i.Name);
                }
                catch
                {
                    return false;
                }
            })
            .SelectMany(j => j.FindAllDescendants())
            .Where(k =>
            {
                try
                {
                    string dummy = $"{k.AutomationId},{k.Name},{k.ControlType}";
                    // Trace.WriteLine($"debug1 {dummy}");
                    return !k.IsOffscreen && listControlType.Contains(k.ControlType);
                }
                catch
                {
                    return false;
                }
            })
            .ToArray();

        return arrayElement;
    }

    public async Task RegisterAutomationEventAsync(int sleepTime = 1500)
    {
        try
        {
            foreach (AutomationEventHandlerBase eventHandler in listKeyPressEventHandler)
            {
                eventHandler.Dispose();
            }

            listKeyPressEventHandler.Clear();
        }
        catch
        {
            listKeyPressEventHandler.Clear();
            await Attach2ProcessAsync();
        }

        await Task.Delay(sleepTime);

        window = app.GetMainWindow(automation);
        arrayPreviousKeyPressElement = window.FindAllDescendants();
        arrayPreviousClickableElement = GetPreviousDesktopElement(listClickableControlType);
        arrayPreviousEvaluableElement = GetPreviousDesktopElement(listEvaluableControlType);

        foreach (ControlType controlType in listKeyPressControlType)
        {
            foreach (AutomationElement element in arrayPreviousKeyPressElement)
            {
                try
                {
                    if (element.ControlType != controlType)
                    {
                        continue;
                    }
                }
                catch
                {
                    continue;
                }

                Trace.WriteLine($"debug1 {element.Name} {element.ControlType}");
                AutomationEventHandlerBase eventHandler = element.RegisterAutomationEvent(automation.EventLibrary.Text.TextChangedEvent, TreeScope.Element, AddKeyPressStep);
                listKeyPressEventHandler.Add(eventHandler);
            }
        }

        /*
        Thread.Sleep(3000);
        Console.WriteLine("\n\n");
        RegisterTargetedAutomationEvent();
        */
        return;
    }

    public async Task InitAsync()
    {
        ShowWaitMessage();
        await Attach2ProcessAsync();
        await RegisterAutomationEventAsync();
        lowLevelRecorder = new LowLevelRecorder(AddMouseClickStepAsync, SetMouseClickStep, AddEvaluationStepAsync, this);
        ShowWaitMessage(false);

        return;
    }

    public Recorder(RecorderConfig _recorderConfig, List<AttacherConfig> listAttacherConfig, RecordViewModel _recordViewModel)
    {
        recorderConfig = _recorderConfig;
        recordViewModel = _recordViewModel;

        automation = new UIA3Automation();
        processManager = new ProcessManager(recorderConfig, listAttacherConfig);
        Task.Run(() => InitAsync());

        return;
    }

    public async Task RecordAsync()
    {
        lowLevelRecorder.bRecord = true;
        await RegisterAutomationEventAsync();

        return;
    }

    public void ClearKeyPressEventHandler()
    {
        try
        {
            foreach (AutomationEventHandlerBase eventHandler in listKeyPressEventHandler)
            {
                eventHandler.Dispose();
            }

            listKeyPressEventHandler.Clear();
        }
        catch
        {
            listKeyPressEventHandler.Clear();
        }

        return;
    }

    public void Pause()
    {
        lowLevelRecorder.bRecord = false;
        ClearKeyPressEventHandler();

        return;
    }

    public async Task StopAsync()
    {
        Trace.WriteLine("debug4");
        await stopLock.WaitAsync();

        try
        {
            Trace.WriteLine("debug5");
            lowLevelRecorder.Stop();
            Trace.WriteLine("debug6");
            ClearKeyPressEventHandler();
            automation.Dispose();
            Trace.WriteLine("debug7");
            processManager.Kill();
        }
        finally
        {
            stopLock.Release();
        }

        string stepName = string.IsNullOrEmpty(recorderConfig.stepName) ? "" : $"_{recorderConfig.stepName}";
        await File.WriteAllTextAsync(
            Path.Join(recorderConfig.stepDirectoryPath, $"step{stepName}_{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.json"),
            JsonSerializer.Serialize(
                listStep,
                new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true
                }
            )
        );

        return;
    }
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