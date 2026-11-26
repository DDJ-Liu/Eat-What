using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace EatWhat.Narrative.Core
{
    // Units live beside the existing parameter declarations; defaults are never copied into the registry.
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class DialogueParameterAttribute : Attribute
    {
        public DialogueParameterAttribute(string unit) { Unit = unit; }
        public string Unit { get; private set; }
    }

    public enum DialogueParameterKind { Number, Boolean, Enum, String, Point, Color, Stations }

    public sealed class DialogueParameterDefinition
    {
        internal DialogueParameterDefinition(string group, FieldInfo field, string unit)
        {
            Group = group; Field = field; FieldName = field.Name; Key = group + "." + field.Name;
            ValueType = field.FieldType; Unit = unit;
            if (ValueType == typeof(float)) Kind = DialogueParameterKind.Number;
            else if (ValueType == typeof(bool)) Kind = DialogueParameterKind.Boolean;
            else if (ValueType.IsEnum) Kind = DialogueParameterKind.Enum;
            else if (ValueType == typeof(string)) Kind = DialogueParameterKind.String;
            else if (ValueType == typeof(PresentationPoint)) Kind = DialogueParameterKind.Point;
            else if (ValueType == typeof(PresentationColor)) Kind = DialogueParameterKind.Color;
            else if (ValueType == typeof(PortraitStationSet[])) Kind = DialogueParameterKind.Stations;
            else throw new InvalidOperationException("Unsupported declared parameter type: " + Key);
        }
        internal FieldInfo Field { get; private set; }
        public string Key { get; private set; }
        public string Group { get; private set; }
        public string FieldName { get; private set; }
        public string Unit { get; private set; }
        public Type ValueType { get; private set; }
        public DialogueParameterKind Kind { get; private set; }
    }

    public static class DialogueParameterRegistry
    {
        private static readonly Dictionary<string, DialogueParameterDefinition> byKey =
            new Dictionary<string, DialogueParameterDefinition>(StringComparer.Ordinal);
        private static readonly IReadOnlyList<DialogueParameterDefinition> entries;
        static DialogueParameterRegistry()
        {
            var list = new List<DialogueParameterDefinition>();
            foreach (var group in typeof(DialoguePresentationData).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var templateType = group.FieldType.GetElementType();
                if (!group.FieldType.IsArray || !typeof(DialogueNamedTemplate).IsAssignableFrom(templateType))
                    throw new InvalidOperationException("Unexpected presentation group: " + group.Name);
                foreach (var field in templateType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    var attributes = field.GetCustomAttributes(typeof(DialogueParameterAttribute), false);
                    if (field.Name == "name") continue; // Template identity, not a D5 presentation parameter.
                    if (attributes.Length != 1) throw new InvalidOperationException("Parameter unit declaration missing: " + group.Name + "." + field.Name);
                    var attribute = (DialogueParameterAttribute)attributes[0];
                    if (string.IsNullOrWhiteSpace(attribute.Unit)) throw new InvalidOperationException("Parameter unit is empty.");
                    var definition = new DialogueParameterDefinition(group.Name, field, attribute.Unit);
                    byKey.Add(definition.Key, definition); list.Add(definition);
                }
            }
            list.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key)); entries = list.AsReadOnly();
        }
        public static IReadOnlyList<DialogueParameterDefinition> Entries { get { return entries; } }
        public static bool TryGet(string key, out DialogueParameterDefinition definition)
        { if (key == null) { definition = null; return false; } return byKey.TryGetValue(key, out definition); }
    }

    public enum DialogueOverrideProblem { Syntax, UnknownKey, InvalidValue, DuplicateKey }
    public sealed class DialogueOverrideDiagnostic
    {
        internal DialogueOverrideDiagnostic(string key, DialogueOverrideProblem problem, int position, string reason)
        { Key = key; Problem = problem; Position = position; Reason = reason; }
        public string Key { get; private set; }
        public DialogueOverrideProblem Problem { get; private set; }
        public int Position { get; private set; }
        public string Reason { get; private set; }
        public override string ToString() { return Key + " @" + Position + ": " + Reason; }
    }
    public sealed class DialogueParameterOverride
    {
        internal DialogueParameterOverride(DialogueParameterDefinition definition, object value)
        { Definition = definition; Value = value; }
        public DialogueParameterDefinition Definition { get; private set; }
        // Composite values are newly parsed authoring values, not N008 immutable resolved snapshots.
        public object Value { get; private set; }
    }
    public sealed class DialogueOverrideParseResult
    {
        internal DialogueOverrideParseResult(List<DialogueParameterOverride> values, List<DialogueOverrideDiagnostic> issues)
        { Values = (issues.Count == 0 ? values : new List<DialogueParameterOverride>()).AsReadOnly(); Diagnostics = issues.AsReadOnly(); }
        public bool Success { get { return Diagnostics.Count == 0; } }
        public IReadOnlyList<DialogueParameterOverride> Values { get; private set; }
        public IReadOnlyList<DialogueOverrideDiagnostic> Diagnostics { get; private set; }
    }

    public sealed class DialogueOverrideItemResult
    {
        internal DialogueOverrideItemResult(string raw, int position, List<DialogueParameterOverride> values,
            List<DialogueOverrideDiagnostic> diagnostics)
        {
            SourceText = raw; Position = position; Value = values.Count == 1 ? values[0] : null;
            Diagnostics = new List<DialogueOverrideDiagnostic>(diagnostics).AsReadOnly();
            Key = Value != null ? Value.Definition.Key : diagnostics.Count == 0 ? "" : diagnostics[0].Key;
        }
        public string SourceText { get; private set; }
        public int Position { get; private set; }
        public string Key { get; private set; }
        public DialogueParameterOverride Value { get; private set; }
        public IReadOnlyList<DialogueOverrideDiagnostic> Diagnostics { get; private set; }
        public bool Success { get { return Value != null && Diagnostics.Count == 0; } }
    }

    public sealed class DialogueOverrideItemsResult
    {
        internal DialogueOverrideItemsResult(string source, List<DialogueOverrideItemResult> items,
            IReadOnlyList<DialogueOverrideDiagnostic> diagnostics)
        {
            SourceText = source ?? ""; Items = items.AsReadOnly(); Diagnostics = diagnostics;
            var values = new List<DialogueParameterOverride>();
            foreach (var item in items) if (item.Success) values.Add(item.Value);
            Values = values.AsReadOnly();
        }
        public string SourceText { get; private set; }
        public IReadOnlyList<DialogueOverrideItemResult> Items { get; private set; }
        public IReadOnlyList<DialogueParameterOverride> Values { get; private set; }
        public IReadOnlyList<DialogueOverrideDiagnostic> Diagnostics { get; private set; }
        public bool Success { get { return Diagnostics.Count == 0; } }
    }

    /// <summary>Parses typed values only. Never accepts or writes a user template/asset.</summary>
    public static class DialogueOverrideParser
    {
        public static DialogueOverrideParseResult Parse(string source)
        { return ParseCore(source, null); }

        // Same scanner, assignment parser and constraints as Parse. The first occurrence reserves a key,
        // including an invalid occurrence; later duplicates are diagnosed and never overwrite it.
        public static DialogueOverrideItemsResult ParseItems(string source)
        {
            var items = new List<DialogueOverrideItemResult>();
            var atomic = ParseCore(source, items);
            return new DialogueOverrideItemsResult(source, items, atomic.Diagnostics);
        }

        private static DialogueOverrideParseResult ParseCore(string source, List<DialogueOverrideItemResult> items)
        {
            source = source ?? "";
            var values = new List<DialogueParameterOverride>();
            var issues = new List<DialogueOverrideDiagnostic>();
            var seen = new Dictionary<string, bool>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(source)) return new DialogueOverrideParseResult(values, issues);
            bool quoted = false, escaped = false; int begin = 0;
            for (int i = 0; i <= source.Length; i++)
            {
                if (i < source.Length)
                {
                    char c = source[i];
                    if (escaped) { escaped = false; continue; }
                    if (quoted && c == '\\') { escaped = true; continue; }
                    if (c == '"') { quoted = !quoted; continue; }
                    if (c != ';' || quoted) continue;
                }
                string segment = source.Substring(begin, i - begin);
                if (i == source.Length && string.IsNullOrWhiteSpace(segment) && begin > 0) break; // Optional trailing delimiter.
                int valueStart = values.Count, issueStart = issues.Count;
                Assignment(segment, begin, values, issues, seen);
                if (items != null) items.Add(new DialogueOverrideItemResult(segment, begin,
                    values.GetRange(valueStart, values.Count - valueStart), issues.GetRange(issueStart, issues.Count - issueStart)));
                begin = i + 1;
            }
            return new DialogueOverrideParseResult(values, issues);
        }

        private static void Assignment(string segment, int offset, List<DialogueParameterOverride> values,
            List<DialogueOverrideDiagnostic> issues, Dictionary<string, bool> seen)
        {
            int equals = segment.IndexOf('=');
            string key = (equals < 0 ? segment : segment.Substring(0, equals)).Trim();
            if (equals < 0 || key.Length == 0)
            { issues.Add(new DialogueOverrideDiagnostic(key, DialogueOverrideProblem.Syntax, offset, "Expected a nonempty key=value assignment.")); return; }
            if (seen.ContainsKey(key))
            { issues.Add(new DialogueOverrideDiagnostic(key, DialogueOverrideProblem.DuplicateKey, offset, "Key occurs more than once; no last-value overwrite is permitted.")); return; }
            seen.Add(key, true);
            DialogueParameterDefinition definition;
            if (!DialogueParameterRegistry.TryGet(key, out definition))
            { issues.Add(new DialogueOverrideDiagnostic(key, DialogueOverrideProblem.UnknownKey, offset, "Unknown parameter key; use the exact group.field name.")); return; }
            var reader = new ValueReader(segment.Substring(equals + 1));
            try
            {
                object value;
                switch (definition.Kind)
                {
                    case DialogueParameterKind.Number: value = reader.Number(); break;
                    case DialogueParameterKind.Boolean:
                        bool boolean;
                        if (!bool.TryParse(reader.Atom(), out boolean)) throw new FormatException("Expected true or false.");
                        value = boolean; break;
                    case DialogueParameterKind.Enum: value = EnumValue(definition.ValueType, reader.Atom()); break;
                    case DialogueParameterKind.String: value = reader.String(false); break;
                    case DialogueParameterKind.Point: value = reader.Point(); break;
                    case DialogueParameterKind.Color: value = reader.Color(); break;
                    case DialogueParameterKind.Stations: value = reader.Stations(); break;
                    default: throw new FormatException("Unsupported parameter kind.");
                }
                reader.End();
                // Reuse N006 constraints on an isolated authoring object. This is not a resolver or user-data write.
                var validationData = new DialoguePresentationData();
                var array = (Array)typeof(DialoguePresentationData).GetField(definition.Group).GetValue(validationData);
                definition.Field.SetValue(array.GetValue(0), value);
                bool valid = true;
                foreach (var issue in DialoguePresentationValidation.Validate(validationData))
                {
                    if (issue.Group != definition.Group) continue;
                    valid = false;
                    issues.Add(new DialogueOverrideDiagnostic(key, DialogueOverrideProblem.InvalidValue, offset + equals + 1,
                        issue.Field + ": " + issue.Reason));
                }
                if (valid) values.Add(new DialogueParameterOverride(definition, value));
            }
            catch (FormatException ex)
            { issues.Add(new DialogueOverrideDiagnostic(key, DialogueOverrideProblem.InvalidValue, offset + equals + 1 + reader.Position, ex.Message)); }
        }

        internal static object EnumValue(Type type, string word)
        {
            if (type == typeof(NarrativeCurvePreset))
            {
                switch (word)
                {
                    case "linear": return NarrativeCurvePreset.Linear;
                    case "quadratic": return NarrativeCurvePreset.Quadratic;
                    case "smooth_step": return NarrativeCurvePreset.SmoothStep;
                    case "ease_in_out": return NarrativeCurvePreset.EaseInOut;
                }
            }
            foreach (string name in Enum.GetNames(type)) if (string.Equals(name, word, StringComparison.Ordinal)) return Enum.Parse(type, name);
            throw new FormatException("Expected a declared " + type.Name + " name; numeric enum ordinals are not accepted.");
        }

        // Dedicated value syntax for N006's point, RGBA and station data, not a general serializer.
        private sealed class ValueReader
        {
            private readonly string text;
            private int at;
            public ValueReader(string text) { this.text = text; }
            public int Position { get { return at; } }
            private void Space() { while (at < text.Length && char.IsWhiteSpace(text[at])) at++; }
            private bool Take(char c) { Space(); if (at < text.Length && text[at] == c) { at++; return true; } return false; }
            private void Need(char c) { if (!Take(c)) throw new FormatException("Expected '" + c + "' in structured value."); }
            public void End() { Space(); if (at != text.Length) throw new FormatException("Unexpected trailing value text."); }
            public string Atom()
            {
                Space(); int begin = at;
                while (at < text.Length && text[at] != ',' && text[at] != '(' && text[at] != ')' && text[at] != '[' && text[at] != ']') at++;
                string value = text.Substring(begin, at - begin).Trim();
                if (value.Length == 0) throw new FormatException("Expected a value."); return value;
            }
            public float Number()
            {
                float value;
                if (!float.TryParse(Atom(), NumberStyles.Float, CultureInfo.InvariantCulture, out value) || float.IsNaN(value) || float.IsInfinity(value))
                    throw new FormatException("Expected a finite invariant-culture number (decimal point '.').");
                return value;
            }
            private int Integer()
            {
                int value;
                if (!int.TryParse(Atom(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value)) throw new FormatException("Expected a 32-bit integer.");
                return value;
            }
            public string String(bool requireQuoted)
            {
                Space();
                if (!Take('"'))
                {
                    if (requireQuoted) throw new FormatException("Station id must be quoted.");
                    string plain = text.Substring(at).Trim(); at = text.Length;
                    if (plain.IndexOf('"') >= 0) throw new FormatException("Quote the entire string value.");
                    return plain;
                }
                var value = new StringBuilder();
                while (at < text.Length)
                {
                    char c = text[at++]; if (c == '"') return value.ToString();
                    if (c != '\\') { value.Append(c); continue; }
                    if (at == text.Length) throw new FormatException("Incomplete string escape.");
                    switch (text[at++])
                    {
                        case '"': value.Append('"'); break;
                        case '\\': value.Append('\\'); break;
                        case 'n': value.Append('\n'); break;
                        case 'r': value.Append('\r'); break;
                        case 't': value.Append('\t'); break;
                        default: throw new FormatException("Unknown string escape; use quote, backslash, n, r or t.");
                    }
                }
                throw new FormatException("Unclosed quoted string.");
            }
            public PresentationPoint Point()
            { Need('('); float x = Number(); Need(','); float y = Number(); Need(')'); return new PresentationPoint(x, y); }
            public PresentationColor Color()
            {
                Need('('); float r = Number(); Need(','); float g = Number(); Need(','); float b = Number(); Need(','); float a = Number(); Need(')');
                return new PresentationColor(r, g, b, a);
            }
            public PortraitStationSet[] Stations()
            {
                var sets = new List<PortraitStationSet>(); Need('[');
                if (Take(']')) return sets.ToArray();
                do
                {
                    Need('('); int count = Integer(); Need(','); Need('[');
                    var stations = new List<PortraitStation>();
                    if (!Take(']'))
                    {
                        do
                        {
                            Need('('); string id = String(true); Need(','); float x = Number(); Need(','); float y = Number();
                            Need(','); float scale = Number(); Need(','); int layer = Integer(); Need(')');
                            stations.Add(new PortraitStation(id, x, y, scale, layer));
                        } while (Take(','));
                        Need(']');
                    }
                    Need(')'); sets.Add(new PortraitStationSet(count, stations.ToArray()));
                } while (Take(','));
                Need(']'); return sets.ToArray();
            }
        }
    }
}
