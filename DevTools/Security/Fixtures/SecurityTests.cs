using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using EatWhat.Tools.Security;
using EatWhat.Tools.Persistence;

public static class ToolkitSecurityTests
{
    // Public, synthetic fixture material; never use this provider for production.
    private sealed class FixtureKeyProvider : IKeyProvider
    {
        private readonly byte offset;
        public FixtureKeyProvider(byte offset = 0) { this.offset = offset; }
        public byte[] GetKey() { var bytes = new byte[32]; for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)(i + offset); return bytes; }
    }
    private sealed class ShortKeyProvider : IKeyProvider { public byte[] GetKey() { return new byte[16]; } }
    private sealed class PlainCodecFixture : ISaveCodec
    {
        public byte[] Encode(string json) { return Encoding.UTF8.GetBytes(json); }
        public bool TryDecode(byte[] bytes, out string json, out string diagnostic)
        { diagnostic = null; json = Encoding.UTF8.GetString(bytes); return true; }
    }
    private static readonly string CrossPlain = "{\"crossRuntime\":\"Unity-Mono-and-host\"}";
    public static IList<string> PassedCases { get; private set; }
    private static void Check(bool passed, string id)
    { if (!passed) throw new Exception(id); PassedCases.Add(id); }
    private static EncryptedJsonCodec NewCodec(byte offset = 0)
    { return new EncryptedJsonCodec(new AesCbcHmacCipher(new FixtureKeyProvider(offset))); }
    private static bool Equal(byte[] a, byte[] b)
    { if (a.Length != b.Length) return false; for (var i = 0; i < a.Length; i++) if (a[i] != b[i]) return false; return true; }
    private static byte[] Copy(byte[] bytes) { return (byte[])bytes.Clone(); }
    public static byte[] EncodeCrossSample() { return NewCodec().Encode(CrossPlain); }
    public static bool DecodeCrossSample(byte[] bytes)
    { string json, diagnostic; return NewCodec().TryDecode(bytes, out json, out diagnostic) && json == CrossPlain && diagnostic == null; }
    public static string Run()
    {
        PassedCases = new List<string>();
        ISaveCodec codec = NewCodec(); string decoded, diagnostic;
        foreach (var json in new[] { "", "{}", "{\"message\":\"中文与emoji🍜\",\"line\":\"a\\nb\"}", new string('x', 4097) })
        {
            var encoded = codec.Encode(json);
            Check(codec.TryDecode(encoded, out decoded, out diagnostic) && decoded == json && diagnostic == null, "roundtrip UTF8/text length " + json.Length);
        }
        var cipher = new AesCbcHmacCipher(new FixtureKeyProvider());
        foreach (var length in new[] { 0, 1, 15, 16, 17, 255 })
        {
            var input = new byte[length]; for (var i = 0; i < length; i++) input[i] = (byte)i;
            Check(Equal(input, cipher.Decrypt(cipher.Encrypt(input))), "binary roundtrip length " + length);
        }
        var packet = codec.Encode("{}"); var blob = EncryptedBlob.Parse(packet);
        Check(packet.Length == 73 && blob.Version == 2 && blob.GetIv().Length == 16 && blob.GetMac().Length == 32 && blob.CiphertextLength == 16, "v2 uses IV16 MAC32 and padded cipher16");
        Check(packet[0] == 69 && packet[1] == 87 && packet[2] == 83 && packet[3] == 66 && packet[4] == 2 && packet[5] == 0 && packet[6] == 0 && packet[7] == 0 && packet[8] == 16, "fixed magic version and uint32 big-endian length golden bytes");
        Check(cipher.Encrypt(new byte[16]).CiphertextLength == 32, "PKCS7 full block is padded with another block");
        Check(Equal(packet, blob.ToArray()), "binary container stable encode parse encode");
        var changed = Copy(packet); var detached = EncryptedBlob.Parse(changed); changed[9] ^= 1;
        Check(Equal(detached.ToArray(), packet), "parser does not alias input bytes");
        var iv = blob.GetIv(); iv[0] ^= 1; var mac = blob.GetMac(); mac[0] ^= 1; var ciphertext = blob.GetCiphertext(); ciphertext[0] ^= 1;
        Check(Equal(blob.ToArray(), packet), "container exports detached arrays");
        Check(!NewCodec(1).TryDecode(packet, out decoded, out diagnostic) && decoded == null && diagnostic == "Invalid or unauthenticated encrypted data.", "wrong key rejects without plaintext or sensitive diagnostic");
        for (var i = 0; i < packet.Length; i++)
        {
            changed = Copy(packet); changed[i] ^= 1;
            Check(!codec.TryDecode(changed, out decoded, out diagnostic) && decoded == null && diagnostic == "Invalid or unauthenticated encrypted data.", "single byte tamper rejected at offset " + i);
        }
        for (var length = 0; length < packet.Length; length++)
        {
            changed = new byte[length]; Buffer.BlockCopy(packet, 0, changed, 0, length);
            Check(!codec.TryDecode(changed, out decoded, out diagnostic) && decoded == null, "truncation rejected at length " + length);
        }
        changed = new byte[packet.Length + 1]; Buffer.BlockCopy(packet, 0, changed, 0, packet.Length);
        Check(!codec.TryDecode(changed, out decoded, out diagnostic), "trailing bytes rejected");
        changed = Copy(packet); changed[4] = 1; Check(!codec.TryDecode(changed, out decoded, out diagnostic), "reserved GCM version1 never silently interpreted as CBC");
        changed = Copy(packet); changed[4] = 255; Check(!codec.TryDecode(changed, out decoded, out diagnostic), "unknown version rejected");
        changed = Copy(packet); for (var i = 5; i < 9; i++) changed[i] = 255;
        Check(!codec.TryDecode(changed, out decoded, out diagnostic), "oversize uint32 declaration rejected before allocation");
        changed = Copy(packet); changed[8] = 15; Check(!codec.TryDecode(changed, out decoded, out diagnostic), "non block-aligned ciphertext rejected");
        Check(!codec.TryDecode(null, out decoded, out diagnostic) && decoded == null, "null packet fails safely");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < 128; i++) Check(seen.Add(Convert.ToBase64String(EncryptedBlob.Parse(codec.Encode("same plaintext")).GetIv())), "secure random IV uniqueness sample " + i);
        var provider = new ObfuscatedStaticKeyProvider(); var key1 = provider.GetKey(); var key2 = provider.GetKey();
        Check(key1.Length == 32 && Equal(key1, key2) && !object.ReferenceEquals(key1, key2), "split provider stable fresh256 bit buffers");
        key1[0] ^= 1; var key3 = provider.GetKey(); Check(Equal(key2, key3), "caller key mutation does not corrupt provider");
        Array.Clear(key1, 0, key1.Length); Array.Clear(key2, 0, key2.Length); Array.Clear(key3, 0, key3.Length);
        var productionCodec = new EncryptedJsonCodec(new AesCbcHmacCipher(provider));
        Check(productionCodec.TryDecode(productionCodec.Encode("provider smoke fixture"), out decoded, out diagnostic) && decoded == "provider smoke fixture", "split provider integration without emitting key material");
        var shortRejected = false; try { new AesCbcHmacCipher(new ShortKeyProvider()).Encrypt(new byte[0]); } catch (CryptographicException) { shortRejected = true; }
        Check(shortRejected, "non256 bit key provider rejected");
        var invalidUtf8 = cipher.Encrypt(new byte[] { 255, 255 }).ToArray();
        Check(!codec.TryDecode(invalidUtf8, out decoded, out diagnostic) && decoded == null, "authenticated but invalid UTF8 rejected without partial text");
        var invalidString = false; try { codec.Encode("\uD800"); } catch (ArgumentException) { invalidString = true; }
        Check(invalidString, "invalid UTF16 input refuses lossy UTF8 encoding");
        ISaveCodec plainFixture = new PlainCodecFixture(); var fixture = "{\"profile\":1}";
        Check(plainFixture.TryDecode(plainFixture.Encode(fixture), out decoded, out diagnostic) && decoded == fixture && codec.TryDecode(codec.Encode(fixture), out decoded, out diagnostic) && decoded == fixture, "shared ISaveCodec can be substituted without a serialization dependency");
        Check(!codec.TryDecode(plainFixture.Encode(fixture), out decoded, out diagnostic), "encrypted codec never falls back to plaintext");
        return "PASS: Toolkit.Security " + PassedCases.Count + " assertions";
    }
    public static int Main(string[] args)
    {
        try
        {
            Console.WriteLine(Run());
            if (args.Length == 1) Check(DecodeCrossSample(Convert.FromBase64String(args[0])), "host to bundled Mono packet interoperability");
            Console.WriteLine("SECURITY_ASSERTIONS=" + PassedCases.Count);
            Console.WriteLine("CROSS_BLOB=" + Convert.ToBase64String(EncodeCrossSample()));
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("SECURITY_TEST_FAILED=" + error.GetType().Name + ":" + error.Message); return 1; }
    }
}
