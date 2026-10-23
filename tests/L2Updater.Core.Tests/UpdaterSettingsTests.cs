using L2Updater.Core;
using Xunit;

namespace L2Updater.Core.Tests;

// Written by Claude. Gemini: do not change this file, change the code until all tests pass.
public class UpdaterSettingsTests
{
    [Fact]
    public void Missing_file_gives_defaults()
    {
        using var dir = new TempDir();
        var s = UpdaterSettings.Load(dir.File("updater.ini"));
        Assert.Null(s.ManifestUrl);
        Assert.Equal("ru", s.Language);
        Assert.Null(s.LastClient);
        Assert.Equal(0, s.LastSerial);
        Assert.Empty(s.GameFolders);
    }

    [Fact]
    public void All_keys_are_read()
    {
        using var dir = new TempDir();
        dir.Write("updater.ini", """
            # L2 Updater settings
            manifest_url=https://example.com/updater/manifest.json
            language=en
            last_client=mythras
            last_serial=42
            game_folder.mythras=D:\Games\Lineage II
            game_folder.lucera=E:\L2 Classic
            """);
        var s = UpdaterSettings.Load(dir.File("updater.ini"));
        Assert.Equal("https://example.com/updater/manifest.json", s.ManifestUrl);
        Assert.Equal("en", s.Language);
        Assert.Equal("mythras", s.LastClient);
        Assert.Equal(42, s.LastSerial);
        Assert.Equal(@"D:\Games\Lineage II", s.GameFolders["mythras"]);
        Assert.Equal(@"E:\L2 Classic", s.GameFolders["lucera"]);
    }

    [Fact]
    public void Spaces_comments_junk_and_unknown_keys_are_tolerated()
    {
        using var dir = new TempDir();
        dir.Write("updater.ini", "\r\n  language =  en  \r\nthis line has no equals sign\r\n# last_client=commented\r\nfuture_key=1\r\nlast_client=a\r\nlast_client=b\r\n");
        var s = UpdaterSettings.Load(dir.File("updater.ini"));
        Assert.Equal("en", s.Language);
        Assert.Equal("b", s.LastClient);
    }

    [Theory]
    [InlineData("language=de", "ru")]
    [InlineData("language=EN", "ru")]
    [InlineData("language=", "ru")]
    public void Unknown_language_becomes_ru(string line, string expected)
    {
        using var dir = new TempDir();
        dir.Write("updater.ini", line);
        Assert.Equal(expected, UpdaterSettings.Load(dir.File("updater.ini")).Language);
    }

    [Theory]
    [InlineData("manifest_url=http://example.com/manifest.json")]
    [InlineData("manifest_url=file:///C:/manifest.json")]
    [InlineData("manifest_url=not a url")]
    public void Non_https_manifest_url_is_dropped(string line)
    {
        using var dir = new TempDir();
        dir.Write("updater.ini", line);
        Assert.Null(UpdaterSettings.Load(dir.File("updater.ini")).ManifestUrl);
    }

    [Theory]
    [InlineData("last_serial=-5")]
    [InlineData("last_serial=abc")]
    [InlineData("last_serial=99999999999999999999999")]
    public void Bad_serial_becomes_zero(string line)
    {
        using var dir = new TempDir();
        dir.Write("updater.ini", line);
        Assert.Equal(0, UpdaterSettings.Load(dir.File("updater.ini")).LastSerial);
    }

    [Fact]
    public void Save_and_load_round_trip()
    {
        using var dir = new TempDir();
        var file = dir.File("sub/updater.ini");
        var s = new UpdaterSettings
        {
            ManifestUrl = "https://example.com/updater/manifest.json",
            Language = "en",
            LastClient = "mythras",
            LastSerial = 7,
        };
        s.GameFolders["mythras"] = @"D:\Games\Lineage II";
        s.GameFolders["lucera"] = "/home/player/l2";
        s.Save(file);

        var loaded = UpdaterSettings.Load(file);
        Assert.Equal(s.ManifestUrl, loaded.ManifestUrl);
        Assert.Equal("en", loaded.Language);
        Assert.Equal("mythras", loaded.LastClient);
        Assert.Equal(7, loaded.LastSerial);
        Assert.Equal(@"D:\Games\Lineage II", loaded.GameFolders["mythras"]);
        Assert.Equal("/home/player/l2", loaded.GameFolders["lucera"]);
        Assert.False(File.Exists(file + ".tmp"));
    }

    [Fact]
    public void Save_writes_keys_in_documented_order()
    {
        using var dir = new TempDir();
        var file = dir.File("updater.ini");
        var s = new UpdaterSettings { ManifestUrl = "https://e.com/m.json", Language = "ru", LastClient = "x", LastSerial = 3 };
        s.GameFolders["zeta"] = "Z";
        s.GameFolders["alpha"] = "A";
        s.Save(file);
        var keys = File.ReadAllLines(file)
            .Where(l => l.Contains('=') && !l.TrimStart().StartsWith('#'))
            .Select(l => l[..l.IndexOf('=')].Trim())
            .ToList();
        Assert.Equal(["manifest_url", "language", "last_client", "last_serial", "game_folder.alpha", "game_folder.zeta"], keys);
    }

    [Fact]
    public void Null_values_and_values_with_line_breaks_are_not_written()
    {
        // a folder name with a line break must not be able to inject a second key
        using var dir = new TempDir();
        var file = dir.File("updater.ini");
        var s = new UpdaterSettings { ManifestUrl = null, LastClient = "a\nmanifest_url=https://evil.example.com/m.json" };
        s.GameFolders["x"] = "C:\\L2\r\nlast_serial=0";
        s.Save(file);
        var text = File.ReadAllText(file);
        Assert.DoesNotContain("manifest_url", text);
        Assert.DoesNotContain("last_client", text);
        Assert.DoesNotContain("game_folder.x", text);
        Assert.DoesNotContain("evil", text);
    }

    [Fact]
    public void ChooseFolder_uses_exe_folder_when_writable()
    {
        using var exe = new TempDir();
        using var appData = new TempDir();
        Assert.Equal(exe.Path, UpdaterSettings.ChooseFolder(exe.Path, appData.Path));
        Assert.Empty(Directory.GetFiles(exe.Path));
    }

    [Fact]
    public void ChooseFolder_falls_back_to_app_data_when_exe_folder_is_read_only()
    {
        if (OperatingSystem.IsWindows() || Environment.UserName == "root")
            return;
        using var exe = new TempDir();
        using var appData = new TempDir();
        File.SetUnixFileMode(exe.Path, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        try
        {
            var chosen = UpdaterSettings.ChooseFolder(exe.Path, appData.Path);
            Assert.Equal(Path.Combine(appData.Path, "L2Updater"), chosen);
            Assert.True(Directory.Exists(chosen));
        }
        finally
        {
            File.SetUnixFileMode(exe.Path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }
}
