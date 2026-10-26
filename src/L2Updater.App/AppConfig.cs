namespace L2Updater.App;

// Written by Claude, do not change. Values a build of the updater is made with.
internal static class AppConfig
{
    /// <summary>Used when updater.ini has no manifest_url.</summary>
    public const string DefaultManifestUrl = "https://dimythras.com/updater/manifest.json";

    /// <summary>
    /// Public keys (SubjectPublicKeyInfo DER, base64) the updater trusts for manifest.json and files.json.
    /// The private keys never leave the publisher's machine.
    /// Release key (generated 2026-10-26 on the owner's Windows machine).
    /// </summary>
    public static readonly string[] TrustedKeys =
    [
        "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEmYpYFsND7/qmyX2bbs2r7arquuwcj85pFIZmklnZqFVdTVz2QPjfp+xgJrr9Vjm9jHLcfuKcKLU3e/y3qshQIQ==",
    ];

    /// <summary>Build version for the window title (the CI build sets 0.1.&lt;run number&gt;).</summary>
    public static string BuildVersion =>
        System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?";

    public static byte[][] TrustedKeyBytes() => TrustedKeys.Select(Convert.FromBase64String).ToArray();
}
