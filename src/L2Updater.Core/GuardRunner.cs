using System.ComponentModel;
using System.Diagnostics;

namespace L2Updater.Core;

/// <summary>What the window does after the guard program has run.</summary>
public enum GuardDecision
{
    Allow,
    BlockVirtualMachine,
    BlockUntrustedServer,
    BlockRejected
}

/// <summary>
/// Runs the optional guard program of a client (manifest field "guard") before the game and reads its exit code.
/// The program itself is a separate file delivered through files.json; this class only starts it and reads the code.
/// Exit codes: 0 ok, 10 virtual machine, 20 weak identifier, 30 server not trusted, 40 server said no,
/// 50 server unreachable, 60 switched off. Anything else (also a crash, a timeout, a missing file) lets the game start:
/// the server decides who may log in.
/// </summary>
public static class GuardRunner
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How to start the guard program: FileName = PathGuard.Resolve(gameRoot, client.Guard), no window, no shell.
    /// No guard in the manifest, or the file is not there → null (the game starts without the check).
    /// </summary>
    public static ProcessStartInfo? BuildStartInfo(ClientInfo client, string gameRoot)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(gameRoot);
        if (client.Guard is null)
            return null;

        var path = PathGuard.Resolve(gameRoot, client.Guard);
        if (!File.Exists(path))
            return null;

        return new ProcessStartInfo
        {
            FileName = path,
            WorkingDirectory = Path.GetDirectoryName(path) ?? gameRoot,
            UseShellExecute = false,
            CreateNoWindow = true
        };
    }

    /// <summary>
    /// Starts the program and waits for it, at most <paramref name="timeout"/> (then the whole process tree is killed).
    /// Returns the exit code, or null if it could not be started or ran too long. Its output is read and thrown away.
    /// Cancelling <paramref name="cancellationToken"/> kills the program and throws OperationCanceledException.
    /// </summary>
    public static async Task<int?> RunAsync(ProcessStartInfo info, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(info);
        info.RedirectStandardOutput = true;
        info.RedirectStandardError = true;

        Process? process;
        try
        {
            process = Process.Start(info);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException)
        {
            return null;
        }
        if (process is null)
            return null;

        using (process)
        {
            // Read the output so a full pipe never blocks the program.
            _ = process.StandardOutput.ReadToEndAsync();
            _ = process.StandardError.ReadToEndAsync();

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(timeout);
            try
            {
                await process.WaitForExitAsync(timeoutSource.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); }
                catch (Exception ex) when (ex is InvalidOperationException or Win32Exception) { }
                cancellationToken.ThrowIfCancellationRequested();
                return null;
            }
            return process.ExitCode;
        }
    }

    /// <summary>10 → BlockVirtualMachine, 30 → BlockUntrustedServer, 40 → BlockRejected, everything else (also null) → Allow.</summary>
    public static GuardDecision Decide(int? exitCode) => exitCode switch
    {
        10 => GuardDecision.BlockVirtualMachine,
        30 => GuardDecision.BlockUntrustedServer,
        40 => GuardDecision.BlockRejected,
        _ => GuardDecision.Allow
    };
}
