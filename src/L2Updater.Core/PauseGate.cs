namespace L2Updater.Core;

/// <summary>
/// "Pause" button of the window. The downloader checks it between chunks: while paused it closes the connection
/// (the partial file stays) and waits; after Resume it asks the server again with Range from the partial file's length.
/// Thread safe: the window calls Pause/Resume, the download calls IsPaused/WaitAsync.
/// </summary>
public sealed class PauseGate
{
    private TaskCompletionSource? _resume;

    public bool IsPaused => Volatile.Read(ref _resume) != null;

    /// <summary>Pauses (does nothing if already paused).</summary>
    public void Pause() =>
        Interlocked.CompareExchange(ref _resume, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously), null);

    /// <summary>Continues (does nothing if not paused).</summary>
    public void Resume() => Interlocked.Exchange(ref _resume, null)?.TrySetResult();

    /// <summary>Completes at once if not paused, else when Resume is called. Cancellation → OperationCanceledException.</summary>
    public Task WaitAsync(CancellationToken cancellationToken = default)
    {
        var current = Volatile.Read(ref _resume);
        return current == null ? Task.CompletedTask : current.Task.WaitAsync(cancellationToken);
    }
}
