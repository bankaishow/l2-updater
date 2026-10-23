namespace L2Updater.Core;

// Written by Claude, do not change.

/// <summary>Manifest or file list is malformed, unsigned, badly signed or older than the last accepted one.</summary>
public sealed class ManifestException(string message) : Exception(message);

/// <summary>A path from a file list is not allowed (see PathGuard).</summary>
public sealed class UnsafePathException(string message) : Exception(message);
