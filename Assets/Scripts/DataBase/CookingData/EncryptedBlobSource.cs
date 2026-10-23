using System;
using System.Collections.Generic;
using EatWhat.Tools.Security;
using UnityEngine;
[Serializable]
public sealed class CatalogSpriteReference
{
    public string key;
    public Sprite sprite;
    public CatalogSpriteReference() { }
    public CatalogSpriteReference(string key, Sprite sprite) { this.key = key; this.sprite = sprite; }
}
public sealed class EncryptedBlobSource : ICatalogSource, IDisposable
{
    private Func<byte[]> read;
    private readonly string fingerprint;
    private readonly Func<string, Sprite> resolve;
    private readonly CatalogBlobCipher cipher;
    private CatalogDecodedData decoded;
    private bool disposed;
    public EncryptedBlobSource(Func<byte[]> read, string fingerprint, Func<string, Sprite> resolve, CatalogBlobCipher cipher)
    { if (read == null || resolve == null || cipher == null) throw new ArgumentNullException(); this.read = read; this.fingerprint = fingerprint; this.resolve = resolve; this.cipher = cipher; }
    public string Kind { get { return "EncryptedBlob"; } }
    public IList<CK01GeneratedDataTable> Tables
    {
        get
        {
            if (disposed) throw new ObjectDisposedException("EncryptedBlobSource");
            if (decoded == null) decoded = CatalogBlobSerializer.Open(read(), fingerprint, resolve, cipher);
            return decoded.Tables;
        }
    }
    public void Dispose() { if (disposed) return; disposed = true; read = null; if (decoded != null) decoded.Dispose(); decoded = null; }
}
