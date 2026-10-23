using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace L2Updater.Core.Tests;

// Written by Claude. Gemini: do not change this file.

/// <summary>A fresh empty folder, deleted after the test.</summary>
internal sealed class TempDir : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "l2upd-" + Guid.NewGuid().ToString("N"));

    public TempDir() => Directory.CreateDirectory(Path);

    public string File(string relativePath) => System.IO.Path.Combine(Path, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));

    public void Write(string relativePath, byte[] content)
    {
        var full = File(relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        System.IO.File.WriteAllBytes(full, content);
    }

    public void Write(string relativePath, string content) => Write(relativePath, Encoding.UTF8.GetBytes(content));

    public void Dispose()
    {
        try { Directory.Delete(Path, recursive: true); } catch (IOException) { }
    }
}

internal static class Bytes
{
    public static byte[] Of(string text) => Encoding.UTF8.GetBytes(text);

    public static byte[] Random(int length)
    {
        var data = new byte[length];
        new Random(length).NextBytes(data);
        return data;
    }

    public static string Sha256(byte[] data) => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();

    public static FileEntry Entry(string path, byte[] content, bool user = false) => new(path, content.Length, Sha256(content), user);
}

/// <summary>
/// Fake web server: URL → content. Supports "Range: bytes=N-" when SupportsRange is true.
/// Records every request. Unknown URL → 404.
/// </summary>
internal sealed class FakeServer : HttpMessageHandler
{
    private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);
    private readonly HashSet<string> _broken = new(StringComparer.Ordinal);

    public bool SupportsRange { get; set; } = true;

    public List<(string Url, long? RangeFrom)> Requests { get; } = [];

    /// <summary>Called before each answer (for example, to cancel a download in the middle).</summary>
    public Action<HttpRequestMessage>? OnRequest { get; set; }

    public void Add(string url, byte[] content) => _files[url] = content;

    /// <summary>Requests to this URL throw HttpRequestException (network error).</summary>
    public void Break(string url) => _broken.Add(url);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var url = request.RequestUri!.AbsoluteUri;
        var range = request.Headers.Range?.Ranges.FirstOrDefault()?.From;
        Requests.Add((url, range));
        OnRequest?.Invoke(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (_broken.Contains(url))
            throw new HttpRequestException("connection refused (test)");
        if (!_files.TryGetValue(url, out var content))
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        if (range is long from && SupportsRange && from < content.Length)
        {
            var part = new HttpResponseMessage(HttpStatusCode.PartialContent) { Content = new ByteArrayContent(content[(int)from..]) };
            part.Content.Headers.ContentRange = new ContentRangeHeaderValue(from, content.Length - 1, content.Length);
            return Task.FromResult(part);
        }
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(content) });
    }
}

/// <summary>IProgress that records reports synchronously (Progress&lt;T&gt; reports later, on another thread).</summary>
internal sealed class RecordingProgress : IProgress<long>
{
    public List<long> Reports { get; } = [];

    public void Report(long value) => Reports.Add(value);
}
