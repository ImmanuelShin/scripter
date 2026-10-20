using Scripter.Models;
using Scripter.Services;

namespace Scripter.Forms;

public class MainForm : Form
{
    private readonly ListView _scriptList = new();
    private readonly Button _newButton = new() { Text = "New" };
    private readonly Button _editButton = new() { Text = "Edit" };
    private readonly Button _deleteButton = new() { Text = "Delete" };
    private readonly Button _runButton = new() { Text = "Run" };
    private readonly Button _refreshButton = new() { Text = "Refresh" };
    private readonly Label _hotkeyHint = new() { AutoSize = true, Padding = new Padding(12, 6, 0, 0) };

    private readonly List<(string Path, Script Script)> _scripts = [];
    private readonly HotkeyService _hotkeys = new();

    public MainForm()
    {
        Text = "Scripter";
        MinimumSize = new Size(640, 420);
        Size = new Size(760, 480);
        StartPosition = FormStartPosition.CenterScreen;

        InitializeLayout();
        WireEvents();
        RefreshScriptList();
    }

    private void InitializeLayout()
    {
        _scriptList.View = View.Details;
        _scriptList.FullRowSelect = true;
        _scriptList.GridLines = true;
        _scriptList.MultiSelect = false;
        _scriptList.Dock = DockStyle.Fill;
        _scriptList.Columns.Add("Name", 220);
        _scriptList.Columns.Add("Actions", 80);
        _scriptList.Columns.Add("Repeat", 120);
        _scriptList.Columns.Add("File", 280);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            Padding = new Padding(8),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        _hotkeyHint.Text = _hotkeys.IsRegistered
            ? "F8 stops a running script"
            : "Could not register F8 stop hotkey";

        buttonPanel.Controls.AddRange([
            _newButton,
            _editButton,
            _deleteButton,
            _runButton,
            _refreshButton,
            _hotkeyHint
        ]);

        var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
        mainPanel.Controls.Add(_scriptList);

        Controls.Add(mainPanel);
        Controls.Add(buttonPanel);
    }

    private void WireEvents()
    {
        _newButton.Click += (_, _) => OpenEditor(new Script(), null);
        _editButton.Click += (_, _) => OpenSelectedScript(false);
        _runButton.Click += (_, _) => OpenSelectedScript(true);
        _deleteButton.Click += (_, _) => DeleteSelectedScript();
        _refreshButton.Click += (_, _) => RefreshScriptList();
        _scriptList.DoubleClick += (_, _) => OpenSelectedScript(false);
        _scriptList.SelectedIndexChanged += (_, _) => UpdateButtonState();
        _hotkeys.StopRequested += (_, _) => MacroRunner.Instance.Stop();
        FormClosed += (_, _) => _hotkeys.Dispose();
    }

    private void RefreshScriptList()
    {
        _scripts.Clear();
        _scriptList.Items.Clear();

        foreach (var (_, path, script) in ScriptStorage.ListScripts().OrderBy(s => s.Name))
        {
            var index = _scripts.Count;
            _scripts.Add((path, script));
            var repeatText = script.RepeatUntilStopped
                ? "Until stopped"
                : script.RepeatCount.ToString();

            var item = new ListViewItem(script.Name)
            {
                Tag = index
            };
            item.SubItems.Add(script.Actions.Count.ToString());
            item.SubItems.Add(repeatText);
            item.SubItems.Add(path);
            _scriptList.Items.Add(item);
        }

        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        var hasSelection = _scriptList.SelectedItems.Count > 0;
        _editButton.Enabled = hasSelection;
        _deleteButton.Enabled = hasSelection;
        _runButton.Enabled = hasSelection;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        UpdateButtonState();
    }

    private (string Path, Script Script)? GetSelectedScript()
    {
        if (_scriptList.SelectedItems.Count == 0)
        {
            return null;
        }

        if (_scriptList.SelectedItems[0].Tag is not int index || index < 0 || index >= _scripts.Count)
        {
            return null;
        }

        return _scripts[index];
    }

    private void OpenSelectedScript(bool runOnOpen)
    {
        var selected = GetSelectedScript();
        if (selected == null)
        {
            MessageBox.Show(this, "Select a script first.", "Scripter", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        OpenEditor(selected.Value.Script.Clone(), selected.Value.Path, runOnOpen);
    }

    private void OpenEditor(Script script, string? filePath, bool runOnOpen = false)
    {
        var editor = new ScriptEditorForm(script, filePath, runOnOpen);
        editor.FormClosed += (_, _) => RefreshScriptList();
        editor.Show(this);
    }

    private void DeleteSelectedScript()
    {
        var selected = GetSelectedScript();
        if (selected == null)
        {
            return;
        }

        var confirm = MessageBox.Show(
            this,
            $"Delete script \"{selected.Value.Script.Name}\"?",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes)
        {
            return;
        }

        ScriptStorage.Delete(selected.Value.Path);
        RefreshScriptList();
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        UpdateButtonState();
    }
}
