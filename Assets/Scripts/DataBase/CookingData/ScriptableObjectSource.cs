using System;
using System.Collections.Generic;
public sealed class ScriptableObjectSource : ICatalogSource
{
    private readonly Func<IList<CK01GeneratedDataTable>> read;
    public ScriptableObjectSource(Func<IList<CK01GeneratedDataTable>> read) { if (read == null) throw new ArgumentNullException("read"); this.read = read; }
    public string Kind { get { return "ScriptableObject"; } }
    public IList<CK01GeneratedDataTable> Tables { get { return read(); } }
}
