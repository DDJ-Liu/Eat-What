using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
#endif

/// <summary>One validated worksheet. Public row indices are zero-based, not Excel row numbers.</summary>
#if UNITY_EDITOR
[HideMonoScript]
#endif
public sealed class ParsedDataTable : ScriptableObject, ISerializationCallbackReceiver
{
    [Serializable]
    public sealed class Column
    {
        [SerializeField] private string name = string.Empty;
        [SerializeField] private string type = string.Empty;
        [SerializeField] private bool isId = false;

        public string Name => name;
        public string Type => type;
        public bool IsId => isId;

#if UNITY_EDITOR
        internal Column(string name, string type, bool isId)
        {
            this.name = name;
            this.type = type;
            this.isId = isId;
        }
#endif
    }

    [Serializable]
    public sealed class Row
    {
        [SerializeField, HideInInspector] private string[] cells = Array.Empty<string>();
        internal int Count => cells.Length;
        internal string At(int column) => cells[column] ?? string.Empty;

#if UNITY_EDITOR
        internal Row(string[] cells) => this.cells = (string[])cells.Clone();
#endif
    }

    [SerializeField, HideInInspector] private Column[] columns = Array.Empty<Column>();
#if UNITY_EDITOR
    [LabelText("表项"), DisableContextMenu(true, true)]
    [ListDrawerSettings(IsReadOnly = true, ShowPaging = true, NumberOfItemsPerPage = 20,
        ShowIndexLabels = false, ShowFoldout = false)]
#endif
    [SerializeField] private Row[] rows = Array.Empty<Row>();

    public int ColumnCount => columns.Length;
    public int RowCount => rows.Length;
    public Column GetColumn(int index) => columns[index];

    public enum CellStatus { Success, Empty, Invalid, Missing, WrongType }
    private enum Kind { Unknown, Text, Integer, Float, Bool, Enum, Reference }

    // Decode the validated declaration once per column, including the enum vocabulary.
    private sealed class Field
    {
        internal readonly int Index;
        internal readonly bool List;
        internal readonly Kind Kind;
        internal readonly HashSet<string> Choices;
        internal Field(int index, string declaration)
        {
            Index = index;
            string t = declaration.Trim();
            List = t.StartsWith("list<", StringComparison.Ordinal) && t.EndsWith(">", StringComparison.Ordinal);
            if (List) t = t.Substring(5, t.Length - 6).Trim();
            switch (t)
            {
                case "string": Kind = Kind.Text; break;
                case "int": Kind = Kind.Integer; break;
                case "float": Kind = Kind.Float; break;
                case "bool": Kind = Kind.Bool; break;
                default:
                    if (t.StartsWith("enum(", StringComparison.Ordinal) && t.EndsWith(")", StringComparison.Ordinal))
                    {
                        Kind = Kind.Enum;
                        Choices = new HashSet<string>(StringComparer.Ordinal);
                        foreach (var choice in t.Substring(5, t.Length - 6).Split(',')) Choices.Add(choice.Trim());
                    }
                    else if (t.StartsWith("ref:", StringComparison.Ordinal)) Kind = Kind.Reference;
                    break;
            }
        }
    }

    [NonSerialized] private Dictionary<string, Field> fields;
    [NonSerialized] private int[] idColumns;
    [NonSerialized] private Dictionary<string, int> rowsById;

    private void EnsureFields()
    {
        if (fields != null) return;
        var byName = new Dictionary<string, Field>(ColumnCount, StringComparer.Ordinal);
        var ids = new List<int>();
        for (int i = 0; i < ColumnCount; i++)
        {
            Column c = columns[i];
            byName.Add(c.Name.Trim(), new Field(i, c.Type));
            if (c.IsId) ids.Add(i);
        }
        idColumns = ids.ToArray();
        fields = byName;
    }

    private bool TryField(int row, string name, out Field field)
    {
        field = null;
        if ((uint)row >= (uint)RowCount || name == null) return false;
        EnsureFields();
        return fields.TryGetValue(name, out field);
    }

    /// <summary>Raw stored text for any declaration; empty cell = "", unknown row/field = null.</summary>
    public string GetString(int row, string field) => TryField(row, field, out var f) ? rows[row].At(f.Index) : null;

    private delegate bool Parse<T>(Field field, string text, out T value);
    private static bool Text(Field f, string s, out string value)
    {
        value = s;
        return f.Choices == null || f.Choices.Contains(s);
    }
    private static bool Integer(Field _, string s, out int value) =>
        int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    private static bool Float(Field _, string s, out float value) =>
        float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !float.IsNaN(value) && !float.IsInfinity(value);
    private static bool Boolean(Field _, string s, out bool value) => bool.TryParse(s, out value);

    private CellStatus Cell(int row, string name, Kind kind, bool list, out Field field, out string raw)
    {
        raw = null;
        if (!TryField(row, name, out field)) return CellStatus.Missing;
        if (field.Kind != kind || field.List != list) return CellStatus.WrongType;
        raw = rows[row].At(field.Index);
        return string.IsNullOrWhiteSpace(raw) ? CellStatus.Empty : CellStatus.Success;
    }

    private CellStatus Scalar<T>(int row, string name, Kind kind, Parse<T> parse, out T value)
    {
        value = default;
        var status = Cell(row, name, kind, false, out var field, out var raw);
        if (status != CellStatus.Success) return status;
        if (!parse(field, kind == Kind.Text ? raw : raw.Trim(), out var parsed)) return CellStatus.Invalid;
        value = parsed;
        return CellStatus.Success;
    }

    private CellStatus List<T>(int row, string name, Kind kind, Parse<T> parse, out IReadOnlyList<T> values)
    {
        values = null;
        var status = Cell(row, name, kind, true, out var field, out var raw);
        if (status != CellStatus.Success) return status;
        var parts = raw.Split(';');
        var result = new T[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            var item = parts[i].Trim();
            if (item.Length == 0 || !parse(field, item, out result[i])) return CellStatus.Invalid;
        }
        values = Array.AsReadOnly(result);
        return CellStatus.Success;
    }

    // Typed reads require a matching header. On non-Success the output is default (lists: null).
    // References are exact keys validated at workbook import, not live Unity object references.
    public CellStatus ReadString(int row, string field, out string value) => Scalar(row, field, Kind.Text, Text, out value);
    public CellStatus ReadInt(int row, string field, out int value) => Scalar(row, field, Kind.Integer, Integer, out value);
    public CellStatus ReadFloat(int row, string field, out float value) => Scalar(row, field, Kind.Float, Float, out value);
    public CellStatus ReadBool(int row, string field, out bool value) => Scalar(row, field, Kind.Bool, Boolean, out value);
    public CellStatus ReadEnum(int row, string field, out string value) => Scalar(row, field, Kind.Enum, Text, out value);
    public CellStatus ReadReference(int row, string field, out string value) => Scalar(row, field, Kind.Reference, Text, out value);
    public CellStatus ReadStringList(int row, string field, out IReadOnlyList<string> values) => List(row, field, Kind.Text, Text, out values);
    public CellStatus ReadIntList(int row, string field, out IReadOnlyList<int> values) => List(row, field, Kind.Integer, Integer, out values);
    public CellStatus ReadFloatList(int row, string field, out IReadOnlyList<float> values) => List(row, field, Kind.Float, Float, out values);
    public CellStatus ReadBoolList(int row, string field, out IReadOnlyList<bool> values) => List(row, field, Kind.Bool, Boolean, out values);
    public CellStatus ReadEnumList(int row, string field, out IReadOnlyList<string> values) => List(row, field, Kind.Enum, Text, out values);
    public CellStatus ReadReferenceList(int row, string field, out IReadOnlyList<string> values) => List(row, field, Kind.Reference, Text, out values);

    // The joined text is the public key: validation, references, lookups and labels agree.
    internal static string ComposeKey(IReadOnlyList<string> parts)
    {
        if (parts == null || parts.Count == 0) return null;
        if (parts.Count == 1) return parts[0]?.Trim();
        var key = new StringBuilder();
        foreach (string part in parts)
        {
            if (string.IsNullOrWhiteSpace(part)) return null;
            if (key.Length != 0) key.Append('/');
            key.Append(part.Trim());
        }
        return key.ToString();
    }

    private string RowKey(int row)
    {
        if (idColumns.Length == 1) return rows[row].At(idColumns[0]).Trim();
        var parts = new string[idColumns.Length];
        for (int i = 0; i < parts.Length; i++) parts[i] = rows[row].At(idColumns[i]);
        return ComposeKey(parts);
    }

    public string GetItemName(int row)
    {
        if ((uint)row >= (uint)RowCount) throw new ArgumentOutOfRangeException(nameof(row));
        EnsureFields();
        return idColumns.Length == 0 ? "item" + (row + 1).ToString(CultureInfo.InvariantCulture) : RowKey(row);
    }

    private void EnsureRowsById()
    {
        if (rowsById != null) return;
        var index = new Dictionary<string, int>(RowCount, StringComparer.Ordinal);
        for (int row = 0; row < RowCount; row++) index.Add(RowKey(row), row);
        rowsById = index;
    }

    private bool FindKey(string key, out int row)
    {
        if (!string.IsNullOrEmpty(key))
        {
            EnsureRowsById();
            if (rowsById.TryGetValue(key, out row)) return true;
        }
        row = -1;
        return false;
    }

    /// <summary>Find a single ID or a complete slash-joined composite ID.</summary>
    public bool TryFindRow(string id, out int row)
    {
        EnsureFields();
        return FindKey(idColumns.Length != 0 ? id?.Trim() : null, out row);
    }

    /// <summary>IDs in worksheet column order. No implicit key; lookup is local to this asset.</summary>
    public bool TryFindRow(IReadOnlyList<string> keyParts, out int row)
    {
        EnsureFields();
        return FindKey(idColumns.Length > 0 && keyParts?.Count == idColumns.Length ? ComposeKey(keyParts) : null, out row);
    }

    private void ResetIndexes() { fields = null; idColumns = null; rowsById = null; }
    void ISerializationCallbackReceiver.OnBeforeSerialize() { }
    void ISerializationCallbackReceiver.OnAfterDeserialize() => ResetIndexes();
    private void OnEnable() => ResetIndexes();

#if UNITY_EDITOR
    private void OnValidate() => ResetIndexes();

    // Called only after the parser validated the complete workbook and checked target ownership.
    internal void Import(Column[] columns, Row[] rows)
    {
        if (columns == null || rows == null) throw new ArgumentNullException(columns == null ? nameof(columns) : nameof(rows));
        foreach (Row row in rows)
            if (row == null || row.Count != columns.Length) throw new ArgumentException("Row width does not match header.", nameof(rows));
        this.columns = (Column[])columns.Clone();
        this.rows = (Row[])rows.Clone();
        ResetIndexes();
    }
#endif
}

#if UNITY_EDITOR
// Odin pages the rows; only the expanded row reads/draws its cells. No display data is serialized.
public sealed class ParsedDataTableRowDrawer : OdinValueDrawer<ParsedDataTable.Row>
{
    protected override void DrawPropertyLayout(GUIContent label)
    {
        // The schema belongs to the table, not the row. A multi-selection may mix schemas.
        if (Property.Tree.WeakTargets.Count != 1 || !(Property.Tree.WeakTargets[0] is ParsedDataTable table))
        {
            EditorGUILayout.LabelField("请单独选择一张表查看表项。");
            return;
        }
        var row = ValueEntry.SmartValue;
        if (row == null) return;
        // The property index is the item's index in the collection, not its slot on this page.
        string title = table.GetItemName(Property.Index);
        Property.State.Expanded = SirenixEditorGUI.Foldout(Property.State.Expanded, title);
        if (!Property.State.Expanded) return;

        float labelWidth = Mathf.Min(EditorGUIUtility.labelWidth, EditorGUIUtility.currentViewWidth * 0.4f);
        for (int i = 0; i < table.ColumnCount; i++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(table.GetColumn(i).Name, EditorStyles.wordWrappedLabel, GUILayout.Width(labelWidth));
                GUILayout.Label(row.At(i), EditorStyles.wordWrappedLabel, GUILayout.MinWidth(0),
                    GUILayout.ExpandWidth(true), GUILayout.MinHeight(EditorGUIUtility.singleLineHeight));
            }
        }
    }
}
#endif
