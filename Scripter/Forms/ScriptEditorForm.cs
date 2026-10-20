using Scripter.Models;
using Scripter.Services;

namespace Scripter.Forms;

public class ScriptEditorForm : Form
{
    private readonly Script _script;
    private string? _filePath;
    private readonly bool _runOnOpen;

    private readonly TextBox _nameBox = new() { Width = 260 };
    private readonly NumericUpDown _repeatCount = new() { Minimum = 1, Maximum = 999999, Value = 1, Width = 100 };
    private readonly CheckBox _repeatUntilStopped = new() { Text = "Repeat until stopped", AutoSize = true };
    private readonly ListView _actionList = new();
    private readonly ComboBox _actionTypeBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
    private readonly Panel _paramPanel = new() { AutoSize = true, MinimumSize = new Size(240, 36) };
    private readonly Button _addButton = new() { Text = "Add Action", AutoSize = true };
    private readonly Button _insertIfButton = new() { Text = "Insert If block", AutoSize = true };
    private readonly Button _insertLoopButton = new() { Text = "Insert Loop", AutoSize = true };
    private readonly Button _insertWaitUntilButton = new() { Text = "Wait until image", AutoSize = true };
    private readonly Button _moveUpButton = new() { Text = "Move Up", AutoSize = true };
    private readonly Button _moveDownButton = new() { Text = "Move Down", AutoSize = true };
    private readonly Button _removeButton = new() { Text = "Delete", AutoSize = true };
    private readonly ContextMenuStrip _actionContextMenu = new();
    private readonly ToolStripMenuItem _editMenuItem = new("Edit");
    private readonly ToolStripMenuItem _duplicateMenuItem = new("Duplicate");
    private readonly ToolStripMenuItem _copyMenuItem = new("Copy");
    private readonly ToolStripMenuItem _cutMenuItem = new("Cut");
    private readonly ToolStripMenuItem _pasteMenuItem = new("Paste");
    private readonly ToolStripMenuItem _deleteMenuItem = new("Delete");
    private readonly ToolStripMenuItem _moveUpMenuItem = new("Move Up");
    private readonly ToolStripMenuItem _moveDownMenuItem = new("Move Down");
    private readonly Button _runButton = new() { Text = "Run", AutoSize = true };
    private readonly Button _stopButton = new() { Text = "Stop", AutoSize = true, Enabled = false };
    private readonly Button _saveButton = new() { Text = "Save", AutoSize = true };
    private readonly Button _saveAsButton = new() { Text = "Save As", AutoSize = true };
    private readonly Label _statusLabel = new() { AutoSize = true, Text = "Ready" };
    private readonly Label _hotkeyHint = new() { AutoSize = true, Text = "F8 stop  ·  Ctrl+Z undo  ·  Ctrl+Shift+Z redo" };

    private readonly MacroRunner _runner = MacroRunner.Instance;
    private readonly PositionPicker _positionPicker = new();
    private readonly UndoRedoStack<Script> _history = new(50);

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
    private bool _loadingUi;
    private List<MacroAction> _clipboard = [];

    public ScriptEditorForm(Script script, string? filePath, bool runOnOpen = false)
    {
        _script = script;
        _filePath = filePath;
        _runOnOpen = runOnOpen;

        Text = string.IsNullOrWhiteSpace(filePath) ? "New Script" : $"Edit - {script.Name}";
        MinimumSize = new Size(760, 560);
        Size = new Size(960, 720);
        StartPosition = FormStartPosition.CenterParent;

        InitializeLayout();
        WireEvents();
        LoadScriptIntoUi();

        if (_runOnOpen)
        {
            Shown += (_, _) => BeginInvoke(StartRun);
        }
    }

    private void InitializeLayout()
    {
        var headerPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(8),
            ColumnCount = 4
        };
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        headerPanel.Controls.Add(new Label { Text = "Name:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        headerPanel.Controls.Add(_nameBox, 1, 0);
        headerPanel.Controls.Add(new Label { Text = "Repeat:", AutoSize = true, Anchor = AnchorStyles.Left }, 2, 0);
        headerPanel.Controls.Add(_repeatCount, 3, 0);
        headerPanel.Controls.Add(_repeatUntilStopped, 1, 1);
        headerPanel.SetColumnSpan(_repeatUntilStopped, 3);

        _actionList.View = View.Details;
        _actionList.FullRowSelect = true;
        _actionList.GridLines = true;
        _actionList.MultiSelect = true;
        _actionList.HideSelection = false;
        _actionList.Dock = DockStyle.Fill;
        _actionList.Columns.Add("#", 40);
        _actionList.Columns.Add("Type", 100);
        _actionList.Columns.Add("Summary", 520);

        _copyMenuItem.ShortcutKeyDisplayString = "Ctrl+C";
        _cutMenuItem.ShortcutKeyDisplayString = "Ctrl+X";
        _pasteMenuItem.ShortcutKeyDisplayString = "Ctrl+V";
        _duplicateMenuItem.ShortcutKeyDisplayString = "Ctrl+D";
        _deleteMenuItem.ShortcutKeyDisplayString = "Del";
        _moveUpMenuItem.ShortcutKeyDisplayString = "Ctrl+Up";
        _moveDownMenuItem.ShortcutKeyDisplayString = "Ctrl+Down";

        _actionContextMenu.Items.AddRange([
            _editMenuItem,
            _duplicateMenuItem,
            new ToolStripSeparator(),
            _copyMenuItem,
            _cutMenuItem,
            _pasteMenuItem,
            _deleteMenuItem,
            new ToolStripSeparator(),
            _moveUpMenuItem,
            _moveDownMenuItem
        ]);
        _actionList.ContextMenuStrip = _actionContextMenu;

        _actionTypeBox.Items.AddRange(Enum.GetNames<ActionType>());
        _actionTypeBox.SelectedIndex = 0;

        var addPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(8, 0, 8, 8),
            WrapContents = true
        };
        addPanel.Controls.Add(new Label { Text = "Action:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
        addPanel.Controls.Add(_actionTypeBox);
        addPanel.Controls.Add(_paramPanel);
        addPanel.Controls.Add(_addButton);

        var actionButtonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            Padding = new Padding(8),
            WrapContents = false
        };
        actionButtonPanel.Controls.AddRange([
            _moveUpButton,
            _moveDownButton,
            _removeButton,
            _insertIfButton,
            _insertLoopButton,
            _insertWaitUntilButton
        ]);

        var footerPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            Padding = new Padding(8),
            WrapContents = false
        };
        footerPanel.Controls.AddRange([_runButton, _stopButton, _saveButton, _saveAsButton, _hotkeyHint, _statusLabel]);

        var centerPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 8, 0) };
        centerPanel.Controls.Add(_actionList);
        centerPanel.Controls.Add(actionButtonPanel);

        Controls.Add(centerPanel);
        Controls.Add(addPanel);
        Controls.Add(headerPanel);
        Controls.Add(footerPanel);

        BuildParamPanelForSelectedType();
    }

    private void WireEvents()
    {
        _actionTypeBox.SelectedIndexChanged += (_, _) => BuildParamPanelForSelectedType();
        _repeatUntilStopped.CheckedChanged += (_, _) =>
        {
            if (_loadingUi)
            {
                _repeatCount.Enabled = !_repeatUntilStopped.Checked;
                return;
            }

            RecordHeaderChange();
            _repeatCount.Enabled = !_repeatUntilStopped.Checked;
        };
        _nameBox.Leave += (_, _) => RecordHeaderChange();
        _repeatCount.Leave += (_, _) => RecordHeaderChange();
        _addButton.Click += (_, _) => AddActionFromPanel();
        _insertIfButton.Click += (_, _) => InsertIfBlock();
        _insertLoopButton.Click += (_, _) => InsertLoopBlock();
        _insertWaitUntilButton.Click += (_, _) => InsertWaitUntilImageSnippet();
        _moveUpButton.Click += (_, _) => MoveSelectedActions(-1);
        _moveDownButton.Click += (_, _) => MoveSelectedActions(1);
        _removeButton.Click += (_, _) => RemoveSelectedActions();
        _runButton.Click += (_, _) => StartRun();
        _stopButton.Click += (_, _) => StopRun();
        _saveButton.Click += (_, _) => SaveScript(promptForPath: false);
        _saveAsButton.Click += (_, _) => SaveScript(promptForPath: true);
        _runner.RunningStateChanged += (_, _) => BeginInvoke(UpdateRunState);
        _actionList.DoubleClick += (_, _) => EditSelectedAction();
        _actionList.MouseDown += OnActionListMouseDown;
        _actionContextMenu.Opening += OnActionContextMenuOpening;
        _editMenuItem.Click += (_, _) => EditSelectedAction();
        _duplicateMenuItem.Click += (_, _) => DuplicateSelectedActions();
        _copyMenuItem.Click += (_, _) => CopySelectedActions();
        _cutMenuItem.Click += (_, _) => CutSelectedActions();
        _pasteMenuItem.Click += (_, _) => PasteActions();
        _deleteMenuItem.Click += (_, _) => RemoveSelectedActions();
        _moveUpMenuItem.Click += (_, _) => MoveSelectedActions(-1);
        _moveDownMenuItem.Click += (_, _) => MoveSelectedActions(1);
        FormClosing += OnFormClosing;
    }

    private void LoadScriptIntoUi()
    {
        _loadingUi = true;
        _nameBox.Text = _script.Name;
        _repeatCount.Value = Math.Max(1, _script.RepeatCount);
        _repeatUntilStopped.Checked = _script.RepeatUntilStopped;
        _repeatCount.Enabled = !_script.RepeatUntilStopped;
        RefreshActionList();
        _loadingUi = false;
    }

    private void RefreshActionList()
    {
        var indents = ControlFlow.GetIndentLevels(_script.Actions);
        _actionList.Items.Clear();
        for (var i = 0; i < _script.Actions.Count; i++)
        {
            var action = _script.Actions[i];
            var item = new ListViewItem((i + 1).ToString()) { Tag = i };
            item.SubItems.Add(action.Type.ToString());
            var indent = new string(' ', indents[i] * 2);
            item.SubItems.Add(indent + action.GetSummary());
            _actionList.Items.Add(item);
        }
    }

    private void BuildParamPanelForSelectedType()
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
            WrapContents = false
        };

        switch (type)
        {
            case ActionType.MouseClick:
            case ActionType.MoveMouse:
                _positionButton = CreatePositionButton(_positionX, _positionY);
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
                    _mouseButtonBox.SelectedIndex = 0;
                    flow.Controls.Add(new Label { Text = "Button:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
                    flow.Controls.Add(_mouseButtonBox);
                }
                break;

            case ActionType.KeyPress:
                KeyPressUi.AddFields(flow, "Space", out _ctrlBox, out _shiftBox, out _altBox, out _keyBox);
                break;

            case ActionType.Wait:
                _waitBox = new NumericUpDown
                {
                    Minimum = 0,
                    Maximum = 3_600_000,
                    Value = 500,
                    Width = 100,
                    Increment = 50
                };
                flow.Controls.Add(new Label { Text = "Milliseconds:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
                flow.Controls.Add(_waitBox);
                break;

            case ActionType.If:
            case ActionType.Loop:
            {
                var condition = new ActionCondition
                {
                    Kind = type == ActionType.Loop ? ConditionKind.Always : ConditionKind.ImageFound,
                    Threshold = 70
                };
                _conditionEditor = new ConditionEditorPanel(condition, type == ActionType.Loop, 0);
                flow.Controls.Add(_conditionEditor);
                break;
            }

            case ActionType.Else:
            case ActionType.EndIf:
            case ActionType.EndLoop:
            case ActionType.Break:
                flow.Controls.Add(new Label
                {
                    Text = "No extra settings.",
                    AutoSize = true,
                    Padding = new Padding(0, 6, 0, 0)
                });
                break;
        }

        _paramPanel.Controls.Add(flow);
    }

    private static Button CreatePositionButton(int x, int y) => new()
    {
        Text = FormatPositionText(x, y),
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
        ApplyUiToScript();

        try
        {
            _statusLabel.Text = "Move the mouse, then click or press Enter to lock...";
            var point = await PositionPickHelper.PickAsync(this, _positionPicker);
            SetPosition(point.X, point.Y);
            _statusLabel.Text = $"Picked ({point.X}, {point.Y})";
        }
        catch (OperationCanceledException)
        {
            _statusLabel.Text = "Pick cancelled";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Pick Position Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _statusLabel.Text = "Pick failed";
        }
        finally
        {
            _isPickingPosition = false;
            _positionButton.Enabled = true;
        }
    }

    private void AddActionFromPanel()
    {
        if (_actionTypeBox.SelectedItem is not string typeName ||
            !Enum.TryParse<ActionType>(typeName, out var type))
        {
            return;
        }

        RecordUndo();
        var action = CreateActionFromPanel(type);
        var insertAt = GetInsertIndex();
        _script.Actions.Insert(insertAt, action);
        RefreshActionList();
        SelectActionIndices([insertAt]);
        _statusLabel.Text = "Action added";
    }

    private int GetInsertIndex()
    {
        var selected = GetSelectedActionIndices();
        return selected.Count > 0 ? selected[^1] + 1 : _script.Actions.Count;
    }

    private void InsertIfBlock()
    {
        if (_runner.IsRunning)
        {
            return;
        }

        RecordUndo();
        var insertAt = GetInsertIndex();
        var condition = (_conditionEditor?.Condition ?? new ActionCondition
        {
            Kind = ConditionKind.ImageFound,
            Threshold = 70
        }).Clone();
        _script.Actions.InsertRange(insertAt, [
            new MacroAction { Type = ActionType.If, Condition = condition },
            new MacroAction { Type = ActionType.Else },
            new MacroAction { Type = ActionType.EndIf }
        ]);
        RefreshActionList();
        SelectActionIndices([insertAt]);
        _statusLabel.Text = "If block inserted";
    }

    private void InsertLoopBlock()
    {
        if (_runner.IsRunning)
        {
            return;
        }

        RecordUndo();
        var insertAt = GetInsertIndex();
        _script.Actions.InsertRange(insertAt, [
            new MacroAction
            {
                Type = ActionType.Loop,
                Condition = new ActionCondition { Kind = ConditionKind.Always },
                TimeoutMs = 0
            },
            new MacroAction { Type = ActionType.EndLoop }
        ]);
        RefreshActionList();
        SelectActionIndices([insertAt]);
        _statusLabel.Text = "Loop inserted";
    }

    private void InsertWaitUntilImageSnippet()
    {
        if (_runner.IsRunning)
        {
            return;
        }

        RecordUndo();
        var insertAt = GetInsertIndex();
        var condition = (_conditionEditor?.Condition ?? new ActionCondition
        {
            Kind = ConditionKind.ImageFound,
            Threshold = 70
        }).Clone();
        if (condition.Kind == ConditionKind.Always)
        {
            condition.Kind = ConditionKind.ImageFound;
        }

        _script.Actions.InsertRange(insertAt, [
            new MacroAction
            {
                Type = ActionType.Loop,
                Condition = new ActionCondition { Kind = ConditionKind.Always }
            },
            new MacroAction { Type = ActionType.Wait, Param1 = 500 },
            new MacroAction { Type = ActionType.If, Condition = condition },
            new MacroAction { Type = ActionType.Break },
            new MacroAction { Type = ActionType.EndIf },
            new MacroAction { Type = ActionType.EndLoop }
        ]);
        RefreshActionList();
        SelectActionIndices(Enumerable.Range(insertAt, 6));
        _statusLabel.Text = "Wait-until-image snippet inserted — snip an image on the If action";
    }

    private MacroAction CreateActionFromPanel(ActionType type) => type switch
    {
        ActionType.MouseClick => new MacroAction
        {
            Type = ActionType.MouseClick,
            Param1 = _positionX,
            Param2 = _positionY,
            Extra = _mouseButtonBox?.SelectedItem?.ToString() ?? "Left"
        },
        ActionType.MoveMouse => new MacroAction
        {
            Type = ActionType.MoveMouse,
            Param1 = _positionX,
            Param2 = _positionY
        },
        ActionType.KeyPress => new MacroAction
        {
            Type = ActionType.KeyPress,
            Extra = KeyPressUi.Format(_ctrlBox, _shiftBox, _altBox, _keyBox)
        },
        ActionType.Wait => new MacroAction
        {
            Type = ActionType.Wait,
            Param1 = (int)(_waitBox?.Value ?? 0)
        },
        ActionType.If => new MacroAction
        {
            Type = ActionType.If,
            Condition = (_conditionEditor?.Condition ?? new ActionCondition { Kind = ConditionKind.ImageFound, Threshold = 70 }).Clone()
        },
        ActionType.Loop => new MacroAction
        {
            Type = ActionType.Loop,
            Condition = (_conditionEditor?.Condition ?? new ActionCondition { Kind = ConditionKind.Always }).Clone(),
            TimeoutMs = _conditionEditor?.TimeoutMs ?? 0
        },
        ActionType.Else => new MacroAction { Type = ActionType.Else },
        ActionType.EndIf => new MacroAction { Type = ActionType.EndIf },
        ActionType.EndLoop => new MacroAction { Type = ActionType.EndLoop },
        ActionType.Break => new MacroAction { Type = ActionType.Break },
        _ => throw new InvalidOperationException("Unsupported action type.")
    };

    private void EditSelectedAction()
    {
        var indices = GetSelectedActionIndices();
        if (indices.Count != 1 || _runner.IsRunning)
        {
            return;
        }

        var index = indices[0];
        var existing = _script.Actions[index];
        if (existing.Type is ActionType.Else or ActionType.EndIf or ActionType.EndLoop or ActionType.Break)
        {
            return;
        }
        using var dialog = new ActionEditDialog(_script.Actions[index]);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        RecordUndo();
        _script.Actions[index] = dialog.EditedAction;
        RefreshActionList();
        SelectActionIndices([index]);
        _statusLabel.Text = "Action updated";
    }

    private void DuplicateSelectedActions()
    {
        var indices = GetSelectedActionIndices();
        if (indices.Count == 0 || _runner.IsRunning)
        {
            return;
        }

        RecordUndo();
        var copies = indices.Select(i => _script.Actions[i].Clone()).ToList();
        var insertAt = indices[^1] + 1;
        _script.Actions.InsertRange(insertAt, copies);
        RefreshActionList();
        SelectActionIndices(Enumerable.Range(insertAt, copies.Count));
        _statusLabel.Text = copies.Count == 1 ? "Action duplicated" : $"{copies.Count} actions duplicated";
    }

    private void CopySelectedActions()
    {
        var indices = GetSelectedActionIndices();
        if (indices.Count == 0)
        {
            return;
        }

        _clipboard = indices.Select(i => _script.Actions[i].Clone()).ToList();
        _statusLabel.Text = _clipboard.Count == 1 ? "Copied 1 action" : $"Copied {_clipboard.Count} actions";
    }

    private void CutSelectedActions()
    {
        var indices = GetSelectedActionIndices();
        if (indices.Count == 0 || _runner.IsRunning)
        {
            return;
        }

        RecordUndo();
        CopySelectedActions();
        RemoveSelectedActions(recordUndo: false);
        if (_clipboard.Count > 0)
        {
            _statusLabel.Text = _clipboard.Count == 1 ? "Cut 1 action" : $"Cut {_clipboard.Count} actions";
        }
    }

    private void PasteActions()
    {
        if (_clipboard.Count == 0 || _runner.IsRunning)
        {
            return;
        }

        RecordUndo();
        var copies = _clipboard.Select(a => a.Clone()).ToList();
        var selected = GetSelectedActionIndices();
        var insertAt = selected.Count > 0 ? selected[^1] + 1 : _script.Actions.Count;
        _script.Actions.InsertRange(insertAt, copies);
        RefreshActionList();
        SelectActionIndices(Enumerable.Range(insertAt, copies.Count));
        _statusLabel.Text = copies.Count == 1 ? "Pasted 1 action" : $"Pasted {copies.Count} actions";
    }

    private void MoveSelectedActions(int delta)
    {
        var selected = GetSelectedActionIndices();
        if (selected.Count == 0 || _runner.IsRunning)
        {
            return;
        }

        ApplyUiToScript();
        var before = _script.Clone();
        var selectedSet = selected.ToHashSet();
        var moved = false;

        if (delta < 0)
        {
            for (var i = 1; i < _script.Actions.Count; i++)
            {
                if (!selectedSet.Contains(i) || selectedSet.Contains(i - 1))
                {
                    continue;
                }

                (_script.Actions[i], _script.Actions[i - 1]) = (_script.Actions[i - 1], _script.Actions[i]);
                selectedSet.Remove(i);
                selectedSet.Add(i - 1);
                moved = true;
            }
        }
        else
        {
            for (var i = _script.Actions.Count - 2; i >= 0; i--)
            {
                if (!selectedSet.Contains(i) || selectedSet.Contains(i + 1))
                {
                    continue;
                }

                (_script.Actions[i], _script.Actions[i + 1]) = (_script.Actions[i + 1], _script.Actions[i]);
                selectedSet.Remove(i);
                selectedSet.Add(i + 1);
                moved = true;
            }
        }

        if (!moved)
        {
            return;
        }

        _history.Record(before);
        RefreshActionList();
        SelectActionIndices(selectedSet.OrderBy(i => i));
    }

    private void RemoveSelectedActions(bool recordUndo = true)
    {
        var indices = GetSelectedActionIndices();
        if (indices.Count == 0 || _runner.IsRunning)
        {
            return;
        }

        if (recordUndo)
        {
            RecordUndo();
        }

        var first = indices[0];
        for (var i = indices.Count - 1; i >= 0; i--)
        {
            _script.Actions.RemoveAt(indices[i]);
        }

        RefreshActionList();
        if (_script.Actions.Count > 0)
        {
            SelectActionIndices([Math.Min(first, _script.Actions.Count - 1)]);
        }

        _statusLabel.Text = indices.Count == 1 ? "Action deleted" : $"{indices.Count} actions deleted";
    }

    private void OnActionListMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right)
        {
            return;
        }

        var hit = _actionList.HitTest(e.Location);
        if (hit.Item == null)
        {
            return;
        }

        if (!hit.Item.Selected)
        {
            _actionList.SelectedItems.Clear();
            hit.Item.Selected = true;
        }

        hit.Item.Focused = true;
    }

    private void OnActionContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_runner.IsRunning)
        {
            e.Cancel = true;
            return;
        }

        var selected = GetSelectedActionIndices();
        var hasSelection = selected.Count > 0;
        var hasClipboard = _clipboard.Count > 0;

        if (!hasSelection && !hasClipboard)
        {
            e.Cancel = true;
            return;
        }

        _editMenuItem.Enabled = selected.Count == 1 &&
            _script.Actions[selected[0]].Type is not (ActionType.Else or ActionType.EndIf or ActionType.EndLoop or ActionType.Break);
        _duplicateMenuItem.Enabled = hasSelection;
        _copyMenuItem.Enabled = hasSelection;
        _cutMenuItem.Enabled = hasSelection;
        _pasteMenuItem.Enabled = hasClipboard;
        _deleteMenuItem.Enabled = hasSelection;
        _moveUpMenuItem.Enabled = selected.Any(i => i > 0 && !selected.Contains(i - 1));
        _moveDownMenuItem.Enabled = selected.Any(i => i < _script.Actions.Count - 1 && !selected.Contains(i + 1));
    }

    private List<int> GetSelectedActionIndices()
    {
        return _actionList.SelectedItems
            .Cast<ListViewItem>()
            .Select(item => item.Tag)
            .OfType<int>()
            .OrderBy(i => i)
            .ToList();
    }

    private void SelectActionIndices(IEnumerable<int> indices)
    {
        _actionList.SelectedItems.Clear();
        var indexList = indices.Where(i => i >= 0 && i < _actionList.Items.Count).ToList();
        foreach (var index in indexList)
        {
            _actionList.Items[index].Selected = true;
        }

        if (indexList.Count == 0)
        {
            return;
        }

        var focusIndex = indexList[^1];
        _actionList.Items[focusIndex].Focused = true;
        _actionList.EnsureVisible(focusIndex);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (!_runner.IsRunning && !_nameBox.Focused)
        {
            if (keyData == (Keys.Control | Keys.Z))
            {
                Undo();
                return true;
            }

            if (keyData == (Keys.Control | Keys.Shift | Keys.Z))
            {
                Redo();
                return true;
            }
        }

        if (_actionList.Focused && !_runner.IsRunning)
        {
            switch (keyData)
            {
                case Keys.Delete:
                    RemoveSelectedActions();
                    return true;
                case Keys.Control | Keys.C:
                    CopySelectedActions();
                    return true;
                case Keys.Control | Keys.X:
                    CutSelectedActions();
                    return true;
                case Keys.Control | Keys.V:
                    PasteActions();
                    return true;
                case Keys.Control | Keys.D:
                    DuplicateSelectedActions();
                    return true;
                case Keys.Control | Keys.Up:
                case Keys.Alt | Keys.Up:
                    MoveSelectedActions(-1);
                    return true;
                case Keys.Control | Keys.Down:
                case Keys.Alt | Keys.Down:
                    MoveSelectedActions(1);
                    return true;
            }
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void RecordUndo()
    {
        ApplyUiToScript();
        _history.Record(_script.Clone());
    }

    private void RecordHeaderChange()
    {
        if (_loadingUi)
        {
            return;
        }

        var name = string.IsNullOrWhiteSpace(_nameBox.Text) ? "Untitled" : _nameBox.Text.Trim();
        var repeatCount = (int)_repeatCount.Value;
        var untilStopped = _repeatUntilStopped.Checked;
        if (name == _script.Name &&
            repeatCount == _script.RepeatCount &&
            untilStopped == _script.RepeatUntilStopped)
        {
            return;
        }

        _history.Record(_script.Clone());
        ApplyUiToScript();
    }

    private void Undo()
    {
        RecordHeaderChange();
        if (!_history.CanUndo)
        {
            return;
        }

        ApplyUiToScript();
        var previous = _history.Undo(_script.Clone());
        if (previous == null)
        {
            return;
        }

        RestoreScript(previous);
        _statusLabel.Text = "Undo";
    }

    private void Redo()
    {
        RecordHeaderChange();
        if (!_history.CanRedo)
        {
            return;
        }

        ApplyUiToScript();
        var next = _history.Redo(_script.Clone());
        if (next == null)
        {
            return;
        }

        RestoreScript(next);
        _statusLabel.Text = "Redo";
    }

    private void RestoreScript(Script snapshot)
    {
        _script.Name = snapshot.Name;
        _script.RepeatCount = snapshot.RepeatCount;
        _script.RepeatUntilStopped = snapshot.RepeatUntilStopped;
        _script.Actions.Clear();
        _script.Actions.AddRange(snapshot.Actions.Select(a => a.Clone()));
        LoadScriptIntoUi();
    }

    private void ApplyUiToScript()
    {
        _script.Name = string.IsNullOrWhiteSpace(_nameBox.Text) ? "Untitled" : _nameBox.Text.Trim();
        _script.RepeatCount = (int)_repeatCount.Value;
        _script.RepeatUntilStopped = _repeatUntilStopped.Checked;
    }

    private bool SaveScript(bool promptForPath)
    {
        ApplyUiToScript();

        if (promptForPath || string.IsNullOrWhiteSpace(_filePath))
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "Script files (*.json)|*.json",
                InitialDirectory = ScriptStorage.ScriptsDirectory,
                FileName = ScriptStorage.GetDefaultPath(_script)
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return false;
            }

            _filePath = dialog.FileName;
        }

        try
        {
            ScriptStorage.Save(_script, _filePath!);
            Text = $"Edit - {_script.Name}";
            _statusLabel.Text = "Saved";
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Save Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private void StartRun()
    {
        if (_runner.IsRunning)
        {
            return;
        }

        ApplyUiToScript();

        if (_script.Actions.Count == 0)
        {
            MessageBox.Show(this, "Add at least one action before running.", "Scripter", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var structureError = ControlFlow.Validate(_script.Actions);
        if (structureError != null)
        {
            MessageBox.Show(this, structureError, "Invalid Script", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _statusLabel.Text = "Running... (F8 to stop)";
        UpdateRunState();

        _ = Task.Run(async () =>
        {
            try
            {
                await _runner.RunAsync(_script);
                BeginInvoke(() => _statusLabel.Text = "Finished");
            }
            catch (OperationCanceledException)
            {
                BeginInvoke(() => _statusLabel.Text = "Stopped");
            }
            catch (Exception ex)
            {
                BeginInvoke(() =>
                {
                    _statusLabel.Text = "Run failed";
                    MessageBox.Show(this, ex.Message, "Run Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
            }
        });
    }

    private void StopRun()
    {
        _runner.Stop();
        _statusLabel.Text = "Stopping...";
    }

    private void UpdateRunState()
    {
        var running = _runner.IsRunning;
        _runButton.Enabled = !running;
        _stopButton.Enabled = running;
        _saveButton.Enabled = !running;
        _saveAsButton.Enabled = !running;
        _addButton.Enabled = !running;
        _insertIfButton.Enabled = !running;
        _insertLoopButton.Enabled = !running;
        _insertWaitUntilButton.Enabled = !running;
        _moveUpButton.Enabled = !running;
        _moveDownButton.Enabled = !running;
        _removeButton.Enabled = !running;
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_isPickingPosition)
        {
            e.Cancel = true;
            return;
        }

        if (_runner.IsRunning)
        {
            _runner.Stop();
        }

        _positionPicker.Dispose();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_filePath) && File.Exists(_filePath))
        {
            DialogResult = DialogResult.OK;
        }

        base.OnFormClosed(e);
    }
}
