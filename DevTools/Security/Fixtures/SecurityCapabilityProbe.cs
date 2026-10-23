using System;
using System.Security.Cryptography;
public static class SecurityCapabilityProbe
{
    public static int Main()
    {
        try { Probe(); Console.WriteLine("GCM_SUPPORTED=true"); }
        catch (Exception error) { Console.WriteLine("GCM_SUPPORTED=false;exception=" + error.GetType().FullName); }
        return 0;
    }
    private static void Probe()
    { using (var aes = new AesGcm(new byte[32])) aes.Encrypt(new byte[12], new byte[1], new byte[1], new byte[16]); }
}
