using System.Text;

namespace L2Updater.Core;

/// <summary>
/// The whole update for the window (docs/format.md, "Update procedure" steps 1–5).
/// </summary>
public sealed class UpdateSession(HttpClient http, SignatureVerifier verifier)
{
    /// <summary>Biggest manifest, file list or signature accepted.</summary>
    public const int MaxDocumentBytes = 16 * 1024 * 1024;

    // Use these for every request and every signature check.
    private readonly HttpClient _http = http;
    private readonly SignatureVerifier _verifier = verifier;

    /// <summary>
    /// GET <paramref name="manifestUrl"/> and <paramref name="manifestUrl"/> + ".sig" (signature text is trimmed),
    /// then SignedDocuments.LoadManifest(..., lastAcceptedSerial).
    /// URL not https → UpdateException without any request. Non-success status, HttpRequestException
    /// or more than MaxDocumentBytes → UpdateException. Bad signature / old serial → ManifestException (from SignedDocuments).
    /// </summary>
    public async Task<Manifest> LoadManifestAsync(string manifestUrl, long lastAcceptedSerial, CancellationToken cancellationToken = default)
    {
        // У-3в (Gemini). Tests: tests/L2Updater.Core.Tests/UpdateSessionTests.cs
        var manifestBytes = await FetchBytesAsync(manifestUrl, cancellationToken);
        var sigBytes = await FetchBytesAsync(manifestUrl + ".sig", cancellationToken);
        var sigText = Encoding.UTF8.GetString(sigBytes).Trim();
        return SignedDocuments.LoadManifest(manifestBytes, sigText, _verifier, lastAcceptedSerial);
    }

    /// <summary>Same for client.Files (+ ".sig"), then SignedDocuments.LoadFileList(..., client.Id).</summary>
    public async Task<FileList> LoadFileListAsync(ClientInfo client, CancellationToken cancellationToken = default)
    {
        // У-3в (Gemini)
        ArgumentNullException.ThrowIfNull(client);
        var fileListBytes = await FetchBytesAsync(client.Files, cancellationToken);
        var sigBytes = await FetchBytesAsync(client.Files + ".sig", cancellationToken);
        var sigText = Encoding.UTF8.GetString(sigBytes).Trim();
        return SignedDocuments.LoadFileList(fileListBytes, sigText, _verifier, client.Id);
    }

    /// <summary>
    /// 1. cache file = PathGuard.Resolve(gameRoot, ".l2updater/cache.json");
    ///    <paramref name="fullCheck"/> ("check all files" button) → start with an empty cache (new HashCache()), else HashCache.Load.
    /// 2. plan = UpdatePlanner.Plan(list, gameRoot, cache).
    /// 3. Files of plan.Download one by one with FileDownloader(_http) and client.BaseUrls;
    ///    after each file: cache.Set with the new file's size and last-write time.
    ///    progress: UpdateProgress(index 1.., count, path, bytes of finished files + bytes of current file, sum of all sizes).
    /// 4. UpdatePlanner.ApplyDeletes(plan, gameRoot), then cache.Save(cache file).
    /// 5. Return UpdateResult(downloaded, deleted, upToDate, userKept).
    /// An exception from a download stops the update (it is passed on); the cache is still saved.
    /// </summary>
    public async Task<UpdateResult> UpdateAsync(ClientInfo client, FileList list, string gameRoot, bool fullCheck,
        IProgress<UpdateProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        // У-3в (Gemini)
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(gameRoot);

        // 1. cache file
        var cacheFile = PathGuard.Resolve(gameRoot, ".l2updater/cache.json");
        var cache = fullCheck ? new HashCache() : HashCache.Load(cacheFile);

        // 2. plan
        var plan = UpdatePlanner.Plan(list, gameRoot, cache);

        // 3. download
        var totalBytes = plan.Download.Sum(f => f.Size);
        var count = plan.Download.Count;
        var finishedBytes = 0L;

        try
        {
            for (var i = 0; i < count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var entry = plan.Download[i];
                var fileIndex = i + 1;
                var downloader = new FileDownloader(_http);

                var fileProgress = progress != null
                    ? new ActionProgress(bytes =>
                    {
                        progress.Report(new UpdateProgress(fileIndex, count, entry.Path, finishedBytes + bytes, totalBytes));
                    })
                    : null;

                await downloader.DownloadAsync(entry, client.BaseUrls, gameRoot, fileProgress, cancellationToken);

                finishedBytes += entry.Size;

                var fullPath = PathGuard.Resolve(gameRoot, entry.Path);
                var fi = new FileInfo(fullPath);
                cache.Set(entry.Path, fi.Length, fi.LastWriteTimeUtc, entry.Sha256);
            }

            // 4. delete
            UpdatePlanner.ApplyDeletes(plan, gameRoot);
        }
        finally
        {
            cache.Save(cacheFile);
        }

        // 5. result
        return new UpdateResult(plan.Download.Count, plan.Delete.Count, plan.UpToDate, plan.UserKept);
    }

    private async Task<byte[]> FetchBytesAsync(string url, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            throw new UpdateException($"URL is not https: {url}");
        }

        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new UpdateException($"Network error requesting {url}: {ex.Message}");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                throw new UpdateException($"Server returned error {(int)response.StatusCode} for {url}");

            if (response.Content.Headers.ContentLength > MaxDocumentBytes)
                throw new UpdateException($"Document at {url} exceeds maximum size of {MaxDocumentBytes} bytes.");

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var ms = new MemoryStream();
            var buffer = new byte[81920];
            while (true)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(), cancellationToken);
                if (read == 0)
                    break;

                if (ms.Length + read > MaxDocumentBytes)
                    throw new UpdateException($"Document at {url} exceeds maximum size of {MaxDocumentBytes} bytes.");

                ms.Write(buffer, 0, read);
            }

            return ms.ToArray();
        }
    }

    private sealed class ActionProgress(Action<long> action) : IProgress<long>
    {
        public void Report(long value) => action(value);
    }
}
