namespace L2Updater.Core;

/// <summary>
/// Signature first, then everything else (docs/format.md, "Update procedure" steps 1–2).
/// </summary>
public static class SignedDocuments
{
    /// <summary>
    /// 1) read only "key_id" from the JSON (no key_id or not JSON → ManifestException);
    /// 2) verifier.Verify(json, signatureBase64, keyId) is false → ManifestException, message contains the word "signature";
    /// 3) ManifestParser.ParseManifest(json);
    /// 4) manifest.Serial &lt; lastAcceptedSerial → ManifestException (equal is allowed).
    /// </summary>
    public static Manifest LoadManifest(byte[] json, string signatureBase64, SignatureVerifier verifier, long lastAcceptedSerial)
    {
        // У-1г (Gemini). Tests: tests/L2Updater.Core.Tests/SignedDocumentsTests.cs
        throw new NotImplementedException();
    }

    /// <summary>
    /// Same steps 1–3 as LoadManifest, then ManifestParser.ParseFileList(json, expectedClient). No serial.
    /// </summary>
    public static FileList LoadFileList(byte[] json, string signatureBase64, SignatureVerifier verifier, string expectedClient)
    {
        // У-1г (Gemini). Tests: tests/L2Updater.Core.Tests/SignedDocumentsTests.cs
        throw new NotImplementedException();
    }
}
