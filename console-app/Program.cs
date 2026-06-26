using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.EventHandlers;
using FlaUI.Core.Identifiers;
using FlaUI.Core.Input;
using FlaUI.UIA3;
using Shared;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using YamlDotNet.Serialization;

[DllImport("user32.dll")] static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);
[DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr hhk);
[DllImport("user32.dll")] static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);
[DllImport("user32.dll")] static extern bool PostThreadMessage(uint idThread, uint Msg, IntPtr wParam, IntPtr lParam);
[DllImport("user32.dll")] static extern uint GetDoubleClickTime();
[DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();

uint hookThreadId = 0;
uint lastLButtonDownTime = 0;

string configText = File.ReadAllText("config.yml");
UserConfig config = new DeserializerBuilder().Build().Deserialize<UserConfig>(configText);

FlaUI.Core.Application app;
using UIA3Automation automation = new();
Window window;
VisualStudioLauncher launcher = new();

List<ControlType> listEventControlType = new()
{
    ControlType.Button,
    ControlType.Custom,
    ControlType.ComboBox,
    ControlType.Edit,
    ControlType.ListItem,
    ControlType.MenuItem,
    ControlType.TabItem
};
List<AutomationEventHandlerBase> listEventHandler = new();

List<ControlType> listStructureChangedControlType = new()
{
    ControlType.ListItem,
    ControlType.Menu,
    ControlType.MenuItem
};
Dictionary<ControlType, HashSet<string>> dictPreviousStructure = listStructureChangedControlType.ToDictionary(i => i, i => new HashSet<string>());
StructureChangedEventHandlerBase structureChangedHandler = null;

AutomationElement[] arrayPreviousElement;
List<StepConfig> listStep = new();
bool bSuppressStructureChangedEvent = false;

void Attach2Process()
{
    Process process = launcher.Attach();
    app = FlaUI.Core.Application.Attach(process);
    window = app.GetMainWindow(automation);
    Thread.Sleep(2000);

    return;
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
            automationId = $"@AutomationId='{recursingElement.AutomationId}' and ";
        }
        catch
        {
            automationId = "";
        }

        string name = $"@Name='{recursingElement.Name}'";
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

    List<string> listIdNameType = setUniqueIdNameType.First()
        .Split(',')
        .ToList();
    xPath = $"{xPath[..^1]} and .//{listIdNameType[2]}[@AutomationId='{listIdNameType[0]}' and @Name='{listIdNameType[1]}']]";

    return xPath;
}

void AddEventStep(AutomationElement element, EventId eventId)
{
    Console.WriteLine("AddEventStep");
    bSuppressStructureChangedEvent = true;
    string xPath = GetXPath(element);
    bool bRefreshWindow = false;

    if (eventId == automation.EventLibrary.Text.TextChangedEvent && element.Properties.IsKeyboardFocusable)
    {
        if (listStep.Last().xPath == xPath)
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
    else if(eventId == automation.EventLibrary.Invoke.InvokedEvent)
    {
        listStep.Add(new StepConfig
        {
            controlType = element.ControlType.ToString(),
            xPath = xPath
        });

        if(element.ControlType == ControlType.MenuItem)
        {
            bRefreshWindow = true;
        }
    }
    else if(element.ControlType == ControlType.ComboBox)
    {
        ComboBoxItem selectedItem = element.AsComboBox().SelectedItem;

        if(selectedItem != null)
        {
            listStep.Add(new StepConfig
            {
                controlType = element.ControlType.ToString(),
                xPath = xPath,
                text = selectedItem.Text
            });
        }
    }
    else
    {
        listStep.Add(new StepConfig
        {
            controlType = element.ControlType.ToString(),
            xPath = xPath
        });
    }

    RegisterTargetedAutomationEvent(bRefreshWindow);

    foreach (StepConfig step in listStep)
    {
        Console.WriteLine($"debug0 {step.controlType}; {step.xPath}; {step.text}; {step.bRightClick}");
    }

    return;
}

void AddStructureChangedStep(AutomationElement element, StructureChangeType changeType, int[] runtimeId)
{
    if(bSuppressStructureChangedEvent)
    {
        bSuppressStructureChangedEvent = false;

        return;
    }

    try
    {
        if(element.ControlType != ControlType.Window)
        {
            return;
        }
    }
    catch
    {
        return;
    }

    try
    {
        Console.WriteLine("AddStructureChangedStep");
    }
    catch { }

    try
    {
        window = app.GetMainWindow(automation);
        ControlType changedControlType = default;
        bool bStructureAdded = false;
        bool bStructureRemoved = false;

        foreach (ControlType controlType in listStructureChangedControlType)
        {
            HashSet<string> setCurrentStructure = window.FindAllDescendants(cf => cf.ByControlType(controlType))
                .Select(i => $"{i.AutomationId},{i.Name}")
                .ToHashSet();

            if (setCurrentStructure.Except(dictPreviousStructure[controlType]).Count() > 0)
            {
                changedControlType = controlType;
                bStructureAdded = true;

                break;
            }
            else if(dictPreviousStructure[controlType].Except(setCurrentStructure).Count() > 0)
            {
                bStructureRemoved = true;

                break;
            }
        }

        if(bStructureAdded)
        {
            if (changedControlType == ControlType.ListItem || changedControlType == ControlType.Menu || changedControlType == ControlType.MenuItem)
            {
                System.Drawing.Point mousePosition = Mouse.Position;
                int minArea = int.MaxValue;
                AutomationElement clickedElement = null;

                foreach(AutomationElement descendantElement in arrayPreviousElement)
                {
                    if (descendantElement.BoundingRectangle.Contains(mousePosition))
                    {
                        int area = descendantElement.BoundingRectangle.Width * descendantElement.BoundingRectangle.Height;

                        if (area < minArea)
                        {
                            minArea = area;
                            clickedElement = descendantElement;
                        }
                    }
                }

                if(clickedElement != null)
                {
                    string xPath = $".//{clickedElement.ControlType}[@AutomationId='{clickedElement.AutomationId}' and @Name='{clickedElement.Name}']";
                    listStep.Add(new StepConfig
                    {
                        controlType = clickedElement.ControlType.ToString(),
                        xPath = xPath,
                        bRightClick = changedControlType == ControlType.Menu
                    });
                }
            }

            RegisterTargetedAutomationEvent(); 
        }
        else if(bStructureRemoved)
        {
            RegisterTargetedAutomationEvent();
        }
    }
    catch { }

    return;
}

IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
{
    if (nCode >= 0)
    {
        MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

        switch ((int)wParam)
        {
            case 0x0201:
                if (hookStruct.time - lastLButtonDownTime <= GetDoubleClickTime())
                {
                    Console.WriteLine($"Double click at {hookStruct.pt}");
                    lastLButtonDownTime = 0;
                }
                else
                {
                    Console.WriteLine($"Left click at {hookStruct.pt}");
                    lastLButtonDownTime = hookStruct.time;
                }
                break;
            case 0x0204:
                Console.WriteLine($"Right click at {hookStruct.pt}");
                break;
        }
    }

    return 0;
}


void RegisterTargetedAutomationEvent(bool bRefreshWindow = false)
{
    try
    {
        foreach (AutomationEventHandlerBase eventHandler in listEventHandler)
        {
            eventHandler.Dispose();
        }

        listEventHandler.Clear();
        structureChangedHandler?.Dispose();
    }
    catch
    {
        listEventHandler.Clear();
        structureChangedHandler = null;
        Attach2Process();
    }
    /*
    Console.WriteLine($"debug4 {window.FindAllDescendants().Count()}");
    if(window.FindAllDescendants().Count() == 0)
    {
        Attach2Process();
    }
    
    window = app.GetMainWindow(automation);
    */

    Thread.Sleep(1500);

    if(bRefreshWindow)
    {
        window = app.GetMainWindow(automation);
    }

    arrayPreviousElement = window.FindAllDescendants();

    foreach (ControlType controlType in listEventControlType)
    {
        foreach (AutomationElement element in arrayPreviousElement.Where(i => i.ControlType == controlType))
        {
            Console.WriteLine($"debug1 {element.Name} {element.ControlType}");
            List<EventId> listEventId = new();

            switch (controlType)
            {
                case ControlType.Button:
                case ControlType.MenuItem:
                    listEventId.Add(automation.EventLibrary.Invoke.InvokedEvent);
                    break;
                case ControlType.ComboBox:
                    listEventId.Add(automation.EventLibrary.Selection.InvalidatedEvent);
                    break;
                case ControlType.Edit:
                    listEventId.Add(automation.EventLibrary.Text.TextChangedEvent);
                    break;
                case ControlType.Custom:
                case ControlType.ListItem:
                case ControlType.TabItem:
                    listEventId.Add(automation.EventLibrary.SelectionItem.ElementSelectedEvent);
                    break;
            }
            
            foreach (EventId eventId in listEventId)
            {
                AutomationEventHandlerBase eventHandler = element.RegisterAutomationEvent(eventId, TreeScope.Element, AddEventStep);
                listEventHandler.Add(eventHandler);
            }
        }
    }

    foreach(ControlType controlType in dictPreviousStructure.Keys)
    {
        dictPreviousStructure[controlType].Clear();

        foreach(AutomationElement element in arrayPreviousElement.Where(i => i.ControlType == controlType))
        {
            try
            {
                dictPreviousStructure[controlType].Add($"{element.AutomationId},{element.Name}");

            }
            catch { }
        }
    }
    
    if(window.Parent != null)
    {
        structureChangedHandler = window.Parent.RegisterStructureChangedEvent(TreeScope.Subtree, AddStructureChangedStep);
    }
    
    /*
    Thread.Sleep(3000);
    Console.WriteLine("\n\n");
    RegisterTargetedAutomationEvent();
    */
    return;
}

Thread hookThread = new Thread(() =>
{
    hookThreadId = GetCurrentThreadId();
    IntPtr mouseHook = SetWindowsHookEx(14, MouseHookCallback, 0, 0);
    
    while (GetMessage(out MSG msg, 0, 0, 0) != 0) { }

    UnhookWindowsHookEx(mouseHook);
});
hookThread.Start();

Attach2Process();
RegisterTargetedAutomationEvent();
Console.ReadKey();

File.WriteAllText(
    Path.Join(config.stepDirectoryPath, $"step_{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.json"),
    JsonSerializer.Serialize(
        listStep,
        new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        }
    )
);

PostThreadMessage(hookThreadId, 0x0012, 0, 0);
hookThread.Join();

delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

[StructLayout(LayoutKind.Sequential)]
struct MSLLHOOKSTRUCT
{
    public System.Drawing.Point pt;
    public uint mouseData;
    public uint flags;
    public uint time;
    public IntPtr dwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
struct MSG
{
    public IntPtr hwnd;
    public uint message;
    public IntPtr wParam;
    public IntPtr lParam;
    public uint time;
    public System.Drawing.Point pt;
    public uint lPrivate;
}
