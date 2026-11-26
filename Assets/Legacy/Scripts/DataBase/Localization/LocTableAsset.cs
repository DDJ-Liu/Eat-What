using System;
using System.Collections.Generic;
using EatWhat.Localization;
using UnityEngine;

[Serializable]
public sealed class LocTableEntry
{
    public string key;
    public string zh_cn;
}

[CreateAssetMenu(fileName = "Localization", menuName = "DataTables/CK01 Localization")]
public sealed class LocTableAsset : ScriptableObject
{
    public List<LocTableEntry> entries = new List<LocTableEntry>();

    private void OnEnable()
    {
        LocService.Configure(this);
    }

    public bool ReplaceEntries(IEnumerable<LocTextEntry> source)
    {
        var next = new List<LocTableEntry>();
        if (source != null)
        {
            foreach (var entry in source)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Key)) continue;
                next.Add(new LocTableEntry { key = entry.Key, zh_cn = entry.ZhCn ?? string.Empty });
            }
        }
        if (entries.Count == next.Count)
        {
            var equal = true;
            for (var index = 0; index < entries.Count; index++)
            {
                if (entries[index].key != next[index].key || entries[index].zh_cn != next[index].zh_cn) { equal = false; break; }
            }
            if (equal) return false;
        }
        entries = next;
        return true;
    }
}

public static class LocService
{
    private static readonly LocTextCatalog Catalog = new LocTextCatalog();

    public static void Configure(LocTableAsset table)
    {
        var source = new List<LocTextEntry>();
        if (table != null)
        {
            foreach (var entry in table.entries ?? new List<LocTableEntry>()) source.Add(new LocTextEntry { Key = entry.key, ZhCn = entry.zh_cn });
        }
        Catalog.Replace(source);
    }

    public static string Get(string key)
    {
        return Catalog.Get(key, Debug.LogWarning);
    }
}
