namespace Scripter.Forms;

public sealed class SetPositionChoiceDialog : Form
{
    public enum ChoiceKind
    {
        Cancelled,
        Manual,
        PickMouse
    }

    public ChoiceKind Choice { get; private set; } = ChoiceKind.Cancelled;

    public SetPositionChoiceDialog()
    {
        Text = "Set Position";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(320, 168);

        var prompt = new Label
        {
            Text = "How would you like to set the position?",
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 40,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var manualButton = new Button
        {
            Text = "Type coordinates...",
            Width = 200,
            Height = 32,
            DialogResult = DialogResult.None
        };
        manualButton.Click += (_, _) =>
        {
            Choice = ChoiceKind.Manual;
            DialogResult = DialogResult.OK;
            Close();
        };

        var pickButton = new Button
        {
            Text = "Pick with mouse...",
            Width = 200,
            Height = 32,
            DialogResult = DialogResult.None
        };
        pickButton.Click += (_, _) =>
        {
            Choice = ChoiceKind.PickMouse;
            DialogResult = DialogResult.OK;
            Close();
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            Width = 200,
            Height = 32,
            DialogResult = DialogResult.Cancel
        };
        cancelButton.Click += (_, _) =>
        {
            Choice = ChoiceKind.Cancelled;
            Close();
        };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(60, 8, 60, 8)
        };
        buttons.Controls.Add(manualButton);
        buttons.Controls.Add(pickButton);
        buttons.Controls.Add(cancelButton);

        Controls.Add(buttons);
        Controls.Add(prompt);
        CancelButton = cancelButton;
        AcceptButton = manualButton;
    }
}
