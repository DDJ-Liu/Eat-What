namespace EatWhat.Tools.Persistence
{
    /// <summary>Shared text/bytes boundary. Serialization, envelopes, paths and storage belong to the caller.</summary>
    public interface ISaveCodec
    {
        byte[] Encode(string json);
        // A failed decode returns false, null JSON and a non-sensitive diagnostic; never partial plaintext.
        bool TryDecode(byte[] encoded, out string json, out string diagnostic);
    }
}
