using System.Text;
using System.Text.Json.Nodes;
using L2Updater.Core;
using Xunit;

namespace L2Updater.Core.Tests;

// Written by Claude. The optional "guard" field of a client (path of the guard program).
public class ManifestGuardTests
{
    private static byte[] Bytes(JsonNode node) => Encoding.UTF8.GetBytes(node.ToJsonString());

    private static JsonObject WithGuard(JsonNode? guard)
    {
        var manifest = ManifestParserTests.ValidManifest();
        manifest["clients"]![0]!.AsObject()["guard"] = guard;
        return manifest;
    }

    [Fact]
    public void Client_without_guard_has_none()
    {
        var manifest = ManifestParser.ParseManifest(Bytes(ManifestParserTests.ValidManifest()));
        Assert.Null(manifest.Clients[0].Guard);
    }

    [Fact]
    public void Guard_path_is_read()
    {
        var manifest = ManifestParser.ParseManifest(Bytes(WithGuard("system/L2Sec.Agent.exe")));
        Assert.Equal("system/L2Sec.Agent.exe", manifest.Clients[0].Guard);
    }

    [Theory]
    [InlineData("../agent.exe")]
    [InlineData("/agent.exe")]
    [InlineData("C:/agent.exe")]
    [InlineData("")]
    public void Unsafe_guard_path_is_refused(string path) =>
        Assert.Throws<ManifestException>(() => ManifestParser.ParseManifest(Bytes(WithGuard(path))));

    [Fact]
    public void Guard_must_be_a_string() =>
        Assert.Throws<ManifestException>(() => ManifestParser.ParseManifest(Bytes(WithGuard(5))));

    [Fact]
    public void Explicit_null_guard_is_refused() =>
        Assert.Throws<ManifestException>(() => ManifestParser.ParseManifest(Bytes(WithGuard(null))));
}
