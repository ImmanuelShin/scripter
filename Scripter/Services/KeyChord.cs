namespace Scripter.Services;

public readonly record struct KeyChord(bool Ctrl, bool Shift, bool Alt, string Key)
{
    public static KeyChord Parse(string? extra)
    {
        var key = "Space";
        var ctrl = false;
        var shift = false;
        var alt = false;

        if (string.IsNullOrWhiteSpace(extra))
        {
            return new KeyChord(false, false, false, key);
        }

        var parts = extra.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
                part.Equals("Control", StringComparison.OrdinalIgnoreCase))
            {
                ctrl = true;
            }
            else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase))
            {
                shift = true;
            }
            else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase))
            {
                alt = true;
            }
            else
            {
                key = part;
            }
        }

        if (parts.Length == 1 && (ctrl || shift || alt))
        {
            key = ctrl ? "Control" : shift ? "Shift" : "Alt";
            ctrl = false;
            shift = false;
            alt = false;
        }

        return new KeyChord(ctrl, shift, alt, key);
    }

    public string Format()
    {
        var parts = new List<string>();
        if (Ctrl)
        {
            parts.Add("Ctrl");
        }

        if (Shift)
        {
            parts.Add("Shift");
        }

        if (Alt)
        {
            parts.Add("Alt");
        }

        parts.Add(string.IsNullOrWhiteSpace(Key) ? "Space" : Key);
        return string.Join("+", parts);
    }

    public override string ToString() => Format();
}
