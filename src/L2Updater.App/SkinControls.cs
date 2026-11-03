using System.Drawing;
using System.Drawing.Drawing2D;

namespace L2Updater.App;

/// <summary>The window that owns the background picture (MainForm).</summary>
internal interface IBackgroundHost
{
    Bitmap GetBackground();
}

/// <summary>
/// Own controls do not use WinForms "transparent" painting (it showed other controls' texts under the buttons):
/// they copy the piece of the window background that lies behind them.
/// </summary>
internal static class Backdrop
{
    public static void Draw(Graphics g, Control control)
    {
        var form = control.FindForm();
        if (form is IBackgroundHost host)
        {
            var origin = form.PointToClient(control.PointToScreen(Point.Empty));
            g.DrawImage(host.GetBackground(), new Rectangle(0, 0, control.Width, control.Height),
                origin.X, origin.Y, control.Width, control.Height, GraphicsUnit.Pixel);
        }
        else
        {
            g.Clear(Theme.Background);
        }
    }
}

/// <summary>Button in the style of the design: cut corners, gold frame; the main one is filled with gold.</summary>
internal sealed class GoldButton : Button
{
    private bool _hover;
    private bool _down;

    public bool Primary { get; set; }

    public GoldButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);
        FlatStyle = FlatStyle.Flat;
        BackColor = Theme.Background;
        Cursor = Cursors.Hand;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // the background is copied in OnPaint
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _down = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
    protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Backdrop.Draw(g, this);
        if (Width < 6 || Height < 6) return; // minimized or collapsed: nothing sensible to draw
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new RectangleF(0.5f, 0.5f, Width - 2f, Height - 2f);
        var cut = Primary ? Math.Min(14f, Height * 0.22f) : Math.Min(9f, Height * 0.22f);
        var active = Enabled && (_hover || Focused);

        using (var path = Skin.Chamfer(rect, cut))
        {
            if (Primary)
            {
                var lighten = !Enabled ? -0.0f : _down ? 0f : _hover ? 0.18f : 0.04f;
                using var fill = Skin.GoldBrush(rect, Math.Max(0f, lighten));
                g.FillPath(fill, path);
                if (!Enabled)
                {
                    using var dim = new SolidBrush(Color.FromArgb(150, 20, 16, 12));
                    g.FillPath(dim, path);
                }
            }
            else
            {
                using var fill = new SolidBrush(Color.FromArgb(_down ? 235 : 200, active ? 44 : 26, active ? 34 : 20, active ? 20 : 14));
                g.FillPath(fill, path);
            }

            using var border = Skin.GoldBrush(rect, active ? 0.2f : 0f);
            using var pen = new Pen(border, Primary ? 2.2f : 1.5f);
            g.DrawPath(pen, path);
        }

        if (!Primary)
        {
            var inner = RectangleF.Inflate(rect, -3f, -3f);
            using var path = Skin.Chamfer(inner, cut * 0.7f);
            using var pen = new Pen(Color.FromArgb(Enabled ? 70 : 30, Theme.Gold), 1f);
            g.DrawPath(pen, path);
        }

        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        var textRect = new RectangleF(4, 0, Width - 8, Height - (_down ? 0 : 1));
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        if (Primary)
        {
            using var shine = new SolidBrush(Color.FromArgb(Enabled ? 110 : 40, 255, 240, 190));
            g.DrawString(Text, Font, shine, new RectangleF(textRect.X, textRect.Y + 1.2f, textRect.Width, textRect.Height), format);
        }
        using var text = new SolidBrush(Primary
            ? (Enabled ? Color.FromArgb(0x2A, 0x1C, 0x08) : Color.FromArgb(0x7A, 0x66, 0x44))
            : (Enabled ? Theme.Text : Theme.MutedText));
        g.DrawString(Text, Font, text, textRect, format);
    }
}

/// <summary>Progress bar: dark trough with a thin gold frame, gold gradient fill and a glow at its end.</summary>
internal sealed class GoldProgressBar : Control
{
    private int _value;

    public int Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, 0, 100);
            if (_value != clamped)
            {
                _value = clamped;
                Invalidate();
            }
        }
    }

    public GoldProgressBar()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Background;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // the background is copied in OnPaint
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Backdrop.Draw(g, this);
        if (Width < 6 || Height < 6) return; // minimized or collapsed: nothing sensible to draw
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new RectangleF(0.5f, 0.5f, Width - 2f, Height - 2f);
        var radius = rect.Height / 2f;

        using var trough = RoundRect(rect, radius);
        using (var fill = new SolidBrush(Color.FromArgb(205, 10, 8, 6)))
            g.FillPath(fill, trough);

        if (_value > 0)
        {
            var width = Math.Max(rect.Height, rect.Width * _value / 100f);
            var bar = new RectangleF(rect.X, rect.Y, Math.Min(width, rect.Width), rect.Height);
            using var barPath = RoundRect(bar, radius);
            using (var gold = Skin.GoldBrush(bar, 0.05f))
                g.FillPath(gold, barPath);

            // light stripe on top and a glow at the end
            var state = g.Save();
            g.SetClip(barPath, CombineMode.Intersect);
            using (var stripe = new SolidBrush(Color.FromArgb(70, 255, 255, 230)))
                g.FillRectangle(stripe, bar.X, bar.Y + 1.5f, bar.Width, Math.Max(1f, bar.Height * 0.28f));
            if (_value < 100)
            {
                var glow = new RectangleF(bar.Right - 22, bar.Y, 22, bar.Height);
                using var glowBrush = new LinearGradientBrush(glow, Color.FromArgb(0, 255, 235, 170), Color.FromArgb(200, 255, 240, 190), LinearGradientMode.Horizontal);
                g.FillRectangle(glowBrush, glow);
            }
            g.Restore(state);
        }

        using var border = new Pen(Color.FromArgb(200, Theme.GoldDark), 1.2f);
        g.DrawPath(border, trough);
    }

    private static GraphicsPath RoundRect(RectangleF r, float radius)
    {
        var d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
