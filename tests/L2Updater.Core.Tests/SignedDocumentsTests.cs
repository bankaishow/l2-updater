using System.Text;
using System.Text.Json.Nodes;
using L2Updater.Core;
using Xunit;

namespace L2Updater.Core.Tests;

// Written by Claude. Gemini: do not change this file, change the code until all tests pass.
public class SignedDocumentsTests
{
    private static byte[] ManifestBytes(TestKey key, long serial = 42)
    {
        var json = ManifestParserTests.ValidManifest();
        json["key_id"] = key.KeyId;
        json["serial"] = serial;
        return Encoding.UTF8.GetBytes(json.ToJsonString());
    }

    private static byte[] FileListBytes(TestKey key)
    {
        var json = ManifestParserTests.ValidFileList();
        json["key_id"] = key.KeyId;
        return Encoding.UTF8.GetBytes(json.ToJsonString());
    }

    [Fact]
    public void Signed_manifest_is_loaded()
    {
        using var key = new TestKey();
        var data = ManifestBytes(key);
        var m = SignedDocuments.LoadManifest(data, key.Sign(data), new SignatureVerifier([key.PublicKey]), 0);
        Assert.Equal(42, m.Serial);
        Assert.Equal(key.KeyId, m.KeyId);
    }

    [Fact]
    public void Manifest_signed_by_other_key_is_rejected()
    {
        using var trusted = new TestKey();
        using var attacker = new TestKey();
        var data = ManifestBytes(attacker);
        Assert.Throws<ManifestException>(() =>
            SignedDocuments.LoadManifest(data, attacker.Sign(data), new SignatureVerifier([trusted.PublicKey]), 0));
    }

    [Fact]
    public void Attacker_cannot_reuse_trusted_key_id()
    {
        using var trusted = new TestKey();
        using var attacker = new TestKey();
        var data = ManifestBytes(trusted); // names the trusted key, but signed by the attacker
        Assert.Throws<ManifestException>(() =>
            SignedDocuments.LoadManifest(data, attacker.Sign(data), new SignatureVerifier([trusted.PublicKey]), 0));
    }

    [Fact]
    public void Changed_manifest_is_rejected()
    {
        using var key = new TestKey();
        var data = ManifestBytes(key);
        var signature = key.Sign(data);
        var changed = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(data).Replace("cdn.example.com", "evil.example.com"));
        Assert.Throws<ManifestException>(() =>
            SignedDocuments.LoadManifest(changed, signature, new SignatureVerifier([key.PublicKey]), 0));
    }

    [Fact]
    public void Signature_is_checked_before_parsing()
    {
        // a badly signed document with invalid content must fail on the signature, not reach the parser
        using var key = new TestKey();
        var json = JsonNode.Parse("{\"key_id\":\"" + key.KeyId + "\",\"format\":99}")!;
        var data = Encoding.UTF8.GetBytes(json.ToJsonString());
        var ex = Assert.Throws<ManifestException>(() =>
            SignedDocuments.LoadManifest(data, "AAAA", new SignatureVerifier([key.PublicKey]), 0));
        Assert.Contains("signature", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{\"format\":1}")]
    [InlineData("{\"key_id\":42}")]
    public void Document_without_key_id_is_rejected(string text)
    {
        using var key = new TestKey();
        var data = Encoding.UTF8.GetBytes(text);
        Assert.Throws<ManifestException>(() =>
            SignedDocuments.LoadManifest(data, key.Sign(data), new SignatureVerifier([key.PublicKey]), 0));
    }

    [Fact]
    public void Older_serial_is_rejected()
    {
        using var key = new TestKey();
        var data = ManifestBytes(key, serial: 41);
        Assert.Throws<ManifestException>(() =>
            SignedDocuments.LoadManifest(data, key.Sign(data), new SignatureVerifier([key.PublicKey]), 42));
    }

    [Fact]
    public void Same_serial_is_accepted()
    {
        using var key = new TestKey();
        var data = ManifestBytes(key, serial: 42);
        var m = SignedDocuments.LoadManifest(data, key.Sign(data), new SignatureVerifier([key.PublicKey]), 42);
        Assert.Equal(42, m.Serial);
    }

    [Fact]
    public void Signed_but_invalid_manifest_is_rejected()
    {
        using var key = new TestKey();
        var json = ManifestParserTests.ValidManifest();
        json["key_id"] = key.KeyId;
        json["news_url"] = "http://example.com/news";
        var data = Encoding.UTF8.GetBytes(json.ToJsonString());
        Assert.Throws<ManifestException>(() =>
            SignedDocuments.LoadManifest(data, key.Sign(data), new SignatureVerifier([key.PublicKey]), 0));
    }

    [Fact]
    public void Signed_file_list_is_loaded()
    {
        using var key = new TestKey();
        var data = FileListBytes(key);
        var f = SignedDocuments.LoadFileList(data, key.Sign(data), new SignatureVerifier([key.PublicKey]), "mythras");
        Assert.Equal(2, f.Files.Count);
    }

    [Fact]
    public void File_list_signed_by_other_key_is_rejected()
    {
        using var trusted = new TestKey();
        using var attacker = new TestKey();
        var data = FileListBytes(attacker);
        Assert.Throws<ManifestException>(() =>
            SignedDocuments.LoadFileList(data, attacker.Sign(data), new SignatureVerifier([trusted.PublicKey]), "mythras"));
    }

    [Fact]
    public void Signed_file_list_of_other_client_is_rejected()
    {
        using var key = new TestKey();
        var data = FileListBytes(key);
        Assert.Throws<ManifestException>(() =>
            SignedDocuments.LoadFileList(data, key.Sign(data), new SignatureVerifier([key.PublicKey]), "lucera"));
    }
}
