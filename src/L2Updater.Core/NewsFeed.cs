namespace L2Updater.Core;

/// <summary>
/// News and server status from the site (docs/format.md, "news and status").
/// NOT signed: untrusted text. Never throws: on any problem returns an empty list.
/// </summary>
public static class NewsFeed
{
    public const int MaxBytes = 256 * 1024;
    public const int MaxItems = 20;

    /// <summary>
    /// null, empty, longer than MaxBytes, not JSON, no "items" array → empty list.
    /// Each item: "title" string required → Clean(title, 200), empty after cleaning → item skipped;
    /// "date" "YYYY-MM-DD" (DateOnly.TryParseExact, invariant) or null; "url" kept only if https.
    /// At most MaxItems items, in file order. Items that are not objects are skipped.
    /// </summary>
    public static IReadOnlyList<NewsItem> ParseNews(byte[]? json)
    {
        // У-3а (Gemini). Tests: tests/L2Updater.Core.Tests/NewsFeedTests.cs
        throw new NotImplementedException();
    }

    /// <summary>
    /// Same rules for "servers": "name" required → Clean(name, 64); "online" bool (anything else → false);
    /// "players" integer ≥ 0 that fits int, else null; "id" string or null (Clean(id, 32)).
    /// </summary>
    public static IReadOnlyList<ServerStatus> ParseStatus(byte[]? json)
    {
        // У-3а (Gemini)
        throw new NotImplementedException();
    }

    /// <summary>
    /// Text safe to show: every control character (char.IsControl) and every format character
    /// (UnicodeCategory.Format, e.g. U+202E right-to-left override) becomes a space,
    /// runs of spaces become one, trimmed, then cut to <paramref name="maxLength"/> characters (and trimmed again).
    /// </summary>
    public static string Clean(string text, int maxLength)
    {
        // У-3а (Gemini)
        throw new NotImplementedException();
    }
}
