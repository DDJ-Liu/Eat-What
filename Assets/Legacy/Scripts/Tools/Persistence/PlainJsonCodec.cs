using System;
using System.Text;
namespace EatWhat.Tools.Persistence
{
    public sealed class PlainJsonCodec : ISaveCodec
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        public byte[] Encode(string json) { if (json == null) throw new ArgumentNullException("json"); return Utf8.GetBytes(json); }
        public bool TryDecode(byte[] encoded, out string json, out string diagnostic)
        {
            json = null; diagnostic = null;
            try { if (encoded != null) { json = Utf8.GetString(encoded); return true; } } catch (ArgumentException) { }
            diagnostic = "Invalid UTF8 save data."; return false;
        }
    }
}
