using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;

namespace L2Updater.App;

/// <summary>
/// Look of the window (task У-6б): picture background, glass panels with gold frames, logo, window frame.
/// All drawing is done once into a bitmap of the window size (MainForm.OnPaintBackground just copies it).
/// Pictures are embedded files from the Assets folder: background.jpg (required) and logo.png (optional:
/// without it the brand name is drawn with a gold font).
/// </summary>
internal static class Skin
{
    public const string BrandName = "DiMythras";
    public const string BrandSite = "dimythras.com";

    /// <summary>Loads an embedded picture by file name; null if there is none.</summary>
    public static Bitmap? LoadImage(string fileName)
    {
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            var name = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("." + fileName, StringComparison.OrdinalIgnoreCase));
            if (name == null) return null;
            using var stream = asm.GetManifestResourceStream(name);
            if (stream == null) return null;
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;
            using var image = Image.FromStream(ms);
            return new Bitmap(image); // a copy: the stream may be closed
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Window and taskbar icon (Assets/app.ico, embedded); null if it cannot be read.</summary>
    public static Icon? LoadIcon()
    {
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            var name = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(".app.ico", StringComparison.OrdinalIgnoreCase));
            if (name == null) return null;
            using var stream = asm.GetManifestResourceStream(name);
            return stream == null ? null : new Icon(stream);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Rectangle with cut corners (like the frames of the design).</summary>
    public static GraphicsPath Chamfer(RectangleF r, float cut)
    {
        var path = new GraphicsPath();
        path.AddPolygon(
        [
            new PointF(r.Left + cut, r.Top), new PointF(r.Right - cut, r.Top), new PointF(r.Right, r.Top + cut),
            new PointF(r.Right, r.Bottom - cut), new PointF(r.Right - cut, r.Bottom), new PointF(r.Left + cut, r.Bottom),
            new PointF(r.Left, r.Bottom - cut), new PointF(r.Left, r.Top + cut),
        ]);
        return path;
    }

    /// <summary>Vertical gold gradient: light on top, gold in the middle, dark at the bottom.</summary>
    public static LinearGradientBrush GoldBrush(RectangleF r, float lighten = 0f)
    {
        var rect = new RectangleF(r.X, r.Y, Math.Max(1f, r.Width), Math.Max(1f, r.Height));
        var brush = new LinearGradientBrush(rect, Theme.GoldLight, Theme.GoldDark, LinearGradientMode.Vertical)
        {
            InterpolationColors = new ColorBlend
            {
                Positions = [0f, 0.5f, 1f],
                Colors = [Mix(Theme.GoldLight, Color.White, lighten), Mix(Theme.Gold, Color.White, lighten), Mix(Theme.GoldDark, Color.White, lighten)],
            },
        };
        return brush;
    }

    public static Color Mix(Color from, Color to, double k) => Color.FromArgb(
        (int)(from.R + (to.R - from.R) * k),
        (int)(from.G + (to.G - from.G) * k),
        (int)(from.B + (to.B - from.B) * k));

    /// <summary>The whole background: picture, shade for readable text, glass panels, logo and window frame.</summary>
    public static Bitmap RenderBackground(Size size, IEnumerable<Rectangle> glassPanels)
    {
        var bmp = new Bitmap(Math.Max(1, size.Width), Math.Max(1, size.Height), System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Theme.Background);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        using (var picture = LoadImage("background.jpg"))
        {
            if (picture != null)
                g.DrawImage(picture, new Rectangle(0, 0, bmp.Width, bmp.Height));
        }

        // Shade at the bottom (progress texts) and a light one on top (logo, client choice).
        // A minimized window is 0 pixels high: a gradient over an empty rectangle throws, so tiny sizes get no shading.
        var bottom = new Rectangle(0, (int)(bmp.Height * 0.52), bmp.Width, bmp.Height - (int)(bmp.Height * 0.52));
        if (bottom.Width > 0 && bottom.Height > 0)
            using (var brush = new LinearGradientBrush(bottom, Color.FromArgb(0, 8, 6, 4), Color.FromArgb(215, 8, 6, 4), LinearGradientMode.Vertical))
                g.FillRectangle(brush, bottom);
        var top = new Rectangle(0, 0, bmp.Width, (int)(bmp.Height * 0.30));
        if (top.Width > 0 && top.Height > 0)
            using (var brush = new LinearGradientBrush(top, Color.FromArgb(120, 8, 6, 4), Color.FromArgb(0, 8, 6, 4), LinearGradientMode.Vertical))
                g.FillRectangle(brush, top);

        foreach (var panel in glassPanels)
            DrawGlassPanel(g, panel);

        DrawLogo(g, bmp.Width / 2f, bmp.Height * 0.035f, bmp.Width * 0.30f);
        DrawFrame(g, bmp.Size);
        return bmp;
    }

    private static void DrawGlassPanel(Graphics g, Rectangle r)
    {
        if (r.Width < 10 || r.Height < 10) return;
        var rect = new RectangleF(r.X + 0.5f, r.Y + 0.5f, r.Width - 1f, r.Height - 1f);
        var cut = Math.Max(8f, r.Height * 0.04f);

        using (var path = Chamfer(rect, cut))
        {
            using var fill = new SolidBrush(Color.FromArgb(178, 18, 14, 10));
            g.FillPath(fill, path);
            using var border = GoldBrush(rect);
            using var pen = new Pen(border, 1.6f);
            g.DrawPath(pen, path);
        }

        var inner = RectangleF.Inflate(rect, -4f, -4f);
        using (var path = Chamfer(inner, cut * 0.7f))
        using (var pen = new Pen(Color.FromArgb(70, Theme.Gold), 1f))
            g.DrawPath(pen, path);

        // thin gold line under the header
        using (var pen = new Pen(Color.FromArgb(110, Theme.Gold), 1f))
            g.DrawLine(pen, rect.Left + 14, rect.Top + 40, rect.Right - 14, rect.Top + 40);

        // corner marks
        foreach (var (x, y) in new[] { (rect.Left, rect.Top), (rect.Right, rect.Top), (rect.Left, rect.Bottom), (rect.Right, rect.Bottom) })
            DrawDiamond(g, x, y, 4f, Theme.GoldLight, null);
    }

    private static void DrawDiamond(Graphics g, float cx, float cy, float radius, Color outer, Color? gem)
    {
        PointF[] points = [new(cx, cy - radius), new(cx + radius, cy), new(cx, cy + radius), new(cx - radius, cy)];
        using (var brush = new SolidBrush(outer)) g.FillPolygon(brush, points);
        using (var pen = new Pen(Theme.GoldDark, 1f)) g.DrawPolygon(pen, points);
        if (gem is { } color)
        {
            var inner = radius * 0.45f;
            using var brush = new SolidBrush(color);
            g.FillEllipse(brush, cx - inner, cy - inner, inner * 2, inner * 2);
        }
    }

    private static void DrawFrame(Graphics g, Size size)
    {
        var outer = new RectangleF(5, 5, size.Width - 11, size.Height - 11);
        using (var border = GoldBrush(outer))
        using (var pen = new Pen(border, 2f))
            g.DrawRectangle(pen, outer.X, outer.Y, outer.Width, outer.Height);
        var inner = RectangleF.Inflate(outer, -5f, -5f);
        using (var pen = new Pen(Color.FromArgb(80, Theme.Gold), 1f))
            g.DrawRectangle(pen, inner.X, inner.Y, inner.Width, inner.Height);

        var gem = Color.FromArgb(0x5E, 0xB4, 0xE8);
        foreach (var (x, y) in new[] { (outer.Left, outer.Top), (outer.Right, outer.Top), (outer.Left, outer.Bottom), (outer.Right, outer.Bottom) })
            DrawDiamond(g, x, y, 9f, Theme.Gold, gem);
    }

    /// <summary>Logo picture (Assets/logo.png) or, if there is none, the brand name in a gold font.</summary>
    private static void DrawLogo(Graphics g, float centerX, float top, float width)
    {
        using (var logo = LoadImage("logo.png"))
        {
            if (logo != null)
            {
                var w = Math.Min(width * 1.3f, logo.Width);
                var h = logo.Height * w / logo.Width;
                g.DrawImage(logo, centerX - w / 2f, top, w, h);
                return;
            }
        }

        FontFamily family;
        try { family = new FontFamily("Georgia"); }
        catch (ArgumentException) { family = FontFamily.GenericSerif; }

        using (family)
        using (var path = new GraphicsPath())
        {
            path.AddString(BrandName, family, (int)FontStyle.Bold, width * 0.20f, PointF.Empty, StringFormat.GenericTypographic);
            var bounds = path.GetBounds();
            using (var move = new System.Drawing.Drawing2D.Matrix())
            {
                move.Translate(centerX - bounds.Width / 2f - bounds.X, top - bounds.Y);
                path.Transform(move);
            }
            bounds = path.GetBounds();

            for (var i = 3; i >= 1; i--)
            {
                using var glow = new Pen(Color.FromArgb(26, 255, 170, 60), i * 4f) { LineJoin = LineJoin.Round };
                g.DrawPath(glow, path);
            }
            using (var shadow = new Pen(Color.FromArgb(235, 38, 20, 6), 5f) { LineJoin = LineJoin.Round })
                g.DrawPath(shadow, path);
            using (var fill = GoldBrush(bounds, 0.08f))
                g.FillPath(fill, path);
            using (var edge = new Pen(Color.FromArgb(150, 255, 240, 190), 1f) { LineJoin = LineJoin.Round })
                g.DrawPath(edge, path);

            // site address under the name, letters spaced out
            using var small = new Font(family, Math.Max(9f, width * 0.04f), FontStyle.Bold, GraphicsUnit.Pixel);
            var spacing = small.Size * 0.35f;
            var widths = BrandSite.Select(c => g.MeasureString(c.ToString(), small, PointF.Empty, StringFormat.GenericTypographic).Width).ToArray();
            var total = widths.Sum() + spacing * (BrandSite.Length - 1);
            var x = centerX - total / 2f;
            var y = bounds.Bottom + small.Size * 0.35f;
            using var textBrush = new SolidBrush(Theme.GoldLight);
            using var shadowBrush = new SolidBrush(Color.FromArgb(200, 20, 10, 2));
            for (var i = 0; i < BrandSite.Length; i++)
            {
                g.DrawString(BrandSite[i].ToString(), small, shadowBrush, x + 1.2f, y + 1.2f, StringFormat.GenericTypographic);
                g.DrawString(BrandSite[i].ToString(), small, textBrush, x, y, StringFormat.GenericTypographic);
                x += widths[i] + spacing;
            }
        }
    }
}
