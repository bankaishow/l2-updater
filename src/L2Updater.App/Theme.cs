using System.Drawing;

namespace L2Updater.App;

internal static class Theme
{
    public static readonly Color Background = Color.FromArgb(0x14, 0x12, 0x0F);
    public static readonly Color Panel = Color.FromArgb(0x1E, 0x1A, 0x15);
    public static readonly Color Gold = Color.FromArgb(0xD6, 0xB2, 0x5E);
    public static readonly Color Text = Color.FromArgb(0xE8, 0xDC, 0xC0);
    public static readonly Color MutedText = Color.FromArgb(0x8A, 0x7F, 0x6A);
    public static readonly Color Online = Color.FromArgb(0x6C, 0xC0, 0x70);
    public static readonly Color Offline = Color.FromArgb(0xA0, 0x50, 0x50);

    public static readonly Font TitleFont = new("Segoe UI", 16f, FontStyle.Bold);
    public static readonly Font SectionFont = new("Segoe UI", 10.5f, FontStyle.Bold);
    public static readonly Font RegularFont = new("Segoe UI", 9.5f, FontStyle.Regular);
    public static readonly Font BoldFont = new("Segoe UI", 9.5f, FontStyle.Bold);
    public static readonly Font PlayFont = new("Segoe UI", 13f, FontStyle.Bold);
    public static readonly Font SmallFont = new("Segoe UI", 8.5f, FontStyle.Regular);

    public static void ApplyFlatButton(Button button, bool isPrimary = false)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = Gold;

        if (isPrimary)
        {
            button.BackColor = Gold;
            button.ForeColor = Background;
            button.Font = PlayFont;
        }
        else
        {
            button.BackColor = Panel;
            button.ForeColor = Text;
            button.Font = RegularFont;
        }
    }
}
