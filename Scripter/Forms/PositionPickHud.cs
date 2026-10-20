namespace Scripter.Forms;

public sealed class PositionPickHud : Form
{
    private readonly Label _coordsLabel = new();
    private readonly Label _hintLabel = new();
    private readonly Button _lockButton = new();
    private readonly Button _cancelButton = new();
    private readonly System.Windows.Forms.Timer _timer = new();

    public Point Candidate { get; private set; }

    public event EventHandler? LockRequested;
    public event EventHandler? CancelRequested;

    public PositionPickHud()
    {
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        ControlBox = false;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        Size = new Size(480, 140);
        Text = "Pick Position";
        KeyPreview = true;

        _coordsLabel.AutoSize = false;
        _coordsLabel.Dock = DockStyle.Top;
        _coordsLabel.Height = 28;
        _coordsLabel.TextAlign = ContentAlignment.MiddleCenter;
        _coordsLabel.Font = new Font(Font.FontFamily, 11, FontStyle.Bold);
        _coordsLabel.Text = "Move the mouse...";

        _hintLabel.AutoSize = false;
        _hintLabel.Dock = DockStyle.Top;
        _hintLabel.Height = 20;
        _hintLabel.TextAlign = ContentAlignment.MiddleCenter;
        _hintLabel.Text = "Click the target, or press Enter to lock  ·  Esc to cancel";

        _lockButton.Text = "Lock Position";
        _lockButton.AutoSize = true;
        _cancelButton.Text = "Cancel";
        _cancelButton.AutoSize = true;

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(8, 2, 8, 2)
        };
        buttons.Controls.Add(_lockButton);
        buttons.Controls.Add(_cancelButton);

        Controls.Add(buttons);
        Controls.Add(_hintLabel);
        Controls.Add(_coordsLabel);

        _lockButton.Click += (_, _) => LockRequested?.Invoke(this, EventArgs.Empty);
        _cancelButton.Click += (_, _) => CancelRequested?.Invoke(this, EventArgs.Empty);

        _timer.Interval = 16;
        _timer.Tick += (_, _) => UpdateCandidate();
    }

    public bool ContainsScreenPoint(Point screenPoint) => Bounds.Contains(screenPoint);

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        UpdateCandidate();
        _timer.Start();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Enter || keyData == Keys.Space)
        {
            LockRequested?.Invoke(this, EventArgs.Empty);
            return true;
        }

        if (keyData == Keys.Escape)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer.Stop();
        _timer.Dispose();
        base.OnFormClosed(e);
    }

    private void UpdateCandidate()
    {
        var cursor = Cursor.Position;
        if (!ContainsScreenPoint(cursor))
        {
            Candidate = cursor;
        }

        _coordsLabel.Text = $"X {Candidate.X}   Y {Candidate.Y}";
        RepositionOnActiveScreen();
    }

    private void RepositionOnActiveScreen()
    {
        var screen = Screen.FromPoint(Cursor.Position);
        var x = screen.WorkingArea.Left + (screen.WorkingArea.Width - Width) / 2;
        var y = screen.WorkingArea.Top + 16;
        if (Location.X != x || Location.Y != y)
        {
            Location = new Point(x, y);
        }
    }
}
