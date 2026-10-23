using System.Text.Json;

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
        VerifySignature(json, signatureBase64, verifier);
        var manifest = ManifestParser.ParseManifest(json);
        if (manifest.Serial < lastAcceptedSerial)
            throw new ManifestException($"manifest serial {manifest.Serial} is older than accepted {lastAcceptedSerial}");
        return manifest;
    }

    /// <summary>
    /// Same steps 1–3 as LoadManifest, then ManifestParser.ParseFileList(json, expectedClient). No serial.
    /// </summary>
    public static FileList LoadFileList(byte[] json, string signatureBase64, SignatureVerifier verifier, string expectedClient)
    {
        // У-1г (Gemini). Tests: tests/L2Updater.Core.Tests/SignedDocumentsTests.cs
        VerifySignature(json, signatureBase64, verifier);
        return ManifestParser.ParseFileList(json, expectedClient);
    }

    // Steps 1–2: read only key_id, then check the signature of the exact bytes.
    private static void VerifySignature(byte[] json, string signatureBase64, SignatureVerifier verifier)
    {
        ArgumentNullException.ThrowIfNull(verifier);
        var keyId = ReadKeyIdOnly(json);
        if (!verifier.Verify(json, signatureBase64, keyId))
            throw new ManifestException("bad signature");
    }

    private static string ReadKeyIdOnly(byte[] json)
    {
        if (json is null)
            throw new ManifestException("document is empty");
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("key_id", out var keyId) &&
                keyId.ValueKind == JsonValueKind.String)
                return keyId.GetString()!;
        }
        catch (JsonException)
        {
            throw new ManifestException("document is not valid JSON");
        }
        throw new ManifestException("document has no key_id");
    }
}
