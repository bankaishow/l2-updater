using System.Drawing;

namespace L2Updater.App;

internal sealed partial class MainForm : Form
{
    private string _currentLang = "ru";
    private string? CurrentFolderPath { get; set; }

    private readonly Label _lblTitle = new();
    private readonly Button _btnLang = new();

    private readonly Label _lblClientHeader = new();
    private readonly FlowLayoutPanel _panelClients = new();

    private readonly Label _lblFolderHeader = new();
    private readonly Label _lblFolderPath = new();
    private readonly Button _btnChangeFolder = new();

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

    private readonly Button _btnSite = new();
    private readonly Button _btnForum = new();
    private readonly Button _btnCabinet = new();
    private readonly Button _btnCheckAll = new();
    private readonly Button _btnPlay = new();

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

    private void InitializeWindow()
    {
        ClientSize = new Size(1000, 680);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        Font = Theme.RegularFont;
    }

    private void InitializeControls()
    {
        // Title & Language
        _lblTitle.Location = new Point(30, 18);
        _lblTitle.AutoSize = true;
        _lblTitle.Font = Theme.TitleFont;
        _lblTitle.ForeColor = Theme.Gold;

        _btnLang.Location = new Point(894, 18);
        _btnLang.Size = new Size(76, 34);
        Theme.ApplyFlatButton(_btnLang);
        _btnLang.Click += (s, e) => ToggleLanguage();

        // Clients
        _lblClientHeader.Location = new Point(30, 68);
        _lblClientHeader.Size = new Size(75, 28);
        _lblClientHeader.ForeColor = Theme.MutedText;
        _lblClientHeader.TextAlign = ContentAlignment.MiddleLeft;

        _panelClients.Location = new Point(110, 65);
        _panelClients.Size = new Size(860, 34);
        _panelClients.BackColor = Theme.Background;
        _panelClients.FlowDirection = FlowDirection.LeftToRight;

        // Folder
        _lblFolderHeader.Location = new Point(30, 108);
        _lblFolderHeader.Size = new Size(75, 28);
        _lblFolderHeader.ForeColor = Theme.MutedText;
        _lblFolderHeader.TextAlign = ContentAlignment.MiddleLeft;

        _lblFolderPath.Location = new Point(110, 106);
        _lblFolderPath.Size = new Size(710, 32);
        _lblFolderPath.BackColor = Theme.Panel;
        _lblFolderPath.ForeColor = Theme.Text;
        _lblFolderPath.TextAlign = ContentAlignment.MiddleLeft;
        _lblFolderPath.AutoEllipsis = true;
        _lblFolderPath.Paint += (s, e) =>
        {
            using var pen = new Pen(Theme.MutedText, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, _lblFolderPath.Width - 1, _lblFolderPath.Height - 1);
        };

        _btnChangeFolder.Location = new Point(830, 106);
        _btnChangeFolder.Size = new Size(140, 32);
        Theme.ApplyFlatButton(_btnChangeFolder);

        // News Panel
        _panelNews.Location = new Point(30, 150);
        _panelNews.Size = new Size(580, 240);
        _panelNews.BackColor = Theme.Panel;
        _panelNews.Paint += (s, e) =>
        {
            using var pen = new Pen(Theme.Gold, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, _panelNews.Width - 1, _panelNews.Height - 1);
        };

        _lblNewsHeader.Location = new Point(14, 12);
        _lblNewsHeader.Size = new Size(200, 28);
        _lblNewsHeader.Font = Theme.SectionFont;
        _lblNewsHeader.ForeColor = Theme.Gold;
        _lblNewsHeader.TextAlign = ContentAlignment.MiddleLeft;

        _lblNoNews.Location = new Point(14, 50);
        _lblNoNews.Size = new Size(552, 175);
        _lblNoNews.ForeColor = Theme.MutedText;
        _lblNoNews.TextAlign = ContentAlignment.MiddleCenter;

        _panelNews.Controls.Add(_lblNewsHeader);
        _panelNews.Controls.Add(_lblNoNews);
        InitializeList(_newsList, new Point(14, 50), new Size(552, 175));
        _panelNews.Controls.Add(_newsList);

        // Servers Panel
        _panelServers.Location = new Point(630, 150);
        _panelServers.Size = new Size(340, 240);
        _panelServers.BackColor = Theme.Panel;
        _panelServers.Paint += (s, e) =>
        {
            using var pen = new Pen(Theme.Gold, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, _panelServers.Width - 1, _panelServers.Height - 1);
        };

        _lblServersHeader.Location = new Point(14, 12);
        _lblServersHeader.Size = new Size(200, 28);
        _lblServersHeader.Font = Theme.SectionFont;
        _lblServersHeader.ForeColor = Theme.Gold;
        _lblServersHeader.TextAlign = ContentAlignment.MiddleLeft;

        _lblNoServers.Location = new Point(14, 50);
        _lblNoServers.Size = new Size(312, 175);
        _lblNoServers.ForeColor = Theme.MutedText;
        _lblNoServers.TextAlign = ContentAlignment.MiddleCenter;

        _panelServers.Controls.Add(_lblServersHeader);
        _panelServers.Controls.Add(_lblNoServers);
        InitializeList(_serversList, new Point(14, 50), new Size(312, 175));
        _panelServers.Controls.Add(_serversList);

        // Progress: File (Header & details above the bar, percent beside the bar)
        _lblFileHeader.Location = new Point(30, 405);
        _lblFileHeader.Size = new Size(160, 26);
        _lblFileHeader.ForeColor = Theme.Text;
        _lblFileHeader.TextAlign = ContentAlignment.MiddleLeft;

        _lblFileDetails.Location = new Point(200, 405);
        _lblFileDetails.Size = new Size(710, 26);
        _lblFileDetails.ForeColor = Theme.MutedText;
        _lblFileDetails.TextAlign = ContentAlignment.MiddleRight;

        _pbFile.Location = new Point(30, 434);
        _pbFile.Size = new Size(880, 14);

        _lblFilePercent.Location = new Point(920, 428);
        _lblFilePercent.Size = new Size(50, 26);
        _lblFilePercent.Font = Theme.BoldFont;
        _lblFilePercent.ForeColor = Theme.Gold;
        _lblFilePercent.TextAlign = ContentAlignment.MiddleLeft;

        // Progress: Total (Header & details above the bar, percent beside the bar)
        _lblTotalHeader.Location = new Point(30, 462);
        _lblTotalHeader.Size = new Size(160, 26);
        _lblTotalHeader.ForeColor = Theme.Text;
        _lblTotalHeader.TextAlign = ContentAlignment.MiddleLeft;

        _lblTotalDetails.Location = new Point(200, 462);
        _lblTotalDetails.Size = new Size(710, 26);
        _lblTotalDetails.ForeColor = Theme.MutedText;
        _lblTotalDetails.TextAlign = ContentAlignment.MiddleRight;

        _pbTotal.Location = new Point(30, 491);
        _pbTotal.Size = new Size(880, 14);

        _lblTotalPercent.Location = new Point(920, 485);
        _lblTotalPercent.Size = new Size(50, 26);
        _lblTotalPercent.Font = Theme.BoldFont;
        _lblTotalPercent.ForeColor = Theme.Gold;
        _lblTotalPercent.TextAlign = ContentAlignment.MiddleLeft;

        // Status
        _lblStatus.Location = new Point(30, 526);
        _lblStatus.Size = new Size(940, 30);
        _lblStatus.ForeColor = Theme.MutedText;
        _lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        _lblStatus.AutoEllipsis = true;

        // Bottom buttons
        _btnSite.Location = new Point(30, 578);
        _btnSite.Size = new Size(100, 40);
        _btnSite.Visible = false;
        Theme.ApplyFlatButton(_btnSite);

        _btnForum.Location = new Point(140, 578);
        _btnForum.Size = new Size(100, 40);
        _btnForum.Visible = false;
        Theme.ApplyFlatButton(_btnForum);

        _btnCabinet.Location = new Point(250, 578);
        _btnCabinet.Size = new Size(110, 40);
        _btnCabinet.Visible = false;
        Theme.ApplyFlatButton(_btnCabinet);

        _btnCheckAll.Location = new Point(540, 578);
        _btnCheckAll.Size = new Size(210, 40);
        Theme.ApplyFlatButton(_btnCheckAll);

        _btnPlay.Location = new Point(770, 570);
        _btnPlay.Size = new Size(200, 54);
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
        Controls.Add(_btnCheckAll);
        Controls.Add(_btnPlay);
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

        RefreshClientButtons();
        RenderLists();
    }
}

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
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        BackColor = Theme.Panel;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;

        using (var bgBrush = new SolidBrush(Theme.Panel))
            g.FillRectangle(bgBrush, ClientRectangle);

        if (_value > 0)
        {
            var fillWidth = (int)((ClientRectangle.Width * _value) / 100.0);
            if (fillWidth > 0)
            {
                using var fillBrush = new SolidBrush(Theme.Gold);
                g.FillRectangle(fillBrush, 0, 0, fillWidth, ClientRectangle.Height);
            }
        }

        using (var borderPen = new Pen(Theme.MutedText, 1))
            g.DrawRectangle(borderPen, 0, 0, ClientRectangle.Width - 1, ClientRectangle.Height - 1);
    }
}
