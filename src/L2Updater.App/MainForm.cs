using System.Drawing;

namespace L2Updater.App;

internal sealed partial class MainForm : Form
{
    private string _currentLang = "ru";
    private string? CurrentFolderPath { get; set; }

    private readonly Label _lblTitle = new();
    private readonly GoldButton _btnLang = new();

    private readonly Label _lblClientHeader = new();
    private readonly FlowLayoutPanel _panelClients = new();

    private readonly Label _lblFolderHeader = new();
    private readonly Label _lblFolderPath = new();
    private readonly GoldButton _btnChangeFolder = new();

    private readonly Panel _panelNews = new();
    private readonly Label _lblNewsHeader = new();
    private readonly Label _lblNoNews = new();

    private readonly Panel _panelServers = new();
    private readonly Label _lblServersHeader = new();
    private readonly Label _lblNoServers = new();

    private readonly Label _lblFileHeader = new();
    private readonly Label _lblFileDetails = new();
    private readonly GoldProgressBar _pbFile = new();
    private readonly Label _lblFilePercent = new();

    private readonly Label _lblTotalHeader = new();
    private readonly Label _lblTotalDetails = new();
    private readonly GoldProgressBar _pbTotal = new();
    private readonly Label _lblTotalPercent = new();

    private readonly Label _lblStatus = new();

    private readonly GoldButton _btnSite = new();
    private readonly GoldButton _btnForum = new();
    private readonly GoldButton _btnCabinet = new();
    private readonly GoldButton _btnCheckAll = new();
    private readonly GoldButton _btnPlay = new();
    private readonly GoldButton _btnPause = new();
    private readonly Label _lblDone = new();

    public MainForm()
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        InitializeWindow();
        InitializeControls();
        ApplyLanguage();
        HookLogic();

        ResumeLayout(false);
        PerformLayout();
    }

    private Bitmap? _background;

    private void InitializeWindow()
    {
        ClientSize = new Size(1120, 630);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        Font = Theme.RegularFont;
        DoubleBuffered = true;
    }

    // Picture, glass panels, logo and frame are drawn once for the current window size (Skin.RenderBackground).
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (_background == null || _background.Size != ClientSize)
        {
            _background?.Dispose();
            _background = Skin.RenderBackground(ClientSize, [_panelNews.Bounds, _panelServers.Bounds]);
        }
        e.Graphics.DrawImage(_background, 0, 0, _background.Width, _background.Height); // explicit size: DrawImageUnscaled would follow the screen DPI
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _background?.Dispose();
            _doneTimer.Dispose();
        }
        base.Dispose(disposing);
    }

    // Texts and panels sit directly on the picture: their own background is transparent.
    private void MakeTransparent(Control parent)
    {
        foreach (Control c in parent.Controls)
        {
            if (c == _lblFolderPath || c == _cmbClient) continue;
            if (c is Label or Panel)
                c.BackColor = Color.Transparent;
            if (c.HasChildren) MakeTransparent(c);
        }
    }

    private void InitializeControls()
    {
        // Brand (drawn in the background picture); this label only keeps the title text.
        _lblTitle.Visible = false;

        _btnLang.Location = new Point(1022, 42);
        _btnLang.Size = new Size(72, 30);
        Theme.ApplyFlatButton(_btnLang);
        _btnLang.Click += (s, e) => ToggleLanguage();

        // Client and folder (top left)
        _lblClientHeader.Location = new Point(34, 74);
        _lblClientHeader.Size = new Size(70, 28);
        _lblClientHeader.ForeColor = Theme.Text;
        _lblClientHeader.TextAlign = ContentAlignment.MiddleLeft;

        _panelClients.Location = new Point(108, 72);
        _panelClients.Size = new Size(262, 32);
        _panelClients.FlowDirection = FlowDirection.LeftToRight;

        _lblFolderHeader.Location = new Point(34, 110);
        _lblFolderHeader.Size = new Size(70, 28);
        _lblFolderHeader.ForeColor = Theme.Text;
        _lblFolderHeader.TextAlign = ContentAlignment.MiddleLeft;

        _lblFolderPath.Location = new Point(108, 109);
        _lblFolderPath.Size = new Size(150, 28);
        _lblFolderPath.BackColor = Color.FromArgb(0x12, 0x0F, 0x0B);
        _lblFolderPath.ForeColor = Theme.Text;
        _lblFolderPath.TextAlign = ContentAlignment.MiddleLeft;
        _lblFolderPath.Padding = new Padding(6, 0, 0, 0);
        _lblFolderPath.AutoEllipsis = true;
        _lblFolderPath.Paint += (s, e) =>
        {
            using var pen = new Pen(Theme.GoldDark, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, _lblFolderPath.Width - 1, _lblFolderPath.Height - 1);
        };

        _btnChangeFolder.Location = new Point(264, 109);
        _btnChangeFolder.Size = new Size(106, 28);
        Theme.ApplyFlatButton(_btnChangeFolder);

        // News panel (glass frame is drawn in the background)
        _panelNews.Location = new Point(28, 150);
        _panelNews.Size = new Size(348, 224);

        _lblNewsHeader.Location = new Point(16, 8);
        _lblNewsHeader.Size = new Size(200, 28);
        _lblNewsHeader.Font = Theme.SectionFont;
        _lblNewsHeader.ForeColor = Theme.Gold;
        _lblNewsHeader.TextAlign = ContentAlignment.MiddleLeft;

        _lblNoNews.Location = new Point(14, 48);
        _lblNoNews.Size = new Size(320, 164);
        _lblNoNews.ForeColor = Theme.MutedText;
        _lblNoNews.TextAlign = ContentAlignment.MiddleCenter;

        _panelNews.Controls.Add(_lblNewsHeader);
        _panelNews.Controls.Add(_lblNoNews);
        InitializeList(_newsList, new Point(14, 48), new Size(320, 164));
        _panelNews.Controls.Add(_newsList);

        // Servers panel
        _panelServers.Location = new Point(743, 150);
        _panelServers.Size = new Size(350, 224);

        _lblServersHeader.Location = new Point(16, 8);
        _lblServersHeader.Size = new Size(200, 28);
        _lblServersHeader.Font = Theme.SectionFont;
        _lblServersHeader.ForeColor = Theme.Gold;
        _lblServersHeader.TextAlign = ContentAlignment.MiddleLeft;

        _lblNoServers.Location = new Point(14, 48);
        _lblNoServers.Size = new Size(322, 164);
        _lblNoServers.ForeColor = Theme.MutedText;
        _lblNoServers.TextAlign = ContentAlignment.MiddleCenter;

        _panelServers.Controls.Add(_lblServersHeader);
        _panelServers.Controls.Add(_lblNoServers);
        InitializeList(_serversList, new Point(14, 48), new Size(322, 164));
        _panelServers.Controls.Add(_serversList);

        // Progress: File (header and details above the bar, percent beside the bar)
        _lblFileHeader.Location = new Point(28, 394);
        _lblFileHeader.Size = new Size(130, 24);
        _lblFileHeader.ForeColor = Theme.Text;
        _lblFileHeader.TextAlign = ContentAlignment.MiddleLeft;

        _lblFileDetails.Location = new Point(160, 394);
        _lblFileDetails.Size = new Size(872, 24);
        _lblFileDetails.ForeColor = Theme.MutedText;
        _lblFileDetails.TextAlign = ContentAlignment.MiddleRight;

        _pbFile.Location = new Point(28, 421);
        _pbFile.Size = new Size(1004, 15);

        _lblFilePercent.Location = new Point(1040, 416);
        _lblFilePercent.Size = new Size(64, 26);
        _lblFilePercent.Font = Theme.BoldFont;
        _lblFilePercent.ForeColor = Theme.Gold;
        _lblFilePercent.TextAlign = ContentAlignment.MiddleLeft;

        // Progress: Total
        _lblTotalHeader.Location = new Point(28, 448);
        _lblTotalHeader.Size = new Size(130, 24);
        _lblTotalHeader.ForeColor = Theme.Text;
        _lblTotalHeader.TextAlign = ContentAlignment.MiddleLeft;

        _lblTotalDetails.Location = new Point(160, 448);
        _lblTotalDetails.Size = new Size(872, 24);
        _lblTotalDetails.ForeColor = Theme.MutedText;
        _lblTotalDetails.TextAlign = ContentAlignment.MiddleRight;

        _pbTotal.Location = new Point(28, 475);
        _pbTotal.Size = new Size(1004, 15);

        _lblTotalPercent.Location = new Point(1040, 470);
        _lblTotalPercent.Size = new Size(64, 26);
        _lblTotalPercent.Font = Theme.BoldFont;
        _lblTotalPercent.ForeColor = Theme.Gold;
        _lblTotalPercent.TextAlign = ContentAlignment.MiddleLeft;

        // Status
        _lblStatus.Location = new Point(28, 504);
        _lblStatus.Size = new Size(1064, 28);
        _lblStatus.ForeColor = Theme.MutedText;
        _lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        _lblStatus.AutoEllipsis = true;

        // Bottom buttons
        _btnSite.Location = new Point(28, 550);
        _btnSite.Size = new Size(104, 40);
        _btnSite.Visible = false;
        Theme.ApplyFlatButton(_btnSite);

        _btnForum.Location = new Point(140, 550);
        _btnForum.Size = new Size(104, 40);
        _btnForum.Visible = false;
        Theme.ApplyFlatButton(_btnForum);

        _btnCabinet.Location = new Point(252, 550);
        _btnCabinet.Size = new Size(112, 40);
        _btnCabinet.Visible = false;
        Theme.ApplyFlatButton(_btnCabinet);

        _btnPause.Location = new Point(384, 550);
        _btnPause.Size = new Size(150, 40);
        _btnPause.Visible = false;
        Theme.ApplyFlatButton(_btnPause);

        // "Done" instead of the progress rows when an update has finished (blinks for a few seconds, see MainForm.Logic.cs)
        _lblDone.Location = new Point(28, 398);
        _lblDone.Size = new Size(1064, 96);
        _lblDone.Font = Theme.DoneFont;
        _lblDone.ForeColor = Theme.Gold;
        _lblDone.TextAlign = ContentAlignment.MiddleCenter;
        _lblDone.Visible = false;

        _btnCheckAll.Location = new Point(652, 550);
        _btnCheckAll.Size = new Size(220, 40);
        Theme.ApplyFlatButton(_btnCheckAll);

        _btnPlay.Location = new Point(884, 540);
        _btnPlay.Size = new Size(210, 60);
        _btnPlay.Enabled = false;
        Theme.ApplyFlatButton(_btnPlay, isPrimary: true);

        // Add to form
        Controls.Add(_lblTitle);
        Controls.Add(_btnLang);
        Controls.Add(_lblClientHeader);
        Controls.Add(_panelClients);
        Controls.Add(_lblFolderHeader);
        Controls.Add(_lblFolderPath);
        Controls.Add(_btnChangeFolder);
        Controls.Add(_panelNews);
        Controls.Add(_panelServers);
        Controls.Add(_lblFileHeader);
        Controls.Add(_lblFileDetails);
        Controls.Add(_pbFile);
        Controls.Add(_lblFilePercent);
        Controls.Add(_lblTotalHeader);
        Controls.Add(_lblTotalDetails);
        Controls.Add(_pbTotal);
        Controls.Add(_lblTotalPercent);
        Controls.Add(_lblStatus);
        Controls.Add(_btnSite);
        Controls.Add(_btnForum);
        Controls.Add(_btnCabinet);
        Controls.Add(_btnPause);
        Controls.Add(_lblDone);
        Controls.Add(_btnCheckAll);
        Controls.Add(_btnPlay);

        MakeTransparent(this);
    }

    private void ToggleLanguage()
    {
        _currentLang = string.Equals(_currentLang, "ru", StringComparison.OrdinalIgnoreCase) ? "en" : "ru";
        _settings.Language = _currentLang;
        SaveSettings();
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        Text = Strings.Get(_currentLang, "app_title") + " " + AppConfig.BuildVersion;
        _lblTitle.Text = Strings.Get(_currentLang, "app_title");
        _btnLang.Text = Strings.Get(_currentLang, "lang_button");

        _lblClientHeader.Text = Strings.Get(_currentLang, "client_label");
        _lblFolderHeader.Text = Strings.Get(_currentLang, "folder_label");
        _lblFolderPath.Text = string.IsNullOrEmpty(CurrentFolderPath)
            ? Strings.Get(_currentLang, "empty_folder")
            : CurrentFolderPath;

        _btnChangeFolder.Text = Strings.Get(_currentLang, "btn_change_folder");
        _lblNewsHeader.Text = Strings.Get(_currentLang, "news_title");
        _lblNoNews.Text = Strings.Get(_currentLang, "no_news");
        _lblServersHeader.Text = Strings.Get(_currentLang, "servers_title");
        _lblNoServers.Text = Strings.Get(_currentLang, "no_servers");

        _lblFileHeader.Text = Strings.Get(_currentLang, "file_label");
        if (!_busy)
        {
            _lblFileDetails.Text = Strings.Get(_currentLang, "zero_bytes");
            _lblFilePercent.Text = Strings.Get(_currentLang, "zero_percent");
        }

        _lblTotalHeader.Text = Strings.Get(_currentLang, "total_label");
        if (!_busy)
        {
            _lblTotalDetails.Text = Strings.Get(_currentLang, "zero_bytes");
            _lblTotalPercent.Text = Strings.Get(_currentLang, "zero_percent");
        }

        _lblStatus.Text = _status();
        _lblStatus.ForeColor = _statusColor;

        _btnSite.Text = Strings.Get(_currentLang, "btn_site");
        _btnForum.Text = Strings.Get(_currentLang, "btn_forum");
        _btnCabinet.Text = Strings.Get(_currentLang, "btn_cabinet");
        _btnCheckAll.Text = Strings.Get(_currentLang, "btn_check_all");
        _btnPlay.Text = Strings.Get(_currentLang, "btn_play");
        _btnPause.Text = Strings.Get(_currentLang, _pause.IsPaused ? "btn_resume" : "btn_pause");
        _lblDone.Text = Strings.Get(_currentLang, "done_big");

        RefreshClientList();
        RenderLists();
    }
}
