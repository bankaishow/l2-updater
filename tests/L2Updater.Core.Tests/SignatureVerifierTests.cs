using System.Security.Cryptography;
using System.Text;
using L2Updater.Core;
using Xunit;

namespace L2Updater.Core.Tests;

// Written by Claude. Gemini: do not change this file, change the code until all tests pass.
public class SignatureVerifierTests
{
    private static readonly byte[] Data = Encoding.UTF8.GetBytes("{\"format\":1,\"serial\":42}");

    [Fact]
    public void KeyId_is_first_16_lowercase_hex_of_sha256_of_public_key()
    {
        using var key = new TestKey();
        var expected = Convert.ToHexString(SHA256.HashData(key.PublicKey))[..16].ToLowerInvariant();
        Assert.Equal(expected, SignatureVerifier.KeyIdOf(key.PublicKey));
        Assert.Equal(16, SignatureVerifier.KeyIdOf(key.PublicKey).Length);
    }

    [Fact]
    public void Correct_signature_is_accepted()
    {
        using var key = new TestKey();
        var verifier = new SignatureVerifier([key.PublicKey]);
        Assert.True(verifier.Verify(Data, key.Sign(Data), key.KeyId));
    }

    [Fact]
    public void Any_of_several_trusted_keys_works()
    {
        using var oldKey = new TestKey();
        using var newKey = new TestKey();
        var verifier = new SignatureVerifier([oldKey.PublicKey, newKey.PublicKey]);
        Assert.True(verifier.Verify(Data, oldKey.Sign(Data), oldKey.KeyId));
        Assert.True(verifier.Verify(Data, newKey.Sign(Data), newKey.KeyId));
    }

    [Fact]
    public void Signature_by_untrusted_key_is_rejected()
    {
        using var trusted = new TestKey();
        using var attacker = new TestKey();
        var verifier = new SignatureVerifier([trusted.PublicKey]);
        Assert.False(verifier.Verify(Data, attacker.Sign(Data), attacker.KeyId));
        Assert.False(verifier.Verify(Data, attacker.Sign(Data), trusted.KeyId));
    }

    [Fact]
    public void One_changed_byte_is_rejected()
    {
        using var key = new TestKey();
        var verifier = new SignatureVerifier([key.PublicKey]);
        var signature = key.Sign(Data);
        var changed = (byte[])Data.Clone();
        changed[^2] ^= 1;
        Assert.False(verifier.Verify(changed, signature, key.KeyId));
    }

    [Fact]
    public void Extra_whitespace_is_rejected()
    {
        using var key = new TestKey();
        var verifier = new SignatureVerifier([key.PublicKey]);
        var signature = key.Sign(Data);
        var withSpace = Data.Concat(Encoding.UTF8.GetBytes("\n")).ToArray();
        Assert.False(verifier.Verify(withSpace, signature, key.KeyId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not base64 !!!")]
    [InlineData("AAAA")]
    public void Broken_signature_returns_false_without_exception(string signature)
    {
        using var key = new TestKey();
        var verifier = new SignatureVerifier([key.PublicKey]);
        Assert.False(verifier.Verify(Data, signature, key.KeyId));
    }

    [Fact]
    public void Der_signature_is_rejected()
    {
        // only the 64-byte P1363 form is allowed
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var publicKey = ecdsa.ExportSubjectPublicKeyInfo();
        var der = Convert.ToBase64String(ecdsa.SignData(Data, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence));
        var verifier = new SignatureVerifier([publicKey]);
        Assert.False(verifier.Verify(Data, der, SignatureVerifier.KeyIdOf(publicKey)));
    }

    [Fact]
    public void Unknown_key_id_returns_false()
    {
        using var key = new TestKey();
        var verifier = new SignatureVerifier([key.PublicKey]);
        Assert.False(verifier.Verify(Data, key.Sign(Data), "0000000000000000"));
    }
}
