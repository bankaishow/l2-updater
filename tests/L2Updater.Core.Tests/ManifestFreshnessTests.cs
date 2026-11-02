using System.Text;
using System.Text.Json.Nodes;
using L2Updater.Core;
using Xunit;

namespace L2Updater.Core.Tests;

// Written by Claude. Optional "expires_at" (manifest) and "files_sha256" (client): protection of files.json against
// rollback and freezing (docs/security-audit.md, finding 1).
public class ManifestFreshnessTests
{
    private static byte[] Bytes(JsonNode node) => Encoding.UTF8.GetBytes(node.ToJsonString());
    private static readonly string Hash = new('a', 64);

    private static JsonObject Manifest(JsonNode? expires, bool setExpires = true)
    {
        var m = ManifestParserTests.ValidManifest();
        if (setExpires) m["expires_at"] = expires;
        return m;
    }

    private static JsonObject WithFilesHash(JsonNode? hash)
    {
        var m = ManifestParserTests.ValidManifest();
        m["clients"]![0]!.AsObject()["files_sha256"] = hash;
        return m;
    }

    [Fact]
    public void Without_new_fields_both_are_empty()
    {
        var m = ManifestParser.ParseManifest(Bytes(ManifestParserTests.ValidManifest()));
        Assert.Null(m.ExpiresAt);
        Assert.Null(m.Clients[0].FilesSha256);
    }

    [Fact]
    public void Expires_at_is_read() =>
        Assert.Equal(DateTimeOffset.Parse("2027-01-01T00:00:00Z"),
            ManifestParser.ParseManifest(Bytes(Manifest("2027-01-01T00:00:00Z"))).ExpiresAt);

    [Theory]
    [InlineData("tomorrow")]
    [InlineData("")]
    public void Bad_expires_at_is_refused(string value) =>
        Assert.Throws<ManifestException>(() => ManifestParser.ParseManifest(Bytes(Manifest(value))));

    [Fact]
    public void Expires_at_must_be_a_string_and_not_null()
    {
        Assert.Throws<ManifestException>(() => ManifestParser.ParseManifest(Bytes(Manifest(5))));
        Assert.Throws<ManifestException>(() => ManifestParser.ParseManifest(Bytes(Manifest(null))));
    }

    [Fact]
    public void Files_sha256_is_read() =>
        Assert.Equal(Hash, ManifestParser.ParseManifest(Bytes(WithFilesHash(Hash))).Clients[0].FilesSha256);

    [Theory]
    [InlineData("abc")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("")]
    public void Bad_files_sha256_is_refused(string value) =>
        Assert.Throws<ManifestException>(() => ManifestParser.ParseManifest(Bytes(WithFilesHash(value))));

    [Fact]
    public void Files_sha256_must_be_a_string_and_not_null()
    {
        Assert.Throws<ManifestException>(() => ManifestParser.ParseManifest(Bytes(WithFilesHash(7))));
        Assert.Throws<ManifestException>(() => ManifestParser.ParseManifest(Bytes(WithFilesHash(null))));
    }

    // ---- signed manifest: the clock ----

    private static (byte[] Data, string Sig, SignatureVerifier Verifier) Signed(string expires)
    {
        var key = new TestKey();
        var json = Manifest(expires);
        json["key_id"] = key.KeyId;
        var data = Bytes(json);
        return (data, key.Sign(data), new SignatureVerifier([key.PublicKey]));
    }

    [Fact]
    public void Manifest_before_expiry_is_accepted()
    {
        var (data, sig, verifier) = Signed("2027-01-01T00:00:00Z");
        var m = SignedDocuments.LoadManifest(data, sig, verifier, 0, DateTimeOffset.Parse("2026-12-31T23:59:59Z"));
        Assert.NotNull(m.ExpiresAt);
    }

    [Theory]
    [InlineData("2027-01-01T00:00:00Z")]
    [InlineData("2028-06-01T00:00:00Z")]
    public void Expired_manifest_is_refused(string now)
    {
        var (data, sig, verifier) = Signed("2027-01-01T00:00:00Z");
        var ex = Assert.Throws<ManifestException>(() =>
            SignedDocuments.LoadManifest(data, sig, verifier, 0, DateTimeOffset.Parse(now)));
        Assert.Contains("expired", ex.Message);
    }

    [Fact]
    public void Manifest_without_expiry_never_expires()
    {
        var key = new TestKey();
        var json = ManifestParserTests.ValidManifest();
        json["key_id"] = key.KeyId;
        var data = Bytes(json);
        SignedDocuments.LoadManifest(data, key.Sign(data), new SignatureVerifier([key.PublicKey]), 0, DateTimeOffset.Parse("2099-01-01T00:00:00Z"));
    }
}
