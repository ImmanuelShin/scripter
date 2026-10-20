using System.Runtime.InteropServices;

namespace Scripter.Services;

public static class InputSimulator
{
    private const int InputMouse = 0;
    private const int InputKeyboard = 1;

    private const uint MouseeventfLeftdown = 0x0002;
    private const uint MouseeventfLeftup = 0x0004;
    private const uint MouseeventfRightdown = 0x0008;
    private const uint MouseeventfRightup = 0x0010;

    private const uint KeyeventfKeyup = 0x0002;

    private static readonly Dictionary<string, ushort> VirtualKeys = BuildVirtualKeyMap();

    public static void MoveMouse(int x, int y)
    {
        SetCursorPos(x, y);
    }

    public static void ClickMouse(int x, int y, string button = "Left")
    {
        SetCursorPos(x, y);
        Thread.Sleep(10);

        var (down, up) = button.Equals("Right", StringComparison.OrdinalIgnoreCase)
            ? (MouseeventfRightdown, MouseeventfRightup)
            : (MouseeventfLeftdown, MouseeventfLeftup);

        SendMouseInput(down);
        SendMouseInput(up);
    }

    public static void PressKey(string chord)
    {
        var parsed = KeyChord.Parse(chord);
        if (!VirtualKeys.TryGetValue(parsed.Key, out var vk))
        {
            throw new ArgumentException($"Unknown key: {parsed.Key}", nameof(chord));
        }

        var modifiers = new List<ushort>();
        if (parsed.Ctrl)
        {
            modifiers.Add(0x11);
        }

        if (parsed.Shift)
        {
            modifiers.Add(0x10);
        }

        if (parsed.Alt)
        {
            modifiers.Add(0x12);
        }

        foreach (var modifier in modifiers)
        {
            SendKeyboardInput(modifier, false);
        }

        SendKeyboardInput(vk, false);
        SendKeyboardInput(vk, true);

        for (var i = modifiers.Count - 1; i >= 0; i--)
        {
            SendKeyboardInput(modifiers[i], true);
        }
    }

    public static IReadOnlyList<string> SupportedKeys => VirtualKeys.Keys.OrderBy(k => k).ToList();

    private static void SendMouseInput(uint flags)
    {
        var input = new Input
        {
            type = InputMouse,
            U = new InputUnion
            {
                mi = new MouseInput { dwFlags = flags }
            }
        };

        if (SendInput(1, [input], Marshal.SizeOf<Input>()) == 0)
        {
            throw new InvalidOperationException("SendInput failed for mouse event.");
        }
    }

    private static void SendKeyboardInput(ushort virtualKey, bool keyUp)
    {
        var input = new Input
        {
            type = InputKeyboard,
            U = new InputUnion
            {
                ki = new KeyboardInput
                {
                    wVk = virtualKey,
                    dwFlags = keyUp ? KeyeventfKeyup : 0
                }
            }
        };

        if (SendInput(1, [input], Marshal.SizeOf<Input>()) == 0)
        {
            throw new InvalidOperationException("SendInput failed for keyboard event.");
        }
    }

    private static Dictionary<string, ushort> BuildVirtualKeyMap()
    {
        var map = new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase)
        {
            ["Space"] = 0x20,
            ["Enter"] = 0x0D,
            ["Tab"] = 0x09,
            ["Escape"] = 0x1B,
            ["Backspace"] = 0x08,
            ["Delete"] = 0x2E,
            ["Up"] = 0x26,
            ["Down"] = 0x28,
            ["Left"] = 0x25,
            ["Right"] = 0x27,
            ["Home"] = 0x24,
            ["End"] = 0x23,
            ["PageUp"] = 0x21,
            ["PageDown"] = 0x22,
            ["Insert"] = 0x2D,
            ["Shift"] = 0x10,
            ["Control"] = 0x11,
            ["Alt"] = 0x12
        };

        for (var i = 0; i <= 9; i++)
        {
            map[i.ToString()] = (ushort)('0' + i);
        }

        for (var i = 0; i < 26; i++)
        {
            var letter = ((char)('A' + i)).ToString();
            map[letter] = (ushort)('A' + i);
        }

        for (var i = 1; i <= 12; i++)
        {
            map[$"F{i}"] = (ushort)(0x70 + i - 1);
        }

        return map;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public int type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MouseInput mi;
        [FieldOffset(0)] public KeyboardInput ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
}
