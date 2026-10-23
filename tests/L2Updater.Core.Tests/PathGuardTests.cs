using L2Updater.Core;
using Xunit;

namespace L2Updater.Core.Tests;

// Written by Claude. Gemini: do not change this file, change the code until all tests pass.
public class PathGuardTests
{
    [Theory]
    [InlineData("system/l2.exe")]
    [InlineData("l2.ini")]
    [InlineData("system/sub folder/file name.dat")]
    [InlineData("maps/17_21.unr")]
    [InlineData("system/Interface.u")]
    [InlineData("a/b/c/d/e.txt")]
    [InlineData(".l2updater/cache.json")]
    [InlineData("console.txt")]
    [InlineData("com10.dat")]
    [InlineData("system/conf/auxiliary.ini")]
    public void Valid_paths_are_accepted(string path)
    {
        Assert.True(PathGuard.IsValidRelativePath(path));
    }

    [Theory]
    [InlineData("")]
    [InlineData("/etc/passwd")]
    [InlineData("/x")]
    [InlineData("../x")]
    [InlineData("a/../../x")]
    [InlineData("a/..")]
    [InlineData("./x")]
    [InlineData("a/./b")]
    [InlineData("a//b")]
    [InlineData("a/")]
    [InlineData("C:\\x")]
    [InlineData("C:/x")]
    [InlineData("system\\l2.exe")]
    [InlineData("a:b")]
    [InlineData("file.txt:stream")]
    [InlineData("con.txt")]
    [InlineData("CON")]
    [InlineData("Nul")]
    [InlineData("system/aux.dat")]
    [InlineData("com1")]
    [InlineData("LPT9.log")]
    [InlineData("prn")]
    [InlineData("name.")]
    [InlineData("name ")]
    [InlineData("dir./file")]
    [InlineData("a<b")]
    [InlineData("a>b")]
    [InlineData("a\"b")]
    [InlineData("a|b")]
    [InlineData("a?b")]
    [InlineData("a*b")]
    [InlineData("a\tb")]
    [InlineData("a\0b")]
    public void Bad_paths_are_rejected(string path)
    {
        Assert.False(PathGuard.IsValidRelativePath(path));
    }

    [Fact]
    public void Resolve_returns_full_path_inside_game_folder()
    {
        var root = Path.Combine(Path.GetTempPath(), "l2game");
        var full = PathGuard.Resolve(root, "system/l2.exe");
        Assert.Equal(Path.GetFullPath(Path.Combine(root, "system", "l2.exe")), full);
    }

    [Fact]
    public void Resolve_works_with_trailing_separator_in_root()
    {
        var root = Path.Combine(Path.GetTempPath(), "l2game") + Path.DirectorySeparatorChar;
        var full = PathGuard.Resolve(root, "l2.ini");
        Assert.Equal(Path.GetFullPath(Path.Combine(root, "l2.ini")), full);
    }

    [Theory]
    [InlineData("../x")]
    [InlineData("a/../../x")]
    [InlineData("/x")]
    [InlineData("con.txt")]
    [InlineData("")]
    public void Resolve_rejects_bad_paths(string path)
    {
        var root = Path.Combine(Path.GetTempPath(), "l2game");
        Assert.Throws<UnsafePathException>(() => PathGuard.Resolve(root, path));
    }

    [Fact]
    public void Resolve_rejects_sibling_folder_with_same_prefix()
    {
        // "l2game" must not accept something that ends up in "l2game-evil"
        var root = Path.Combine(Path.GetTempPath(), "l2game");
        Assert.Throws<UnsafePathException>(() => PathGuard.Resolve(root, "../l2game-evil/x"));
    }

    [Fact]
    public void Resolve_rejects_too_long_full_path()
    {
        var root = Path.Combine(Path.GetTempPath(), "l2game");
        var longName = new string('a', 250) + ".dat";
        Assert.Throws<UnsafePathException>(() => PathGuard.Resolve(root, "system/" + longName));
    }

    [Fact]
    public void Resolve_accepts_path_of_exactly_max_length()
    {
        var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "l2game"));
        var prefix = Path.Combine(root, "x");
        var name = new string('b', PathGuard.MaxFullPath - prefix.Length);
        var full = PathGuard.Resolve(root, "x" + name);
        Assert.Equal(PathGuard.MaxFullPath, full.Length);
    }
}
