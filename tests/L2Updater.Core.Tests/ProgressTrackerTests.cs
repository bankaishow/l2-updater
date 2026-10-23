using L2Updater.Core;
using Xunit;

namespace L2Updater.Core.Tests;

// Written by Claude. Gemini: do not change this file, change the code until all tests pass.
public class ProgressTrackerTests
{
    private sealed class FakeClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;

        public void Advance(double seconds) => Now = Now.AddSeconds(seconds);
    }

    [Fact]
    public void Nothing_reported_is_zero()
    {
        var t = new ProgressTracker(new FakeClock());
        Assert.Equal(0, t.Percent);
        Assert.Equal(0, t.BytesPerSecond);
        Assert.Null(t.Remaining);
    }

    [Theory]
    [InlineData(0, 1000, 0)]
    [InlineData(1, 1000, 0)]
    [InlineData(999, 1000, 99)]
    [InlineData(1000, 1000, 100)]
    [InlineData(500, 1000, 50)]
    public void Percent_is_rounded_down(long done, long total, int expected)
    {
        var t = new ProgressTracker(new FakeClock());
        t.Report(done, total);
        Assert.Equal(expected, t.Percent);
    }

    [Fact]
    public void Nothing_to_download_is_100_percent()
    {
        var t = new ProgressTracker(new FakeClock());
        t.Report(0, 0);
        Assert.Equal(100, t.Percent);
    }

    [Fact]
    public void Speed_and_remaining_time()
    {
        var clock = new FakeClock();
        var t = new ProgressTracker(clock);
        t.Report(0, 10_000);
        clock.Advance(2);
        t.Report(2_000, 10_000);
        Assert.Equal(1_000, t.BytesPerSecond, 3);
        Assert.Equal(TimeSpan.FromSeconds(8), t.Remaining);
    }

    [Fact]
    public void Speed_uses_only_last_five_seconds()
    {
        var clock = new FakeClock();
        var t = new ProgressTracker(clock);
        t.Report(0, 1_000_000);
        clock.Advance(10);
        t.Report(1_000, 1_000_000);     // slow start: 100 B/s
        clock.Advance(1);
        t.Report(11_000, 1_000_000);
        clock.Advance(1);
        t.Report(21_000, 1_000_000);    // now 10 000 B/s
        Assert.Equal(10_000, t.BytesPerSecond, 3);
    }

    [Fact]
    public void Two_newest_samples_are_kept_even_if_old()
    {
        var clock = new FakeClock();
        var t = new ProgressTracker(clock);
        t.Report(0, 1000);
        clock.Advance(20);
        t.Report(200, 1000);
        Assert.Equal(10, t.BytesPerSecond, 3);
    }

    [Fact]
    public void Same_moment_gives_zero_speed()
    {
        var t = new ProgressTracker(new FakeClock());
        t.Report(0, 1000);
        t.Report(500, 1000);
        Assert.Equal(0, t.BytesPerSecond);
        Assert.Null(t.Remaining);
    }

    [Fact]
    public void Smaller_value_starts_over()
    {
        var clock = new FakeClock();
        var t = new ProgressTracker(clock);
        t.Report(0, 1000);
        clock.Advance(1);
        t.Report(900, 1000);
        clock.Advance(1);
        t.Report(100, 1000);
        Assert.Equal(0, t.BytesPerSecond);
        Assert.Equal(10, t.Percent);
    }

    [Fact]
    public void Done_has_zero_remaining()
    {
        var clock = new FakeClock();
        var t = new ProgressTracker(clock);
        t.Report(0, 1000);
        clock.Advance(1);
        t.Report(1000, 1000);
        Assert.Equal(TimeSpan.Zero, t.Remaining);
    }

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(12 * 1024 * 1024, "12.0 MB")]
    [InlineData(1342177280, "1.25 GB")]
    public void FormatBytes(long bytes, string expected)
    {
        Assert.Equal(expected, ProgressTracker.FormatBytes(bytes));
    }
}
