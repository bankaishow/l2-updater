using System.Security.Cryptography;
using System.Text;

namespace L2Updater.Core.Tests;

// Written by Claude. Test-only keys, generated on every run. No real key is ever stored in this repository.
internal sealed class TestKey : IDisposable
{
    private readonly ECDsa _ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

    public byte[] PublicKey => _ecdsa.ExportSubjectPublicKeyInfo();

    public string KeyId => Convert.ToHexString(SHA256.HashData(PublicKey))[..16].ToLowerInvariant();

    public string Sign(byte[] data) =>
        Convert.ToBase64String(_ecdsa.SignData(data, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));

    public string Sign(string text) => Sign(Encoding.UTF8.GetBytes(text));

    public void Dispose() => _ecdsa.Dispose();
}
