using System.Text;
using System.Text.Json.Nodes;
using L2Updater.Core;
using Xunit;

namespace L2Updater.Core.Tests;

// Written by Claude. Security audit 2026-10-29, finding 4: a file list must not touch the updater's own folder.
public class ManifestServiceFolderTests
{
    private static byte[] Bytes(JsonNode node) => Encoding.UTF8.GetBytes(node.ToJsonString());

    private static JsonObject ListWithFile(string path)
    {
        var list = ManifestParserTests.ValidFileList();
        list["files"]![0]!.AsObject()["path"] = path;
        return list;
    }

    private static JsonObject ListWithDelete(string path)
    {
        var list = ManifestParserTests.ValidFileList();
        list["delete"] = new JsonArray(path);
        return list;
    }

    [Theory]
    [InlineData(".l2updater/cache.json")]
    [InlineData(".l2updater/tmp/0123.part")]
    [InlineData(".L2Updater/cache.json")]
    [InlineData(".l2updater")]
    public void File_in_the_updater_folder_is_rejected(string path) =>
        Assert.Throws<ManifestException>(() => ManifestParser.ParseFileList(Bytes(ListWithFile(path)), "mythras"));

    [Theory]
    [InlineData(".l2updater/cache.json")]
    [InlineData(".L2UPDATER/tmp/x.part")]
    public void Delete_in_the_updater_folder_is_rejected(string path) =>
        Assert.Throws<ManifestException>(() => ManifestParser.ParseFileList(Bytes(ListWithDelete(path)), "mythras"));

    [Theory]
    [InlineData("l2updater.ini")]
    [InlineData(".l2updater2/x.dat")]
    [InlineData("system/.l2updater/x.dat")]
    public void Similar_names_are_still_fine(string path)
    {
        var list = ManifestParser.ParseFileList(Bytes(ListWithFile(path)), "mythras");
        Assert.Equal(path, list.Files[0].Path);
    }
}
