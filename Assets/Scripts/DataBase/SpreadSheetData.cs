using System.Collections.Generic;

[System.Serializable]
public class SpreadSheetData<T>
{
    public SerializableDictionary<string, List<T>> sheets = new SerializableDictionary<string, List<T>>();

    public List<T> this[string sheetName]
    {
        get => sheets != null && sheets.TryGetValue(sheetName, out var value) ? value : null;
        set
        {
            sheets ??= new SerializableDictionary<string, List<T>>();
            sheets[sheetName] = value;
        }
    }

    public void Clear()
    {
        sheets.Clear();
    }
}
