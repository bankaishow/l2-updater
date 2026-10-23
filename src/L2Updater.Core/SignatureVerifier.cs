using System.Security.Cryptography;

namespace L2Updater.Core;

/// <summary>
/// Verifies ECDSA P-256 / SHA-256 signatures of manifest.json and files.json (docs/format.md, "Signature").
/// Use only System.Security.Cryptography (ECDsa, SHA256), no other libraries.
/// </summary>
public sealed class SignatureVerifier
{
    // P1363 signature size for P-256: r and s, 32 bytes each.
    private const int SignatureLength = 64;

    private readonly Dictionary<string, byte[]> _keys = new(StringComparer.Ordinal);

    /// <param name="trustedKeys">Public keys in SubjectPublicKeyInfo DER form (ECDsa.ExportSubjectPublicKeyInfo).</param>
    public SignatureVerifier(IEnumerable<byte[]> trustedKeys)
    {
        // У-1б (Gemini). Tests: tests/L2Updater.Core.Tests/SignatureVerifierTests.cs
        ArgumentNullException.ThrowIfNull(trustedKeys);
        foreach (var key in trustedKeys)
        {
            // Fail early on a broken or non-P-256 trusted key.
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(key, out _);
            if (ecdsa.KeySize != 256)
                throw new ArgumentException("Trusted key is not ECDSA P-256.", nameof(trustedKeys));
            _keys[KeyIdOf(key)] = (byte[])key.Clone();
        }
    }

    /// <summary>First 16 lowercase hex characters of SHA-256 of the SubjectPublicKeyInfo DER bytes.</summary>
    public static string KeyIdOf(byte[] subjectPublicKeyInfo)
    {
        // У-1б (Gemini)
        var hash = SHA256.HashData(subjectPublicKeyInfo);
        return Convert.ToHexString(hash, 0, 8).ToLowerInvariant();
    }

    /// <summary>
    /// True only if <paramref name="signatureBase64"/> is a valid IEEE P1363 signature (64 bytes, base64)
    /// of exactly these <paramref name="data"/> bytes by the trusted key <paramref name="keyId"/>.
    /// Unknown key, bad base64, wrong length, wrong signature: false, never an exception.
    /// </summary>
    public bool Verify(byte[] data, string signatureBase64, string keyId)
    {
        // У-1б (Gemini)
        if (data is null || string.IsNullOrEmpty(signatureBase64) || keyId is null)
            return false;
        if (!_keys.TryGetValue(keyId, out var publicKey))
            return false;

        // Exactly 64 bytes: DER and other sizes are rejected.
        var signature = new byte[SignatureLength + 1];
        if (!Convert.TryFromBase64String(signatureBase64, signature, out var written) || written != SignatureLength)
            return false;

        try
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(publicKey, out _);
            return ecdsa.VerifyData(data, signature.AsSpan(0, SignatureLength), HashAlgorithmName.SHA256,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
