namespace L2Updater.Core;

/// <summary>
/// Remembers SHA-256 of local files so a game folder of many gigabytes is not rehashed on every start
/// (docs/format.md, "Update procedure" step 3). Stored as JSON in &lt;game&gt;/.l2updater/cache.json.
/// A cached hash is valid only while the file has the same size and last-write time.
/// </summary>
public sealed class HashCache
{
    /// <summary>
    /// Reads the cache file. Missing, empty or broken file → empty cache, never an exception
    /// (the cache only saves time, losing it is harmless).
    /// </summary>
    public static HashCache Load(string cacheFile)
    {
        // У-2а (Gemini). Tests: tests/L2Updater.Core.Tests/HashCacheTests.cs
        throw new NotImplementedException();
    }

    /// <summary>Writes the cache file (System.Text.Json), creating its folder if needed.</summary>
    public void Save(string cacheFile)
    {
        // У-2а (Gemini)
        throw new NotImplementedException();
    }

    /// <summary>
    /// Cached hash of <paramref name="relativePath"/> (path compared ignoring case),
    /// or null if there is none or size / last-write time differ from what was stored.
    /// </summary>
    public string? Get(string relativePath, long size, DateTime lastWriteUtc)
    {
        // У-2а (Gemini)
        throw new NotImplementedException();
    }

    /// <summary>Stores (or replaces) the hash of <paramref name="relativePath"/>.</summary>
    public void Set(string relativePath, long size, DateTime lastWriteUtc, string sha256)
    {
        // У-2а (Gemini)
        throw new NotImplementedException();
    }
}
