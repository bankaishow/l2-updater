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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    /// <summary>Deletes the files of plan.Delete (each through PathGuard.Resolve). A file already gone is fine.</summary>
    public static void ApplyDeletes(UpdatePlan plan, string gameRoot)
    {
        // У-2а (Gemini)
        throw new NotImplementedException();
    }
}
