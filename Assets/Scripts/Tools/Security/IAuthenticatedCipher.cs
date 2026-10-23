namespace EatWhat.Tools.Security
{
    public interface IAuthenticatedCipher
    {
        EncryptedBlob Encrypt(byte[] plaintext);
        byte[] Decrypt(EncryptedBlob blob);
    }
}
