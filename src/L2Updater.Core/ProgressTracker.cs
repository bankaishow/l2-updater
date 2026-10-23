namespace L2Updater.Core;

/// <summary>
/// Percent, speed and time left for the progress bar. Time comes from <paramref name="time"/>
/// (tests pass a fake clock; the window passes TimeProvider.System).
/// </summary>
public sealed class ProgressTracker(TimeProvider time)
{
    /// <summary>Speed is measured over the last this many seconds.</summary>
    public static readonly TimeSpan Window = TimeSpan.FromSeconds(5);

    // Use this clock for every time reading.
    private readonly TimeProvider _time = time;

    /// <summary>
    /// New state: <paramref name="bytesDone"/> of <paramref name="bytesTotal"/>.
    /// Remembers (now, bytesDone); forgets samples older than Window, but always keeps at least the two newest.
    /// A bytesDone smaller than the previous one (new download) clears the samples first.
    /// </summary>
    public void Report(long bytesDone, long bytesTotal)
    {
        // У-3а (Gemini). Tests: tests/L2Updater.Core.Tests/ProgressTrackerTests.cs
        throw new NotImplementedException();
    }

    /// <summary>0..100, rounded down. Nothing reported yet or bytesTotal ≤ 0 → 100 if anything was reported, else 0.</summary>
    public int Percent => throw new NotImplementedException();

    /// <summary>(newest bytes − oldest bytes) / seconds between them; fewer than two samples or 0 seconds → 0.</summary>
    public double BytesPerSecond => throw new NotImplementedException();

    /// <summary>(bytesTotal − bytesDone) / BytesPerSecond, or null if speed is 0. Done → TimeSpan.Zero.</summary>
    public TimeSpan? Remaining => throw new NotImplementedException();

    /// <summary>"0 B", "512 B", "1.5 KB", "12.0 MB", "1.25 GB": 1024-based, invariant culture,
    /// B without decimals, KB and MB with one decimal, GB with two.</summary>
    public static string FormatBytes(long bytes)
    {
        // У-3а (Gemini)
        throw new NotImplementedException();
    }
}
