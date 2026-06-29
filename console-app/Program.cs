using Recorder;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.EventHandlers;
using FlaUI.Core.Identifiers;
using FlaUI.UIA3;
using System.Diagnostics;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using YamlDotNet.Serialization;

RecorderConfig recorderConfig = new DeserializerBuilder().Build().Deserialize<RecorderConfig>(File.ReadAllText("config.yml"));

FlaUI.Core.Application app;
using UIA3Automation automation = new();
Window window;
ProcessManager processManager = new();
LowLevelRecorder lowLevelRecorder = new();

List<ControlType> listKeyPressControlType = new()
{
    ControlType.ComboBox,
    ControlType.Edit
};
List<AutomationEventHandlerBase> listKeyPressEventHandler = new();
AutomationElement[] arrayPreviousKeyPressElement;

List<ControlType> listClickableControlType = new()
{
    ControlType.Button,
    ControlType.ComboBox,
    ControlType.DataItem,
    ControlType.ListItem,
    ControlType.MenuItem,
    ControlType.TreeItem
};
AutomationElement[] arrayPreviousClickableElement;

List<ControlType> listEvaluableControlType = new()
{
    ControlType.Text
};
AutomationElement[] arrayPreviousEvaluableElement;
List<StepConfig> listStep = new();

void Attach2Process()
{
    Process process = processManager.GetSequentialProcess();
    app = FlaUI.Core.Application.Attach(process);
    Thread.Sleep(2000);
    window = app.GetMainWindow(automation);

    return;
}

string EscapeXPathValue(string value)
{
    if (!value.Contains('\'' ))

        return $"'{value}'";
    else if (!value.Contains('"'))

        return $"\"{value}\"";

    return "concat('" + value.Replace("'", "',\"'\",' ") + "')"; 
}

string GetXPath(AutomationElement element)
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

void AddKeyPressStep(AutomationElement element, EventId eventId)
{
    if (eventId == automation.EventLibrary.Text.TextChangedEvent && element.Properties.IsKeyboardFocusable && lowLevelRecorder.bKeyPress)
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

    RegisterAutomationEvent(100);

    foreach (StepConfig step in listStep)
    {
        Console.WriteLine($"debug0 {step.controlType}; {step.xPath}; {step.text}; {step.clickType}");
    }

    return;
}

AutomationElement GetMousePointedElement(AutomationElement[] arrayElement)
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

void AddMouseClickStep()
{
    Console.WriteLine("Click detected, please wait");
    lowLevelRecorder.bRecord = false;
    AutomationElement element = GetMousePointedElement(arrayPreviousClickableElement);

    if (element != null)
    {
        string xPath = GetXPath(element);
        ClickType clickType = lowLevelRecorder.clickType;
        listStep.Add(new StepConfig
        {
            controlType = element.ControlType.ToString(),
            xPath = xPath,
            clickType = clickType == ClickType.Left ? null : clickType.ToString()
        });
    }

    RegisterAutomationEvent();
    lowLevelRecorder.bRecord = true;

    foreach (StepConfig step in listStep)
    {
        Console.WriteLine($"debug0 {step.controlType}; {step.xPath}; {step.text}; {step.clickType}");
    }

    return;
}

void AddEvaluationStep()
{
    AutomationElement element = GetMousePointedElement(arrayPreviousEvaluableElement);

    if (element != null)
    {
        string xPath = GetXPath(element);
        listStep.Add(new StepConfig
        {
            controlType = element.ControlType.ToString(),
            xPath = xPath,
            text = element.AsLabel().Text,
            bEvaluation = true
        });
    }

    foreach (StepConfig step in listStep)
    {
        Console.WriteLine($"debug0 {step.controlType}; {step.xPath}; {step.text}; {step.clickType}");
    }

    return;
}

void SetMouseClickStep()
{
    listStep.Last().clickType = lowLevelRecorder.clickType.ToString();

    return;
}

AutomationElement[] GetPreviousDesktopElement(List<ControlType> listControlType)
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
                Console.WriteLine($"debug2 {dummy}");
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

void RegisterAutomationEvent(int sleepTime = 2000)
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
        Attach2Process();
    }

    Thread.Sleep(sleepTime);

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
                if(element.ControlType != controlType)
                {
                    continue;
                }
            }
            catch
            {
                continue;
            }

            Console.WriteLine($"debug1 {element.Name} {element.ControlType}");
            AutomationEventHandlerBase eventHandler = element.RegisterAutomationEvent(automation.EventLibrary.Text.TextChangedEvent, TreeScope.Element, AddKeyPressStep);
            listKeyPressEventHandler.Add(eventHandler);
        }
    }

    Console.WriteLine("\nPlease continue");

    if(listKeyPressEventHandler.Count == 0)
    {
        Console.WriteLine("You may want to press right `ctrl` key for refreshing");
    }

    Console.WriteLine();

    /*
    Thread.Sleep(3000);
    Console.WriteLine("\n\n");
    RegisterTargetedAutomationEvent();
    */
    return;
}

Console.WriteLine("Please wait");

Attach2Process();
RegisterAutomationEvent();
lowLevelRecorder.SetStep(AddMouseClickStep, SetMouseClickStep, AddEvaluationStep, RegisterAutomationEvent);
lowLevelRecorder.bRecord = true;
Console.ReadKey();

lowLevelRecorder.Stop();

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

string stepName = string.IsNullOrEmpty(recorderConfig.stepName) ? "" : $"_{recorderConfig.stepName}";
File.WriteAllText(
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
processManager.Kill();