using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// 数据加密/解密工具，将List<Data>加密为.dat文件，或从.dat解密还原
/// </summary>
public static class DataEncryptor
{
    // AES-256-CBC 密钥（32字节）和IV（16字节）
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("BtnProj2024!SecureKey#AES256Data");  // 32 chars = 32 bytes
    private static readonly byte[] IV  = Encoding.UTF8.GetBytes("BtnProjIV!16Byte");                  // 16 chars = 16 bytes

    [Serializable]
    private class DataListWrapper
    {
        public List<Data> items;
    }

    [Serializable]
    private class SpreadSheetDataWrapper
    {
        public SpreadSheetData<Data> data;
    }

    /// <summary>
    /// 将数据列表加密并写入文件
    /// </summary>
    public static void EncryptToFile(List<Data> dataList, string outputPath)
    {
        var wrapper = new DataListWrapper { items = dataList };
        string json = JsonUtility.ToJson(wrapper);
        byte[] encrypted = AesEncrypt(json);
        File.WriteAllBytes(outputPath, encrypted);
    }

    /// <summary>
    /// 从加密文件解密并还原为数据列表
    /// </summary>
    public static List<Data> DecryptFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"数据文件未找到: {filePath}");
            return new List<Data>();
        }

        byte[] encrypted = File.ReadAllBytes(filePath);
        string json = AesDecrypt(encrypted);

        var wrapper = JsonUtility.FromJson<DataListWrapper>(json);
        return wrapper?.items ?? new List<Data>();
    }

    /// <summary>
    /// 将 SpreadSheetData 加密并写入文件
    /// </summary>
    public static void EncryptSpreadSheetToFile(SpreadSheetData<Data> spreadSheet, string outputPath)
    {
        var wrapper = new SpreadSheetDataWrapper { data = spreadSheet };
        string json = JsonUtility.ToJson(wrapper);
        byte[] encrypted = AesEncrypt(json);
        File.WriteAllBytes(outputPath, encrypted);
    }

    /// <summary>
    /// 从加密文件解密并还原为 SpreadSheetData
    /// </summary>
    public static SpreadSheetData<Data> DecryptSpreadSheetFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"数据文件未找到: {filePath}");
            return new SpreadSheetData<Data>();
        }

        byte[] encrypted = File.ReadAllBytes(filePath);
        string json = AesDecrypt(encrypted);

        var wrapper = JsonUtility.FromJson<SpreadSheetDataWrapper>(json);
        return wrapper?.data ?? new SpreadSheetData<Data>();
    }

    private static byte[] AesEncrypt(string plainText)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = Key;
            aes.IV = IV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(plainText);
                    cs.Write(bytes, 0, bytes.Length);
                }
                return ms.ToArray();
            }
        }
    }

    private static string AesDecrypt(byte[] cipherBytes)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = Key;
            aes.IV = IV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (var ms = new MemoryStream(cipherBytes))
            using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
            using (var sr = new StreamReader(cs, Encoding.UTF8))
            {
                return sr.ReadToEnd();
            }
        }
    }
}
