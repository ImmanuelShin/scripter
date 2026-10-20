using System.Runtime.InteropServices;

namespace Scripter.Services;

public sealed class HotkeyService : NativeWindow, IDisposable
{
    public const Keys DefaultStopKey = Keys.F8;

    private const int WmHotkey = 0x0312;
    private const int HotkeyId = 1;
    private const uint ModNorepeat = 0x4000;
    private const int HwndMessage = -3;

    private bool _registered;
    private bool _disposed;

    public Keys StopKey { get; private set; } = DefaultStopKey;

    public event EventHandler? StopRequested;

    public HotkeyService()
    {
        var cp = new CreateParams
        {
            Parent = new IntPtr(HwndMessage)
        };
        CreateHandle(cp);
        _registered = RegisterHotKey(Handle, HotkeyId, ModNorepeat, (uint)StopKey);
    }

    public bool IsRegistered => _registered;

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey && m.WParam.ToInt32() == HotkeyId)
        {
            StopRequested?.Invoke(this, EventArgs.Empty);
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_registered && Handle != IntPtr.Zero)
        {
            UnregisterHotKey(Handle, HotkeyId);
            _registered = false;
        }

        DestroyHandle();
        _disposed = true;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
