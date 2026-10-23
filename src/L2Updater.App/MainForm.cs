using System.Drawing;

namespace L2Updater.App;

internal sealed class MainForm : Form
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
        InitializeWindow();
        InitializeControls();
        ApplyLanguage();
    }

    private void InitializeWindow()
    {
        ClientSize = new Size(900, 600);
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
        _lblTitle.Location = new Point(25, 16);
        _lblTitle.AutoSize = true;
        _lblTitle.Font = Theme.TitleFont;
        _lblTitle.ForeColor = Theme.Gold;

        _btnLang.Location = new Point(810, 14);
        _btnLang.Size = new Size(65, 30);
        Theme.ApplyFlatButton(_btnLang);
        _btnLang.Click += (s, e) => ToggleLanguage();

        // Clients
        _lblClientHeader.Location = new Point(25, 62);
        _lblClientHeader.AutoSize = true;
        _lblClientHeader.ForeColor = Theme.MutedText;

        _panelClients.Location = new Point(90, 56);
        _panelClients.Size = new Size(785, 32);
        _panelClients.BackColor = Theme.Background;
        _panelClients.FlowDirection = FlowDirection.LeftToRight;

        // Folder
        _lblFolderHeader.Location = new Point(25, 99);
        _lblFolderHeader.AutoSize = true;
        _lblFolderHeader.ForeColor = Theme.MutedText;

        _lblFolderPath.Location = new Point(90, 96);
        _lblFolderPath.Size = new Size(665, 26);
        _lblFolderPath.BackColor = Theme.Panel;
        _lblFolderPath.ForeColor = Theme.Text;
        _lblFolderPath.TextAlign = ContentAlignment.MiddleLeft;
        _lblFolderPath.AutoEllipsis = true;
        _lblFolderPath.Paint += (s, e) =>
        {
            using var pen = new Pen(Theme.MutedText, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, _lblFolderPath.Width - 1, _lblFolderPath.Height - 1);
        };

        _btnChangeFolder.Location = new Point(765, 95);
        _btnChangeFolder.Size = new Size(110, 28);
        Theme.ApplyFlatButton(_btnChangeFolder);

        // News Panel
        _panelNews.Location = new Point(25, 135);
        _panelNews.Size = new Size(520, 220);
        _panelNews.BackColor = Theme.Panel;
        _panelNews.Paint += (s, e) =>
        {
            using var pen = new Pen(Theme.Gold, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, _panelNews.Width - 1, _panelNews.Height - 1);
        };

        _lblNewsHeader.Location = new Point(12, 10);
        _lblNewsHeader.AutoSize = true;
        _lblNewsHeader.Font = Theme.SectionFont;
        _lblNewsHeader.ForeColor = Theme.Gold;

        _lblNoNews.Location = new Point(12, 45);
        _lblNoNews.Size = new Size(496, 160);
        _lblNoNews.ForeColor = Theme.MutedText;
        _lblNoNews.TextAlign = ContentAlignment.MiddleCenter;

        _panelNews.Controls.Add(_lblNewsHeader);
        _panelNews.Controls.Add(_lblNoNews);

        // Servers Panel
        _panelServers.Location = new Point(560, 135);
        _panelServers.Size = new Size(315, 220);
        _panelServers.BackColor = Theme.Panel;
        _panelServers.Paint += (s, e) =>
        {
            using var pen = new Pen(Theme.Gold, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, _panelServers.Width - 1, _panelServers.Height - 1);
        };

        _lblServersHeader.Location = new Point(12, 10);
        _lblServersHeader.AutoSize = true;
        _lblServersHeader.Font = Theme.SectionFont;
        _lblServersHeader.ForeColor = Theme.Gold;

        _lblNoServers.Location = new Point(12, 45);
        _lblNoServers.Size = new Size(291, 160);
        _lblNoServers.ForeColor = Theme.MutedText;
        _lblNoServers.TextAlign = ContentAlignment.MiddleCenter;

        _panelServers.Controls.Add(_lblServersHeader);
        _panelServers.Controls.Add(_lblNoServers);

        // Progress: File
        _lblFileHeader.Location = new Point(25, 370);
        _lblFileHeader.AutoSize = true;
        _lblFileHeader.ForeColor = Theme.Text;

        _lblFileDetails.Location = new Point(200, 370);
        _lblFileDetails.Size = new Size(625, 20);
        _lblFileDetails.ForeColor = Theme.MutedText;
        _lblFileDetails.TextAlign = ContentAlignment.TopRight;

        _pbFile.Location = new Point(25, 394);
        _pbFile.Size = new Size(800, 12);

        _lblFilePercent.Location = new Point(835, 390);
        _lblFilePercent.Size = new Size(40, 20);
        _lblFilePercent.Font = Theme.BoldFont;
        _lblFilePercent.ForeColor = Theme.Gold;
        _lblFilePercent.TextAlign = ContentAlignment.MiddleLeft;

        // Progress: Total
        _lblTotalHeader.Location = new Point(25, 418);
        _lblTotalHeader.AutoSize = true;
        _lblTotalHeader.ForeColor = Theme.Text;

        _lblTotalDetails.Location = new Point(200, 418);
        _lblTotalDetails.Size = new Size(625, 20);
        _lblTotalDetails.ForeColor = Theme.MutedText;
        _lblTotalDetails.TextAlign = ContentAlignment.TopRight;

        _pbTotal.Location = new Point(25, 442);
        _pbTotal.Size = new Size(800, 12);

        _lblTotalPercent.Location = new Point(835, 438);
        _lblTotalPercent.Size = new Size(40, 20);
        _lblTotalPercent.Font = Theme.BoldFont;
        _lblTotalPercent.ForeColor = Theme.Gold;
        _lblTotalPercent.TextAlign = ContentAlignment.MiddleLeft;

        // Status
        _lblStatus.Location = new Point(25, 472);
        _lblStatus.Size = new Size(850, 24);
        _lblStatus.ForeColor = Theme.MutedText;
        _lblStatus.AutoEllipsis = true;

        // Bottom buttons
        _btnSite.Location = new Point(25, 514);
        _btnSite.Size = new Size(90, 36);
        _btnSite.Visible = false;
        Theme.ApplyFlatButton(_btnSite);

        _btnForum.Location = new Point(125, 514);
        _btnForum.Size = new Size(90, 36);
        _btnForum.Visible = false;
        Theme.ApplyFlatButton(_btnForum);

        _btnCabinet.Location = new Point(225, 514);
        _btnCabinet.Size = new Size(90, 36);
        _btnCabinet.Visible = false;
        Theme.ApplyFlatButton(_btnCabinet);

        _btnCheckAll.Location = new Point(515, 514);
        _btnCheckAll.Size = new Size(180, 36);
        Theme.ApplyFlatButton(_btnCheckAll);

        _btnPlay.Location = new Point(710, 508);
        _btnPlay.Size = new Size(165, 46);
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
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        Text = Strings.Get(_currentLang, "app_title");
        _lblTitle.Text = Strings.Get(_currentLang, "app_title");
        _btnLang.Text = Strings.Get(_currentLang, "lang_button");

        _lblClientHeader.Text = Strings.Get(_currentLang, "client_label");
        _lblFolderHeader.Text = Strings.Get(_currentLang, "folder_label");
        if (string.IsNullOrEmpty(CurrentFolderPath))
            _lblFolderPath.Text = Strings.Get(_currentLang, "empty_folder");
        else
            _lblFolderPath.Text = CurrentFolderPath;

        _btnChangeFolder.Text = Strings.Get(_currentLang, "btn_change_folder");
        _lblNewsHeader.Text = Strings.Get(_currentLang, "news_title");
        _lblNoNews.Text = Strings.Get(_currentLang, "no_news");
        _lblServersHeader.Text = Strings.Get(_currentLang, "servers_title");
        _lblNoServers.Text = Strings.Get(_currentLang, "no_servers");

        _lblFileHeader.Text = Strings.Get(_currentLang, "file_label");
        _lblFileDetails.Text = Strings.Get(_currentLang, "zero_bytes");
        _lblFilePercent.Text = Strings.Get(_currentLang, "zero_percent");

        _lblTotalHeader.Text = Strings.Get(_currentLang, "total_label");
        _lblTotalDetails.Text = Strings.Get(_currentLang, "zero_bytes");
        _lblTotalPercent.Text = Strings.Get(_currentLang, "zero_percent");

        _lblStatus.Text = Strings.Get(_currentLang, "status_ready");

        _btnSite.Text = Strings.Get(_currentLang, "btn_site");
        _btnForum.Text = Strings.Get(_currentLang, "btn_forum");
        _btnCabinet.Text = Strings.Get(_currentLang, "btn_cabinet");
        _btnCheckAll.Text = Strings.Get(_currentLang, "btn_check_all");
        _btnPlay.Text = Strings.Get(_currentLang, "btn_play");
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
