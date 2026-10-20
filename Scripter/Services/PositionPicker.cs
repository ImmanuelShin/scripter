using System.Runtime.InteropServices;
using Scripter.Forms;

namespace Scripter.Services;

public sealed class PositionPicker : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WhMouseLl = 14;
    private const int WmKeydown = 0x0100;
    private const int WmSyskeydown = 0x0104;
    private const int WmLbuttondown = 0x0201;
    private const int WmLbuttonup = 0x0202;
    private const int VkEscape = 0x1B;
    private const int VkReturn = 0x0D;
    private const int VkSpace = 0x20;

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private LowLevelMouseProc? _mouseProc;
    private LowLevelKeyboardProc? _keyboardProc;
    private IntPtr _mouseHook = IntPtr.Zero;
    private IntPtr _keyboardHook = IntPtr.Zero;
    private TaskCompletionSource<Point>? _tcs;
    private PositionPickHud? _hud;
    private Point _pendingClick;
    private bool _swallowClick;
    private bool _disposed;

    public Task<Point> PickPositionAsync()
    {
        if (_tcs != null && !_tcs.Task.IsCompleted)
        {
            throw new InvalidOperationException("Position pick already in progress.");
        }

        _tcs = new TaskCompletionSource<Point>(TaskCreationOptions.RunContinuationsAsynchronously);
        _swallowClick = false;

        _hud = new PositionPickHud();
        _hud.LockRequested += (_, _) => Complete(_hud.Candidate);
        _hud.CancelRequested += (_, _) => Cancel();
        _hud.Show();

        _mouseProc = MouseHookCallback;
        _keyboardProc = KeyboardHookCallback;
        _mouseHook = SetMouseHookEx(WhMouseLl, _mouseProc, GetModuleHandle(null), 0);
        _keyboardHook = SetKeyboardHookEx(WhKeyboardLl, _keyboardProc, GetModuleHandle(null), 0);

        if (_mouseHook == IntPtr.Zero || _keyboardHook == IntPtr.Zero)
        {
            Cleanup();
            _tcs.TrySetException(new InvalidOperationException("Failed to install input hooks for position picking."));
        }

        return _tcs.Task;
    }

    public void Cancel()
    {
        if (_tcs != null && !_tcs.Task.IsCompleted)
        {
            Cleanup();
            _tcs.TrySetCanceled();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Cancel();
        Cleanup();
        _disposed = true;
    }

    private void Complete(Point point)
    {
        if (_tcs == null || _tcs.Task.IsCompleted)
        {
            return;
        }

        Cleanup();
        _tcs.TrySetResult(point);
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _tcs != null && !_tcs.Task.IsCompleted)
        {
            var hookStruct = Marshal.PtrToStructure<Msllhookstruct>(lParam);
            var point = new Point(hookStruct.pt.x, hookStruct.pt.y);
            var onHud = _hud?.ContainsScreenPoint(point) == true;

            if (wParam == (IntPtr)WmLbuttondown && !onHud)
            {
                _pendingClick = point;
                _swallowClick = true;
                return (IntPtr)1;
            }

            if (wParam == (IntPtr)WmLbuttonup && _swallowClick)
            {
                _swallowClick = false;
                Complete(_pendingClick);
                return (IntPtr)1;
            }
        }

        return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == (IntPtr)WmKeydown || wParam == (IntPtr)WmSyskeydown)
            && _tcs != null && !_tcs.Task.IsCompleted)
        {
            var hookStruct = Marshal.PtrToStructure<Kbdllhookstruct>(lParam);
            if (hookStruct.vkCode == VkReturn || hookStruct.vkCode == VkSpace)
            {
                Complete(_hud?.Candidate ?? Cursor.Position);
                return (IntPtr)1;
            }

            if (hookStruct.vkCode == VkEscape)
            {
                Cancel();
                return (IntPtr)1;
            }
        }

        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private void Cleanup()
    {
        Unhook(ref _mouseHook);
        Unhook(ref _keyboardHook);

        if (_hud == null)
        {
            return;
        }

        var hud = _hud;
        _hud = null;
        if (hud.IsHandleCreated)
        {
            hud.BeginInvoke(() =>
            {
                hud.Hide();
                hud.Dispose();
            });
        }
        else
        {
            hud.Dispose();
        }
    }

    private static void Unhook(ref IntPtr hook)
    {
        if (hook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(hook);
            hook = IntPtr.Zero;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Win32Point
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Msllhookstruct
    {
        public Win32Point pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Kbdllhookstruct
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "SetWindowsHookEx")]
    private static extern IntPtr SetMouseHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "SetWindowsHookEx")]
    private static extern IntPtr SetKeyboardHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
