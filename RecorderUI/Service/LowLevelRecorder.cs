using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace RecorderUI.Service;

public class LowLevelRecorder
{
    [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] static extern uint GetDoubleClickTime();
    [DllImport("user32.dll")] static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);
    [DllImport("user32.dll")] static extern bool PostThreadMessage(uint idThread, uint Msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);
    [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr hhk);

    private uint hookThreadId = 0;
    private LowLevelProc lowLevelKeyboardProc;
    private LowLevelProc lowLevelMouseProc;
    private IntPtr keyboardHook;
    private IntPtr mouseHook;
    private uint previousLeftButtonDownTime = 0;
    private Thread thread;
    private Func<Task> addMouseClickStepAsync = null;
    private Action setMouseClickStep = null;
    private Func<Task> addEvaluationStepAsync = null;
    private Recorder recorder;

    public bool bKeyPress = true;
    public ClickType clickType { get; set; } = default;
    public Point mousePosition { get; set; }
    public bool bRecord { get; set; } = true;

    private IntPtr GetKeyPressData(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && bRecord)
        {
            bKeyPress = true;
            KBDLLHOOKSTRUCT keyPressData = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

            if ((int)wParam == 0x100 && keyPressData.vkCode == 0xA2)
            {
                Task.Run(() => addEvaluationStepAsync());
            }
            else if((int)wParam == 0x100 && keyPressData.vkCode == 0xA3)
            {
                Task.Run(() => recorder.RegisterAutomationEventAsync(0));
            }

            Thread.Sleep(200);
            bKeyPress = false;
        }

        return CallNextHookEx(keyboardHook, nCode, wParam, lParam);
    }

    private IntPtr GetMouseClickData(int nCode, IntPtr wParam, IntPtr lParam)
    {

        if (nCode >= 0 && bRecord)
        {
            MSLLHOOKSTRUCT mouseClickData = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            mousePosition = mouseClickData.pt;

            if ((int)wParam == 0x0201)
            {
                if (mouseClickData.time - previousLeftButtonDownTime <= GetDoubleClickTime())
                {
                    previousLeftButtonDownTime = 0;
                    clickType = ClickType.Double;
                    setMouseClickStep();
                }
                else
                {
                    previousLeftButtonDownTime = mouseClickData.time;
                    clickType = ClickType.Left;
                    Task.Run(() => addMouseClickStepAsync());
                }
            }
            else if ((int)wParam == 0x0204)
            {
                clickType = ClickType.Right;
                Task.Run(() => addMouseClickStepAsync());
            }
        }

        return CallNextHookEx(mouseHook, nCode, wParam, lParam);
    }

    public void Hook()
    {
        keyboardHook = SetWindowsHookEx(13, lowLevelKeyboardProc, 0, 0);
        mouseHook = SetWindowsHookEx(14, lowLevelMouseProc, 0, 0);

        return;
    }

    public void Unhook()
    {
        UnhookWindowsHookEx(keyboardHook);
        UnhookWindowsHookEx(mouseHook);

        return;
    }

    public LowLevelRecorder(Func<Task> _addMouseClickStepAsync, Action _setMouseClickStep, Func<Task> _addEvaluationStepAsync, Recorder _recorder)
    {
        addMouseClickStepAsync = _addMouseClickStepAsync;
        setMouseClickStep = _setMouseClickStep;
        addEvaluationStepAsync = _addEvaluationStepAsync;
        recorder = _recorder;
        lowLevelKeyboardProc = GetKeyPressData;
        lowLevelMouseProc = GetMouseClickData;

        thread = new Thread(() =>
        {
            hookThreadId = GetCurrentThreadId();
            Hook();
            while (GetMessage(out MSG msg, 0, 0, 0) != 0) { }
            Unhook();
        });
        thread.Start();

        return;
    }

    public void Stop()
    {
        PostThreadMessage(hookThreadId, 0x0012, 0, 0);
        thread.Join();

        return;
    }
}

delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

[StructLayout(LayoutKind.Sequential)]
struct KBDLLHOOKSTRUCT
{
    public uint vkCode;
    public uint scanCode;
    public uint flags;
    public uint time;
    public UIntPtr dwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
struct MSLLHOOKSTRUCT
{
    public Point pt;
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
    public Point pt;
    public uint lPrivate;
}

public enum ClickType
{
    None,
    Left,
    Double,
    Right
}