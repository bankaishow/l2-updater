using System.Text.Json;

namespace L2Updater.Core;

/// <summary>
/// Remembers SHA-256 of local files so a game folder of many gigabytes is not rehashed on every start
/// (docs/format.md, "Update procedure" step 3). Stored as JSON in &lt;game&gt;/.l2updater/cache.json.
/// A cached hash is valid only while the file has the same size and last-write time.
/// </summary>
public sealed class HashCache
{
    // Windows file names are case-insensitive.
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);

    private sealed record Entry(long Size, long Ticks, string Sha256);

    /// <summary>
    /// Reads the cache file. Missing, empty or broken file → empty cache, never an exception
    /// (the cache only saves time, losing it is harmless).
    /// </summary>
    public static HashCache Load(string cacheFile)
    {
        // У-2а (Gemini). Tests: tests/L2Updater.Core.Tests/HashCacheTests.cs
        var cache = new HashCache();
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllBytes(cacheFile));
            if (doc.RootElement.ValueKind != JsonValueKind.Object ||
                !doc.RootElement.TryGetProperty("entries", out var entries) ||
                entries.ValueKind != JsonValueKind.Array)
                return cache;

            foreach (var item in entries.EnumerateArray())
            {
                // Skip broken entries, keep the good ones.
                if (item.ValueKind != JsonValueKind.Object ||
                    !item.TryGetProperty("path", out var path) || path.ValueKind != JsonValueKind.String ||
                    !item.TryGetProperty("size", out var size) || !size.TryGetInt64(out var sizeValue) ||
                    !item.TryGetProperty("ticks", out var ticks) || !ticks.TryGetInt64(out var ticksValue) ||
                    !item.TryGetProperty("sha256", out var sha) || sha.ValueKind != JsonValueKind.String)
                    continue;
                cache._entries[path.GetString()!] = new Entry(sizeValue, ticksValue, sha.GetString()!);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException
                                      or ArgumentException or NotSupportedException or InvalidOperationException)
        {
            cache._entries.Clear();
        }
        return cache;
    }

    /// <summary>Writes the cache file (System.Text.Json), creating its folder if needed.</summary>
    public void Save(string cacheFile)
    {
        // У-2а (Gemini)
        var folder = Path.GetDirectoryName(Path.GetFullPath(cacheFile));
        if (!string.IsNullOrEmpty(folder))
            Directory.CreateDirectory(folder);

        // Write to a temp file first, so a crash never leaves a half-written cache.
        var tempFile = cacheFile + ".tmp";
        using (var stream = File.Create(tempFile))
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteStartArray("entries");
            foreach (var (path, entry) in _entries)
            {
                writer.WriteStartObject();
                writer.WriteString("path", path);
                writer.WriteNumber("size", entry.Size);
                writer.WriteNumber("ticks", entry.Ticks);
                writer.WriteString("sha256", entry.Sha256);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        File.Move(tempFile, cacheFile, overwrite: true);
    }

    /// <summary>
    /// Cached hash of <paramref name="relativePath"/> (path compared ignoring case),
    /// or null if there is none or size / last-write time differ from what was stored.
    /// </summary>
    public string? Get(string relativePath, long size, DateTime lastWriteUtc)
    {
        // У-2а (Gemini)
        if (_entries.TryGetValue(relativePath, out var entry) &&
            entry.Size == size && entry.Ticks == UtcTicks(lastWriteUtc))
            return entry.Sha256;
        return null;
    }

    /// <summary>Stores (or replaces) the hash of <paramref name="relativePath"/>.</summary>
    public void Set(string relativePath, long size, DateTime lastWriteUtc, string sha256)
    {
        // У-2а (Gemini)
        _entries[relativePath] = new Entry(size, UtcTicks(lastWriteUtc), sha256);
    }

    // Local times are converted; Utc and Unspecified are taken as UTC.
    private static long UtcTicks(DateTime time) =>
        (time.Kind == DateTimeKind.Local ? time.ToUniversalTime() : time).Ticks;
}
