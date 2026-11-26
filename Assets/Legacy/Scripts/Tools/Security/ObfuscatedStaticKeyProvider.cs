namespace EatWhat.Tools.Security
{
    /// <summary>Extraction deterrence only. Client code contains sufficient information to recover the key.</summary>
    public sealed class ObfuscatedStaticKeyProvider : IKeyProvider
    {
        private static readonly uint[] PartA = { 0x777015CCu, 0x74258190u, 0x9BE0A8D6u, 0x66E4E6D0u, 0x4CEA598Du, 0xD50F736Eu, 0x3EC0EBC8u, 0x9D1B3204u };
        private static readonly uint[] PartB = { 0xE46B8072u, 0x1C8BC92Du, 0xCBE6439Au, 0x2EED164Eu, 0xC9A23084u, 0xC96369DEu, 0xC9BFA9C1u, 0x58999D75u };
        public byte[] GetKey()
        {
            var result = new byte[32];
            for (var i = 0; i < 8; i++)
            {
                var word = PartA[i] ^ PartB[7 - i];
                var shift = i + 1; word = (word << shift) | (word >> (32 - shift));
                for (var j = 0; j < 4; j++) result[i * 4 + j] = (byte)(word >> (8 * j));
            }
            return result;
        }
    }
}
