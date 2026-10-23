namespace L2Updater.Core;

// Data from manifest.json and files.json (docs/format.md). Written by Claude, do not change.

public sealed record Manifest(
    int Format,
    string KeyId,
    long Serial,
    DateTimeOffset IssuedAt,
    UpdaterInfo? Updater,
    IReadOnlyList<ClientInfo> Clients,
    Links? Links,
    string? NewsUrl,
    string? StatusUrl);

public sealed record UpdaterInfo(string Version, string Url, string Sha256, long Size);

public sealed record ClientInfo(
    string Id,
    IReadOnlyDictionary<string, string> Title,
    string Exe,
    string Args,
    string Files,
    IReadOnlyList<string> BaseUrls,
    IReadOnlyList<string> Servers);

public sealed record Links(string? Site, string? Forum, string? Cabinet);

public sealed record FileList(
    int Format,
    string KeyId,
    string Client,
    string Version,
    IReadOnlyList<FileEntry> Files,
    IReadOnlyList<string> Delete);

public sealed record FileEntry(string Path, long Size, string Sha256, bool User);
