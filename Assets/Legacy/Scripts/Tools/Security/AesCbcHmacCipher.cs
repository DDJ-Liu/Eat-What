using System;
using System.Security.Cryptography;
namespace EatWhat.Tools.Security
{
    /// <summary>Explicit V2 authenticated fallback. No automatic GCM/plaintext downgrade or file access.</summary>
    public sealed class AesCbcHmacCipher : IAuthenticatedCipher
    {
        private readonly IKeyProvider keyProvider;
        public AesCbcHmacCipher(IKeyProvider keyProvider)
        { if (keyProvider == null) throw new ArgumentNullException("keyProvider"); this.keyProvider = keyProvider; }
        public EncryptedBlob Encrypt(byte[] plaintext)
        {
            if (plaintext == null) throw new ArgumentNullException("plaintext");
            if (plaintext.Length > EncryptedBlob.MaxCiphertextLength - 16) throw new ArgumentException("Plaintext is too large.");
            byte[] encKey = null, macKey = null;
            try
            {
                DeriveKeys(out encKey, out macKey);
                var iv = new byte[EncryptedBlob.IvLength];
                using (var random = RandomNumberGenerator.Create()) random.GetBytes(iv);
                byte[] encrypted;
                using (var aes = CreateAes(encKey, iv))
                using (var transform = aes.CreateEncryptor()) encrypted = transform.TransformFinalBlock(plaintext, 0, plaintext.Length);
                byte[] tag;
                using (var hmac = new HMACSHA256(macKey)) tag = hmac.ComputeHash(EncryptedBlob.AuthenticationInput(iv, encrypted));
                return new EncryptedBlob(iv, tag, encrypted);
            }
            finally { Clear(encKey); Clear(macKey); }
        }
        public byte[] Decrypt(EncryptedBlob blob)
        {
            if (blob == null) throw new ArgumentNullException("blob");
            byte[] encKey = null, macKey = null;
            try
            {
                DeriveKeys(out encKey, out macKey);
                var iv = blob.GetIv(); var cipher = blob.GetCiphertext(); byte[] expected;
                using (var hmac = new HMACSHA256(macKey)) expected = hmac.ComputeHash(EncryptedBlob.AuthenticationInput(iv, cipher));
                var valid = FixedTimeMacEquals(expected, blob.GetMac()); Clear(expected);
                if (!valid) throw new CryptographicException("Authentication failed.");
                // Authentication precedes all decryption/padding processing. Nothing is returned on failure.
                using (var aes = CreateAes(encKey, iv))
                using (var transform = aes.CreateDecryptor()) return transform.TransformFinalBlock(cipher, 0, cipher.Length);
            }
            finally { Clear(encKey); Clear(macKey); }
        }
        private static Aes CreateAes(byte[] key, byte[] iv)
        {
            var aes = Aes.Create();
            try { aes.KeySize = 256; aes.BlockSize = 128; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7; aes.Key = key; aes.IV = iv; return aes; }
            catch { aes.Dispose(); throw; }
        }
        private void DeriveKeys(out byte[] encryptionKey, out byte[] authenticationKey)
        {
            var keys = KeyDerivation.Derive(keyProvider, "EatWhat.Toolkit.Security/v2/encryption", "EatWhat.Toolkit.Security/v2/authentication");
            encryptionKey = keys[0]; authenticationKey = keys[1];
        }
        private static bool FixedTimeMacEquals(byte[] left, byte[] right)
        {
            if (left.Length != EncryptedBlob.MacLength || right.Length != EncryptedBlob.MacLength) return false;
            var difference = 0;
            for (var i = 0; i < EncryptedBlob.MacLength; i++) difference |= left[i] ^ right[i];
            return difference == 0;
        }
        private static void Clear(byte[] bytes) { if (bytes != null) Array.Clear(bytes, 0, bytes.Length); }
    }
}
