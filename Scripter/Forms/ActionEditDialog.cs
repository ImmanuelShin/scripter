using Scripter.Models;
using Scripter.Services;

namespace Scripter.Forms;

public sealed class ActionEditDialog : Form
{
    private readonly MacroAction _action;
    private readonly PositionPicker _positionPicker = new();

    private readonly ComboBox _actionTypeBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
    private readonly Panel _paramPanel = new() { AutoSize = true, MinimumSize = new Size(400, 40) };

    private Button? _positionButton;
    private ComboBox? _mouseButtonBox;
    private ComboBox? _keyBox;
    private CheckBox? _ctrlBox;
    private CheckBox? _shiftBox;
    private CheckBox? _altBox;
    private NumericUpDown? _waitBox;
    private ConditionEditorPanel? _conditionEditor;

    private int _positionX;
    private int _positionY;
    private bool _isPickingPosition;

    public MacroAction EditedAction { get; }

    public ActionEditDialog(MacroAction action)
    {
        _action = action.Clone();
        EditedAction = _action;

        _positionX = action.Param1;
        _positionY = action.Param2;

        Text = "Edit Action";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(720, 280);

        _actionTypeBox.Items.AddRange(Enum.GetNames<ActionType>());
        _actionTypeBox.SelectedItem = action.Type.ToString();
        _actionTypeBox.SelectedIndexChanged += (_, _) => BuildParamPanel();

        var header = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(12, 12, 12, 0),
            WrapContents = true
        };
        header.Controls.Add(new Label { Text = "Type:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
        header.Controls.Add(_actionTypeBox);
        header.Controls.Add(_paramPanel);

        var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 80 };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 80 };
        okButton.Click += (_, _) =>
        {
            if (!ApplyPanelToAction())
            {
                DialogResult = DialogResult.None;
            }
        };

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(12),
            WrapContents = false
        };
        footer.Controls.Add(cancelButton);
        footer.Controls.Add(okButton);

        Controls.Add(footer);
        Controls.Add(header);

        AcceptButton = okButton;
        CancelButton = cancelButton;
        FormClosing += OnFormClosing;
        FormClosed += (_, _) => _positionPicker.Dispose();

        BuildParamPanel();
    }

    private void BuildParamPanel()
    {
        _paramPanel.Controls.Clear();
        _positionButton = null;
        _mouseButtonBox = null;
        _keyBox = null;
        _ctrlBox = null;
        _shiftBox = null;
        _altBox = null;
        _waitBox = null;
        _conditionEditor?.Dispose();
        _conditionEditor = null;

        if (_actionTypeBox.SelectedItem is not string typeName ||
            !Enum.TryParse<ActionType>(typeName, out var type))
        {
            return;
        }

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true
        };

        switch (type)
        {
            case ActionType.MouseClick:
            case ActionType.MoveMouse:
                _positionButton = CreatePositionButton();
                _positionButton.Click += async (_, _) => await ChoosePositionAsync();
                flow.Controls.Add(_positionButton);

                if (type == ActionType.MouseClick)
                {
                    _mouseButtonBox = new ComboBox
                    {
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        Width = 80
                    };
                    _mouseButtonBox.Items.AddRange(["Left", "Right"]);
                    _mouseButtonBox.SelectedItem = _action.Extra ?? "Left";
                    if (_mouseButtonBox.SelectedIndex < 0)
                    {
                        _mouseButtonBox.SelectedIndex = 0;
                    }
                    flow.Controls.Add(new Label { Text = "Button:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
                    flow.Controls.Add(_mouseButtonBox);
                }
                break;

            case ActionType.KeyPress:
                KeyPressUi.AddFields(flow, _action.Extra, out _ctrlBox, out _shiftBox, out _altBox, out _keyBox);
                break;

            case ActionType.Wait:
                _waitBox = new NumericUpDown
                {
                    Minimum = 0,
                    Maximum = 3_600_000,
                    Value = Math.Clamp(_action.Param1, 0, 3_600_000),
                    Width = 100,
                    Increment = 50
                };
                flow.Controls.Add(new Label { Text = "Milliseconds:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
                flow.Controls.Add(_waitBox);
                break;

            case ActionType.If:
            case ActionType.Loop:
            {
                var condition = (_action.Condition ?? new ActionCondition
                {
                    Kind = type == ActionType.Loop ? ConditionKind.Always : ConditionKind.ImageFound,
                    Threshold = 70
                }).Clone();
                _conditionEditor = new ConditionEditorPanel(condition, type == ActionType.Loop, _action.TimeoutMs);
                flow.Controls.Add(_conditionEditor);
                break;
            }

            case ActionType.Else:
            case ActionType.EndIf:
            case ActionType.EndLoop:
            case ActionType.Break:
                flow.Controls.Add(new Label { Text = "No extra settings.", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
                break;
        }

        _paramPanel.Controls.Add(flow);
    }

    private Button CreatePositionButton() => new()
    {
        Text = FormatPositionText(_positionX, _positionY),
        AutoSize = true
    };

    private static string FormatPositionText(int x, int y) => $"Position ({x}, {y})";

    private void SetPosition(int x, int y)
    {
        _positionX = x;
        _positionY = y;
        if (_positionButton != null)
        {
            _positionButton.Text = FormatPositionText(x, y);
        }
    }

    private async Task ChoosePositionAsync()
    {
        using var choice = new SetPositionChoiceDialog();
        if (choice.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        switch (choice.Choice)
        {
            case SetPositionChoiceDialog.ChoiceKind.Manual:
                using (var manual = new ManualPositionDialog(_positionX, _positionY))
                {
                    if (manual.ShowDialog(this) == DialogResult.OK)
                    {
                        SetPosition(manual.X, manual.Y);
                    }
                }
                break;

            case SetPositionChoiceDialog.ChoiceKind.PickMouse:
                await PickPositionWithMouseAsync();
                break;
        }
    }

    private async Task PickPositionWithMouseAsync()
    {
        if (_positionButton == null)
        {
            return;
        }

        _positionButton.Enabled = false;
        _isPickingPosition = true;

        try
        {
            var point = await PositionPickHelper.PickAsync(this, _positionPicker);
            SetPosition(point.X, point.Y);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Pick Position Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _isPickingPosition = false;
            _positionButton.Enabled = true;
        }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_isPickingPosition)
        {
            e.Cancel = true;
        }
    }

    private bool ApplyPanelToAction()
    {
        if (_actionTypeBox.SelectedItem is not string typeName ||
            !Enum.TryParse<ActionType>(typeName, out var type))
        {
            return false;
        }

        _action.Type = type;
        _action.Param1 = 0;
        _action.Param2 = 0;
        _action.Extra = null;
        _action.Condition = null;
        _action.TimeoutMs = 0;

        switch (type)
        {
            case ActionType.MouseClick:
                _action.Param1 = _positionX;
                _action.Param2 = _positionY;
                _action.Extra = _mouseButtonBox?.SelectedItem?.ToString() ?? "Left";
                break;

            case ActionType.MoveMouse:
                _action.Param1 = _positionX;
                _action.Param2 = _positionY;
                break;

            case ActionType.KeyPress:
                _action.Extra = KeyPressUi.Format(_ctrlBox, _shiftBox, _altBox, _keyBox);
                break;

            case ActionType.Wait:
                _action.Param1 = (int)(_waitBox?.Value ?? 0);
                break;

            case ActionType.If:
                _action.Condition = (_conditionEditor?.Condition ?? new ActionCondition
                {
                    Kind = ConditionKind.ImageFound,
                    Threshold = 70
                }).Clone();
                break;

            case ActionType.Loop:
                _action.Condition = (_conditionEditor?.Condition ?? new ActionCondition
                {
                    Kind = ConditionKind.Always
                }).Clone();
                _action.TimeoutMs = _conditionEditor?.TimeoutMs ?? 0;
                break;
        }

        return true;
    }
}
