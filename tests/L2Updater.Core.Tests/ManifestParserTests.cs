using System.Text;
using System.Text.Json.Nodes;
using L2Updater.Core;
using Xunit;

namespace L2Updater.Core.Tests;

// Written by Claude. Gemini: do not change this file, change the code until all tests pass.
public class ManifestParserTests
{
    internal const string Hash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    internal static JsonObject ValidManifest() => JsonNode.Parse("""
        {
          "format": 1,
          "key_id": "0123456789abcdef",
          "serial": 42,
          "issued_at": "2026-10-22T12:00:00Z",
          "updater": { "version": "1.0.2", "url": "https://cdn.example.com/updater/L2Updater-1.0.2.exe", "sha256": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", "size": 63000000 },
          "clients": [
            {
              "id": "mythras",
              "title": { "ru": "Хай Файв", "en": "High Five" },
              "exe": "system/l2.exe",
              "args": "-fast",
              "files": "https://cdn.example.com/mythras/files.json",
              "base_urls": ["https://cdn.example.com/mythras/", "https://mirror.example.net/mythras/"],
              "servers": ["mythras_x1"]
            }
          ],
          "links": { "site": "https://example.com", "forum": "https://forum.example.com", "cabinet": "https://example.com/cabinet" },
          "news_url": "https://example.com/api/updater/news",
          "status_url": "https://example.com/api/updater/status",
          "some_future_field": { "anything": true }
        }
        """)!.AsObject();

    internal static JsonObject ValidFileList() => JsonNode.Parse("""
        {
          "format": 1,
          "key_id": "0123456789abcdef",
          "client": "mythras",
          "version": "2026.10.22-1",
          "files": [
            { "path": "system/l2.exe", "size": 12345678, "sha256": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef" },
            { "path": "system/l2.ini", "size": 2048, "sha256": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", "user": true }
          ],
          "delete": ["system/old_patch.dat"]
        }
        """)!.AsObject();

    private static byte[] Bytes(JsonNode node) => Encoding.UTF8.GetBytes(node.ToJsonString());

    private static JsonObject Client(JsonObject manifest) => manifest["clients"]![0]!.AsObject();

    // ---------- manifest: good ----------

    [Fact]
    public void Valid_manifest_is_read_completely()
    {
        var m = ManifestParser.ParseManifest(Bytes(ValidManifest()));
        Assert.Equal(1, m.Format);
        Assert.Equal("0123456789abcdef", m.KeyId);
        Assert.Equal(42, m.Serial);
        Assert.Equal(new DateTimeOffset(2026, 10, 22, 12, 0, 0, TimeSpan.Zero), m.IssuedAt);
        Assert.NotNull(m.Updater);
        Assert.Equal("1.0.2", m.Updater!.Version);
        Assert.Equal("https://cdn.example.com/updater/L2Updater-1.0.2.exe", m.Updater.Url);
        Assert.Equal(Hash, m.Updater.Sha256);
        Assert.Equal(63000000, m.Updater.Size);
        var c = Assert.Single(m.Clients);
        Assert.Equal("mythras", c.Id);
        Assert.Equal("High Five", c.Title["en"]);
        Assert.Equal("Хай Файв", c.Title["ru"]);
        Assert.Equal("system/l2.exe", c.Exe);
        Assert.Equal("-fast", c.Args);
        Assert.Equal("https://cdn.example.com/mythras/files.json", c.Files);
        Assert.Equal(["https://cdn.example.com/mythras/", "https://mirror.example.net/mythras/"], c.BaseUrls);
        Assert.Equal(["mythras_x1"], c.Servers);
        Assert.Equal("https://example.com", m.Links!.Site);
        Assert.Equal("https://forum.example.com", m.Links.Forum);
        Assert.Equal("https://example.com/cabinet", m.Links.Cabinet);
        Assert.Equal("https://example.com/api/updater/news", m.NewsUrl);
        Assert.Equal("https://example.com/api/updater/status", m.StatusUrl);
    }

    [Fact]
    public void Optional_parts_may_be_missing()
    {
        var json = ValidManifest();
        json.Remove("updater");
        json.Remove("links");
        json.Remove("news_url");
        json.Remove("status_url");
        Client(json).Remove("args");
        Client(json).Remove("servers");
        var m = ManifestParser.ParseManifest(Bytes(json));
        Assert.Null(m.Updater);
        Assert.Null(m.Links);
        Assert.Null(m.NewsUrl);
        Assert.Null(m.StatusUrl);
        Assert.Equal("", m.Clients[0].Args);
        Assert.Empty(m.Clients[0].Servers);
    }

    [Fact]
    public void Two_clients_are_read()
    {
        var json = ValidManifest();
        var second = Client(json).DeepClone().AsObject();
        second["id"] = "lucera";
        json["clients"]!.AsArray().Add(second);
        var m = ManifestParser.ParseManifest(Bytes(json));
        Assert.Equal(["mythras", "lucera"], m.Clients.Select(c => c.Id));
    }

    // ---------- manifest: bad ----------

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("42")]
    [InlineData("{\"format\":1")]
    public void Not_a_json_object_is_rejected(string text)
    {
        Assert.Throws<ManifestException>(() => ManifestParser.ParseManifest(Encoding.UTF8.GetBytes(text)));
    }

    [Theory]
    [InlineData("format")]
    [InlineData("key_id")]
    [InlineData("serial")]
    [InlineData("issued_at")]
    [InlineData("clients")]
    public void Missing_required_field_is_rejected(string field)
    {
        var json = ValidManifest();
        json.Remove(field);
        Assert.Throws<ManifestException>(() => ManifestParser.ParseManifest(Bytes(json)));
    }

    public static TheoryData<string, JsonNode?> BadTopValues => new()
    {
        { "format", 2 },
        { "format", "1" },
        { "key_id", "0123" },
        { "key_id", "0123456789ABCDEF" },
        { "key_id", "0123456789abcdeg" },
        { "serial", -1 },
        { "serial", "42" },
        { "issued_at", "yesterday" },
        { "clients", new JsonArray() },
        { "clients", "mythras" },
        { "news_url", "http://example.com/news" },
        { "status_url", "file:///etc/passwd" },
        { "news_url", "not a url" },
    };

    [Theory]
    [MemberData(nameof(BadTopValues))]
    public void Bad_top_level_value_is_rejected(string field, JsonNode? value)
    {
        var json = ValidManifest();
        json[field] = value?.DeepClone();
        Assert.Throws<ManifestException>(() => ManifestParser.ParseManifest(Bytes(json)));
    }

    [Theory]
    [InlineData("id")]
    [InlineData("title")]
    [InlineData("exe")]
    [InlineData("files")]
    [InlineData("base_urls")]
    public void Missing_client_field_is_rejected(string field)
    {
        var json = ValidManifest();
        Client(json).Remove(field);
        Assert.Throws<ManifestException>(() => ManifestParser.ParseManifest(Bytes(json)));
    }

    public static TheoryData<string, JsonNode?> BadClientValues => new()
    {
        { "id", "" },
        { "id", "Mythras" },
        { "id", "my-thras" },
        { "id", "../x" },
        { "id", new string('a', 33) },
        { "title", "High Five" },
        { "title", new JsonObject { ["ru"] = "Хай Файв" } },
        { "exe", "../l2.exe" },
        { "exe", "C:/Windows/system32/cmd.exe" },
        { "exe", "/bin/sh" },
        { "files", "http://cdn.example.com/mythras/files.json" },
        { "base_urls", new JsonArray() },
        { "base_urls", new JsonArray("https://ok.example.com/", "http://bad.example.com/") },
        { "base_urls", "https://cdn.example.com/" },
    };

    [Theory]
    [MemberData(nameof(BadClientValues))]
    public void Bad_client_value_is_rejected(string field, JsonNode? value)
    {
        var json = ValidManifest();
        Client(json)[field] = value?.DeepClone();
        Assert.Throws<ManifestException>(() => ManifestParser.ParseManifest(Bytes(json)));
    }

    [Fact]
    public void Same_client_id_twice_is_rejected()
    {
        var json = ValidManifest();
        json["clients"]!.AsArray().Add(Client(json).DeepClone());
        Assert.Throws<ManifestException>(() => ManifestParser.ParseManifest(Bytes(json)));
    }

    [Theory]
    [InlineData("version")]
    [InlineData("url")]
    [InlineData("sha256")]
    [InlineData("size")]
    public void Updater_without_required_field_is_rejected(string field)
    {
        var json = ValidManifest();
        json["updater"]!.AsObject().Remove(field);
        Assert.Throws<ManifestException>(() => ManifestParser.ParseManifest(Bytes(json)));
    }

    public static TheoryData<string, JsonNode?> BadUpdaterValues => new()
    {
        { "url", "http://cdn.example.com/updater.exe" },
        { "sha256", "abc" },
        { "sha256", Hash.ToUpperInvariant() },
        { "size", 0 },
        { "size", -5 },
    };

    [Theory]
    [MemberData(nameof(BadUpdaterValues))]
    public void Bad_updater_value_is_rejected(string field, JsonNode? value)
    {
        var json = ValidManifest();
        json["updater"]![field] = value?.DeepClone();
        Assert.Throws<ManifestException>(() => ManifestParser.ParseManifest(Bytes(json)));
    }

    [Theory]
    [InlineData("site")]
    [InlineData("forum")]
    [InlineData("cabinet")]
    public void Non_https_link_is_rejected(string field)
    {
        var json = ValidManifest();
        json["links"]![field] = "javascript:alert(1)";
        Assert.Throws<ManifestException>(() => ManifestParser.ParseManifest(Bytes(json)));
    }

    // ---------- files.json ----------

    [Fact]
    public void Valid_file_list_is_read_completely()
    {
        var f = ManifestParser.ParseFileList(Bytes(ValidFileList()), "mythras");
        Assert.Equal(1, f.Format);
        Assert.Equal("0123456789abcdef", f.KeyId);
        Assert.Equal("mythras", f.Client);
        Assert.Equal("2026.10.22-1", f.Version);
        Assert.Equal(2, f.Files.Count);
        Assert.Equal(new FileEntry("system/l2.exe", 12345678, Hash, false), f.Files[0]);
        Assert.Equal(new FileEntry("system/l2.ini", 2048, Hash, true), f.Files[1]);
        Assert.Equal(["system/old_patch.dat"], f.Delete);
    }

    [Fact]
    public void File_list_without_delete_and_with_no_files_is_ok()
    {
        var json = ValidFileList();
        json.Remove("delete");
        json["files"] = new JsonArray();
        var f = ManifestParser.ParseFileList(Bytes(json), "mythras");
        Assert.Empty(f.Files);
        Assert.Empty(f.Delete);
    }

    [Fact]
    public void File_list_of_other_client_is_rejected()
    {
        Assert.Throws<ManifestException>(() => ManifestParser.ParseFileList(Bytes(ValidFileList()), "lucera"));
    }

    [Theory]
    [InlineData("format")]
    [InlineData("key_id")]
    [InlineData("client")]
    [InlineData("version")]
    [InlineData("files")]
    public void File_list_missing_required_field_is_rejected(string field)
    {
        var json = ValidFileList();
        json.Remove(field);
        Assert.Throws<ManifestException>(() => ManifestParser.ParseFileList(Bytes(json), "mythras"));
    }

    [Theory]
    [InlineData("path")]
    [InlineData("size")]
    [InlineData("sha256")]
    public void File_entry_missing_required_field_is_rejected(string field)
    {
        var json = ValidFileList();
        json["files"]![0]!.AsObject().Remove(field);
        Assert.Throws<ManifestException>(() => ManifestParser.ParseFileList(Bytes(json), "mythras"));
    }

    public static TheoryData<string, JsonNode?> BadFileValues => new()
    {
        { "path", "../../Windows/system32/evil.dll" },
        { "path", "/etc/cron.d/x" },
        { "path", "C:/x.dll" },
        { "path", "system\\l2.exe" },
        { "path", "con.txt" },
        { "path", "a:b" },
        { "size", -1 },
        { "size", "10" },
        { "sha256", "xyz" },
        { "sha256", Hash.ToUpperInvariant() },
        { "sha256", Hash + "00" },
        { "user", "yes" },
    };

    [Theory]
    [MemberData(nameof(BadFileValues))]
    public void Bad_file_value_is_rejected(string field, JsonNode? value)
    {
        var json = ValidFileList();
        json["files"]![0]![field] = value?.DeepClone();
        Assert.Throws<ManifestException>(() => ManifestParser.ParseFileList(Bytes(json), "mythras"));
    }

    [Fact]
    public void Same_path_twice_ignoring_case_is_rejected()
    {
        var json = ValidFileList();
        var copy = json["files"]![0]!.DeepClone().AsObject();
        copy["path"] = "SYSTEM/L2.EXE";
        json["files"]!.AsArray().Add(copy);
        Assert.Throws<ManifestException>(() => ManifestParser.ParseFileList(Bytes(json), "mythras"));
    }

    [Theory]
    [InlineData("../boot.ini")]
    [InlineData("/x")]
    [InlineData("nul")]
    public void Bad_delete_path_is_rejected(string path)
    {
        var json = ValidFileList();
        json["delete"]!.AsArray().Add(path);
        Assert.Throws<ManifestException>(() => ManifestParser.ParseFileList(Bytes(json), "mythras"));
    }

    [Fact]
    public void Path_both_in_files_and_delete_is_rejected()
    {
        var json = ValidFileList();
        json["delete"]!.AsArray().Add("system/l2.exe");
        Assert.Throws<ManifestException>(() => ManifestParser.ParseFileList(Bytes(json), "mythras"));
    }
}
