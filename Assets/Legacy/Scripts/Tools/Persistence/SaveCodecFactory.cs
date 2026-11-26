using EatWhat.Tools.Security;
namespace EatWhat.Tools.Persistence
{
    public static class SaveCodecFactory
    {
        public static ISaveCodec CreateForCurrentBuild(IKeyProvider keys)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return new PlainJsonCodec();
#else
            return new EncryptedJsonCodec(new AesCbcHmacCipher(keys));
#endif
        }
    }
}
