using L2Updater.Core;
using Xunit;

namespace L2Updater.Core.Tests;

// Written by Claude. Gemini: do not change this file, change the code until all tests pass.
public class FileDownloaderTests
{
    private const string Mirror1 = "https://cdn.example.com/mythras/";
    private const string Mirror2 = "https://mirror.example.net/mythras/";

    private static readonly byte[] Content = Bytes.Random(10_000);
    private static readonly FileEntry Entry = Bytes.Entry("system/l2.exe", Content);

    private static FileDownloader Downloader(FakeServer server) => new(new HttpClient(server));

    private static string PartFile(TempDir dir, FileEntry entry) => dir.File(".l2updater/tmp/" + entry.Sha256 + ".part");

    // ---------- URL ----------

    [Theory]
    [InlineData("https://cdn.example.com/mythras/", "system/l2.exe", "https://cdn.example.com/mythras/system/l2.exe")]
    [InlineData("https://cdn.example.com/mythras", "system/l2.exe", "https://cdn.example.com/mythras/system/l2.exe")]
    [InlineData("https://cdn.example.com/m/", "system/file name#1.dat", "https://cdn.example.com/m/system/file%20name%231.dat")]
    [InlineData("https://cdn.example.com/m/", "maps/Гиран?.unr", "https://cdn.example.com/m/maps/%D0%93%D0%B8%D1%80%D0%B0%D0%BD%3F.unr")]
    public void UrlOf_joins_and_escapes_segments(string baseUrl, string path, string expected)
    {
        Assert.Equal(expected, FileDownloader.UrlOf(baseUrl, path));
    }

    // ---------- normal download ----------

    [Fact]
    public async Task Downloads_from_first_mirror_into_place()
    {
        using var dir = new TempDir();
        var server = new FakeServer();
        server.Add(Mirror1 + "system/l2.exe", Content);
        await Downloader(server).DownloadAsync(Entry, [Mirror1, Mirror2], dir.Path);
        Assert.Equal(Content, File.ReadAllBytes(dir.File("system/l2.exe")));
        Assert.False(File.Exists(PartFile(dir, Entry)));
        Assert.Single(server.Requests);
    }

    [Fact]
    public async Task Replaces_old_file()
    {
        using var dir = new TempDir();
        dir.Write("system/l2.exe", "old version");
        var server = new FakeServer();
        server.Add(Mirror1 + "system/l2.exe", Content);
        await Downloader(server).DownloadAsync(Entry, [Mirror1], dir.Path);
        Assert.Equal(Content, File.ReadAllBytes(dir.File("system/l2.exe")));
    }

    [Fact]
    public async Task Progress_ends_with_file_size()
    {
        using var dir = new TempDir();
        var server = new FakeServer();
        server.Add(Mirror1 + "system/l2.exe", Content);
        var progress = new RecordingProgress();
        await Downloader(server).DownloadAsync(Entry, [Mirror1], dir.Path, progress);
        Assert.NotEmpty(progress.Reports);
        Assert.Equal(Content.Length, progress.Reports[^1]);
        Assert.True(progress.Reports.SequenceEqual(progress.Reports.Order()), "progress must not go back");
    }

    [Fact]
    public async Task Empty_file_is_created()
    {
        using var dir = new TempDir();
        var empty = Bytes.Entry("system/empty.dat", []);
        var server = new FakeServer();
        server.Add(Mirror1 + "system/empty.dat", []);
        await Downloader(server).DownloadAsync(empty, [Mirror1], dir.Path);
        Assert.True(File.Exists(dir.File("system/empty.dat")));
        Assert.Equal(0, new FileInfo(dir.File("system/empty.dat")).Length);
    }

    // ---------- mirrors ----------

    [Fact]
    public async Task Bad_content_on_first_mirror_uses_second()
    {
        using var dir = new TempDir();
        var server = new FakeServer();
        var wrong = (byte[])Content.Clone();
        wrong[5000] ^= 0xFF;
        server.Add(Mirror1 + "system/l2.exe", wrong);
        server.Add(Mirror2 + "system/l2.exe", Content);
        await Downloader(server).DownloadAsync(Entry, [Mirror1, Mirror2], dir.Path);
        Assert.Equal(Content, File.ReadAllBytes(dir.File("system/l2.exe")));
        Assert.Equal(2, server.Requests.Count);
    }

    [Fact]
    public async Task Missing_file_on_first_mirror_uses_second()
    {
        using var dir = new TempDir();
        var server = new FakeServer();
        server.Add(Mirror2 + "system/l2.exe", Content);
        await Downloader(server).DownloadAsync(Entry, [Mirror1, Mirror2], dir.Path);
        Assert.Equal(Content, File.ReadAllBytes(dir.File("system/l2.exe")));
    }

    [Fact]
    public async Task Network_error_on_first_mirror_uses_second()
    {
        using var dir = new TempDir();
        var server = new FakeServer();
        server.Break(Mirror1 + "system/l2.exe");
        server.Add(Mirror2 + "system/l2.exe", Content);
        await Downloader(server).DownloadAsync(Entry, [Mirror1, Mirror2], dir.Path);
        Assert.Equal(Content, File.ReadAllBytes(dir.File("system/l2.exe")));
    }

    [Fact]
    public async Task Short_file_on_first_mirror_uses_second()
    {
        using var dir = new TempDir();
        var server = new FakeServer();
        server.Add(Mirror1 + "system/l2.exe", Content[..5000]);
        server.Add(Mirror2 + "system/l2.exe", Content);
        await Downloader(server).DownloadAsync(Entry, [Mirror1, Mirror2], dir.Path);
        Assert.Equal(Content, File.ReadAllBytes(dir.File("system/l2.exe")));
    }

    [Fact]
    public async Task Too_long_file_is_rejected()
    {
        // a server that sends more than the signed size must not fill the disk
        using var dir = new TempDir();
        var server = new FakeServer();
        server.Add(Mirror1 + "system/l2.exe", Content.Concat(Bytes.Random(50_000)).ToArray());
        server.Add(Mirror2 + "system/l2.exe", Content);
        await Downloader(server).DownloadAsync(Entry, [Mirror1, Mirror2], dir.Path);
        Assert.Equal(Content, File.ReadAllBytes(dir.File("system/l2.exe")));
    }

    [Fact]
    public async Task All_mirrors_bad_keeps_old_file_and_throws()
    {
        using var dir = new TempDir();
        dir.Write("system/l2.exe", "old version");
        var server = new FakeServer();
        server.Add(Mirror1 + "system/l2.exe", Bytes.Of("garbage"));
        server.Break(Mirror2 + "system/l2.exe");
        await Assert.ThrowsAsync<UpdateException>(() => Downloader(server).DownloadAsync(Entry, [Mirror1, Mirror2], dir.Path));
        Assert.Equal("old version", File.ReadAllText(dir.File("system/l2.exe")));
        Assert.False(File.Exists(PartFile(dir, Entry)));
    }

    [Fact]
    public async Task Http_mirror_is_skipped_without_request()
    {
        using var dir = new TempDir();
        var server = new FakeServer();
        server.Add("http://insecure.example.com/m/system/l2.exe", Content);
        server.Add(Mirror1 + "system/l2.exe", Content);
        await Downloader(server).DownloadAsync(Entry, ["http://insecure.example.com/m/", Mirror1], dir.Path);
        Assert.DoesNotContain(server.Requests, r => r.Url.StartsWith("http://", StringComparison.Ordinal));
        Assert.Equal(Content, File.ReadAllBytes(dir.File("system/l2.exe")));
    }

    [Fact]
    public async Task Only_http_mirrors_throw()
    {
        using var dir = new TempDir();
        var server = new FakeServer();
        server.Add("http://insecure.example.com/m/system/l2.exe", Content);
        await Assert.ThrowsAsync<UpdateException>(() =>
            Downloader(server).DownloadAsync(Entry, ["http://insecure.example.com/m/"], dir.Path));
        Assert.Empty(server.Requests);
    }

    // ---------- resume ----------

    [Fact]
    public async Task Partial_file_is_resumed_with_range()
    {
        using var dir = new TempDir();
        dir.Write(".l2updater/tmp/" + Entry.Sha256 + ".part", Content[..4000]);
        var server = new FakeServer();
        server.Add(Mirror1 + "system/l2.exe", Content);
        var progress = new RecordingProgress();
        await Downloader(server).DownloadAsync(Entry, [Mirror1], dir.Path, progress);
        Assert.Equal(4000, server.Requests[0].RangeFrom);
        Assert.Equal(Content, File.ReadAllBytes(dir.File("system/l2.exe")));
        Assert.Equal(Content.Length, progress.Reports[^1]);
    }

    [Fact]
    public async Task Server_without_range_support_restarts_from_zero()
    {
        using var dir = new TempDir();
        dir.Write(".l2updater/tmp/" + Entry.Sha256 + ".part", Content[..4000]);
        var server = new FakeServer { SupportsRange = false };
        server.Add(Mirror1 + "system/l2.exe", Content);
        await Downloader(server).DownloadAsync(Entry, [Mirror1], dir.Path);
        Assert.Equal(Content, File.ReadAllBytes(dir.File("system/l2.exe")));
    }

    [Fact]
    public async Task Partial_file_too_long_is_discarded()
    {
        using var dir = new TempDir();
        dir.Write(".l2updater/tmp/" + Entry.Sha256 + ".part", Bytes.Random(20_000));
        var server = new FakeServer();
        server.Add(Mirror1 + "system/l2.exe", Content);
        await Downloader(server).DownloadAsync(Entry, [Mirror1], dir.Path);
        Assert.Null(server.Requests[0].RangeFrom);
        Assert.Equal(Content, File.ReadAllBytes(dir.File("system/l2.exe")));
    }

    [Fact]
    public async Task Bad_partial_file_does_not_poison_result()
    {
        // the start of the partial file is garbage: the hash check fails, the next mirror downloads from zero
        using var dir = new TempDir();
        dir.Write(".l2updater/tmp/" + Entry.Sha256 + ".part", Bytes.Random(4000));
        var server = new FakeServer();
        server.Add(Mirror1 + "system/l2.exe", Content);
        server.Add(Mirror2 + "system/l2.exe", Content);
        await Downloader(server).DownloadAsync(Entry, [Mirror1, Mirror2], dir.Path);
        Assert.Equal(Content, File.ReadAllBytes(dir.File("system/l2.exe")));
        Assert.Null(server.Requests[^1].RangeFrom);
    }

    // ---------- safety ----------

    [Fact]
    public async Task Unsafe_path_is_refused_before_any_request()
    {
        using var dir = new TempDir();
        var server = new FakeServer();
        var bad = new FileEntry("../evil.dll", Content.Length, Entry.Sha256, false);
        await Assert.ThrowsAsync<UnsafePathException>(() => Downloader(server).DownloadAsync(bad, [Mirror1], dir.Path));
        Assert.Empty(server.Requests);
    }

    [Fact]
    public async Task Cancellation_keeps_old_file()
    {
        using var dir = new TempDir();
        dir.Write("system/l2.exe", "old version");
        using var cts = new CancellationTokenSource();
        var server = new FakeServer { OnRequest = _ => cts.Cancel() };
        server.Add(Mirror1 + "system/l2.exe", Content);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Downloader(server).DownloadAsync(Entry, [Mirror1], dir.Path, null, cts.Token));
        Assert.Equal("old version", File.ReadAllText(dir.File("system/l2.exe")));
    }

    [Fact]
    public async Task Busy_target_gives_close_the_game_message()
    {
        // the target is a folder: File.Move fails with IOException like a file locked by the running game
        using var dir = new TempDir();
        Directory.CreateDirectory(dir.File("system/l2.exe"));
        File.WriteAllText(Path.Combine(dir.File("system/l2.exe"), "inside.txt"), "x");
        var server = new FakeServer();
        server.Add(Mirror1 + "system/l2.exe", Content);
        var ex = await Assert.ThrowsAsync<UpdateException>(() => Downloader(server).DownloadAsync(Entry, [Mirror1], dir.Path));
        Assert.Contains("close the game", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Access_denied_on_replace_gives_close_the_game_message()
    {
        // Windows answers "access denied" (UnauthorizedAccessException), not only IOException.
        // Here: the target folder is read-only for the user, File.Move cannot put the file there.
        if (OperatingSystem.IsWindows() || Environment.UserName == "root")
            return;
        using var dir = new TempDir();
        dir.Write("system/l2.exe", "old version");
        var folder = dir.File("system");
        var server = new FakeServer();
        server.Add(Mirror1 + "system/l2.exe", Content);
        File.SetUnixFileMode(folder, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        try
        {
            var ex = await Assert.ThrowsAsync<UpdateException>(() => Downloader(server).DownloadAsync(Entry, [Mirror1], dir.Path));
            Assert.Contains("close the game", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("old version", File.ReadAllText(dir.File("system/l2.exe")));
        }
        finally
        {
            File.SetUnixFileMode(folder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }
}
