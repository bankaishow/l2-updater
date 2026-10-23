using System.Globalization;
using System.Text;
using System.Text.Json;

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
        // У-3б (Gemini). Tests: tests/L2Updater.Core.Tests/NewsFeedTests.cs
        if (json == null || json.Length == 0 || json.Length > MaxBytes)
            return [];

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object ||
                !doc.RootElement.TryGetProperty("items", out var itemsElement) ||
                itemsElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var result = new List<NewsItem>();
            foreach (var item in itemsElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;

                if (!item.TryGetProperty("title", out var titleProp) || titleProp.ValueKind != JsonValueKind.String)
                    continue;

                var title = Clean(titleProp.GetString()!, 200);
                if (title.Length == 0)
                    continue;

                DateOnly? date = null;
                if (item.TryGetProperty("date", out var dateProp) && dateProp.ValueKind == JsonValueKind.String)
                {
                    if (DateOnly.TryParseExact(dateProp.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                        date = parsedDate;
                }

                string? url = null;
                if (item.TryGetProperty("url", out var urlProp) && urlProp.ValueKind == JsonValueKind.String)
                {
                    var u = urlProp.GetString();
                    if (!string.IsNullOrEmpty(u) && Uri.TryCreate(u, UriKind.Absolute, out var uri) &&
                        string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
                    {
                        url = u;
                    }
                }

                result.Add(new NewsItem(date, title, url));
                if (result.Count >= MaxItems)
                    break;
            }

            return result;
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Same rules for "servers": "name" required → Clean(name, 64); "online" bool (anything else → false);
    /// "players" integer ≥ 0 that fits int, else null; "id" string or null (Clean(id, 32)).
    /// </summary>
    public static IReadOnlyList<ServerStatus> ParseStatus(byte[]? json)
    {
        // У-3б (Gemini)
        if (json == null || json.Length == 0 || json.Length > MaxBytes)
            return [];

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object ||
                !doc.RootElement.TryGetProperty("servers", out var serversElement) ||
                serversElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var result = new List<ServerStatus>();
            foreach (var item in serversElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;

                if (!item.TryGetProperty("name", out var nameProp) || nameProp.ValueKind != JsonValueKind.String)
                    continue;

                var name = Clean(nameProp.GetString()!, 64);
                if (name.Length == 0)
                    continue;

                var online = false;
                if (item.TryGetProperty("online", out var onlineProp) &&
                    (onlineProp.ValueKind == JsonValueKind.True || onlineProp.ValueKind == JsonValueKind.False))
                {
                    online = onlineProp.GetBoolean();
                }

                int? players = null;
                if (item.TryGetProperty("players", out var playersProp) && playersProp.ValueKind == JsonValueKind.Number)
                {
                    if (playersProp.TryGetInt32(out var p) && p >= 0)
                        players = p;
                }

                string? id = null;
                if (item.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String)
                {
                    var cleanedId = Clean(idProp.GetString()!, 32);
                    if (cleanedId.Length > 0)
                        id = cleanedId;
                }

                result.Add(new ServerStatus(id, name, online, players));
                if (result.Count >= MaxItems)
                    break;
            }

            return result;
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Text safe to show: every control character (char.IsControl) and every format character
    /// (UnicodeCategory.Format, e.g. U+202E right-to-left override) becomes a space,
    /// runs of spaces become one, trimmed, then cut to <paramref name="maxLength"/> characters (and trimmed again).
    /// </summary>
    public static string Clean(string text, int maxLength)
    {
        // У-3б (Gemini)
        if (string.IsNullOrEmpty(text) || maxLength <= 0)
            return "";

        var sb = new StringBuilder(text.Length);
        var lastWasSpace = true;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            var isSpaceOrFormat = char.IsWhiteSpace(c) || char.IsControl(c) ||
                                  char.GetUnicodeCategory(c) == UnicodeCategory.Format;

            if (isSpaceOrFormat)
            {
                if (!lastWasSpace)
                {
                    sb.Append(' ');
                    lastWasSpace = true;
                }
            }
            else
            {
                sb.Append(c);
                lastWasSpace = false;
            }
        }

        while (sb.Length > 0 && sb[^1] == ' ')
            sb.Length--;

        if (sb.Length > maxLength)
        {
            sb.Length = maxLength;
            while (sb.Length > 0 && sb[^1] == ' ')
                sb.Length--;
        }

        return sb.ToString();
    }
}
