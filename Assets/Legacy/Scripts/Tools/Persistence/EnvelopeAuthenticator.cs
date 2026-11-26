using System;
using System.Security.Cryptography;
using System.Text;
using EatWhat.Tools.Security;
namespace EatWhat.Tools.Persistence
{
    public sealed class EnvelopeAuthenticator
    {
        private readonly IKeyProvider envelopeKey;
        public EnvelopeAuthenticator(IKeyProvider master)
        { envelopeKey = new DerivedKeyProvider(master, "EatWhat.Toolkit.Persistence/save-envelope-authentication/v1"); }
        public SaveEnvelope Sign(SaveEnvelope envelope) { return envelope.WithHmac(Convert.ToBase64String(Compute(envelope))); }
        public bool Verify(SaveEnvelope envelope)
        {
            byte[] actual;
            try { actual = Convert.FromBase64String(envelope.Hmac); } catch (FormatException) { return false; }
            if (actual.Length != 32 || Convert.ToBase64String(actual) != envelope.Hmac) return false;
            var expected = Compute(envelope); var difference = 0;
            for (var i = 0; i < 32; i++) difference |= actual[i] ^ expected[i];
            return difference == 0;
        }
        private byte[] Compute(SaveEnvelope envelope)
        {
            var key = envelopeKey.GetKey();
            try { using (var hmac = new HMACSHA256(key)) return hmac.ComputeHash(Encoding.UTF8.GetBytes("EatWhat.SaveEnvelope/v1\n" + envelope.AuthenticationJson())); }
            finally { Array.Clear(key, 0, key.Length); }
        }
    }
}
