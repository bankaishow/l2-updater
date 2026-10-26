using L2Updater.Core;
using Xunit;

namespace L2Updater.Core.Tests;

public class SelfUpdaterTests
{
    private const string Url = "https://cdn.example.com/updater/L2Updater-1.0.2.exe";
    private static readonly byte[] Content = Bytes.Random(5000);

    private static UpdaterInfo Info(byte[]? content = null, string url = Url)
    {
        content ??= Content;
        return new UpdaterInfo("1.0.2", url, Bytes.Sha256(content), content.Length);
    }

    private static SelfUpdater Updater(FakeServer server) => new(new HttpClient(server));

    [Theory]
    [InlineData("0.1.5", "0.1.6", true)]
    [InlineData("0.1.5", "0.1.5", false)]
    [InlineData("0.1.5", "0.1.4", false)]
    [InlineData("0.1.5", "1.0.2", true)]
    [InlineData("0.1.9", "0.1.10", true)]
    [InlineData("0.1.5", "abc", false)]
    [InlineData("?", "1.0.0", false)]
    [InlineData("0.1.5", null, false)]
    public void IsNewer_compares_numbers(string? current, string? offered, bool expected)
    {
        Assert.Equal(expected, SelfUpdater.IsNewer(current, offered));
    }

    [Fact]
    public async Task Downloads_and_verifies()
    {
        using var dir = new TempDir();
        var server = new FakeServer();
        server.Add(Url, Content);
        var reports = new RecordingProgress();
        var path = await Updater(server).DownloadAsync(Info(), dir.File("L2Updater.exe"), reports);
        Assert.Equal(dir.File("L2Updater.exe.new"), path);
        Assert.Equal(Content, File.ReadAllBytes(path));
        Assert.Equal(Content.Length, reports.Reports[^1]);
    }

    [Fact]
    public async Task Wrong_hash_deletes_file()
    {
        using var dir = new TempDir();
        var server = new FakeServer();
        server.Add(Url, Bytes.Random(5000).Select(b => (byte)(b ^ 1)).ToArray());
        await Assert.ThrowsAsync<UpdateException>(() => Updater(server).DownloadAsync(Info(), dir.File("L2Updater.exe")));
        Assert.False(File.Exists(dir.File("L2Updater.exe.new")));
    }

    [Theory]
    [InlineData(4000)]
    [InlineData(6000)]
    public async Task Wrong_length_fails(int actual)
    {
        using var dir = new TempDir();
        var server = new FakeServer();
        server.Add(Url, Bytes.Random(actual));
        await Assert.ThrowsAsync<UpdateException>(() => Updater(server).DownloadAsync(Info(), dir.File("L2Updater.exe")));
        Assert.False(File.Exists(dir.File("L2Updater.exe.new")));
    }

    [Fact]
    public async Task Http_url_is_refused_without_request()
    {
        using var dir = new TempDir();
        var server = new FakeServer();
        await Assert.ThrowsAsync<UpdateException>(() =>
            Updater(server).DownloadAsync(Info(url: "http://cdn.example.com/x.exe"), dir.File("L2Updater.exe")));
        Assert.Empty(server.Requests);
    }

    [Fact]
    public async Task Missing_file_and_network_error_fail()
    {
        using var dir = new TempDir();
        var server = new FakeServer();
        await Assert.ThrowsAsync<UpdateException>(() => Updater(server).DownloadAsync(Info(), dir.File("L2Updater.exe")));
        server.Break(Url);
        await Assert.ThrowsAsync<UpdateException>(() => Updater(server).DownloadAsync(Info(), dir.File("L2Updater.exe")));
    }

    [Fact]
    public async Task Absurd_size_is_refused()
    {
        using var dir = new TempDir();
        var server = new FakeServer();
        var info = Info() with { Size = SelfUpdater.MaxBytes + 1 };
        await Assert.ThrowsAsync<UpdateException>(() => Updater(server).DownloadAsync(info, dir.File("L2Updater.exe")));
        Assert.Empty(server.Requests);
    }

    [Fact]
    public void Replace_swaps_files_and_keeps_old()
    {
        using var dir = new TempDir();
        dir.Write("L2Updater.exe", "old");
        dir.Write("L2Updater.exe.new", "new");
        SelfUpdater.Replace(dir.File("L2Updater.exe"), dir.File("L2Updater.exe.new"));
        Assert.Equal("new", File.ReadAllText(dir.File("L2Updater.exe")));
        Assert.Equal("old", File.ReadAllText(dir.File("L2Updater.exe.old")));
        Assert.False(File.Exists(dir.File("L2Updater.exe.new")));
    }

    [Fact]
    public void Replace_rolls_back_when_new_file_is_missing()
    {
        using var dir = new TempDir();
        dir.Write("L2Updater.exe", "old");
        Assert.Throws<UpdateException>(() =>
            SelfUpdater.Replace(dir.File("L2Updater.exe"), dir.File("L2Updater.exe.new")));
        Assert.Equal("old", File.ReadAllText(dir.File("L2Updater.exe")));
    }

    [Fact]
    public void CleanUp_removes_leftovers_and_ignores_missing()
    {
        using var dir = new TempDir();
        dir.Write("L2Updater.exe", "cur");
        dir.Write("L2Updater.exe.old", "old");
        SelfUpdater.CleanUp(dir.File("L2Updater.exe"));
        SelfUpdater.CleanUp(dir.File("L2Updater.exe"));
        Assert.False(File.Exists(dir.File("L2Updater.exe.old")));
        Assert.True(File.Exists(dir.File("L2Updater.exe")));
    }
}
