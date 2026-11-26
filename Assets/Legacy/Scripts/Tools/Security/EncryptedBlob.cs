using System;
namespace EatWhat.Tools.Security
{
    /// <summary>V2: EWSB | version(2) | ciphertext length(uint32 BE) | IV(16) | HMAC(32) | ciphertext.</summary>
    public sealed class EncryptedBlob
    {
        public const byte CurrentVersion = 2;
        public const int HeaderLength = 9;
        public const int IvLength = 16;
        public const int MacLength = 32;
        public const int MaxCiphertextLength = 16 * 1024 * 1024;
        private readonly byte[] iv;
        private readonly byte[] mac;
        private readonly byte[] ciphertext;
        public byte Version { get { return CurrentVersion; } }
        public int CiphertextLength { get { return ciphertext.Length; } }
        internal EncryptedBlob(byte[] iv, byte[] mac, byte[] ciphertext)
        {
            if (iv == null || iv.Length != IvLength || mac == null || mac.Length != MacLength || !ValidCiphertextLength(ciphertext == null ? 0 : ciphertext.Length))
                throw new FormatException("Invalid encrypted container.");
            this.iv = (byte[])iv.Clone(); this.mac = (byte[])mac.Clone(); this.ciphertext = (byte[])ciphertext.Clone();
        }
        public byte[] GetIv() { return (byte[])iv.Clone(); }
        public byte[] GetMac() { return (byte[])mac.Clone(); }
        public byte[] GetCiphertext() { return (byte[])ciphertext.Clone(); }
        public byte[] ToArray()
        {
            var result = new byte[HeaderLength + IvLength + MacLength + ciphertext.Length];
            WriteHeader(result, ciphertext.Length);
            Buffer.BlockCopy(iv, 0, result, HeaderLength, IvLength);
            Buffer.BlockCopy(mac, 0, result, HeaderLength + IvLength, MacLength);
            Buffer.BlockCopy(ciphertext, 0, result, HeaderLength + IvLength + MacLength, ciphertext.Length);
            return result;
        }
        public static EncryptedBlob Parse(byte[] bytes)
        {
            if (bytes == null || bytes.Length < HeaderLength + IvLength + MacLength + 16 ||
                bytes[0] != (byte)'E' || bytes[1] != (byte)'W' || bytes[2] != (byte)'S' || bytes[3] != (byte)'B' || bytes[4] != CurrentVersion)
                throw new FormatException("Invalid encrypted container.");
            var length = ((uint)bytes[5] << 24) | ((uint)bytes[6] << 16) | ((uint)bytes[7] << 8) | bytes[8];
            if (length > MaxCiphertextLength || !ValidCiphertextLength((int)length) || bytes.Length != HeaderLength + IvLength + MacLength + (int)length)
                throw new FormatException("Invalid encrypted container.");
            var iv = new byte[IvLength]; var mac = new byte[MacLength]; var cipher = new byte[(int)length];
            Buffer.BlockCopy(bytes, HeaderLength, iv, 0, IvLength);
            Buffer.BlockCopy(bytes, HeaderLength + IvLength, mac, 0, MacLength);
            Buffer.BlockCopy(bytes, HeaderLength + IvLength + MacLength, cipher, 0, cipher.Length);
            return new EncryptedBlob(iv, mac, cipher);
        }
        internal static byte[] AuthenticationInput(byte[] iv, byte[] cipher)
        {
            var result = new byte[HeaderLength + IvLength + cipher.Length];
            WriteHeader(result, cipher.Length); Buffer.BlockCopy(iv, 0, result, HeaderLength, IvLength);
            Buffer.BlockCopy(cipher, 0, result, HeaderLength + IvLength, cipher.Length); return result;
        }
        private static bool ValidCiphertextLength(int length)
        { return length >= 16 && length <= MaxCiphertextLength && length % 16 == 0; }
        private static void WriteHeader(byte[] bytes, int length)
        {
            bytes[0] = (byte)'E'; bytes[1] = (byte)'W'; bytes[2] = (byte)'S'; bytes[3] = (byte)'B'; bytes[4] = CurrentVersion;
            bytes[5] = (byte)((uint)length >> 24); bytes[6] = (byte)((uint)length >> 16); bytes[7] = (byte)((uint)length >> 8); bytes[8] = (byte)length;
        }
    }
}
