namespace Scripter.Forms;

public sealed class ManualPositionDialog : Form
{
    private readonly NumericUpDown _xBox = new()
    {
        Minimum = -20000,
        Maximum = 20000,
        Width = 100
    };

    private readonly NumericUpDown _yBox = new()
    {
        Minimum = -20000,
        Maximum = 20000,
        Width = 100
    };

    public int X => (int)_xBox.Value;
    public int Y => (int)_yBox.Value;

    public ManualPositionDialog(int x, int y)
    {
        Text = "Type Coordinates";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(280, 120);

        _xBox.Value = Math.Clamp(x, _xBox.Minimum, _xBox.Maximum);
        _yBox.Value = Math.Clamp(y, _yBox.Minimum, _yBox.Maximum);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 4,
            RowCount = 2
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        layout.Controls.Add(new Label { Text = "X:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        layout.Controls.Add(_xBox, 1, 0);
        layout.Controls.Add(new Label { Text = "Y:", AutoSize = true, Anchor = AnchorStyles.Left }, 2, 0);
        layout.Controls.Add(_yBox, 3, 0);

        var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 80 };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 80 };
        var buttonRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        buttonRow.Controls.Add(cancelButton);
        buttonRow.Controls.Add(okButton);
        layout.Controls.Add(buttonRow, 0, 1);
        layout.SetColumnSpan(buttonRow, 4);

        Controls.Add(layout);
        AcceptButton = okButton;
        CancelButton = cancelButton;
    }
}
