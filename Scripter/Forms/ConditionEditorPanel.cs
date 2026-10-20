using Scripter.Models;
using Scripter.Services;

namespace Scripter.Forms;

public sealed class ConditionEditorPanel : FlowLayoutPanel
{
    private readonly ComboBox _kindBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
    private readonly Button _snipButton = new() { Text = "Snip image", AutoSize = true };
    private readonly Button _regionButton = new() { Text = "Select region", AutoSize = true };
    private readonly Button _browseButton = new() { Text = "Browse...", AutoSize = true };
    private readonly PictureBox _preview = new()
    {
        Width = 72,
        Height = 48,
        SizeMode = PictureBoxSizeMode.Zoom,
        BorderStyle = BorderStyle.FixedSingle
    };
    private readonly Label _regionLabel = new() { AutoSize = true, Padding = new Padding(0, 8, 0, 0) };
    private readonly TrackBar _thresholdBar = new()
    {
        Minimum = 1,
        Maximum = 100,
        TickFrequency = 10,
        Width = 140,
        Value = 70
    };
    private readonly NumericUpDown _thresholdBox = new()
    {
        Minimum = 1,
        Maximum = 100,
        Width = 50,
        Value = 70
    };
    private readonly NumericUpDown? _timeoutBox;
    private readonly ActionCondition _condition;
    private bool _syncing;

    public ConditionEditorPanel(ActionCondition condition, bool showLoopTimeout, int timeoutMs)
    {
        _condition = condition;
        AutoSize = true;
        WrapContents = true;
        FlowDirection = FlowDirection.LeftToRight;

        _kindBox.Items.AddRange([
            "Always",
            "Image found",
            "Image not found"
        ]);

        Controls.Add(new Label { Text = "When:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
        Controls.Add(_kindBox);
        Controls.Add(_snipButton);
        Controls.Add(_regionButton);
        Controls.Add(_browseButton);
        Controls.Add(_preview);
        Controls.Add(_regionLabel);
        Controls.Add(new Label { Text = "Certainty:", AutoSize = true, Padding = new Padding(8, 6, 0, 0) });
        Controls.Add(_thresholdBar);
        Controls.Add(_thresholdBox);

        if (showLoopTimeout)
        {
            _timeoutBox = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 3_600_000,
                Increment = 500,
                Width = 90,
                Value = Math.Clamp(timeoutMs, 0, 3_600_000)
            };
            Controls.Add(new Label { Text = "Loop timeout ms (0=none):", AutoSize = true, Padding = new Padding(8, 6, 0, 0) });
            Controls.Add(_timeoutBox);
        }

        _kindBox.SelectedIndexChanged += (_, _) =>
        {
            if (_syncing)
            {
                return;
            }

            _condition.Kind = SelectedKind();
            UpdateImageControls();
        };
        _snipButton.Click += async (_, _) => await SnipAsync();
        _regionButton.Click += async (_, _) => await PickRegionAsync();
        _browseButton.Click += (_, _) => Browse();
        _thresholdBar.Scroll += (_, _) =>
        {
            if (_syncing)
            {
                return;
            }

            _syncing = true;
            _thresholdBox.Value = _thresholdBar.Value;
            _condition.Threshold = _thresholdBar.Value;
            _syncing = false;
        };
        _thresholdBox.ValueChanged += (_, _) =>
        {
            if (_syncing)
            {
                return;
            }

            _syncing = true;
            _thresholdBar.Value = (int)_thresholdBox.Value;
            _condition.Threshold = (int)_thresholdBox.Value;
            _syncing = false;
        };

        LoadFromCondition();
    }

    public int TimeoutMs => (int)(_timeoutBox?.Value ?? 0);

    public ActionCondition Condition => _condition;

    private void LoadFromCondition()
    {
        _syncing = true;
        _kindBox.SelectedIndex = _condition.Kind switch
        {
            ConditionKind.ImageFound => 1,
            ConditionKind.ImageNotFound => 2,
            _ => 0
        };
        var threshold = Math.Clamp(_condition.Threshold <= 0 ? 70 : _condition.Threshold, 1, 100);
        _condition.Threshold = threshold;
        _thresholdBar.Value = threshold;
        _thresholdBox.Value = threshold;
        _syncing = false;
        UpdateImageControls();
        RefreshPreview();
        RefreshRegionLabel();
    }

    private ConditionKind SelectedKind() => _kindBox.SelectedIndex switch
    {
        1 => ConditionKind.ImageFound,
        2 => ConditionKind.ImageNotFound,
        _ => ConditionKind.Always
    };

    private void UpdateImageControls()
    {
        var image = _condition.Kind != ConditionKind.Always;
        _snipButton.Visible = image;
        _regionButton.Visible = image;
        _browseButton.Visible = image;
        _preview.Visible = image;
        _regionLabel.Visible = image;
        _thresholdBar.Visible = image;
        _thresholdBox.Visible = image;
    }

    private async Task SnipAsync()
    {
        var host = FindForm();
        if (host == null)
        {
            return;
        }

        try
        {
            var result = await ScreenCaptureOverlay.SnipAsync(host);
            if (result == null)
            {
                return;
            }

            var (rect, bitmap) = result.Value;
            using (bitmap)
            {
                _condition.ImagePath = ScriptStorage.SaveImage(bitmap);
            }

            if (!_condition.HasRegion)
            {
                SetRegion(rect);
            }

            RefreshPreview();
        }
        catch (Exception ex)
        {
            MessageBox.Show(host, ex.Message, "Snip Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task PickRegionAsync()
    {
        var host = FindForm();
        if (host == null)
        {
            return;
        }

        try
        {
            var rect = await ScreenCaptureOverlay.PickRegionAsync(host);
            if (rect == null)
            {
                return;
            }

            SetRegion(rect.Value);
        }
        catch (Exception ex)
        {
            MessageBox.Show(host, ex.Message, "Region Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Browse()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Images (*.png;*.jpg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        using var bitmap = new Bitmap(dialog.FileName);
        _condition.ImagePath = ScriptStorage.SaveImage(bitmap);
        RefreshPreview();
    }

    private void SetRegion(Rectangle rect)
    {
        _condition.RegionX = rect.X;
        _condition.RegionY = rect.Y;
        _condition.RegionW = rect.Width;
        _condition.RegionH = rect.Height;
        RefreshRegionLabel();
    }

    private void RefreshRegionLabel()
    {
        _regionLabel.Text = _condition.HasRegion
            ? $"Region ({_condition.RegionX}, {_condition.RegionY}) {_condition.RegionW}x{_condition.RegionH}"
            : "No region";
    }

    private void RefreshPreview()
    {
        var path = ScriptStorage.ResolveImagePath(_condition.ImagePath);
        _preview.Image?.Dispose();
        _preview.Image = path != null && File.Exists(path) ? new Bitmap(path) : null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _preview.Image?.Dispose();
        }

        base.Dispose(disposing);
    }
}
