using Scripter.Services;

namespace Scripter.Forms;

internal static class PositionPickHelper
{
    public static async Task<Point> PickAsync(
        Form host,
        PositionPicker picker,
        Action? syncState = null)
    {
        syncState?.Invoke();

        var snapshots = new List<FormSnapshot>();
        for (Form? current = host; current != null; current = current.Owner)
        {
            snapshots.Add(FormSnapshot.Capture(current));
            // Hide() on a ShowDialog form ends the modal loop and disposes the editor.
            // Opacity keeps the form alive while getting it out of the way.
            current.Opacity = 0;
        }

        try
        {
            await Task.Delay(200);
            return await picker.PickPositionAsync();
        }
        finally
        {
            for (var i = snapshots.Count - 1; i >= 0; i--)
            {
                snapshots[i].Restore();
            }

            if (!host.IsDisposed)
            {
                host.Activate();
                host.BringToFront();
            }
        }
    }

    private readonly record struct FormSnapshot(Form Form, double Opacity)
    {
        public static FormSnapshot Capture(Form form) => new(form, form.Opacity);

        public void Restore()
        {
            if (Form.IsDisposed)
            {
                return;
            }

            Form.Opacity = Opacity;
        }
    }
}
