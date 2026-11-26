using System;
namespace EatWhat.Tools.Security
{
    /// <summary>Same authenticated V2 packet, separately derived catalog purpose. No key rotation or IO.</summary>
    public sealed class CatalogBlobCipher
    {
        private readonly IAuthenticatedCipher cipher;
        public CatalogBlobCipher(IKeyProvider master)
        { cipher = new AesCbcHmacCipher(new DerivedKeyProvider(master, "EatWhat/catalog/blob/v2")); }
        public byte[] Encrypt(byte[] plaintext) { return cipher.Encrypt(plaintext).ToArray(); }
        public byte[] Decrypt(byte[] packet) { return cipher.Decrypt(EncryptedBlob.Parse(packet)); }
    }
}
