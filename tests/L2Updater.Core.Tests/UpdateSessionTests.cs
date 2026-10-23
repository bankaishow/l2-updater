using System.Text;
using System.Text.Json.Nodes;
using L2Updater.Core;
using Xunit;

namespace L2Updater.Core.Tests;

// Written by Claude. Gemini: do not change this file, change the code until all tests pass.
public class UpdateSessionTests : IDisposable
{
    private const string Site = "https://example.com/updater/";
    private const string Cdn = "https://cdn.example.com/mythras/";

    private readonly TestKey _key = new();
    private readonly FakeServer _server = new();

    public void Dispose() => _key.Dispose();

    private UpdateSession Session() => new(new HttpClient(_server), new SignatureVerifier([_key.PublicKey]));

    private void Publish(string url, string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        _server.Add(url, bytes);
        _server.Add(url + ".sig", Encoding.UTF8.GetBytes(_key.Sign(bytes) + "\n"));
    }

    private void PublishManifest(long serial = 42)
    {
        var json = ManifestParserTests.ValidManifest();
        json["key_id"] = _key.KeyId;
        json["serial"] = serial;
        Publish(Site + "manifest.json", json.ToJsonString());
    }

    private ClientInfo Client() =>
        new("mythras", new Dictionary<string, string> { ["en"] = "High Five" }, "system/l2.exe", "",
            Cdn + "files.json", [Cdn], []);

    private FileList PublishFiles(IEnumerable<(string Path, byte[] Content, bool User)> files, params string[] delete)
    {
        var list = new JsonArray();
        foreach (var (path, content, user) in files)
        {
            _server.Add(Cdn + path, content);
            list.Add(new JsonObject { ["path"] = path, ["size"] = content.Length, ["sha256"] = Bytes.Sha256(content), ["user"] = user });
        }
        var json = new JsonObject
        {
            ["format"] = 1, ["key_id"] = _key.KeyId, ["client"] = "mythras", ["version"] = "1",
            ["files"] = list, ["delete"] = new JsonArray(delete.Select(d => (JsonNode)d!).ToArray()),
        };
        Publish(Cdn + "files.json", json.ToJsonString());
        return ManifestParser.ParseFileList(Encoding.UTF8.GetBytes(json.ToJsonString()), "mythras");
    }

    private sealed class Recorder : IProgress<UpdateProgress>
    {
        public List<UpdateProgress> Reports { get; } = [];

        public void Report(UpdateProgress value) => Reports.Add(value);
    }

    // ---------- manifest ----------

    [Fact]
    public async Task Manifest_is_loaded_and_verified()
    {
        PublishManifest();
        var m = await Session().LoadManifestAsync(Site + "manifest.json", 0);
        Assert.Equal(42, m.Serial);
        Assert.Equal([Site + "manifest.json", Site + "manifest.json.sig"], _server.Requests.Select(r => r.Url).Order());
    }

    [Fact]
    public async Task Manifest_with_wrong_signature_is_rejected()
    {
        PublishManifest();
        using var attacker = new TestKey();
        var bytes = Encoding.UTF8.GetBytes("{}");
        _server.Add(Site + "manifest.json.sig", Encoding.UTF8.GetBytes(attacker.Sign(bytes)));
        await Assert.ThrowsAsync<ManifestException>(() => Session().LoadManifestAsync(Site + "manifest.json", 0));
    }

    [Fact]
    public async Task Older_manifest_is_rejected()
    {
        PublishManifest(serial: 41);
        await Assert.ThrowsAsync<ManifestException>(() => Session().LoadManifestAsync(Site + "manifest.json", 42));
    }

    [Fact]
    public async Task Http_manifest_url_is_refused_without_request()
    {
        await Assert.ThrowsAsync<UpdateException>(() => Session().LoadManifestAsync("http://example.com/updater/manifest.json", 0));
        Assert.Empty(_server.Requests);
    }

    [Fact]
    public async Task Missing_manifest_or_signature_gives_update_exception()
    {
        await Assert.ThrowsAsync<UpdateException>(() => Session().LoadManifestAsync(Site + "manifest.json", 0));
        _server.Add(Site + "manifest.json", Encoding.UTF8.GetBytes("{}"));
        await Assert.ThrowsAsync<UpdateException>(() => Session().LoadManifestAsync(Site + "manifest.json", 0));
    }

    [Fact]
    public async Task Network_error_gives_update_exception()
    {
        _server.Break(Site + "manifest.json");
        await Assert.ThrowsAsync<UpdateException>(() => Session().LoadManifestAsync(Site + "manifest.json", 0));
    }

    [Fact]
    public async Task Too_big_manifest_gives_update_exception()
    {
        _server.Add(Site + "manifest.json", new byte[UpdateSession.MaxDocumentBytes + 1]);
        _server.Add(Site + "manifest.json.sig", Encoding.UTF8.GetBytes("AAAA"));
        await Assert.ThrowsAsync<UpdateException>(() => Session().LoadManifestAsync(Site + "manifest.json", 0));
    }

    // ---------- file list ----------

    [Fact]
    public async Task File_list_is_loaded_and_verified()
    {
        PublishFiles([("system/l2.exe", Bytes.Of("exe"), false)]);
        var list = await Session().LoadFileListAsync(Client());
        Assert.Equal("mythras", list.Client);
        Assert.Single(list.Files);
    }

    [Fact]
    public async Task File_list_of_other_client_is_rejected()
    {
        PublishFiles([("system/l2.exe", Bytes.Of("exe"), false)]);
        var other = Client() with { Id = "lucera" };
        await Assert.ThrowsAsync<ManifestException>(() => Session().LoadFileListAsync(other));
    }

    // ---------- update ----------

    [Fact]
    public async Task Update_downloads_keeps_user_files_and_deletes()
    {
        using var dir = new TempDir();
        dir.Write("system/l2.ini", "my settings");
        dir.Write("system/old_patch.dat", "old");
        dir.Write("system/same.dat", "same");
        var list = PublishFiles([
            ("system/l2.exe", Bytes.Random(3000), false),
            ("system/l2.ini", Bytes.Of("default settings"), true),
            ("system/same.dat", Bytes.Of("same"), false),
            ("maps/new.unr", Bytes.Random(1000), false),
        ], "system/old_patch.dat");

        var result = await Session().UpdateAsync(Client(), list, dir.Path, fullCheck: false);

        Assert.Equal(new UpdateResult(2, 1, 1, 1), result);
        Assert.True(File.Exists(dir.File("system/l2.exe")));
        Assert.True(File.Exists(dir.File("maps/new.unr")));
        Assert.Equal("my settings", File.ReadAllText(dir.File("system/l2.ini")));
        Assert.False(File.Exists(dir.File("system/old_patch.dat")));
        Assert.True(File.Exists(dir.File(".l2updater/cache.json")));
    }

    [Fact]
    public async Task Second_update_downloads_nothing()
    {
        using var dir = new TempDir();
        var list = PublishFiles([("system/l2.exe", Bytes.Random(3000), false), ("maps/a.unr", Bytes.Random(500), false)]);
        await Session().UpdateAsync(Client(), list, dir.Path, fullCheck: false);
        _server.Requests.Clear();

        var result = await Session().UpdateAsync(Client(), list, dir.Path, fullCheck: false);
        Assert.Equal(new UpdateResult(0, 0, 2, 0), result);
        Assert.Empty(_server.Requests);
    }

    [Fact]
    public async Task Full_check_finds_file_damaged_behind_the_cache()
    {
        // same size, same time, other content: the normal check trusts the cache, "check all files" does not
        using var dir = new TempDir();
        var content = Bytes.Random(3000);
        var list = PublishFiles([("system/l2.exe", content, false)]);
        await Session().UpdateAsync(Client(), list, dir.Path, fullCheck: false);

        var file = dir.File("system/l2.exe");
        var time = File.GetLastWriteTimeUtc(file);
        var damaged = (byte[])content.Clone();
        damaged[100] ^= 0xFF;
        File.WriteAllBytes(file, damaged);
        File.SetLastWriteTimeUtc(file, time);

        var normal = await Session().UpdateAsync(Client(), list, dir.Path, fullCheck: false);
        Assert.Equal(0, normal.Downloaded);

        var full = await Session().UpdateAsync(Client(), list, dir.Path, fullCheck: true);
        Assert.Equal(1, full.Downloaded);
        Assert.Equal(content, File.ReadAllBytes(file));
    }

    [Fact]
    public async Task Progress_covers_all_files()
    {
        using var dir = new TempDir();
        var list = PublishFiles([("a.dat", Bytes.Random(3000), false), ("b.dat", Bytes.Random(2000), false)]);
        var recorder = new Recorder();
        await Session().UpdateAsync(Client(), list, dir.Path, fullCheck: false, recorder);

        Assert.NotEmpty(recorder.Reports);
        Assert.All(recorder.Reports, r => Assert.Equal(5000, r.BytesTotal));
        Assert.All(recorder.Reports, r => Assert.Equal(2, r.FileCount));
        Assert.Equal(new UpdateProgress(2, 2, "b.dat", 5000, 5000), recorder.Reports[^1]);
        Assert.Contains(recorder.Reports, r => r.FileIndex == 1 && r.CurrentPath == "a.dat");
        Assert.True(recorder.Reports.Select(r => r.BytesDone).SequenceEqual(recorder.Reports.Select(r => r.BytesDone).Order()),
            "overall progress must not go back");
    }

    [Fact]
    public async Task Failed_download_stops_update_and_keeps_cache()
    {
        using var dir = new TempDir();
        var ok = Bytes.Random(1000);
        dir.Write("ok.dat", ok);
        var list = PublishFiles([("ok.dat", ok, false), ("broken.dat", Bytes.Random(1000), false)]);
        _server.Add(Cdn + "broken.dat", Bytes.Of("wrong content"));

        await Assert.ThrowsAsync<UpdateException>(() => Session().UpdateAsync(Client(), list, dir.Path, fullCheck: false));

        var cache = HashCache.Load(dir.File(".l2updater/cache.json"));
        var info = new FileInfo(dir.File("ok.dat"));
        Assert.Equal(Bytes.Sha256(ok), cache.Get("ok.dat", info.Length, info.LastWriteTimeUtc));
    }
}
