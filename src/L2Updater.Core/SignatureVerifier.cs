namespace L2Updater.Core;

/// <summary>
/// Verifies ECDSA P-256 / SHA-256 signatures of manifest.json and files.json (docs/format.md, "Signature").
/// Use only System.Security.Cryptography (ECDsa, SHA256), no other libraries.
/// </summary>
public sealed class SignatureVerifier
{
    /// <param name="trustedKeys">Public keys in SubjectPublicKeyInfo DER form (ECDsa.ExportSubjectPublicKeyInfo).</param>
    public SignatureVerifier(IEnumerable<byte[]> trustedKeys)
    {
        // У-1б (Gemini): store the keys by KeyIdOf(key). Tests: tests/L2Updater.Core.Tests/SignatureVerifierTests.cs
        throw new NotImplementedException();
    }

    /// <summary>First 16 lowercase hex characters of SHA-256 of the SubjectPublicKeyInfo DER bytes.</summary>
    public static string KeyIdOf(byte[] subjectPublicKeyInfo)
    {
        // У-1б (Gemini)
        throw new NotImplementedException();
    }

    /// <summary>
    /// True only if <paramref name="signatureBase64"/> is a valid IEEE P1363 signature (64 bytes, base64)
    /// of exactly these <paramref name="data"/> bytes by the trusted key <paramref name="keyId"/>.
    /// Unknown key, bad base64, wrong length, wrong signature: false, never an exception.
    /// </summary>
    public bool Verify(byte[] data, string signatureBase64, string keyId)
    {
        // У-1б (Gemini)
        throw new NotImplementedException();
    }
}
