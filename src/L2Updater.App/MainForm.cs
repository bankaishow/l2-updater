using System.Reflection;

namespace L2Updater.App;

// Placeholder window so the CI build produces a runnable exe. The real window is task У-3б.
internal sealed class MainForm : Form
{
    public MainForm()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?";
        Text = "L2 Updater " + version;
        ClientSize = new Size(480, 200);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(20, 18, 16);
        Controls.Add(new Label
        {
            Text = "L2 Updater " + version + "\n\nTest build. The real window is coming soon.",
            ForeColor = Color.FromArgb(214, 178, 94),
            Font = new Font("Segoe UI", 12f),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
        });
    }
}
