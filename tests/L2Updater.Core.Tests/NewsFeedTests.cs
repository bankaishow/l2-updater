using System.Text;
using L2Updater.Core;
using Xunit;

namespace L2Updater.Core.Tests;

// Written by Claude. Gemini: do not change this file, change the code until all tests pass.
public class NewsFeedTests
{
    private static byte[] B(string s) => Encoding.UTF8.GetBytes(s);

    // ---------- Clean ----------

    [Theory]
    [InlineData("Server open", 200, "Server open")]
    [InlineData("  spaces   inside  ", 200, "spaces inside")]
    [InlineData("line\nbreak\r\ntab\there", 200, "line break tab here")]
    [InlineData("evil‮txt.exe", 200, "evil txt.exe")]
    [InlineData("zero​width", 200, "zero width")]
    [InlineData("abcdef", 3, "abc")]
    [InlineData("ab cdef", 3, "ab")]
    [InlineData("\u0000\u0001", 200, "")]
    [InlineData("Сервер открыт", 200, "Сервер открыт")]
    public void Clean_makes_text_safe(string input, int max, string expected)
    {
        Assert.Equal(expected, NewsFeed.Clean(input, max));
    }

    // ---------- news ----------

    [Fact]
    public void News_are_read()
    {
        var items = NewsFeed.ParseNews(B("""
            { "items": [
              { "date": "2026-10-23", "title": "Server x1 is open", "url": "https://example.com/news/12" },
              { "title": "No date, no url" }
            ] }
            """));
        Assert.Equal(2, items.Count);
        Assert.Equal(new NewsItem(new DateOnly(2026, 10, 23), "Server x1 is open", "https://example.com/news/12"), items[0]);
        Assert.Equal(new NewsItem(null, "No date, no url", null), items[1]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"items\": 5}")]
    [InlineData("{\"items\": {}}")]
    public void Bad_news_document_gives_empty_list(string? text)
    {
        Assert.Empty(NewsFeed.ParseNews(text is null ? null : B(text)));
    }

    [Fact]
    public void Too_big_news_document_gives_empty_list()
    {
        var big = "{\"items\":[{\"title\":\"" + new string('a', NewsFeed.MaxBytes) + "\"}]}";
        Assert.Empty(NewsFeed.ParseNews(B(big)));
    }

    [Fact]
    public void Bad_news_items_are_skipped_good_ones_kept()
    {
        var items = NewsFeed.ParseNews(B("""
            { "items": [
              42,
              { "date": "2026-10-23" },
              { "title": "   " },
              { "title": 5 },
              { "title": "kept", "date": "23.10.2026", "url": "javascript:alert(1)" },
              { "title": "http url dropped", "url": "http://example.com" }
            ] }
            """));
        Assert.Equal([new NewsItem(null, "kept", null), new NewsItem(null, "http url dropped", null)], items);
    }

    [Fact]
    public void Only_first_20_news_are_kept()
    {
        var json = "{\"items\":[" + string.Join(",", Enumerable.Range(1, 30).Select(i => $"{{\"title\":\"n{i}\"}}")) + "]}";
        var items = NewsFeed.ParseNews(B(json));
        Assert.Equal(NewsFeed.MaxItems, items.Count);
        Assert.Equal("n1", items[0].Title);
        Assert.Equal("n20", items[^1].Title);
    }

    [Fact]
    public void Long_title_is_cut_to_200()
    {
        var items = NewsFeed.ParseNews(B("{\"items\":[{\"title\":\"" + new string('x', 500) + "\"}]}"));
        Assert.Equal(200, items[0].Title.Length);
    }

    // ---------- status ----------

    [Fact]
    public void Status_is_read()
    {
        var servers = NewsFeed.ParseStatus(B("""
            { "servers": [
              { "id": "mythras_x1", "name": "Mythras x1", "online": true, "players": 12 },
              { "name": "Lucera x5", "online": false }
            ] }
            """));
        Assert.Equal([
            new ServerStatus("mythras_x1", "Mythras x1", true, 12),
            new ServerStatus(null, "Lucera x5", false, null),
        ], servers);
    }

    [Fact]
    public void Bad_status_values_are_made_safe()
    {
        var servers = NewsFeed.ParseStatus(B("""
            { "servers": [
              { "name": "A", "online": "yes", "players": -3 },
              { "name": "B", "online": 1, "players": 1.5 },
              { "name": "C", "players": 99999999999 },
              { "online": true },
              { "name": "‮", "online": true }
            ] }
            """));
        Assert.Equal([
            new ServerStatus(null, "A", false, null),
            new ServerStatus(null, "B", false, null),
            new ServerStatus(null, "C", false, null),
        ], servers);
    }

    [Fact]
    public void Long_server_name_is_cut_to_64()
    {
        var servers = NewsFeed.ParseStatus(B("{\"servers\":[{\"name\":\"" + new string('s', 100) + "\"}]}"));
        Assert.Equal(64, servers[0].Name.Length);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("garbage")]
    [InlineData("{\"servers\": null}")]
    public void Bad_status_document_gives_empty_list(string? text)
    {
        Assert.Empty(NewsFeed.ParseStatus(text is null ? null : B(text)));
    }
}
