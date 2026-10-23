using System;
using System.Security.Cryptography;
using System.Text;
using EatWhat.Tools.Security;
namespace EatWhat.Tools.Persistence
{
    public sealed class EncryptedJsonCodec : ISaveCodec
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        private readonly IAuthenticatedCipher cipher;
        public EncryptedJsonCodec(IAuthenticatedCipher cipher)
        { if (cipher == null) throw new ArgumentNullException("cipher"); this.cipher = cipher; }
        public byte[] Encode(string json)
        {
            if (json == null) throw new ArgumentNullException("json");
            var plaintext = Utf8.GetBytes(json);
            try { return cipher.Encrypt(plaintext).ToArray(); }
            finally { Array.Clear(plaintext, 0, plaintext.Length); }
        }
        public bool TryDecode(byte[] encoded, out string json, out string diagnostic)
        {
            json = null; diagnostic = null; byte[] plaintext = null;
            try
            { plaintext = cipher.Decrypt(EncryptedBlob.Parse(encoded)); json = Utf8.GetString(plaintext); return true; }
            catch (FormatException) { }
            catch (CryptographicException) { }
            catch (ArgumentException) { }
            finally { if (plaintext != null) Array.Clear(plaintext, 0, plaintext.Length); }
            json = null; diagnostic = "Invalid or unauthenticated encrypted data."; return false;
        }
    }
}
