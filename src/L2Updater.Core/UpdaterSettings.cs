using System.Globalization;
using System.Text;

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
        var settings = new UpdaterSettings();
        if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
            return settings;

        try
        {
            var lines = File.ReadAllLines(file);
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                    continue;

                var eqIndex = line.IndexOf('=');
                if (eqIndex < 0)
                    continue;

                var key = line[..eqIndex].Trim();
                var val = line[(eqIndex + 1)..].Trim();

                if (string.Equals(key, "manifest_url", StringComparison.Ordinal))
                {
                    if (Uri.TryCreate(val, UriKind.Absolute, out var uri) &&
                        string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
                    {
                        settings.ManifestUrl = val;
                    }
                    else
                    {
                        settings.ManifestUrl = null;
                    }
                }
                else if (string.Equals(key, "language", StringComparison.Ordinal))
                {
                    settings.Language = (val is "ru" or "en") ? val : "ru";
                }
                else if (string.Equals(key, "last_client", StringComparison.Ordinal))
                {
                    settings.LastClient = val;
                }
                else if (string.Equals(key, "last_serial", StringComparison.Ordinal))
                {
                    if (long.TryParse(val, NumberStyles.None, CultureInfo.InvariantCulture, out var serial) && serial >= 0)
                        settings.LastSerial = serial;
                    else
                        settings.LastSerial = 0;
                }
                else if (key.StartsWith("game_folder.", StringComparison.Ordinal))
                {
                    var clientId = key["game_folder.".Length..];
                    if (clientId.Length > 0)
                        settings.GameFolders[clientId] = val;
                }
            }
        }
        catch
        {
            return new UpdaterSettings();
        }

        return settings;
    }

    /// <summary>
    /// Writes all settings (keys in the order of the example above, then game folders sorted by id),
    /// creating the folder if needed, through a temporary file (like HashCache.Save).
    /// Null values are not written. A value containing '\r' or '\n' is not written.
    /// </summary>
    public void Save(string file)
    {
        // У-3а (Gemini)
        var lines = new List<string>();

        if (IsValidValue(ManifestUrl))
            lines.Add($"manifest_url={ManifestUrl}");

        if (IsValidValue(Language))
            lines.Add($"language={Language}");

        if (IsValidValue(LastClient))
            lines.Add($"last_client={LastClient}");

        lines.Add($"last_serial={LastSerial}");

        foreach (var (id, folder) in GameFolders.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            if (IsValidValue(id) && IsValidValue(folder))
                lines.Add($"game_folder.{id}={folder}");
        }

        var dir = Path.GetDirectoryName(Path.GetFullPath(file));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var tempFile = file + ".tmp";
        File.WriteAllLines(tempFile, lines, Encoding.UTF8);

        if (File.Exists(file))
        {
            var attr = File.GetAttributes(file);
            if ((attr & FileAttributes.ReadOnly) != 0)
                File.SetAttributes(file, attr & ~FileAttributes.ReadOnly);
        }

        File.Move(tempFile, file, overwrite: true);
    }

    private static bool IsValidValue(string? val) =>
        val != null && val.IndexOfAny(['\r', '\n']) < 0;

    /// <summary>
    /// Folder for updater.ini: <paramref name="exeFolder"/> if a file can be created and deleted there,
    /// otherwise Path.Combine(<paramref name="appDataFolder"/>, "L2Updater") (created if missing).
    /// </summary>
    public static string ChooseFolder(string exeFolder, string appDataFolder)
    {
        // У-3а (Gemini)
        var isWritable = false;
        if (!string.IsNullOrWhiteSpace(exeFolder) && Directory.Exists(exeFolder))
        {
            var probeFile = Path.Combine(exeFolder, $".probe_{Guid.NewGuid():N}.tmp");
            try
            {
                File.WriteAllBytes(probeFile, [1]);
                isWritable = true;
            }
            catch
            {
                isWritable = false;
            }
            finally
            {
                try
                {
                    if (File.Exists(probeFile))
                        File.Delete(probeFile);
                }
                catch
                {
                }
            }
        }

        if (isWritable)
            return exeFolder;

        var fallback = Path.Combine(appDataFolder, "L2Updater");
        Directory.CreateDirectory(fallback);
        return fallback;
    }
}
