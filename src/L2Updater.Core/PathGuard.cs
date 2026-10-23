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
        // У-1а (Gemini): write this method. Tests: tests/L2Updater.Core.Tests/PathGuardTests.cs
        throw new NotImplementedException();
    }

    /// <summary>
    /// Full path of <paramref name="relativePath"/> inside <paramref name="gameRoot"/>.
    /// Throws UnsafePathException if the path is not valid (IsValidRelativePath),
    /// if the full path (Path.GetFullPath) is not inside gameRoot,
    /// or if it is longer than MaxFullPath characters.
    /// </summary>
    public static string Resolve(string gameRoot, string relativePath)
    {
        // У-1а (Gemini): write this method. Tests: tests/L2Updater.Core.Tests/PathGuardTests.cs
        throw new NotImplementedException();
    }
}
