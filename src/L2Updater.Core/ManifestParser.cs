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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }
}
