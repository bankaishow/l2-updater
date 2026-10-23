using System.Net;
using System.Net.Http.Headers;

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
        var escapedPath = string.Join('/', relativePath.Split('/').Select(Uri.EscapeDataString));
        var basePrefix = baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
        return basePrefix + escapedPath;
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
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(baseUrls);
        ArgumentNullException.ThrowIfNull(gameRoot);

        // 1. target first (unsafe path throws UnsafePathException before any network request)
        var target = PathGuard.Resolve(gameRoot, entry.Path);
        var partDir = Path.Combine(gameRoot, TempFolder.Replace('/', Path.DirectorySeparatorChar));
        var partFile = Path.Combine(partDir, $"{entry.Sha256}.part");

        var success = false;

        foreach (var baseUrl in baseUrls)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 2. Skip non-https mirror without request
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) ||
                !string.Equals(baseUri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Directory.CreateDirectory(partDir);

            // 3. Partial file checks
            long existingLength = 0;
            if (File.Exists(partFile))
            {
                var fi = new FileInfo(partFile);
                if (fi.Length >= entry.Size)
                {
                    TryDeleteFile(partFile);
                }
                else
                {
                    existingLength = fi.Length;
                }
            }

            var url = UrlOf(baseUrl, entry.Path);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (existingLength > 0)
            {
                request.Headers.Range = new RangeHeaderValue(existingLength, null);
            }

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            }
            catch (HttpRequestException)
            {
                TryDeleteFile(partFile);
                continue;
            }

            using (response)
            {
                var append = false;
                if (existingLength > 0 && response.StatusCode == HttpStatusCode.PartialContent)
                {
                    append = true;
                }
                else if (response.StatusCode == HttpStatusCode.OK)
                {
                    append = false;
                    existingLength = 0;
                }
                else
                {
                    TryDeleteFile(partFile);
                    continue;
                }

                var downloaded = existingLength;
                var mirrorFailed = false;

                try
                {
                    using (var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken))
                    using (var fileStream = new FileStream(partFile, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[81920];
                        while (true)
                        {
                            var read = await responseStream.ReadAsync(buffer.AsMemory(), cancellationToken);
                            if (read == 0)
                                break;

                            downloaded += read;
                            if (downloaded > entry.Size)
                            {
                                mirrorFailed = true;
                                break;
                            }

                            await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                            progress?.Report(downloaded);
                        }
                    }

                    if (entry.Size == 0 && downloaded == 0)
                    {
                        progress?.Report(0);
                    }
                }
                catch (HttpRequestException)
                {
                    mirrorFailed = true;
                }

                if (mirrorFailed || downloaded != entry.Size)
                {
                    TryDeleteFile(partFile);
                    continue;
                }

                // 4. Verify SHA-256
                var hash = UpdatePlanner.Sha256OfFile(partFile);
                if (!string.Equals(hash, entry.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    TryDeleteFile(partFile);
                    continue;
                }

                success = true;
                break;
            }
        }

        if (!success)
        {
            TryDeleteFile(partFile);
            throw new UpdateException($"Failed to download '{entry.Path}' from any mirror.");
        }

        // 5. Success: create target folder and move into place
        var targetDir = Path.GetDirectoryName(target);
        if (!string.IsNullOrEmpty(targetDir))
            Directory.CreateDirectory(targetDir);

        if (File.Exists(target))
        {
            var attr = File.GetAttributes(target);
            if ((attr & FileAttributes.ReadOnly) != 0)
                File.SetAttributes(target, attr & ~FileAttributes.ReadOnly);
        }

        try
        {
            File.Move(partFile, target, overwrite: true);
        }
        catch (IOException ex)
        {
            throw new UpdateException($"Failed to replace '{entry.Path}'. Please close the game and try again: {ex.Message}");
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var attr = File.GetAttributes(path);
                if ((attr & FileAttributes.ReadOnly) != 0)
                    File.SetAttributes(path, attr & ~FileAttributes.ReadOnly);
                File.Delete(path);
            }
        }
        catch
        {
            // Ignore
        }
    }
}
