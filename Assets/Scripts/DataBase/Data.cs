using System.Collections.Generic;
using UnityEngine;

//读取的单条数据，通过表头来索引其中数据
[System.Serializable]
public class Data
{
    public enum DataType {Dialog, MISC}
    public DataType dataType = DataType.MISC;
    public SerializableDictionary<string, string> data = new SerializableDictionary<string, string>();

    public string this[string key]
    {
        get => data != null && data.TryGetValue(key, out var value) ? value : null;
        set
        {
            data ??= new SerializableDictionary<string, string>();
            data[key] = value;
        }
    }
}
