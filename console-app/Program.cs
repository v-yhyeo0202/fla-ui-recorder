using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.EventHandlers;
using FlaUI.Core.Identifiers;
using FlaUI.UIA3;
using System.Diagnostics;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

Process.Start("C:\\Program Files\\Microsoft Visual Studio\\18\\Community\\Common7\\IDE\\devenv.exe");
Process process = null;

while (process == null)
{
    Thread.Sleep(200);
    process = Process.GetProcessesByName("devenv")
        .Where(i => i.MainWindowTitle == "Microsoft Visual Studio")
        .OrderByDescending(j => j.StartTime)
        .FirstOrDefault();
}

FlaUI.Core.Application app = FlaUI.Core.Application.Attach(process);
using UIA3Automation automation = new();
Window window = app.GetMainWindow(automation);
Thread.Sleep(2000);

// using UIA3Automation automation = new();
// FlaUI.Core.Application app;
List<ControlType> listControlType = new List<ControlType>
{
    ControlType.Button,
    ControlType.Edit,
    ControlType.ListItem
};
List<AutomationEventHandlerBase> listEventHandler = new();
List<Dictionary<string, string>> listStep = new();
Lock eventLock = new();

string GetUniqueXPath(AutomationElement selectedElement, string xPath)
{
    AutomationElement[] arrayElement = window.FindAllByXPath(xPath);
    Console.WriteLine($"debug4 {xPath}");
    if (arrayElement.Length < 2)
    {
        return xPath;
    }

    HashSet<string> setSelectedIdNameType = new();

    foreach (AutomationElement descendentElement in selectedElement.FindAllDescendants())
    {
        setSelectedIdNameType.Add($"{descendentElement.AutomationId},{descendentElement.Name},{descendentElement.ControlType}");
    }

    HashSet<string> setIdNameType = new();
    HashSet<string> setUniqueIdNameType = null;

    foreach (AutomationElement element in arrayElement)
    {
        setIdNameType.Clear();

        foreach (AutomationElement descendentElement in element.FindAllDescendants())
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

    List<string> listIdNameType = setUniqueIdNameType.First()
        .Split(',')
        .ToList();
    xPath = $"{xPath[..^1]} and .//{listIdNameType[2]}[@AutomaticId='{listIdNameType[0]}' and @Name='{listIdNameType[1]}']]";

    return xPath;
}

void AddXPath(AutomationElement element, EventId eventId)
{
    lock (eventLock)
    {
        string xPath = "";
        AutomationElement recursingElement = element;

        do
        {
            if (!string.IsNullOrEmpty(xPath))
            {
                xPath = $"/{xPath}";
            }

            string automationId = $"@AutomationId='{recursingElement.AutomationId}'";
            string name = $"@Name='{recursingElement.Name}'";
            xPath = $"{recursingElement.ControlType}[{automationId} and {name}]{xPath}";
            recursingElement = recursingElement.Parent;
        } while (recursingElement.ControlType != ControlType.Window);

        xPath = GetUniqueXPath(element, xPath);

        if (eventId == automation.EventLibrary.Text.TextChangedEvent)
        {
            if (listStep.Last()["xPath"] == xPath)
            {
                listStep.Last()["text"] = element.AsTextBox().Text;
            }
            else
            {
                listStep.Add(new Dictionary<string, string>
                {
                    { "xPath", xPath },
                    { "text", element.AsTextBox().Text }
                });
            }
        }
        else
        {
            listStep.Add(new Dictionary<string, string>
            {
                { "xPath", xPath }
            });
        }

        RegisterTargetedAutomationEvent();
        Console.WriteLine("debug0");

        foreach (Dictionary<string, string> dictStep in listStep)
        {
            foreach (KeyValuePair<string, string> kvp in dictStep)
            {
                Console.WriteLine($"{kvp.Key} {kvp.Value}");
            }
        }
    }

    return;
}

void RegisterTargetedAutomationEvent()
{
    foreach (AutomationEventHandlerBase eventHandler in listEventHandler)
    {
        eventHandler.Dispose();
    }

    listEventHandler.Clear();

    foreach (ControlType controlType in listControlType)
    {
        foreach (AutomationElement element in window.FindAllDescendants(cf => cf.ByControlType(controlType)))
        {
            Console.WriteLine($"debug1 {element.Name} {element.ControlType}");
            EventId eventId = null;

            switch (controlType)
            {
                case ControlType.Button:
                    eventId = automation.EventLibrary.Invoke.InvokedEvent;
                    break;
                case ControlType.Edit:
                    eventId = automation.EventLibrary.Text.TextChangedEvent;
                    break;
                case ControlType.ListItem:
                    eventId = automation.EventLibrary.SelectionItem.ElementSelectedEvent;
                    break;
            }

            AutomationEventHandlerBase eventHandler = element.RegisterAutomationEvent(eventId, TreeScope.Element, AddXPath);
            listEventHandler.Add(eventHandler);
        }
    }

    return;
}

RegisterTargetedAutomationEvent();
Console.ReadKey();

File.WriteAllText(
    "C:\\fla-ui-recorder\\xPath.json",
    JsonSerializer.Serialize(
        listStep,
        new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        }
    )
);
