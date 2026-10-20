using Scripter.Services;

namespace Scripter.Forms;

internal static class KeyPressUi
{
    public static void AddFields(
        Control parent,
        string? extra,
        out CheckBox ctrlBox,
        out CheckBox shiftBox,
        out CheckBox altBox,
        out ComboBox keyBox)
    {
        var chord = KeyChord.Parse(extra);

        ctrlBox = new CheckBox { Text = "Ctrl", AutoSize = true, Checked = chord.Ctrl };
        shiftBox = new CheckBox { Text = "Shift", AutoSize = true, Checked = chord.Shift };
        altBox = new CheckBox { Text = "Alt", AutoSize = true, Checked = chord.Alt };
        keyBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 120
        };
        keyBox.Items.AddRange(InputSimulator.SupportedKeys.Cast<object>().ToArray());
        keyBox.SelectedItem = chord.Key;
        if (keyBox.SelectedIndex < 0 && keyBox.Items.Count > 0)
        {
            keyBox.SelectedIndex = 0;
        }

        parent.Controls.Add(ctrlBox);
        parent.Controls.Add(shiftBox);
        parent.Controls.Add(altBox);
        parent.Controls.Add(new Label { Text = "Key:", AutoSize = true, Padding = new Padding(8, 6, 0, 0) });
        parent.Controls.Add(keyBox);
    }

    public static string Format(CheckBox? ctrlBox, CheckBox? shiftBox, CheckBox? altBox, ComboBox? keyBox) =>
        new KeyChord(
            ctrlBox?.Checked == true,
            shiftBox?.Checked == true,
            altBox?.Checked == true,
            keyBox?.SelectedItem?.ToString() ?? "Space").Format();
}
