using System;
using System.Security.Cryptography;
using System.Text;
namespace EatWhat.Tools.Security
{
    internal static class KeyDerivation
    {
        internal static byte[][] Derive(IKeyProvider provider, params string[] labels)
        {
            var keys = new byte[labels.Length][]; byte[] master = null, prk = null;
            try
            {
                master = provider.GetKey();
                if (master == null || master.Length != 32) throw new CryptographicException("A 256-bit key is required.");
                using (var extract = new HMACSHA256(new byte[32])) prk = extract.ComputeHash(master);
                for (var i = 0; i < labels.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(labels[i])) throw new ArgumentException("A key purpose is required.");
                    var info = Encoding.UTF8.GetBytes(labels[i]); var block = new byte[info.Length + 1];
                    Buffer.BlockCopy(info, 0, block, 0, info.Length); block[block.Length - 1] = 1;
                    using (var expand = new HMACSHA256(prk)) keys[i] = expand.ComputeHash(block);
                }
                return keys;
            }
            catch { foreach (var key in keys) if (key != null) Array.Clear(key, 0, key.Length); throw; }
            finally { if (master != null) Array.Clear(master, 0, master.Length); if (prk != null) Array.Clear(prk, 0, prk.Length); }
        }
    }
}
