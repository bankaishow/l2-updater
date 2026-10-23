using L2Updater.Core;
using Xunit;

namespace L2Updater.Core.Tests;

// Written by Claude. Gemini: do not change this file, change the code until all tests pass.
public class UpdatePlannerTests
{
    private static FileList List(IEnumerable<FileEntry> files, params string[] delete) =>
        new(1, "0123456789abcdef", "mythras", "1", files.ToList(), delete);

    private static HashCache NoCache() => HashCache.Load("/nonexistent/cache.json");

    [Fact]
    public void Sha256OfFile_is_lowercase_hex()
    {
        using var dir = new TempDir();
        dir.Write("a.txt", "abc");
        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", UpdatePlanner.Sha256OfFile(dir.File("a.txt")));
    }

    [Fact]
    public void Missing_file_is_downloaded()
    {
        using var dir = new TempDir();
        var entry = Bytes.Entry("system/l2.exe", Bytes.Of("new"));
        var plan = UpdatePlanner.Plan(List([entry]), dir.Path, NoCache());
        Assert.Equal([entry], plan.Download);
        Assert.Equal(0, plan.UpToDate);
    }

    [Fact]
    public void Same_file_is_up_to_date()
    {
        using var dir = new TempDir();
        var content = Bytes.Of("same content");
        dir.Write("system/l2.exe", content);
        var plan = UpdatePlanner.Plan(List([Bytes.Entry("system/l2.exe", content)]), dir.Path, NoCache());
        Assert.Empty(plan.Download);
        Assert.Equal(1, plan.UpToDate);
    }

    [Fact]
    public void Same_size_other_content_is_downloaded()
    {
        using var dir = new TempDir();
        dir.Write("system/l2.exe", "aaaa");
        var entry = Bytes.Entry("system/l2.exe", Bytes.Of("bbbb"));
        var plan = UpdatePlanner.Plan(List([entry]), dir.Path, NoCache());
        Assert.Equal([entry], plan.Download);
    }

    [Fact]
    public void Other_size_is_downloaded_without_hashing()
    {
        using var dir = new TempDir();
        dir.Write("system/l2.exe", "short");
        var entry = Bytes.Entry("system/l2.exe", Bytes.Of("much longer content"));
        var cache = NoCache();
        var plan = UpdatePlanner.Plan(List([entry]), dir.Path, cache);
        Assert.Equal([entry], plan.Download);
        var info = new FileInfo(dir.File("system/l2.exe"));
        Assert.Null(cache.Get("system/l2.exe", info.Length, info.LastWriteTimeUtc));
    }

    [Fact]
    public void Existing_user_file_is_kept_even_if_different()
    {
        using var dir = new TempDir();
        dir.Write("system/l2.ini", "my own settings");
        var entry = Bytes.Entry("system/l2.ini", Bytes.Of("default settings"), user: true);
        var plan = UpdatePlanner.Plan(List([entry]), dir.Path, NoCache());
        Assert.Empty(plan.Download);
        Assert.Equal(1, plan.UserKept);
    }

    [Fact]
    public void Missing_user_file_is_downloaded()
    {
        using var dir = new TempDir();
        var entry = Bytes.Entry("system/l2.ini", Bytes.Of("default settings"), user: true);
        var plan = UpdatePlanner.Plan(List([entry]), dir.Path, NoCache());
        Assert.Equal([entry], plan.Download);
        Assert.Equal(0, plan.UserKept);
    }

    [Fact]
    public void Hash_is_stored_in_cache()
    {
        using var dir = new TempDir();
        var content = Bytes.Of("content");
        dir.Write("a.dat", content);
        var cache = NoCache();
        UpdatePlanner.Plan(List([Bytes.Entry("a.dat", content)]), dir.Path, cache);
        var info = new FileInfo(dir.File("a.dat"));
        Assert.Equal(Bytes.Sha256(content), cache.Get("a.dat", info.Length, info.LastWriteTimeUtc));
    }

    [Fact]
    public void Cached_hash_is_used_instead_of_reading_the_file()
    {
        // the cache claims the file is correct: the planner must trust it while size and time match
        using var dir = new TempDir();
        dir.Write("a.dat", "local");
        var entry = Bytes.Entry("a.dat", Bytes.Of("other"));
        var info = new FileInfo(dir.File("a.dat"));
        var cache = NoCache();
        cache.Set("a.dat", info.Length, info.LastWriteTimeUtc, entry.Sha256);
        var plan = UpdatePlanner.Plan(List([entry]), dir.Path, cache);
        Assert.Empty(plan.Download);
        Assert.Equal(1, plan.UpToDate);
    }

    [Fact]
    public void Changed_file_time_invalidates_cache()
    {
        using var dir = new TempDir();
        dir.Write("a.dat", "local");
        var entry = Bytes.Entry("a.dat", Bytes.Of("other"));
        var info = new FileInfo(dir.File("a.dat"));
        var cache = NoCache();
        cache.Set("a.dat", info.Length, info.LastWriteTimeUtc, entry.Sha256);
        File.SetLastWriteTimeUtc(dir.File("a.dat"), info.LastWriteTimeUtc.AddMinutes(5));
        var plan = UpdatePlanner.Plan(List([entry]), dir.Path, cache);
        Assert.Equal([entry], plan.Download);
    }

    [Fact]
    public void Several_files_are_sorted_into_the_plan()
    {
        using var dir = new TempDir();
        var same = Bytes.Of("same");
        dir.Write("ok.dat", same);
        dir.Write("old.dat", "old!");
        dir.Write("user.ini", "mine");
        var files = new[]
        {
            Bytes.Entry("ok.dat", same),
            Bytes.Entry("old.dat", Bytes.Of("new!")),
            Bytes.Entry("user.ini", Bytes.Of("default"), user: true),
            Bytes.Entry("missing.dat", Bytes.Of("x")),
        };
        var plan = UpdatePlanner.Plan(List(files), dir.Path, NoCache());
        Assert.Equal(["old.dat", "missing.dat"], plan.Download.Select(f => f.Path));
        Assert.Equal(1, plan.UpToDate);
        Assert.Equal(1, plan.UserKept);
    }

    [Fact]
    public void Delete_lists_only_existing_files()
    {
        using var dir = new TempDir();
        dir.Write("system/old_patch.dat", "old");
        Directory.CreateDirectory(dir.File("system/old_folder"));
        var plan = UpdatePlanner.Plan(List([], "system/old_patch.dat", "system/never_existed.dat", "system/old_folder"), dir.Path, NoCache());
        Assert.Equal(["system/old_patch.dat"], plan.Delete);
    }

    [Fact]
    public void ApplyDeletes_removes_files_and_keeps_folders()
    {
        using var dir = new TempDir();
        dir.Write("system/old_patch.dat", "old");
        dir.Write("system/keep.dat", "keep");
        var plan = new UpdatePlan([], ["system/old_patch.dat", "system/already_gone.dat"], 0, 0);
        UpdatePlanner.ApplyDeletes(plan, dir.Path);
        Assert.False(File.Exists(dir.File("system/old_patch.dat")));
        Assert.True(File.Exists(dir.File("system/keep.dat")));
        Assert.True(Directory.Exists(dir.File("system")));
    }

    [Fact]
    public void ApplyDeletes_refuses_paths_outside_game_folder()
    {
        using var outside = new TempDir();
        outside.Write("victim.txt", "do not delete");
        using var dir = new TempDir();
        var escape = "../" + Path.GetFileName(outside.Path) + "/victim.txt";
        var plan = new UpdatePlan([], [escape], 0, 0);
        Assert.Throws<UnsafePathException>(() => UpdatePlanner.ApplyDeletes(plan, dir.Path));
        Assert.True(File.Exists(outside.File("victim.txt")));
    }

    [Fact]
    public void Plan_refuses_unsafe_paths()
    {
        using var dir = new TempDir();
        var bad = new FileEntry("../evil.dll", 1, new string('0', 64), false);
        Assert.Throws<UnsafePathException>(() => UpdatePlanner.Plan(List([bad]), dir.Path, NoCache()));
    }
}
