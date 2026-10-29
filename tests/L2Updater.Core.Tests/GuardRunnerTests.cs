using System.Diagnostics;
using L2Updater.Core;
using Xunit;

namespace L2Updater.Core.Tests;

// Written by Claude.
public class GuardRunnerTests
{
    private static ClientInfo Client(string? guard) =>
        new("mythras", new Dictionary<string, string> { ["en"] = "High Five" }, "system/l2.exe", "",
            "https://cdn.example.com/mythras/files.json", ["https://cdn.example.com/mythras/"], [], guard);

    private static ProcessStartInfo Shell(string script)
    {
        var info = OperatingSystem.IsWindows()
            ? new ProcessStartInfo("cmd.exe") { ArgumentList = { "/c", script } }
            : new ProcessStartInfo("/bin/sh") { ArgumentList = { "-c", script } };
        info.UseShellExecute = false;
        info.CreateNoWindow = true;
        return info;
    }

    private static string Sleep() => OperatingSystem.IsWindows() ? "ping -n 30 127.0.0.1 >nul" : "sleep 30";

    // ---------- start info ----------

    [Fact]
    public void No_guard_in_manifest_means_no_start_info()
    {
        using var dir = new TempDir();
        Assert.Null(GuardRunner.BuildStartInfo(Client(null), dir.Path));
    }

    [Fact]
    public void Guard_file_missing_means_no_start_info()
    {
        using var dir = new TempDir();
        Assert.Null(GuardRunner.BuildStartInfo(Client("system/L2Sec.Agent.exe"), dir.Path));
    }

    [Fact]
    public void Start_info_points_to_guard_file_without_window_or_shell()
    {
        using var dir = new TempDir();
        dir.Write("system/L2Sec.Agent.exe", "x");
        var info = GuardRunner.BuildStartInfo(Client("system/L2Sec.Agent.exe"), dir.Path);
        Assert.NotNull(info);
        Assert.Equal(Path.GetFullPath(dir.File("system/L2Sec.Agent.exe")), info!.FileName);
        Assert.Equal(Path.GetFullPath(dir.File("system")), info.WorkingDirectory);
        Assert.False(info.UseShellExecute);
        Assert.True(info.CreateNoWindow);
    }

    [Fact]
    public void Guard_outside_game_folder_is_refused()
    {
        using var dir = new TempDir();
        Assert.Throws<UnsafePathException>(() => GuardRunner.BuildStartInfo(Client("../agent.exe"), dir.Path));
    }

    // ---------- decision ----------

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(20)]
    [InlineData(50)]
    [InlineData(60)]
    [InlineData(99)]
    [InlineData(-1)]
    public void These_codes_let_the_game_start(int code) =>
        Assert.Equal(GuardDecision.Allow, GuardRunner.Decide(code));

    [Fact]
    public void No_exit_code_lets_the_game_start() =>
        Assert.Equal(GuardDecision.Allow, GuardRunner.Decide(null));

    [Fact]
    public void Virtual_machine_blocks() =>
        Assert.Equal(GuardDecision.BlockVirtualMachine, GuardRunner.Decide(10));

    [Fact]
    public void Untrusted_server_blocks() =>
        Assert.Equal(GuardDecision.BlockUntrustedServer, GuardRunner.Decide(30));

    [Fact]
    public void Rejected_blocks() =>
        Assert.Equal(GuardDecision.BlockRejected, GuardRunner.Decide(40));

    // ---------- running ----------

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(40)]
    public async Task Exit_code_of_the_program_is_returned(int code)
    {
        var result = await GuardRunner.RunAsync(Shell($"exit {code}"), TimeSpan.FromSeconds(20));
        Assert.Equal(code, result);
    }

    [Fact]
    public async Task Program_that_cannot_start_gives_null()
    {
        var info = new ProcessStartInfo(Path.Combine(Path.GetTempPath(), "no-such-guard-" + Guid.NewGuid().ToString("N")))
        {
            UseShellExecute = false
        };
        Assert.Null(await GuardRunner.RunAsync(info, TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task Program_that_runs_too_long_is_killed_and_gives_null()
    {
        var watch = Stopwatch.StartNew();
        var result = await GuardRunner.RunAsync(Shell(Sleep()), TimeSpan.FromMilliseconds(300));
        Assert.Null(result);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(20));
    }

    [Fact]
    public async Task Cancelling_stops_the_program_and_throws()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => GuardRunner.RunAsync(Shell(Sleep()), TimeSpan.FromSeconds(20), cts.Token));
    }
}
