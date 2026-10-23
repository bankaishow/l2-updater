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

/// <summary>
/// What an update has to do (UpdatePlanner.Plan). Paths are relative, as in files.json.
/// Download: files that are missing or differ. Delete: files from "delete" that exist.
/// UpToDate: files that already match. UserKept: "user" files that exist and are left alone.
/// </summary>
public sealed record UpdatePlan(
    IReadOnlyList<FileEntry> Download,
    IReadOnlyList<string> Delete,
    int UpToDate,
    int UserKept);

/// <summary>One news line for the window (NewsFeed.ParseNews). Url is https or null.</summary>
public sealed record NewsItem(DateOnly? Date, string Title, string? Url);

/// <summary>One server line for the window (NewsFeed.ParseStatus). Players is null if unknown.</summary>
public sealed record ServerStatus(string? Id, string Name, bool Online, int? Players);

/// <summary>
/// Progress of a whole update (UpdateSession.UpdateAsync).
/// FileIndex is 1-based; BytesDone and BytesTotal are for all files to download together.
/// </summary>
public sealed record UpdateProgress(int FileIndex, int FileCount, string CurrentPath, long BytesDone, long BytesTotal);

/// <summary>What an update did.</summary>
public sealed record UpdateResult(int Downloaded, int Deleted, int UpToDate, int UserKept);
