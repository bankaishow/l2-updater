using L2Updater.Core;
using Xunit;

namespace L2Updater.Core.Tests;

// Written by Claude. The "Pause" button: PauseGate and its use by FileDownloader.
public class PauseTests
{
    private const string Mirror = "https://cdn.example.com/mythras/";

    // bigger than one read buffer (81920), so a pause can happen in the middle of the file
    private static readonly byte[] Content = Bytes.Random(300_000);
    private static readonly FileEntry Entry = Bytes.Entry("system/big.dat", Content);

    private sealed class PauseOnFirstReport(PauseGate gate) : IProgress<long>
    {
        public List<long> Reports { get; } = [];

        public void Report(long value)
        {
            Reports.Add(value);
            if (Reports.Count == 1) gate.Pause();
        }
    }

    [Fact]
    public async Task Gate_waits_while_paused_and_continues_after_resume()
    {
        var gate = new PauseGate();
        Assert.False(gate.IsPaused);
        await gate.WaitAsync();

        gate.Pause();
        Assert.True(gate.IsPaused);
        var waiting = gate.WaitAsync();
        Assert.False(waiting.IsCompleted);

        gate.Resume();
        await waiting.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(gate.IsPaused);
    }

    [Fact]
    public async Task Waiting_on_a_paused_gate_can_be_cancelled()
    {
        var gate = new PauseGate();
        gate.Pause();
        using var cts = new CancellationTokenSource();
        var waiting = gate.WaitAsync(cts.Token);
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
    }

    [Fact]
    public async Task Pause_and_resume_when_nothing_is_waiting_do_nothing_bad()
    {
        var gate = new PauseGate();
        gate.Resume();
        gate.Pause();
        gate.Pause();
        gate.Resume();
        gate.Resume();
        await gate.WaitAsync();
        Assert.False(gate.IsPaused);
    }

    [Fact]
    public async Task Download_stops_on_pause_and_continues_with_a_range_request()
    {
        using var dir = new TempDir();
        var server = new FakeServer();
        server.Add(Mirror + "system/big.dat", Content);
        var gate = new PauseGate();
        var progress = new PauseOnFirstReport(gate);

        var task = new FileDownloader(new HttpClient(server)).DownloadAsync(Entry, [Mirror], dir.Path, progress, default, gate);

        // it must stop in the middle: not finished, partial file on disk
        await Task.Delay(300);
        Assert.False(task.IsCompleted);
        var part = dir.File(".l2updater/tmp/" + Entry.Sha256 + ".part");
        Assert.True(File.Exists(part));
        Assert.InRange(new FileInfo(part).Length, 1, Content.Length - 1);
        Assert.False(File.Exists(dir.File("system/big.dat")));

        gate.Resume();
        await task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(Content, File.ReadAllBytes(dir.File("system/big.dat")));
        Assert.False(File.Exists(part));
        Assert.Equal(2, server.Requests.Count);
        Assert.Null(server.Requests[0].RangeFrom);
        Assert.True(server.Requests[1].RangeFrom > 0);
        Assert.Equal(Content.Length, progress.Reports[^1]);
    }

    [Fact]
    public async Task Cancelling_while_paused_stops_the_download_and_keeps_the_partial_file()
    {
        using var dir = new TempDir();
        var server = new FakeServer();
        server.Add(Mirror + "system/big.dat", Content);
        var gate = new PauseGate();
        using var cts = new CancellationTokenSource();

        var task = new FileDownloader(new HttpClient(server))
            .DownloadAsync(Entry, [Mirror], dir.Path, new PauseOnFirstReport(gate), cts.Token, gate);
        await Task.Delay(300);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.True(File.Exists(dir.File(".l2updater/tmp/" + Entry.Sha256 + ".part")));
        Assert.False(File.Exists(dir.File("system/big.dat")));
    }

    [Fact]
    public async Task Without_pause_nothing_changes()
    {
        using var dir = new TempDir();
        var server = new FakeServer();
        server.Add(Mirror + "system/big.dat", Content);
        await new FileDownloader(new HttpClient(server)).DownloadAsync(Entry, [Mirror], dir.Path, null, default, new PauseGate());
        Assert.Equal(Content, File.ReadAllBytes(dir.File("system/big.dat")));
        Assert.Single(server.Requests);
    }
}
