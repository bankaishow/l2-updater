using System.Text.Json;

namespace L2Updater.Core;

/// <summary>
/// Reads manifest.json and files.json (docs/format.md) with System.Text.Json (JsonDocument).
/// Any problem: throw ManifestException with a short English message. Unknown fields are ignored.
/// Does NOT check signatures (that is SignedDocuments).
/// </summary>
public static class ManifestParser
{
    /// <summary>
    /// Rules (each one has a test in ManifestParserTests.cs):
    /// - not valid JSON or not an object → error;
    /// - required: format (must be 1), key_id (16 lowercase hex), serial (number ≥ 0),
    ///   issued_at (ISO 8601 date), clients (array, at least one);
    /// - client required: id ([a-z0-9_]{1,32}, unique in the manifest), title (object of strings, has "en"),
    ///   exe (PathGuard.IsValidRelativePath), files (https URL), base_urls (array of https URLs, at least one);
    ///   optional: args (default ""), servers (default empty);
    /// - updater optional; if present, required version, url (https), sha256 (64 lowercase hex), size (number &gt; 0);
    /// - links optional, each of site/forum/cabinet optional, if present https;
    /// - news_url, status_url optional, if present https;
    /// - "https URL" = Uri.TryCreate(..., UriKind.Absolute) and Scheme == "https".
    /// </summary>
    public static Manifest ParseManifest(byte[] json)
    {
        // У-1в (Gemini). Tests: tests/L2Updater.Core.Tests/ManifestParserTests.cs
        using var doc = ParseObject(json);
        var root = doc.RootElement;

        var format = ReadFormat(root);
        var keyId = ReadKeyId(root);
        var serial = ReadLong(root, "serial", min: 0);

        var issuedAtElement = Required(root, "issued_at");
        if (issuedAtElement.ValueKind != JsonValueKind.String || !issuedAtElement.TryGetDateTimeOffset(out var issuedAt))
            throw Fail("issued_at must be an ISO 8601 date");

        UpdaterInfo? updater = null;
        if (Optional(root, "updater", out var updaterElement))
            updater = ReadUpdater(updaterElement);

        var clientsElement = Required(root, "clients");
        if (clientsElement.ValueKind != JsonValueKind.Array || clientsElement.GetArrayLength() == 0)
            throw Fail("clients must be a non-empty array");
        var clients = new List<ClientInfo>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var clientElement in clientsElement.EnumerateArray())
        {
            var client = ReadClient(clientElement);
            if (!ids.Add(client.Id))
                throw Fail($"duplicate client id: {client.Id}");
            clients.Add(client);
        }

        Links? links = null;
        if (Optional(root, "links", out var linksElement))
        {
            if (linksElement.ValueKind != JsonValueKind.Object)
                throw Fail("links must be an object");
            links = new Links(
                OptionalHttps(linksElement, "site"),
                OptionalHttps(linksElement, "forum"),
                OptionalHttps(linksElement, "cabinet"));
        }

        var newsUrl = OptionalHttps(root, "news_url");
        var statusUrl = OptionalHttps(root, "status_url");

        return new Manifest(format, keyId, serial, issuedAt, updater, clients, links, newsUrl, statusUrl);
    }

    private static UpdaterInfo ReadUpdater(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw Fail("updater must be an object");
        var version = ReadString(element, "version");
        if (version.Length == 0)
            throw Fail("updater.version must not be empty");
        var url = ReadHttps(element, "url");
        var sha256 = ReadSha256(element);
        var size = ReadLong(element, "size", min: 1);
        return new UpdaterInfo(version, url, sha256, size);
    }

    private static ClientInfo ReadClient(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw Fail("client must be an object");

        var id = ReadString(element, "id");
        if (!IsClientId(id))
            throw Fail("client id must match [a-z0-9_]{1,32}");

        var titleElement = Required(element, "title");
        if (titleElement.ValueKind != JsonValueKind.Object)
            throw Fail("client title must be an object");
        var title = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in titleElement.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.String)
                throw Fail("client title values must be strings");
            title[property.Name] = property.Value.GetString()!;
        }
        if (!title.ContainsKey("en"))
            throw Fail("client title must have \"en\"");

        var exe = ReadString(element, "exe");
        if (!PathGuard.IsValidRelativePath(exe))
            throw Fail("client exe is not a valid relative path");

        var args = "";
        if (Optional(element, "args", out var argsElement))
        {
            if (argsElement.ValueKind != JsonValueKind.String)
                throw Fail("client args must be a string");
            args = argsElement.GetString()!;
        }

        var files = ReadHttps(element, "files");

        var baseUrlsElement = Required(element, "base_urls");
        if (baseUrlsElement.ValueKind != JsonValueKind.Array || baseUrlsElement.GetArrayLength() == 0)
            throw Fail("client base_urls must be a non-empty array");
        var baseUrls = new List<string>();
        foreach (var item in baseUrlsElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || !IsHttps(item.GetString()!))
                throw Fail("client base_urls must be https URLs");
            baseUrls.Add(item.GetString()!);
        }

        var servers = new List<string>();
        if (Optional(element, "servers", out var serversElement))
        {
            if (serversElement.ValueKind != JsonValueKind.Array)
                throw Fail("client servers must be an array");
            foreach (var item in serversElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                    throw Fail("client servers must be strings");
                servers.Add(item.GetString()!);
            }
        }

        return new ClientInfo(id, title, exe, args, files, baseUrls, servers);
    }

    /// <summary>
    /// Rules (each one has a test in ManifestParserTests.cs):
    /// - not valid JSON or not an object → error;
    /// - required: format (must be 1), key_id (16 lowercase hex), client (must equal expectedClient),
    ///   version (non-empty string), files (array, may be empty);
    /// - file required: path (PathGuard.IsValidRelativePath), size (number ≥ 0), sha256 (64 lowercase hex);
    ///   optional: user (bool, default false);
    /// - the same path twice (ignoring case, Windows) → error;
    /// - delete optional (default empty), each a valid path; a path both in files and in delete → error.
    /// </summary>
    public static FileList ParseFileList(byte[] json, string expectedClient)
    {
        // У-1в (Gemini). Tests: tests/L2Updater.Core.Tests/ManifestParserTests.cs
        using var doc = ParseObject(json);
        var root = doc.RootElement;

        var format = ReadFormat(root);
        var keyId = ReadKeyId(root);

        var client = ReadString(root, "client");
        if (!string.Equals(client, expectedClient, StringComparison.Ordinal))
            throw Fail("file list is for another client");

        var version = ReadString(root, "version");
        if (version.Length == 0)
            throw Fail("version must not be empty");

        var filesElement = Required(root, "files");
        if (filesElement.ValueKind != JsonValueKind.Array)
            throw Fail("files must be an array");
        var files = new List<FileEntry>();
        // Windows file names are case-insensitive.
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in filesElement.EnumerateArray())
        {
            var entry = ReadFileEntry(item);
            if (!paths.Add(entry.Path))
                throw Fail($"duplicate path: {entry.Path}");
            files.Add(entry);
        }

        var delete = new List<string>();
        if (Optional(root, "delete", out var deleteElement))
        {
            if (deleteElement.ValueKind != JsonValueKind.Array)
                throw Fail("delete must be an array");
            foreach (var item in deleteElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                    throw Fail("delete entries must be strings");
                var path = item.GetString()!;
                if (!PathGuard.IsValidRelativePath(path))
                    throw Fail($"invalid delete path: {path}");
                if (paths.Contains(path))
                    throw Fail($"path is both in files and delete: {path}");
                delete.Add(path);
            }
        }

        return new FileList(format, keyId, client, version, files, delete);
    }

    private static FileEntry ReadFileEntry(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw Fail("file entry must be an object");

        var path = ReadString(element, "path");
        if (!PathGuard.IsValidRelativePath(path))
            throw Fail($"invalid file path: {path}");
        var size = ReadLong(element, "size", min: 0);
        var sha256 = ReadSha256(element);

        var user = false;
        if (Optional(element, "user", out var userElement))
        {
            if (userElement.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                throw Fail("user must be true or false");
            user = userElement.GetBoolean();
        }

        return new FileEntry(path, size, sha256, user);
    }

    // ---------- helpers ----------

    private static ManifestException Fail(string message) => new(message);

    // Parses the bytes and checks that the root is a JSON object.
    private static JsonDocument ParseObject(byte[] json)
    {
        if (json is null)
            throw Fail("document is empty");
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            throw Fail("document is not valid JSON");
        }
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
        {
            doc.Dispose();
            throw Fail("document is not a JSON object");
        }
        return doc;
    }

    private static JsonElement Required(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var value))
            throw Fail($"missing field: {name}");
        return value;
    }

    // False if the field is absent; explicit null is not allowed.
    private static bool Optional(JsonElement obj, string name, out JsonElement value)
    {
        if (!obj.TryGetProperty(name, out value))
            return false;
        if (value.ValueKind == JsonValueKind.Null)
            throw Fail($"field must not be null: {name}");
        return true;
    }

    private static string ReadString(JsonElement obj, string name)
    {
        var value = Required(obj, name);
        if (value.ValueKind != JsonValueKind.String)
            throw Fail($"field must be a string: {name}");
        return value.GetString()!;
    }

    private static long ReadLong(JsonElement obj, string name, long min)
    {
        var value = Required(obj, name);
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out var number))
            throw Fail($"field must be an integer: {name}");
        if (number < min)
            throw Fail($"field is out of range: {name}");
        return number;
    }

    private static int ReadFormat(JsonElement obj)
    {
        var value = Required(obj, "format");
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var format) || format != 1)
            throw Fail("format must be 1");
        return format;
    }

    private static string ReadKeyId(JsonElement obj)
    {
        var keyId = ReadString(obj, "key_id");
        if (!IsLowerHex(keyId, 16))
            throw Fail("key_id must be 16 lowercase hex characters");
        return keyId;
    }

    private static string ReadSha256(JsonElement obj)
    {
        var sha256 = ReadString(obj, "sha256");
        if (!IsLowerHex(sha256, 64))
            throw Fail("sha256 must be 64 lowercase hex characters");
        return sha256;
    }

    private static string ReadHttps(JsonElement obj, string name)
    {
        var url = ReadString(obj, name);
        if (!IsHttps(url))
            throw Fail($"field must be an https URL: {name}");
        return url;
    }

    private static string? OptionalHttps(JsonElement obj, string name)
    {
        if (!Optional(obj, name, out var value))
            return null;
        if (value.ValueKind != JsonValueKind.String || !IsHttps(value.GetString()!))
            throw Fail($"field must be an https URL: {name}");
        return value.GetString();
    }

    private static bool IsHttps(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == "https";

    private static bool IsLowerHex(string value, int length)
    {
        if (value.Length != length)
            return false;
        foreach (var c in value)
        {
            if (!(c is >= '0' and <= '9' or >= 'a' and <= 'f'))
                return false;
        }
        return true;
    }

    private static bool IsClientId(string id)
    {
        if (id.Length is < 1 or > 32)
            return false;
        foreach (var c in id)
        {
            if (!(c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_'))
                return false;
        }
        return true;
    }
}
