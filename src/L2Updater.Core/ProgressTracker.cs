using System.Globalization;

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
    private readonly List<(DateTimeOffset Time, long BytesDone)> _samples = [];
    private long _lastBytesDone;
    private long _lastBytesTotal;
    private bool _hasReported;

    /// <summary>
    /// New state: <paramref name="bytesDone"/> of <paramref name="bytesTotal"/>.
    /// Remembers (now, bytesDone); forgets samples older than Window, but always keeps at least the two newest.
    /// A bytesDone smaller than the previous one (new download) clears the samples first.
    /// </summary>
    public void Report(long bytesDone, long bytesTotal)
    {
        // У-3б (Gemini). Tests: tests/L2Updater.Core.Tests/ProgressTrackerTests.cs
        var now = _time.GetUtcNow();

        if (_hasReported && bytesDone < _lastBytesDone)
        {
            _samples.Clear();
        }

        _lastBytesDone = bytesDone;
        _lastBytesTotal = bytesTotal;
        _hasReported = true;

        _samples.Add((now, bytesDone));

        var cutoff = now - Window;
        while (_samples.Count > 2 && _samples[0].Time < cutoff)
        {
            _samples.RemoveAt(0);
        }
    }

    /// <summary>0..100, rounded down. Nothing reported yet or bytesTotal ≤ 0 → 100 if anything was reported, else 0.</summary>
    public int Percent
    {
        get
        {
            if (!_hasReported)
                return 0;
            if (_lastBytesTotal <= 0 || _lastBytesDone >= _lastBytesTotal)
                return 100;
            if (_lastBytesDone <= 0)
                return 0;
            return Math.Clamp((int)(_lastBytesDone * 100 / _lastBytesTotal), 0, 100);
        }
    }

    /// <summary>(newest bytes − oldest bytes) / seconds between them; fewer than two samples or 0 seconds → 0.</summary>
    public double BytesPerSecond
    {
        get
        {
            if (_samples.Count < 2)
                return 0;

            var oldest = _samples[0];
            var newest = _samples[^1];
            var seconds = (newest.Time - oldest.Time).TotalSeconds;
            if (seconds <= 0)
                return 0;

            var bytesDiff = newest.BytesDone - oldest.BytesDone;
            if (bytesDiff < 0)
                return 0;

            return bytesDiff / seconds;
        }
    }

    /// <summary>(bytesTotal − bytesDone) / BytesPerSecond, or null if speed is 0. Done → TimeSpan.Zero.</summary>
    public TimeSpan? Remaining
    {
        get
        {
            if (!_hasReported)
                return null;

            if (_lastBytesTotal <= 0 || _lastBytesDone >= _lastBytesTotal)
                return TimeSpan.Zero;

            var speed = BytesPerSecond;
            if (speed <= 0)
                return null;

            var remainingBytes = _lastBytesTotal - _lastBytesDone;
            var seconds = remainingBytes / speed;
            return TimeSpan.FromSeconds(seconds);
        }
    }

    /// <summary>"0 B", "512 B", "1.5 KB", "12.0 MB", "1.25 GB": 1024-based, invariant culture,
    /// B without decimals, KB and MB with one decimal, GB with two.</summary>
    public static string FormatBytes(long bytes)
    {
        // У-3б (Gemini)
        if (bytes < 1024)
            return $"{bytes} B";

        if (bytes < 1024L * 1024L)
            return (bytes / 1024.0).ToString("0.0", CultureInfo.InvariantCulture) + " KB";

        if (bytes < 1024L * 1024L * 1024L)
            return (bytes / (1024.0 * 1024.0)).ToString("0.0", CultureInfo.InvariantCulture) + " MB";

        return (bytes / (1024.0 * 1024.0 * 1024.0)).ToString("0.00", CultureInfo.InvariantCulture) + " GB";
    }
}
