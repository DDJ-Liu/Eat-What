using System.Collections.Generic;
public interface ICatalogSource
{
    string Kind { get; }
    IList<CK01GeneratedDataTable> Tables { get; }
}
public static class CatalogSourcePolicy
{
    public static bool RequiresBlob(bool editor, bool development, bool encryptedDefine)
    {
        if (editor || development) return false;
        if (!encryptedDefine) throw new System.InvalidOperationException("Release catalog requires the prepared encrypted build entry.");
        return true;
    }
}
