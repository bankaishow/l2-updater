namespace L2Updater.Core;

/// <summary>
/// updater.ini: what the updater remembers between starts. Plain "key=value" lines, UTF-8.
/// Lines starting with '#' and unknown keys are ignored. Example:
///   manifest_url=https://example.com/updater/manifest.json
///   language=ru
///   last_client=mythras
///   last_serial=42
///   game_folder.mythras=D:\Games\Lineage II
/// </summary>
public sealed class UpdaterSettings
{
    public const string FileName = "updater.ini";

    /// <summary>https only; anything else read from the file → null.</summary>
    public string? ManifestUrl { get; set; }

    /// <summary>"ru" or "en"; anything else read from the file → "ru".</summary>
    public string Language { get; set; } = "ru";

    public string? LastClient { get; set; }

    /// <summary>Highest manifest serial accepted so far (rollback protection). Bad or negative value → 0.</summary>
    public long LastSerial { get; set; }

    /// <summary>Game folder per client id (keys "game_folder.&lt;id&gt;").</summary>
    public Dictionary<string, string> GameFolders { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Reads the file. Missing or unreadable file → default settings, never an exception.
    /// Spaces around key and value are trimmed. A line without '=' is ignored. A later duplicate key wins.
    /// </summary>
    public static UpdaterSettings Load(string file)
    {
        // У-3а (Gemini). Tests: tests/L2Updater.Core.Tests/UpdaterSettingsTests.cs
        throw new NotImplementedException();
    }

    /// <summary>
    /// Writes all settings (keys in the order of the example above, then game folders sorted by id),
    /// creating the folder if needed, through a temporary file (like HashCache.Save).
    /// Null values are not written. A value containing '\r' or '\n' is not written.
    /// </summary>
    public void Save(string file)
    {
        // У-3а (Gemini)
        throw new NotImplementedException();
    }

    /// <summary>
    /// Folder for updater.ini: <paramref name="exeFolder"/> if a file can be created and deleted there,
    /// otherwise Path.Combine(<paramref name="appDataFolder"/>, "L2Updater") (created if missing).
    /// </summary>
    public static string ChooseFolder(string exeFolder, string appDataFolder)
    {
        // У-3а (Gemini)
        throw new NotImplementedException();
    }
}
