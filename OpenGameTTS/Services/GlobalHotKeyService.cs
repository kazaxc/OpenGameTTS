using System;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Threading;

namespace OpenGameTTS.Services;

/// <summary>
/// Registers a system-wide hotkey (Ctrl+Enter) via a native message-only window,
/// since Avalonia exposes no public hook into its own window's WndProc.
/// </summary>
public sealed class GlobalHotKeyService : IDisposable
{
    private const int HotKeyId = 0x1001;
    private const uint ModControl = 0x0002;
    private const uint VkReturn = 0x0D;
    private const uint WmHotKey = 0x0312;
    private const uint WmDestroy = 0x0002;
    private const uint WmClose = 0x0010;
    private static readonly IntPtr HwndMessage = new(-3);

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WndClassEx
    {
        public int cbSize;
        public uint style;
        public WndProcDelegate lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)] public string? lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Msg
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public Point pt;
    }

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandleW(string? lpModuleName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClassExW(ref WndClassEx lpwcx);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool UnregisterClassW(string lpClassName, IntPtr hInstance);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowExW(uint dwExStyle, string lpClassName, string lpWindowName,
        uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu,
        IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern void PostQuitMessage(int nExitCode);

    [DllImport("user32.dll")]
    private static extern bool GetMessageW(out Msg lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref Msg lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessageW(ref Msg lpMsg);

    [DllImport("user32.dll")]
    private static extern bool PostMessageW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly string _className = "OpenGameTTS_HotkeyWnd_" + Guid.NewGuid().ToString("N");
    private readonly WndProcDelegate _wndProcDelegate;
    private readonly ManualResetEventSlim _ready = new(false);
    private Thread? _thread;
    private IntPtr _hwnd;
    private bool _hotKeyRegistered;

    public event EventHandler? HotKeyPressed;

    public GlobalHotKeyService()
    {
        _wndProcDelegate = WndProcImpl;
    }

    public void Start()
    {
        if (_thread != null) return;

        _thread = new Thread(RunMessageLoop) { IsBackground = true, Name = "OpenGameTTS-HotkeyWnd" };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        _ready.Wait();
    }

    private void RunMessageLoop()
    {
        var hInstance = GetModuleHandleW(null);
        var wndClass = new WndClassEx
        {
            cbSize = Marshal.SizeOf<WndClassEx>(),
            lpfnWndProc = _wndProcDelegate,
            hInstance = hInstance,
            lpszClassName = _className
        };
        RegisterClassExW(ref wndClass);

        _hwnd = CreateWindowExW(0, _className, string.Empty, 0, 0, 0, 0, 0,
            HwndMessage, IntPtr.Zero, hInstance, IntPtr.Zero);

        if (_hwnd != IntPtr.Zero)
        {
            _hotKeyRegistered = RegisterHotKey(_hwnd, HotKeyId, ModControl, VkReturn);
        }

        _ready.Set();

        if (_hwnd == IntPtr.Zero) return;

        while (GetMessageW(out var msg, IntPtr.Zero, 0, 0))
        {
            TranslateMessage(ref msg);
            DispatchMessageW(ref msg);
        }

        if (_hotKeyRegistered)
        {
            UnregisterHotKey(_hwnd, HotKeyId);
        }
        UnregisterClassW(_className, hInstance);
    }

    private IntPtr WndProcImpl(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case WmHotKey when wParam.ToInt32() == HotKeyId:
                Dispatcher.UIThread.Post(() => HotKeyPressed?.Invoke(this, EventArgs.Empty));
                return IntPtr.Zero;
            case WmClose:
                DestroyWindow(hWnd);
                return IntPtr.Zero;
            case WmDestroy:
                PostQuitMessage(0);
                return IntPtr.Zero;
            default:
                return DefWindowProcW(hWnd, msg, wParam, lParam);
        }
    }

    public void Dispose()
    {
        if (_thread == null) return;

        if (_hwnd != IntPtr.Zero)
        {
            PostMessageW(_hwnd, WmClose, IntPtr.Zero, IntPtr.Zero);
        }
        _thread.Join(TimeSpan.FromSeconds(2));
        _thread = null;
        _ready.Dispose();
    }
}
