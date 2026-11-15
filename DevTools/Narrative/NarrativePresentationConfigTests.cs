using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using EatWhat.Narrative.Core;

internal static class NarrativePresentationConfigTests
{
    private static int checks, numericCases, enumCases;
    private static void Check(bool ok, string reason)
    { checks++; if (!ok) throw new Exception("Presentation config: " + reason); }

    private sealed class Expected
    { public string Path, Type, Value, Rule; }
    private sealed class Actual
    { public string Type, Value; }

    private static string Scalar(object value, Type type)
    {
        if (value == null) return "<null>";
        if (type == typeof(float)) return BitConverter.ToInt32(BitConverter.GetBytes((float)value), 0).ToString("X8");
        if (type == typeof(bool)) return (bool)value ? "true" : "false";
        if (type.IsEnum) return value.ToString();
        if (type.IsArray) return ((Array)value).Length.ToString(CultureInfo.InvariantCulture);
        if (type == typeof(string) || type == typeof(int)) return Convert.ToString(value, CultureInfo.InvariantCulture);
        return "-";
    }

    // Only this task's ten concrete authoring groups are walked, within the existing test EXE.
    private static void Visit(object value, Type type, string path, Dictionary<string, Actual> result)
    {
        if (path.Length != 0) result.Add(path, new Actual { Type = type.Name, Value = Scalar(value, type) });
        if (value == null || type.IsPrimitive || type.IsEnum || type == typeof(string)) return;
        if (type.IsArray)
        {
            var array = (Array)value;
            for (int i = 0; i < array.Length; i++) Visit(array.GetValue(i), type.GetElementType(), path + "[" + i + "]", result);
        }
        else foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            Visit(field.GetValue(value), field.FieldType, path.Length == 0 ? field.Name : path + "." + field.Name, result);
    }
    private static Dictionary<string, Actual> Snapshot(DialoguePresentationData data)
    { var result = new Dictionary<string, Actual>(); Visit(data, typeof(DialoguePresentationData), "", result); return result; }

    private static void Same(Dictionary<string, Actual> a, Dictionary<string, Actual> b)
    {
        Check(a.Count == b.Count, "validation must not add/remove data");
        foreach (var row in a)
        {
            Actual other;
            if (!b.TryGetValue(row.Key, out other) || row.Value.Type != other.Type || row.Value.Value != other.Value)
                throw new Exception("Validation mutated " + row.Key);
        }
        checks++;
    }
    private static IReadOnlyList<PresentationConfigDiagnostic> ValidateUnchanged(DialoguePresentationData data)
    { var before = Snapshot(data); var issues = DialoguePresentationValidation.Validate(data); Same(before, Snapshot(data)); return issues; }

    private static FieldInfo Field(object target, string segment, out int index)
    {
        int bracket = segment.IndexOf('['); index = -1;
        if (bracket >= 0) index = int.Parse(segment.Substring(bracket + 1, segment.Length - bracket - 2), CultureInfo.InvariantCulture);
        return target.GetType().GetField(bracket < 0 ? segment : segment.Substring(0, bracket));
    }
    private static object Get(object target, string path)
    {
        foreach (var segment in path.Split('.'))
        { int index; var field = Field(target, segment, out index); target = field.GetValue(target); if (index >= 0) target = ((Array)target).GetValue(index); }
        return target;
    }
    private static object Set(object target, string[] segments, int at, object value)
    {
        int index; var field = Field(target, segments[at], out index); object child = field.GetValue(target);
        if (index >= 0)
        {
            var array = (Array)child;
            array.SetValue(at == segments.Length - 1 ? value : Set(array.GetValue(index), segments, at + 1, value), index);
        }
        else field.SetValue(target, at == segments.Length - 1 ? value : Set(child, segments, at + 1, value));
        return target; // Boxed point/color structs are written back into their parent.
    }
    private static void Set(DialoguePresentationData data, string path, object value) { Set(data, path.Split('.'), 0, value); }
    private static bool Has(IReadOnlyList<PresentationConfigDiagnostic> issues, string group, string field)
    {
        foreach (var issue in issues) if (issue.Group == group && issue.Template == "default" && issue.Field == field && issue.IsError && !string.IsNullOrEmpty(issue.Reason)) return true;
        return false;
    }

    private static List<Expected> Defaults(string fixture)
    {
        var expected = new List<Expected>();
        foreach (var line in File.ReadAllLines(fixture))
        {
            if (line.Length == 0 || line[0] == '#') continue;
            var cells = line.Split('\t'); Check(cells.Length == 4, "independent fixture row shape");
            expected.Add(new Expected { Path = cells[0], Type = cells[1], Value = cells[2], Rule = cells[3] });
        }
        var actual = Snapshot(new DialoguePresentationData());
        var paths = new Dictionary<string, bool>();
        foreach (var row in expected)
        {
            Check(!paths.ContainsKey(row.Path), "unique fixture path " + row.Path); paths.Add(row.Path, true);
            Actual got; Check(actual.TryGetValue(row.Path, out got), "field exists: " + row.Path);
            Check(got.Type == row.Type, "exact semantic type: " + row.Path);
            string value = row.Type == "Single" ? Scalar(float.Parse(row.Value, CultureInfo.InvariantCulture), typeof(float)) : row.Value;
            Check(got.Value == value, "D5/Schema preset: " + row.Path);
        }
        Check(actual.Count == expected.Count, "complete field/array/station list, no undocumented extra fields");
        Check(ValidateUnchanged(new DialoguePresentationData()).Count == 0, "all development presets valid");
        Check(typeof(DialoguePresentationData).GetFields().Length == 10, "exact ten groups");
        Check(typeof(DialoguePresentationData).IsSerializable && typeof(PresentationPoint).IsSerializable && typeof(PresentationColor).IsSerializable,
            "pure data is marked serializable without Unity types");
        foreach (var reference in typeof(DialoguePresentationData).Assembly.GetReferencedAssemblies())
            Check(reference.Name == "mscorlib", "Core actual metadata remains mscorlib only");
        return expected;
    }

    private static void Values(List<Expected> expected)
    {
        foreach (var row in expected)
        {
            if (row.Type != "Single" && row.Rule != "enum") continue;
            string group = row.Path.Substring(0, row.Path.IndexOf('['));
            string field = row.Path.Substring(row.Path.IndexOf(']') + 2);
            if (row.Type == "Single")
            {
                foreach (float value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, -1f, 0f })
                {
                    var data = new DialoguePresentationData(); Set(data, row.Path, value);
                    var issues = ValidateUnchanged(data); bool illegal = float.IsNaN(value) || float.IsInfinity(value) || (value < 0 && row.Rule != "signed");
                    Check(illegal ? Has(issues, group, field) : issues.Count == 0, "signed/finite/nonnegative boundary " + row.Path + "=" + value); numericCases++;
                }
                if (row.Rule == "ratio")
                {
                    var data = new DialoguePresentationData(); Set(data, row.Path, 1.01f);
                    Check(Has(ValidateUnchanged(data), group, field), "ratio upper bound " + row.Path); numericCases++;
                    Set(data, row.Path, 1f); Check(ValidateUnchanged(data).Count == 0, "ratio one accepted " + row.Path);
                }
            }
            else if (row.Rule == "enum")
            {
                var data = new DialoguePresentationData(); var value = Get(data, row.Path);
                Set(data, row.Path, Enum.ToObject(value.GetType(), 999));
                Check(Has(ValidateUnchanged(data), group, field), "undefined enum field location " + row.Path); enumCases++;
                foreach (var allowed in Enum.GetValues(value.GetType()))
                { Set(data, row.Path, allowed); Check(ValidateUnchanged(data).Count == 0, "declared enum accepted " + row.Path); }
            }
        }
        var signed = new DialoguePresentationData(); signed.layout[0].stations[0].entries[0].layer = -3;
        Check(ValidateUnchanged(signed).Count == 0, "layer is signed");
        var invalid = new DialoguePresentationData(); invalid.layout[0].stations[0].actorCount = -1;
        Check(Has(ValidateUnchanged(invalid), "layout", "stations[0].actorCount"), "invalid station count located");
        invalid = new DialoguePresentationData(); invalid.layout[0].stations[1].entries[1].id = "s2-left";
        Check(Has(ValidateUnchanged(invalid), "layout", "stations[1].entries[1].id"), "duplicate station id located");
        invalid = new DialoguePresentationData(); invalid.layout[0].stations[0].entries = null;
        Check(Has(ValidateUnchanged(invalid), "layout", "stations[0].entries"), "null entries located without reconstruction");
        invalid = new DialoguePresentationData(); invalid.layout[0].stations[0] = null;
        Check(Has(ValidateUnchanged(invalid), "layout", "stations[0]"), "null set located");
        invalid = new DialoguePresentationData(); invalid.layout[0].stations[0].entries[0] = null;
        Check(Has(ValidateUnchanged(invalid), "layout", "stations[0].entries[0]"), "null station located");
        Check(DialoguePresentationValidation.Validate(null).Count == 1, "missing root data diagnosed");
    }

    private static object Lookup(DialoguePresentationData data, string method, string name, out IReadOnlyList<PresentationConfigDiagnostic> issues)
    {
        var args = new object[] { name, null };
        var result = typeof(DialoguePresentationData).GetMethod(method).Invoke(data, args);
        issues = (IReadOnlyList<PresentationConfigDiagnostic>)args[1]; return result;
    }
    private static void LookupGroups()
    {
        string[] groups = { "tier", "trigger", "entrance", "layout", "speaker", "expression", "text", "reaction", "cg", "exit" };
        string[] methods = { "FindTier", "FindTrigger", "FindEntrance", "FindLayout", "FindSpeaker", "FindExpression", "FindText", "FindReaction", "FindCg", "FindExit" };
        for (int i = 0; i < groups.Length; i++)
        {
            var data = new DialoguePresentationData(); var field = typeof(DialoguePresentationData).GetField(groups[i]);
            var defaults = (Array)field.GetValue(data); var defaultTemplate = defaults.GetValue(0);
            var custom = (DialogueNamedTemplate)Activator.CreateInstance(field.FieldType.GetElementType()); custom.name = "custom";
            var entries = Array.CreateInstance(field.FieldType.GetElementType(), 2); entries.SetValue(defaultTemplate, 0); entries.SetValue(custom, 1); field.SetValue(data, entries);
            IReadOnlyList<PresentationConfigDiagnostic> issues;
            Check(ReferenceEquals(Lookup(data, methods[i], "custom", out issues), custom) && issues.Count == 0, "exact custom lookup " + groups[i]);
            Check(ReferenceEquals(Lookup(data, methods[i], "default", out issues), defaultTemplate) && issues.Count == 0, "exact default " + groups[i]);
            Check(ReferenceEquals(Lookup(data, methods[i], null, out issues), defaultTemplate) && issues.Count == 0, "null profile is default " + groups[i]);
            Check(ReferenceEquals(Lookup(data, methods[i], "", out issues), defaultTemplate) && issues.Count == 0, "empty profile is default " + groups[i]);
            var before = Snapshot(data);
            Check(ReferenceEquals(Lookup(data, methods[i], "CUSTOM", out issues), defaultTemplate) && issues.Count == 1 && !issues[0].IsError && issues[0].Group == groups[i] && issues[0].Template == "CUSTOM", "ordinal missing-name fallback diagnostic " + groups[i]);
            Same(before, Snapshot(data)); Check(ValidateUnchanged(data).Count == 0, "custom template valid " + groups[i]);
            custom.name = " custom "; Check(ReferenceEquals(Lookup(data, methods[i], " custom ", out issues), custom) && issues.Count == 0, "names never trimmed " + groups[i]);
            entries.SetValue(custom, 0); before = Snapshot(data);
            Check(Lookup(data, methods[i], "missing", out issues) == null && issues.Count == 2, "missing default never synthesized " + groups[i]);
            Same(before, Snapshot(data));
            Check(ReferenceEquals(Lookup(data, methods[i], custom.name, out issues), custom) == false && issues.Count >= 2, "duplicate custom not chosen " + groups[i]);
            entries.SetValue(null, 0); Check(ReferenceEquals(Lookup(data, methods[i], custom.name, out issues), custom) && issues.Count == 1 && issues[0].IsError, "existing custom with missing default is reported " + groups[i]);
            Check(ValidateUnchanged(data).Count >= 2, "null template plus missing default validation " + groups[i]);
            custom.name = "default"; entries.SetValue(custom, 0);
            Check(Lookup(data, methods[i], "default", out issues) == null && issues.Count >= 2, "duplicate default refused " + groups[i]);
            Check(ValidateUnchanged(data).Count >= 2, "duplicate/default validation " + groups[i]);
            field.SetValue(data, null); before = Snapshot(data);
            Check(Lookup(data, methods[i], "default", out issues) == null && issues.Count == 1 && issues[0].IsError, "null group diagnosed " + groups[i]);
            Check(ValidateUnchanged(data).Count == 1, "null group validator " + groups[i]); Same(before, Snapshot(data));
        }
        var isolated = new DialoguePresentationData(); isolated.tier[0].normalDim = 0.9f;
        Check(new DialoguePresentationData().tier[0].normalDim == 0.4f, "no shared mutable default templates");
    }

    public static void Run(string fixture)
    {
        var expected = Defaults(fixture); Values(expected); LookupGroups();
        Console.WriteLine("PASS narrative presentation config: " + checks + " checks; fieldRows=" + expected.Count +
            "; groups=10; numericCases=" + numericCases + "; enumFields=" + enumCases + "; nativeAssetInstances=0");
    }
}
