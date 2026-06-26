using System.Drawing;
using System.Runtime.InteropServices;

namespace console_app;

public class LowLevelRecorder
{
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] static extern uint GetDoubleClickTime();
    [DllImport("user32.dll")] static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);
    [DllImport("user32.dll")] static extern bool PostThreadMessage(uint idThread, uint Msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);
    [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr hhk);

    private uint hookThreadId = 0;
    private uint previousLeftButtonDownTime = 0;
    private Thread thread;

    public ClickType clickType { get; set; } = default;
    public Point mousePosition { get; set; }

    private IntPtr GetMouseClickData(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            MSLLHOOKSTRUCT mouseLowLevelHook = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            mousePosition = mouseLowLevelHook.pt;

            if ((int)wParam == 0x0201)
            {
                if (mouseLowLevelHook.time - previousLeftButtonDownTime <= GetDoubleClickTime())
                {
                    previousLeftButtonDownTime = 0;
                    clickType = ClickType.DoubleLeft;
                }
                else
                {
                    previousLeftButtonDownTime = mouseLowLevelHook.time;
                    clickType = ClickType.Left;
                }
            }
            else if ((int)wParam == 0x0204)
            {
                clickType = ClickType.Right;
            }

            Thread.Sleep(100);


        }

        return 0;
    }

    public LowLevelRecorder()
    {
        thread = new Thread(() =>
        {
            hookThreadId = GetCurrentThreadId();
            IntPtr mouseHook = SetWindowsHookEx(14, GetMouseClickData, 0, 0);
            while (GetMessage(out MSG msg, 0, 0, 0) != 0) { }
            UnhookWindowsHookEx(mouseHook);
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

delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

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
    public System.Drawing.Point pt;
    public uint lPrivate;
}

public enum ClickType
{
    None,
    Left,
    DoubleLeft,
    Right
}