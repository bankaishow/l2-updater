using System.Security.Cryptography;

namespace L2Updater.Core;

/// <summary>
/// Compares a signed file list with the game folder (docs/format.md, "Update procedure" steps 3 and 5).
/// Every path goes through PathGuard.Resolve before the disk is touched.
/// </summary>
public static class UpdatePlanner
{
    /// <summary>Lowercase hex SHA-256 of a file (System.Security.Cryptography.SHA256, read as a stream).</summary>
    public static string Sha256OfFile(string fullPath)
    {
        // У-2а (Gemini). Tests: tests/L2Updater.Core.Tests/UpdatePlannerTests.cs
        using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 1 << 20, FileOptions.SequentialScan);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    /// <summary>
    /// For each file of <paramref name="list"/>, in order:
    /// - file does not exist → Download;
    /// - file exists and entry.User → UserKept (never downloaded, never hashed);
    /// - size differs → Download (no hashing);
    /// - otherwise hash: cache.Get(path, size, last-write UTC) or Sha256OfFile + cache.Set;
    ///   equal to entry.Sha256 → UpToDate, else Download.
    /// For each path of list.Delete: goes to Delete only if a FILE exists there (folders are never deleted).
    /// </summary>
    public static UpdatePlan Plan(FileList list, string gameRoot, HashCache cache)
    {
        // У-2а (Gemini)
        var download = new List<FileEntry>();
        var upToDate = 0;
        var userKept = 0;

        foreach (var entry in list.Files)
        {
            var info = new FileInfo(PathGuard.Resolve(gameRoot, entry.Path));
            if (!info.Exists)
            {
                download.Add(entry);
                continue;
            }
            if (entry.User)
            {
                userKept++;
                continue;
            }
            if (info.Length != entry.Size)
            {
                download.Add(entry);
                continue;
            }

            var lastWrite = info.LastWriteTimeUtc;
            var hash = cache.Get(entry.Path, info.Length, lastWrite);
            if (hash is null)
            {
                hash = Sha256OfFile(info.FullName);
                cache.Set(entry.Path, info.Length, lastWrite, hash);
            }

            if (string.Equals(hash, entry.Sha256, StringComparison.Ordinal))
                upToDate++;
            else
                download.Add(entry);
        }

        var delete = new List<string>();
        foreach (var path in list.Delete)
        {
            // File.Exists is false for folders, so folders are never deleted.
            if (File.Exists(PathGuard.Resolve(gameRoot, path)))
                delete.Add(path);
        }

        return new UpdatePlan(download, delete, upToDate, userKept);
    }

    /// <summary>Deletes the files of plan.Delete (each through PathGuard.Resolve). A file already gone is fine.</summary>
    public static void ApplyDeletes(UpdatePlan plan, string gameRoot)
    {
        // У-2а (Gemini)
        // Check every path first: one bad path stops everything before anything is deleted.
        var fullPaths = plan.Delete.Select(path => PathGuard.Resolve(gameRoot, path)).ToList();
        foreach (var fullPath in fullPaths)
        {
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
    }
}
