namespace L2Updater.App;

// Written by Claude, do not change. Values a build of the updater is made with.
internal static class AppConfig
{
    /// <summary>Used when updater.ini has no manifest_url.</summary>
    public const string DefaultManifestUrl = "https://l2zalupa.com/updater/manifest.json";

    /// <summary>
    /// Public keys (SubjectPublicKeyInfo DER, base64) the updater trusts for manifest.json and files.json.
    /// The private keys never leave the publisher's machine.
    /// TEST KEY (key_id 923a1f0c7dd9e1ae): for the test stand only, replace with the release key before giving the updater to players.
    /// </summary>
    public static readonly string[] TrustedKeys =
    [
        "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEk5vQLowa5OiwaDhQo4PXAQq/z447S4J3qMe+ztWyIrEXGkdtBUc1jMziN5C7c9PfUN2BrB0eXUS3fpU4/GUHqA==",
    ];

    public static byte[][] TrustedKeyBytes() => TrustedKeys.Select(Convert.FromBase64String).ToArray();
}
