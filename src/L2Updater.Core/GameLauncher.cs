using System.Diagnostics;

namespace L2Updater.Core;

/// <summary>Starts the game and checks where it is installed.</summary>
public static class GameLauncher
{
    /// <summary>
    /// How to start the game: FileName = PathGuard.Resolve(gameRoot, client.Exe),
    /// WorkingDirectory = the folder of that file, Arguments = client.Args, UseShellExecute = false.
    /// The exe file does not exist → UpdateException.
    /// </summary>
    public static ProcessStartInfo BuildStartInfo(ClientInfo client, string gameRoot)
    {
        // У-3а (Gemini). Tests: tests/L2Updater.Core.Tests/GameLauncherTests.cs
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(gameRoot);

        var exePath = PathGuard.Resolve(gameRoot, client.Exe);
        if (!File.Exists(exePath))
            throw new UpdateException($"Game executable not found: {exePath}");

        var workDir = Path.GetDirectoryName(exePath) ?? gameRoot;
        return new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = workDir,
            Arguments = client.Args ?? "",
            UseShellExecute = false
        };
    }

    /// <summary>
    /// True if <paramref name="gameFolder"/> is one of <paramref name="programFilesFolders"/> or inside one
    /// (full paths, ignoring case and a trailing separator; "C:\Program Files Games" is NOT inside "C:\Program Files").
    /// The window then warns that updating there needs administrator rights.
    /// </summary>
    public static bool IsUnderProgramFiles(string gameFolder, IEnumerable<string> programFilesFolders)
    {
        // У-3а (Gemini)
        if (string.IsNullOrWhiteSpace(gameFolder) || programFilesFolders == null)
            return false;

        var fullGame = Path.TrimEndingDirectorySeparator(Path.GetFullPath(gameFolder));

        foreach (var pf in programFilesFolders)
        {
            if (string.IsNullOrWhiteSpace(pf))
                continue;

            var fullPf = Path.TrimEndingDirectorySeparator(Path.GetFullPath(pf));
            if (string.Equals(fullGame, fullPf, StringComparison.OrdinalIgnoreCase))
                return true;

            var prefix = fullPf + Path.DirectorySeparatorChar;
            if (fullGame.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
