using System.Diagnostics;
using System.Drawing;
using L2Updater.Core;

namespace L2Updater.App;

// Window behaviour (task У-3г2): startup, client choice, update run, news, launch.
internal sealed partial class MainForm
{
    private static readonly TimeSpan NewsTimeout = TimeSpan.FromSeconds(10);

    private readonly Panel _newsList = new();
    private readonly Panel _serversList = new();

    private readonly CancellationTokenSource _lifetime = new();
    private readonly HashSet<string> _updatedClients = new(StringComparer.Ordinal);
    private readonly HashSet<string> _warnedFolders = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(ClientInfo Client, Button Button)> _clientButtons = [];

    private UpdaterSettings _settings = new();
    private string _settingsFile = "";
    private HttpClient? _http;
    private UpdateSession? _session;
    private Manifest? _manifest;
    private ClientInfo? _client;
    private bool _busy;
    private IReadOnlyList<NewsItem> _news = [];
    private IReadOnlyList<ServerStatus> _servers = [];
    private Func<string> _status = () => "";
    private Color _statusColor = Theme.MutedText;

    private string T(string key, params object[] args) =>
        args.Length == 0 ? Strings.Get(_currentLang, key) : string.Format(Strings.Get(_currentLang, key), args);

    private float Dpi96 => DeviceDpi / 96f;

    private void HookLogic()
    {
        _status = () => T("status_ready");
        _statusColor = Theme.MutedText;
        _lblStatus.Text = _status();

        Shown += OnShown;
        FormClosing += (_, _) => _lifetime.Cancel();

        _btnSite.Click += (_, _) => OpenUrl(_manifest?.Links?.Site);
        _btnForum.Click += (_, _) => OpenUrl(_manifest?.Links?.Forum);
        _btnCabinet.Click += (_, _) => OpenUrl(_manifest?.Links?.Cabinet);
        _btnChangeFolder.Click += async (_, _) => await Guarded(ChangeFolderAsync);
        _btnCheckAll.Click += async (_, _) => await Guarded(CheckAllAsync);
        _btnPlay.Click += (_, _) => Play();

        UpdateControls();
    }

    private static void InitializeList(Panel list, Point location, Size size)
    {
        list.Location = location;
        list.Size = size;
        list.BackColor = Theme.Panel;
        list.AutoScroll = true;
        list.Visible = false;
    }

    // ---- startup ----

    private async void OnShown(object? sender, EventArgs e) => await Guarded(StartupAsync);

    private async Task Guarded(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            if (!IsDisposed) HandleError(ex);
        }
    }

    private async Task StartupAsync()
    {
        var folder = UpdaterSettings.ChooseFolder(AppContext.BaseDirectory,
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        if (Environment.ProcessPath is { } self) SelfUpdater.CleanUp(self);
        _settingsFile = Path.Combine(folder, UpdaterSettings.FileName);
        _settings = UpdaterSettings.Load(_settingsFile);
        _currentLang = string.Equals(_settings.Language, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "ru";

        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("L2Updater/" + AppConfig.BuildVersion);
        _session = new UpdateSession(_http, new SignatureVerifier(AppConfig.TrustedKeyBytes()));

        SetStatus(() => T("status_ready"));
        ApplyLanguage();
        await LoadManifestAsync();
    }

    private async Task LoadManifestAsync()
    {
        _busy = true;
        UpdateControls();
        try
        {
            SetStatus(() => T("status_loading"));
            var url = _settings.ManifestUrl ?? AppConfig.DefaultManifestUrl;
            var manifest = await _session!.LoadManifestAsync(url, _settings.LastSerial, _lifetime.Token);
            _manifest = manifest;
            _settings.LastSerial = Math.Max(_settings.LastSerial, manifest.Serial);
            SaveSettings();
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            if (!IsDisposed) HandleError(ex);
            return;
        }
        finally
        {
            _busy = false;
            if (!IsDisposed) UpdateControls();
        }

        if (await SelfUpdateAsync(_manifest!)) return;

        ApplyManifest(_manifest!);
        _ = LoadExtrasAsync(_manifest!);

        if (_clientButtons.Count == 0)
        {
            SetStatus(() => T("no_clients"), error: true);
            return;
        }

        var selected = _clientButtons.FirstOrDefault(c => c.Client.Id == _settings.LastClient).Client
                       ?? _clientButtons[0].Client;
        await SelectClientAsync(selected, force: true);
    }

    // ---- self-update (task У-4) ----

    private const string UpdatedFlag = "--updated";

    /// <summary>
    /// Downloads a newer updater, swaps the exe and starts it. True = the new copy was started and this window closes.
    /// Any failure only shows a message; the game update goes on with the current version.
    /// The flag stops a restart loop if the new exe still looks older than the manifest says.
    /// </summary>
    private async Task<bool> SelfUpdateAsync(Manifest manifest)
    {
        var exe = Environment.ProcessPath;
        if (exe == null || !exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(Path.GetFileName(exe), "L2Updater.exe", StringComparison.OrdinalIgnoreCase))
            return false;
        if (Environment.GetCommandLineArgs().Contains(UpdatedFlag)) return false;
        var info = manifest.Updater;
        if (info == null || !SelfUpdater.IsNewer(AppConfig.BuildVersion, info.Version)) return false;

        _busy = true;
        ResetProgress();
        UpdateControls();
        try
        {
            SetStatus(() => T("status_self_update", info.Version));
            var progress = new Progress<long>(done =>
            {
                if (IsDisposed || !_busy) return;
                var percent = (int)(done * 100 / info.Size);
                _pbTotal.Value = percent;
                _lblTotalPercent.Text = percent + "%";
                _lblTotalDetails.Text = $"{ProgressTracker.FormatBytes(done)} / {ProgressTracker.FormatBytes(info.Size)}";
            });
            var newFile = await new SelfUpdater(_http!).DownloadAsync(info, exe, progress, _lifetime.Token);
            SelfUpdater.Replace(exe, newFile);
            try
            {
                Process.Start(new ProcessStartInfo(exe, UpdatedFlag) { UseShellExecute = false });
            }
            catch
            {
                // The new exe is in place but did not start: put the old one back.
                File.Move(exe, newFile, overwrite: true);
                File.Move(exe + SelfUpdater.OldSuffix, exe, overwrite: true);
                throw;
            }
            Close();
            return true;
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
            return true;
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
                SetStatus(() => T("err_self_update", ex is OperationCanceledException ? T("err_timeout") : ex.Message), error: true);
            return false;
        }
        finally
        {
            _busy = false;
            if (!IsDisposed) UpdateControls();
        }
    }

    private void ApplyManifest(Manifest manifest)
    {
        _btnSite.Visible = !string.IsNullOrWhiteSpace(manifest.Links?.Site);
        _btnForum.Visible = !string.IsNullOrWhiteSpace(manifest.Links?.Forum);
        _btnCabinet.Visible = !string.IsNullOrWhiteSpace(manifest.Links?.Cabinet);

        foreach (var (_, button) in _clientButtons)
            button.Dispose();
        _clientButtons.Clear();
        _panelClients.Controls.Clear();

        foreach (var client in manifest.Clients)
        {
            var button = new Button
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, (int)(8 * Dpi96), 0),
                Padding = new Padding((int)(10 * Dpi96), 0, (int)(10 * Dpi96), 0),
                MinimumSize = new Size(0, (int)(34 * Dpi96)),
            };
            Theme.ApplyFlatButton(button);
            var captured = client;
            button.Click += async (_, _) => await Guarded(() => SelectClientAsync(captured, force: false));
            _clientButtons.Add((client, button));
            _panelClients.Controls.Add(button);
        }

        RefreshClientButtons();
    }

    // ---- clients ----

    private string ClientTitle(ClientInfo client)
    {
        if (client.Title.TryGetValue(_currentLang, out var title) && !string.IsNullOrWhiteSpace(title)) return title;
        if (client.Title.TryGetValue("en", out title) && !string.IsNullOrWhiteSpace(title)) return title;
        return client.Id;
    }

    private void RefreshClientButtons()
    {
        foreach (var (client, button) in _clientButtons)
        {
            button.Text = ClientTitle(client);
            var selected = _client != null && _client.Id == client.Id;
            button.BackColor = selected ? Theme.Gold : Theme.Panel;
            button.ForeColor = selected ? Theme.Background : Theme.Text;
            button.Font = selected ? Theme.BoldFont : Theme.RegularFont;
        }
    }

    private async Task SelectClientAsync(ClientInfo client, bool force)
    {
        if (_busy) return;
        if (!force && _client != null && _client.Id == client.Id) return;

        _client = client;
        _settings.LastClient = client.Id;
        SaveSettings();
        RefreshClientButtons();
        RefreshFolderLabel();
        await RunUpdateAsync(fullCheck: false);
    }

    // ---- folder ----

    private string? GetFolder(ClientInfo client) =>
        _settings.GameFolders.TryGetValue(client.Id, out var folder) && Directory.Exists(folder) ? folder : null;

    private void RefreshFolderLabel()
    {
        CurrentFolderPath = _client == null ? null : GetFolder(_client);
        _lblFolderPath.Text = string.IsNullOrEmpty(CurrentFolderPath) ? T("empty_folder") : CurrentFolderPath;
    }

    private string? PickFolder(ClientInfo client)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = T("folder_dialog") + ": " + ClientTitle(client),
            UseDescriptionForTitle = true,
        };
        if (_settings.GameFolders.TryGetValue(client.Id, out var current) && Directory.Exists(current))
            dialog.InitialDirectory = current;
        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedPath))
            return null;

        _settings.GameFolders[client.Id] = dialog.SelectedPath;
        _warnedFolders.RemoveWhere(k => k.StartsWith(client.Id + "|", StringComparison.Ordinal));
        SaveSettings();
        RefreshFolderLabel();
        return dialog.SelectedPath;
    }

    private async Task ChangeFolderAsync()
    {
        if (_busy || _client == null) return;
        if (PickFolder(_client) == null) return;
        await RunUpdateAsync(fullCheck: false);
    }

    private async Task CheckAllAsync()
    {
        if (_busy) return;
        if (_manifest == null)
        {
            await LoadManifestAsync();
            return;
        }
        await RunUpdateAsync(fullCheck: true);
    }

    private void WarnProgramFiles(ClientInfo client, string folder)
    {
        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
        }.Where(p => !string.IsNullOrEmpty(p));

        if (!GameLauncher.IsUnderProgramFiles(folder, roots)) return;
        if (!_warnedFolders.Add(client.Id + "|" + folder)) return;
        MessageBox.Show(this, T("pf_warning"), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    // ---- update ----

    private async Task RunUpdateAsync(bool fullCheck)
    {
        var client = _client;
        if (client == null || _session == null) return;

        var folder = GetFolder(client) ?? PickFolder(client);
        if (folder == null)
        {
            SetStatus(() => $"{ClientTitle(client)}: {T("folder_required")}");
            UpdateControls();
            return;
        }
        WarnProgramFiles(client, folder);

        _busy = true;
        ResetProgress();
        UpdateControls();
        var running = true;
        try
        {
            SetStatus(() => $"{ClientTitle(client)}: {T("status_checking")}");
            var list = await _session.LoadFileListAsync(client, _lifetime.Token);

            var sizes = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (var file in list.Files) sizes.TryAdd(file.Path, file.Size);

            var tracker = new ProgressTracker(TimeProvider.System);
            string? lastPath = null;
            long finishedBytes = 0;
            var progress = new Progress<UpdateProgress>(p =>
            {
                if (!running || IsDisposed) return;
                if (p.CurrentPath != lastPath)
                {
                    if (lastPath != null && sizes.TryGetValue(lastPath, out var done)) finishedBytes += done;
                    lastPath = p.CurrentPath;
                }

                var fileTotal = sizes.TryGetValue(p.CurrentPath, out var size) ? size : 0;
                var fileDone = Math.Clamp(p.BytesDone - finishedBytes, 0, fileTotal > 0 ? fileTotal : long.MaxValue);
                var filePercent = fileTotal > 0 ? (int)(fileDone * 100 / fileTotal) : 100;
                _pbFile.Value = filePercent;
                _lblFilePercent.Text = filePercent + "%";
                _lblFileDetails.Text = $"{p.CurrentPath} · {ProgressTracker.FormatBytes(fileDone)} / {ProgressTracker.FormatBytes(fileTotal)}";

                tracker.Report(p.BytesDone, p.BytesTotal);
                _pbTotal.Value = tracker.Percent;
                _lblTotalPercent.Text = tracker.Percent + "%";
                _lblTotalDetails.Text = TotalDetails(p, tracker);
            });

            var result = await _session.UpdateAsync(client, list, folder, fullCheck, progress, _lifetime.Token);
            running = false;
            _updatedClients.Add(client.Id);

            _pbFile.Value = 100;
            _pbTotal.Value = 100;
            _lblFilePercent.Text = "100%";
            _lblTotalPercent.Text = "100%";
            if (result.Downloaded == 0)
                SetStatus(() => $"{ClientTitle(client)}: {T("status_all_ok")}");
            else
                SetStatus(() => $"{ClientTitle(client)}: {T("status_done", result.Downloaded, result.Deleted)}");
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            running = false;
            if (!IsDisposed) HandleError(ex);
        }
        finally
        {
            running = false;
            _busy = false;
            if (!IsDisposed) UpdateControls();
        }
    }

    private string TotalDetails(UpdateProgress p, ProgressTracker tracker)
    {
        var parts = new List<string> { $"{ProgressTracker.FormatBytes(p.BytesDone)} / {ProgressTracker.FormatBytes(p.BytesTotal)}" };
        var speed = tracker.BytesPerSecond;
        if (speed > 0) parts.Add(ProgressTracker.FormatBytes((long)speed) + T("per_second"));
        if (tracker.Remaining is { } left)
            parts.Add(left < TimeSpan.FromMinutes(1)
                ? T("less_than_minute")
                : T("minutes", (int)Math.Round(left.TotalMinutes)));
        return string.Join(" · ", parts);
    }

    private void ResetProgress()
    {
        _pbFile.Value = 0;
        _pbTotal.Value = 0;
        _lblFilePercent.Text = T("zero_percent");
        _lblTotalPercent.Text = T("zero_percent");
        _lblFileDetails.Text = T("zero_bytes");
        _lblTotalDetails.Text = T("zero_bytes");
    }

    private void HandleError(Exception ex)
    {
        switch (ex)
        {
            case ManifestException or UnsafePathException:
                SetStatus(() => T("err_tampered"), error: true);
                break;
            case UpdateException:
                SetStatus(() => T("err_update", ex.Message), error: true);
                break;
            case OperationCanceledException:
                // Not our cancellation (that is handled by the callers): an HttpClient timeout.
                SetStatus(() => T("err_update", T("err_timeout")), error: true);
                break;
            default:
                SetStatus(() => T("err_unknown", ex.Message), error: true);
                break;
        }
    }

    private void SetStatus(Func<string> text, bool error = false)
    {
        _status = text;
        _statusColor = error ? Theme.Offline : Theme.MutedText;
        _lblStatus.Text = _status();
        _lblStatus.ForeColor = _statusColor;
    }

    private void UpdateControls()
    {
        foreach (var (_, button) in _clientButtons) button.Enabled = !_busy;
        _btnChangeFolder.Enabled = !_busy && _client != null;
        _btnCheckAll.Enabled = !_busy;
        _btnPlay.Enabled = !_busy && _client != null && _updatedClients.Contains(_client.Id) && GetFolder(_client) != null;
    }

    // ---- play, links ----

    private void Play()
    {
        var client = _client;
        if (client == null) return;
        var folder = GetFolder(client);
        if (folder == null) return;
        try
        {
            Process.Start(GameLauncher.BuildStartInfo(client, folder));
            WindowState = FormWindowState.Minimized;
        }
        catch (Exception ex)
        {
            SetStatus(() => T("err_launch", ex.Message), error: true);
        }
    }

    private static void OpenUrl(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return;
        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) return;
        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch
        {
            // No browser: nothing to do.
        }
    }

    private void SaveSettings()
    {
        if (_settingsFile.Length == 0) return;
        try
        {
            _settings.Save(_settingsFile);
        }
        catch
        {
            // Settings are a convenience; the window works without them.
        }
    }

    // ---- news and server status (independent of the update) ----

    private async Task LoadExtrasAsync(Manifest manifest)
    {
        try
        {
            var newsTask = FetchLimitedAsync(manifest.NewsUrl);
            var statusTask = FetchLimitedAsync(manifest.StatusUrl);
            var news = NewsFeed.ParseNews(await newsTask);
            var servers = NewsFeed.ParseStatus(await statusTask);
            if (IsDisposed) return;
            _news = news;
            _servers = servers;
            RenderLists();
        }
        catch
        {
            // News and status are optional: any failure leaves the "no data" captions.
        }
    }

    private async Task<byte[]?> FetchLimitedAsync(string? url)
    {
        try
        {
            if (_http == null || !Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                return null;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            cts.CancelAfter(NewsTimeout);
            using var response = await _http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            if (!response.IsSuccessStatusCode) return null;

            await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
            var buffer = new byte[NewsFeed.MaxBytes + 1];
            var total = 0;
            while (total < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(total), cts.Token);
                if (read == 0) break;
                total += read;
            }
            return buffer[..total];
        }
        catch
        {
            return null;
        }
    }

    private void RenderLists()
    {
        FillList(_newsList, _lblNoNews, _news.Select(n => (
            Text: n.Date is { } d ? $"{d:yyyy-MM-dd}  {n.Title}" : n.Title,
            Color: n.Url != null ? Theme.Gold : Theme.Text,
            Url: n.Url)).ToList());

        FillList(_serversList, _lblNoServers, _servers.Select(s => (
            Text: s.Online
                ? (s.Players is { } p ? $"{s.Name} — {p} {T("players_suffix")}" : s.Name)
                : $"{s.Name} — {T("offline")}",
            Color: s.Online ? Theme.Online : Theme.Offline,
            Url: (string?)null)).ToList());
    }

    private void FillList(Panel list, Label emptyCaption, List<(string Text, Color Color, string? Url)> rows)
    {
        list.SuspendLayout();
        foreach (Control old in list.Controls) old.Dispose();
        list.Controls.Clear();

        var rowHeight = (int)(28 * Dpi96);
        var width = list.ClientSize.Width - SystemInformation.VerticalScrollBarWidth;
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var label = new Label
            {
                Location = new Point(0, i * rowHeight),
                Size = new Size(width, rowHeight),
                Text = row.Text,
                ForeColor = row.Color,
                BackColor = Theme.Panel,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
            };
            if (row.Url != null)
            {
                var url = row.Url;
                label.Cursor = Cursors.Hand;
                label.Click += (_, _) => OpenUrl(url);
            }
            list.Controls.Add(label);
        }

        list.Visible = rows.Count > 0;
        emptyCaption.Visible = rows.Count == 0;
        list.ResumeLayout();
    }
}
