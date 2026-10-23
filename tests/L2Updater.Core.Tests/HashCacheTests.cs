using L2Updater.Core;
using Xunit;

namespace L2Updater.Core.Tests;

// Written by Claude. Gemini: do not change this file, change the code until all tests pass.
public class HashCacheTests
{
    private static readonly DateTime Time = new(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc);
    private const string Hash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Fact]
    public void Missing_file_gives_empty_cache()
    {
        using var dir = new TempDir();
        var cache = HashCache.Load(dir.File(".l2updater/cache.json"));
        Assert.Null(cache.Get("system/l2.exe", 10, Time));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[1,2,3]")]
    [InlineData("{\"entries\": 42}")]
    public void Broken_file_gives_empty_cache(string content)
    {
        using var dir = new TempDir();
        dir.Write(".l2updater/cache.json", content);
        var cache = HashCache.Load(dir.File(".l2updater/cache.json"));
        Assert.Null(cache.Get("system/l2.exe", 10, Time));
    }

    [Fact]
    public void Set_then_get_returns_hash()
    {
        var cache = HashCache.Load("/nonexistent/cache.json");
        cache.Set("system/l2.exe", 10, Time, Hash);
        Assert.Equal(Hash, cache.Get("system/l2.exe", 10, Time));
    }

    [Fact]
    public void Path_is_compared_ignoring_case()
    {
        var cache = HashCache.Load("/nonexistent/cache.json");
        cache.Set("System/L2.exe", 10, Time, Hash);
        Assert.Equal(Hash, cache.Get("system/l2.EXE", 10, Time));
    }

    [Fact]
    public void Other_size_or_time_gives_null()
    {
        var cache = HashCache.Load("/nonexistent/cache.json");
        cache.Set("system/l2.exe", 10, Time, Hash);
        Assert.Null(cache.Get("system/l2.exe", 11, Time));
        Assert.Null(cache.Get("system/l2.exe", 10, Time.AddSeconds(1)));
        Assert.Null(cache.Get("system/other.exe", 10, Time));
    }

    [Fact]
    public void Set_replaces_old_value()
    {
        var cache = HashCache.Load("/nonexistent/cache.json");
        cache.Set("a.dat", 10, Time, Hash);
        var newHash = new string('f', 64);
        cache.Set("a.dat", 20, Time.AddMinutes(1), newHash);
        Assert.Equal(newHash, cache.Get("a.dat", 20, Time.AddMinutes(1)));
        Assert.Null(cache.Get("a.dat", 10, Time));
    }

    [Fact]
    public void Save_and_load_keep_values_and_create_folder()
    {
        using var dir = new TempDir();
        var file = dir.File(".l2updater/cache.json");
        var cache = HashCache.Load(file);
        cache.Set("system/l2.exe", 10, Time, Hash);
        cache.Set("l2.ini", 5, Time.AddTicks(1), new string('a', 64));
        cache.Save(file);

        Assert.True(File.Exists(file));
        var loaded = HashCache.Load(file);
        Assert.Equal(Hash, loaded.Get("system/l2.exe", 10, Time));
        Assert.Equal(new string('a', 64), loaded.Get("l2.ini", 5, Time.AddTicks(1)));
        Assert.Null(loaded.Get("l2.ini", 5, Time));
    }
}
