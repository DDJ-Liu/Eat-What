using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DictPair<TKey, TValue>
{
    public TKey key;
    public TValue value;
}

[System.Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    [SerializeField]
    private List<DictPair<TKey, TValue>> serializedPairs = new List<DictPair<TKey, TValue>>();

    public void OnBeforeSerialize()
    {
        serializedPairs.Clear();
        foreach (var kvp in this)
        {
            serializedPairs.Add(new DictPair<TKey, TValue> { key = kvp.Key, value = kvp.Value });
        }
    }

    public void OnAfterDeserialize()
    {
        Clear();
        foreach (var pair in serializedPairs)
        {
            this[pair.key] = pair.value;
        }
    }
}
