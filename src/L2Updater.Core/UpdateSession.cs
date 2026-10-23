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
        // У-3а (Gemini). Tests: tests/L2Updater.Core.Tests/UpdateSessionTests.cs
        await Task.CompletedTask;
        throw new NotImplementedException();
    }

    /// <summary>Same for client.Files (+ ".sig"), then SignedDocuments.LoadFileList(..., client.Id).</summary>
    public async Task<FileList> LoadFileListAsync(ClientInfo client, CancellationToken cancellationToken = default)
    {
        // У-3а (Gemini)
        await Task.CompletedTask;
        throw new NotImplementedException();
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
        // У-3а (Gemini)
        await Task.CompletedTask;
        throw new NotImplementedException();
    }
}
