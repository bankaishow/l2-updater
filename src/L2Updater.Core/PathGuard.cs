namespace L2Updater.Core;

/// <summary>
/// Checks paths from files.json before anything touches the disk (docs/format.md, "path").
/// </summary>
public static class PathGuard
{
    /// <summary>Longest allowed full path (game folder + relative path).</summary>
    public const int MaxFullPath = 260;

    /// <summary>
    /// True if <paramref name="path"/> is an allowed relative path:
    /// not empty; forward slashes only (no '\'); does not start with '/';
    /// no ':' anywhere (drive letters, alternate data streams);
    /// no empty segment ("a//b", trailing "/"); no segment "." or "..";
    /// no segment ending with '.' or ' ' (Windows drops them);
    /// no characters &lt; 0x20 and none of &lt; &gt; " | ? *;
    /// no reserved Windows name as a segment, with or without extension, any case:
    /// CON, PRN, AUX, NUL, COM1–COM9, LPT1–LPT9 (so "con.txt" and "Nul" are rejected).
    /// </summary>
    public static bool IsValidRelativePath(string path)
    {
        // У-1а (Gemini). Tests: tests/L2Updater.Core.Tests/PathGuardTests.cs
        if (string.IsNullOrEmpty(path) || path[0] == '/')
            return false;

        foreach (var c in path)
        {
            if (c < 0x20 || c == '\\' || c == ':' || c == '<' || c == '>' ||
                c == '"' || c == '|' || c == '?' || c == '*')
                return false;
        }

        foreach (var segment in path.Split('/'))
        {
            if (segment.Length == 0 || segment == "." || segment == "..")
                return false;
            if (segment.EndsWith('.') || segment.EndsWith(' '))
                return false;
            if (IsReservedName(segment))
                return false;
        }

        return true;
    }

    // Name before the first dot must not be a reserved Windows device name.
    private static bool IsReservedName(string segment)
    {
        var dot = segment.IndexOf('.');
        var name = (dot >= 0 ? segment[..dot] : segment).ToUpperInvariant();
        if (name is "CON" or "PRN" or "AUX" or "NUL")
            return true;
        return name.Length == 4 &&
               (name.StartsWith("COM", StringComparison.Ordinal) || name.StartsWith("LPT", StringComparison.Ordinal)) &&
               name[3] >= '1' && name[3] <= '9';
    }

    /// <summary>
    /// Full path of <paramref name="relativePath"/> inside <paramref name="gameRoot"/>.
    /// Throws UnsafePathException if the path is not valid (IsValidRelativePath),
    /// if the full path (Path.GetFullPath) is not inside gameRoot,
    /// or if it is longer than MaxFullPath characters.
    /// </summary>
    public static string Resolve(string gameRoot, string relativePath)
    {
        // У-1а (Gemini). Tests: tests/L2Updater.Core.Tests/PathGuardTests.cs
        if (!IsValidRelativePath(relativePath))
            throw new UnsafePathException($"Invalid path: {relativePath}");

        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(gameRoot));
        var full = Path.GetFullPath(Path.Combine(root, relativePath));

        // Root plus separator, so "l2game-evil" does not pass as "l2game".
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!full.StartsWith(root + Path.DirectorySeparatorChar, comparison))
            throw new UnsafePathException($"Path is outside the game folder: {relativePath}");

        if (full.Length > MaxFullPath)
            throw new UnsafePathException($"Path is too long: {relativePath}");

        return full;
    }
}
