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
[Serializable]
public sealed class CatalogPrefabReference
{
    public string key;
    public GameObject prefab;
    public CatalogPrefabReference() { }
    public CatalogPrefabReference(string key, GameObject prefab) { this.key=key;this.prefab=prefab; }
}
public sealed class EncryptedBlobSource : ICatalogSource, IDisposable
{
    private Func<byte[]> read;
    private readonly string fingerprint;
    private readonly Func<string, Sprite> resolve;
    private readonly CatalogBlobCipher cipher;
    private readonly Func<string, GameObject> resolvePrefab;
    private CatalogDecodedData decoded;
    private bool disposed;
    public EncryptedBlobSource(Func<byte[]> read, string fingerprint, Func<string, Sprite> resolve, CatalogBlobCipher cipher)
    : this(read,fingerprint,resolve,key=>null,cipher) { }
    public EncryptedBlobSource(Func<byte[]> read, string fingerprint, Func<string, Sprite> resolve, Func<string, GameObject> resolvePrefab, CatalogBlobCipher cipher)
    { if (read == null || resolve == null || resolvePrefab == null || cipher == null) throw new ArgumentNullException(); this.read = read; this.fingerprint = fingerprint; this.resolve = resolve; this.resolvePrefab=resolvePrefab; this.cipher = cipher; }
    public string Kind { get { return "EncryptedBlob"; } }
    public IList<CK01GeneratedDataTable> Tables
    {
        get
        {
            if (disposed) throw new ObjectDisposedException("EncryptedBlobSource");
            if (decoded == null) decoded = CatalogBlobSerializer.Open(read(), fingerprint, resolve, resolvePrefab, cipher);
            return decoded.Tables;
        }
    }
    public void Dispose() { if (disposed) return; disposed = true; read = null; if (decoded != null) decoded.Dispose(); decoded = null; }
}
