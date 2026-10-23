using L2Updater.Core;
using Xunit;

namespace L2Updater.Core.Tests;

// Written by Claude. Gemini: do not change this file, change the code until all tests pass.
public class GameLauncherTests
{
    private static ClientInfo Client(string exe, string args = "") =>
        new("mythras", new Dictionary<string, string> { ["en"] = "High Five" }, exe, args,
            "https://cdn.example.com/mythras/files.json", ["https://cdn.example.com/mythras/"], []);

    [Fact]
    public void Start_info_points_to_exe_in_game_folder()
    {
        using var dir = new TempDir();
        dir.Write("system/l2.exe", "exe");
        var info = GameLauncher.BuildStartInfo(Client("system/l2.exe", "-fast"), dir.Path);
        Assert.Equal(Path.GetFullPath(dir.File("system/l2.exe")), info.FileName);
        Assert.Equal(Path.GetFullPath(dir.File("system")), info.WorkingDirectory);
        Assert.Equal("-fast", info.Arguments);
        Assert.False(info.UseShellExecute);
    }

    [Fact]
    public void Missing_exe_throws_update_exception()
    {
        using var dir = new TempDir();
        Assert.Throws<UpdateException>(() => GameLauncher.BuildStartInfo(Client("system/l2.exe"), dir.Path));
    }

    [Fact]
    public void Exe_outside_game_folder_is_refused()
    {
        using var dir = new TempDir();
        Assert.Throws<UnsafePathException>(() => GameLauncher.BuildStartInfo(Client("../cmd.exe"), dir.Path));
    }

    [Theory]
    [InlineData("/pf", true)]
    [InlineData("/pf/", true)]
    [InlineData("/PF/Lineage II", true)]
    [InlineData("/pf86/Games/L2", true)]
    [InlineData("/pf Games/L2", false)]
    [InlineData("/games/l2", false)]
    [InlineData("/pfx", false)]
    public void Program_files_check(string folder, bool expected)
    {
        Assert.Equal(expected, GameLauncher.IsUnderProgramFiles(folder, ["/pf", "/pf86/"]));
    }

    [Fact]
    public void Program_files_check_with_empty_list_is_false()
    {
        Assert.False(GameLauncher.IsUnderProgramFiles("/pf/l2", []));
    }
}
