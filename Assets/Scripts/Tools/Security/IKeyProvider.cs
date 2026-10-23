namespace EatWhat.Tools.Security
{
    public interface IKeyProvider
    {
        // Return a fresh caller-owned 32-byte key buffer. The cipher clears it after use.
        byte[] GetKey();
    }
}
