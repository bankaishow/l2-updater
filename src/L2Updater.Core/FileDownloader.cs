namespace L2Updater.Core;

/// <summary>
/// Downloads one file of a file list into the game folder (docs/format.md, "Update procedure" step 4).
/// </summary>
public sealed class FileDownloader(HttpClient http)
{
    // Use this client for all requests (tests pass a fake server here).
    private readonly HttpClient _http = http;

    /// <summary>Folder for partial downloads, inside the game folder.</summary>
    public const string TempFolder = ".l2updater/tmp";

    /// <summary>
    /// Download URL = base URL (a '/' added if missing) + path, each path segment Uri.EscapeDataString'd
    /// ("system/file name#1.dat" → "system/file%20name%231.dat").
    /// </summary>
    public static string UrlOf(string baseUrl, string relativePath)
    {
        // У-2б (Gemini). Tests: tests/L2Updater.Core.Tests/FileDownloaderTests.cs
        throw new NotImplementedException();
    }

    /// <summary>
    /// Rules (each one has a test in FileDownloaderTests.cs):
    /// 1. target = PathGuard.Resolve(gameRoot, entry.Path) first (unsafe path → UnsafePathException, no request);
    ///    partial file = &lt;gameRoot&gt;/.l2updater/tmp/&lt;entry.Sha256&gt;.part.
    /// 2. Mirrors in order; a base URL that is not https is skipped without a request.
    /// 3. If the partial file exists and is shorter than entry.Size: request with header Range: bytes=&lt;its length&gt;-;
    ///    answer 206 → append; answer 200 → start the partial file again from zero.
    ///    Partial file longer than or equal to entry.Size → delete it and download from zero.
    /// 4. Any non-success status, network error (HttpRequestException), more bytes than entry.Size,
    ///    final length ≠ entry.Size or SHA-256 ≠ entry.Sha256 → delete the partial file, try the next mirror.
    /// 5. Success: create the target's folder, File.Move(partial, target, overwrite: true).
    ///    IOException on the move → UpdateException whose message contains "close the game".
    /// 6. All mirrors failed → UpdateException; the existing target file is left exactly as it was.
    /// 7. <paramref name="progress"/> gets the number of bytes of this file downloaded so far
    ///    (including resumed bytes); the last report equals entry.Size.
    /// 8. Cancellation → OperationCanceledException, existing target untouched (the partial file may stay for resume).
    /// </summary>
    public async Task DownloadAsync(FileEntry entry, IReadOnlyList<string> baseUrls, string gameRoot,
        IProgress<long>? progress = null, CancellationToken cancellationToken = default)
    {
        // У-2б (Gemini)
        await Task.CompletedTask;
        throw new NotImplementedException();
    }
}
