namespace Scripter.Forms;

public sealed class ScreenCaptureOverlay : Form
{
    public enum CaptureKind
    {
        Region,
        Snip
    }

    private readonly CaptureKind _kind;
    private readonly Bitmap? _frozen;
    private readonly Point _origin;
    private Point _dragStart;
    private Point _dragEnd;
    private bool _dragging;
    private TaskCompletionSource<(Rectangle Rect, Bitmap? Image)?>? _tcs;

    private ScreenCaptureOverlay(CaptureKind kind, Bitmap? frozen, Point origin)
    {
        _kind = kind;
        _frozen = frozen;
        _origin = origin;

        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Normal;
        StartPosition = FormStartPosition.Manual;
        Bounds = SystemInformation.VirtualScreen;
        TopMost = true;
        ShowInTaskbar = false;
        DoubleBuffered = true;
        KeyPreview = true;
        Cursor = Cursors.Cross;
        BackColor = Color.Black;
        Opacity = kind == CaptureKind.Snip ? 1 : 0.35;
    }

    public static async Task<Rectangle?> PickRegionAsync(Form host)
    {
        var result = await RunAsync(host, CaptureKind.Region);
        return result?.Rect;
    }

    public static async Task<(Rectangle Rect, Bitmap Image)?> SnipAsync(Form host)
    {
        var result = await RunAsync(host, CaptureKind.Snip);
        if (result == null || result.Value.Image == null)
        {
            return null;
        }

        return (result.Value.Rect, result.Value.Image);
    }

    private static async Task<(Rectangle Rect, Bitmap? Image)?> RunAsync(Form host, CaptureKind kind)
    {
        var snapshots = new List<(Form Form, double Opacity)>();
        for (Form? current = host; current != null; current = current.Owner)
        {
            snapshots.Add((current, current.Opacity));
            current.Opacity = 0;
        }

        Bitmap? frozen = null;
        try
        {
            await Task.Delay(200);
            var origin = SystemInformation.VirtualScreen.Location;
            if (kind == CaptureKind.Snip)
            {
                frozen = ImageMatcherCapture();
            }

            var overlay = new ScreenCaptureOverlay(kind, frozen, origin);
            var tcs = new TaskCompletionSource<(Rectangle Rect, Bitmap? Image)?>(TaskCreationOptions.RunContinuationsAsynchronously);
            overlay._tcs = tcs;
            overlay.Show();
            overlay.Activate();
            return await tcs.Task;
        }
        finally
        {
            frozen?.Dispose();
            for (var i = snapshots.Count - 1; i >= 0; i--)
            {
                var (form, opacity) = snapshots[i];
                if (!form.IsDisposed)
                {
                    form.Opacity = opacity;
                }
            }

            if (!host.IsDisposed)
            {
                host.Activate();
                host.BringToFront();
            }
        }
    }

    private static Bitmap ImageMatcherCapture() => Services.ImageMatcher.CaptureVirtualScreen();

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        _dragging = true;
        _dragStart = e.Location;
        _dragEnd = e.Location;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        _dragEnd = e.Location;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || !_dragging)
        {
            return;
        }

        _dragging = false;
        var overlayRect = NormalizedRect(_dragStart, _dragEnd);
        if (overlayRect.Width < 4 || overlayRect.Height < 4)
        {
            return;
        }

        var screenRect = new Rectangle(
            overlayRect.X + _origin.X,
            overlayRect.Y + _origin.Y,
            overlayRect.Width,
            overlayRect.Height);

        Bitmap? snip = null;
        if (_kind == CaptureKind.Snip && _frozen != null)
        {
            snip = _frozen.Clone(overlayRect, _frozen.PixelFormat);
        }
        else if (_kind == CaptureKind.Snip)
        {
            Hide();
            snip = Services.ImageMatcher.CaptureRegion(screenRect);
        }

        Complete((screenRect, snip));
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            Complete(null);
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (_frozen != null)
        {
            e.Graphics.DrawImage(_frozen, Point.Empty);
            using var dim = new SolidBrush(Color.FromArgb(90, 0, 0, 0));
            e.Graphics.FillRectangle(dim, ClientRectangle);
        }

        if (_dragging)
        {
            var rect = NormalizedRect(_dragStart, _dragEnd);
            if (_frozen != null)
            {
                e.Graphics.SetClip(rect);
                e.Graphics.DrawImage(_frozen, Point.Empty);
                e.Graphics.ResetClip();
            }

            using var pen = new Pen(Color.DeepSkyBlue, 2);
            e.Graphics.DrawRectangle(pen, rect.X, rect.Y, Math.Max(0, rect.Width - 1), Math.Max(0, rect.Height - 1));
            var label = $"{rect.Width} x {rect.Height}";
            TextRenderer.DrawText(e.Graphics, label, Font, new Point(rect.Right + 6, rect.Top), Color.White);
        }
        else
        {
            var hint = _kind == CaptureKind.Snip
                ? "Drag to snip a reference image  ·  Esc to cancel"
                : "Drag to select the search region  ·  Esc to cancel";
            TextRenderer.DrawText(e.Graphics, hint, Font, new Point(16, 16), Color.White);
        }
    }

    private void Complete((Rectangle Rect, Bitmap? Image)? result)
    {
        var tcs = _tcs;
        _tcs = null;
        Hide();
        tcs?.TrySetResult(result);
        Close();
        Dispose();
    }

    private static Rectangle NormalizedRect(Point a, Point b)
    {
        var x = Math.Min(a.X, b.X);
        var y = Math.Min(a.Y, b.Y);
        return new Rectangle(x, y, Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
    }
}
