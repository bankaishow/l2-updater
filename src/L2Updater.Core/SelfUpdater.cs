using System.Security.Cryptography;

namespace L2Updater.Core;

/// <summary>
/// Replaces the running L2Updater.exe with the version from manifest.updater (docs/format.md).
/// The manifest is already verified by its signature; the file itself is checked by size and SHA-256.
/// </summary>
public sealed class SelfUpdater(HttpClient http)
{
    private readonly HttpClient _http = http;

    /// <summary>Biggest updater download accepted, whatever the manifest says.</summary>
    public const long MaxBytes = 300L * 1024 * 1024;

    public const string NewSuffix = ".new";
    public const string OldSuffix = ".old";

    /// <summary>True if <paramref name="offered"/> is a valid version higher than <paramref name="current"/>.</summary>
    public static bool IsNewer(string? current, string? offered) =>
        Version.TryParse(current, out var cur) && Version.TryParse(offered, out var off) && off > cur;

    /// <summary>
    /// Downloads info.Url to exePath + ".new". Not https, size ≤ 0 or above MaxBytes, bad status, network error,
    /// more or fewer bytes than info.Size or SHA-256 ≠ info.Sha256 → UpdateException and the .new file is deleted.
    /// Returns the path of the .new file.
    /// </summary>
    public async Task<string> DownloadAsync(UpdaterInfo info, string exePath,
        IProgress<long>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(info);
        ArgumentNullException.ThrowIfNull(exePath);

        if (!Uri.TryCreate(info.Url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new UpdateException("updater address is not https");
        if (info.Size <= 0 || info.Size > MaxBytes)
            throw new UpdateException("updater size is not acceptable");

        var target = exePath + NewSuffix;
        try
        {
            using var response = await _http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new UpdateException($"updater download failed: HTTP {(int)response.StatusCode}");

            using var sha = SHA256.Create();
            long total = 0;
            await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var output = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[81920];
                int read;
                while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    total += read;
                    if (total > info.Size)
                        throw new UpdateException("updater file is bigger than announced");
                    sha.TransformBlock(buffer, 0, read, null, 0);
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    progress?.Report(total);
                }
            }

            if (total != info.Size)
                throw new UpdateException("updater file is smaller than announced");
            sha.TransformFinalBlock([], 0, 0);
            var hash = Convert.ToHexString(sha.Hash!).ToLowerInvariant();
            if (!string.Equals(hash, info.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new UpdateException("updater file checksum does not match");
            return target;
        }
        catch (HttpRequestException ex)
        {
            DeleteQuietly(target);
            throw new UpdateException("updater download failed: " + ex.Message);
        }
        catch
        {
            DeleteQuietly(target);
            throw;
        }
    }

    /// <summary>
    /// exePath → exePath + ".old", newFile → exePath. If the second move fails the first is undone
    /// (the old exe is back in place) and UpdateException is thrown.
    /// </summary>
    public static void Replace(string exePath, string newFile)
    {
        var old = exePath + OldSuffix;
        try
        {
            File.Move(exePath, old, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new UpdateException("cannot replace the updater: " + ex.Message);
        }

        try
        {
            File.Move(newFile, exePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            File.Move(old, exePath, overwrite: true);
            throw new UpdateException("cannot replace the updater: " + ex.Message);
        }
    }

    /// <summary>Removes the leftovers of an earlier update (.old, .new). Never throws.</summary>
    public static void CleanUp(string exePath)
    {
        DeleteQuietly(exePath + OldSuffix);
        DeleteQuietly(exePath + NewSuffix);
    }

    private static void DeleteQuietly(string path)
    {
        try { File.Delete(path); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
}
