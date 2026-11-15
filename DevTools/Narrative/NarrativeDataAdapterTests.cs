using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using EatWhat.DataTables;
using EatWhat.Narrative.Core;
using EatWhat.Narrative.Unity;
using UnityEngine;

// Actual generated rows and production importer/adapter, hosted only by existing StaticCompile.
public static class NarrativeDataAdapterTests
{
    static int checks, fieldComparisons;
    static void Check(bool value, string reason) { checks++; if (!value) throw new Exception(reason); }
    static readonly Dictionary<string, string[]> Keys = new Dictionary<string, string[]> {
        {"Characters",new[]{"character_id"}}, {"Dialogues",new[]{"dialogue_id"}}, {"DialogueLines",new[]{"line_id"}},
        {"CGs",new[]{"cg_id"}}, {"Scenes",new[]{"scene_id"}}, {"SceneStates",new[]{"scene_id","state_id"}},
        {"SceneStatePlacements",new[]{"scene_id","state_id","kind","target"}}, {"AmbientPools",new[]{"pool_id"}} };
    static readonly Dictionary<string, string> Queries = new Dictionary<string, string> {
        {"Characters","TryGetCharacter"}, {"Dialogues","TryGetDialogue"}, {"DialogueLines","TryGetDialogueLine"},
        {"CGs","TryGetCg"}, {"Scenes","TryGetScene"}, {"SceneStates","TryGetSceneState"},
        {"SceneStatePlacements","TryGetSceneStatePlacement"}, {"AmbientPools","TryGetAmbientPool"} };
    static string Pascal(string column) { return string.Concat(column.Split('_').Select(x => char.ToUpperInvariant(x[0]) + x.Substring(1))); }
    static object Property(object value, string property)
    {
        var member = value.GetType().GetProperty(property); Check(member != null, value.GetType().Name + "." + property + " exists");
        return member.GetValue(value, null);
    }
    static object Cell(object model, string table, string column)
    {
        if (table == "Characters" && new[] { "portrait_offset_x", "portrait_offset_y", "portrait_scale" }.Contains(column))
            return Property(model, "Configured" + Pascal(column));
        if (table == "Dialogues" && column.StartsWith("cast_", StringComparison.Ordinal))
        {
            var slots = ((NarrativeDialogue)model).CastSlots; int slot = int.Parse(column.Substring(5, 2), CultureInfo.InvariantCulture) - 1;
            Check(slots[slot].Slot == slot + 1, "cast source slot retained");
            return Property(slots[slot], column.Length == 7 ? "CharacterId" : Pascal(column.Substring(8)));
        }
        if (table == "Dialogues" && column.EndsWith("_profile", StringComparison.Ordinal))
            return Property(((NarrativeDialogue)model).Profiles, Pascal(column.Substring(0, column.Length - 8)));
        if (column == "commands") return ((NarrativeDialogueLine)model).Commands.SourceText;
        if (column == "on_end") return ((NarrativeDialogue)model).OnEnd.SourceText;
        return Property(model, Pascal(column));
    }
    static object Find(INarrativeDataDirectory directory, string table, DataTableCsvTable csv, DataTableCsvRow row)
    {
        var args = Keys[table].Select(k => (object)row.Values[csv.GetHeaderIndex(k)]).Concat(new object[] { null, null }).ToArray();
        bool found = (bool)typeof(INarrativeDataDirectory).GetMethod(Queries[table]).Invoke(directory, args);
        Check(found && args[args.Length - 2] != null && args[args.Length - 1] == null, table + " query by complete fixture key");
        return args[args.Length - 2];
    }
    static int VerifyAll(INarrativeDataDirectory directory, Dictionary<string, DataTableCsvTable> source, JObject expected)
    {
        int rows = 0;
        foreach (var table in source)
        foreach (var row in table.Value.Rows)
        {
            rows++; object model = Find(directory, table.Key, table.Value, row);
            Check(model.GetType().GetProperty("Comment") == null, "authoring-only comment is not invented in runtime model");
            foreach (var column in (JArray)expected["tables"][table.Key])
            {
                string name = (string)column["name"], type = (string)column["type"];
                if (name == "comment") continue;
                string raw = row.Values[table.Value.GetHeaderIndex(name)]; object actual = Cell(model, table.Key, name); fieldComparisons++;
                if (type == "float") Check(raw.Length == 0 ? actual == null : actual is float && (float)actual == float.Parse(raw, CultureInfo.InvariantCulture), table.Key + "." + name + " nullable float");
                else if (type == "int") Check(raw.Length == 0 ? actual == null : actual is int && (int)actual == int.Parse(raw, CultureInfo.InvariantCulture), table.Key + "." + name + " nullable int");
                else if (type == "bool") Check(raw.Length == 0 ? actual == null : actual is bool && (bool)actual == bool.Parse(raw), table.Key + "." + name + " bool presence");
                else if (type == "list<ref>") Check(((IEnumerable<string>)actual).SequenceEqual(raw.Split(';')), table.Key + "." + name + " copied list");
                else Check((string)actual == (table.Key == "CGs" && raw.Length == 0 ? null : raw), table.Key + "." + name + " exact text");
            }
        }
        return rows;
    }

    public static string Run(string root)
    {
        checks = 0; fieldComparisons = 0;
        string fixture = Path.Combine(root, "DevTools/Narrative/Fixtures/TablesV25");
        var expected = JObject.Parse(File.ReadAllText(Path.Combine(fixture, "expected-contract.json")));
        var tables = new List<CK01GeneratedDataTable>(); var source = new Dictionary<string, DataTableCsvTable>();
        var generatedRows = new List<ScriptableObject>();
        var apply = typeof(DataTableImportService).GetMethod("TryApplyRow", BindingFlags.NonPublic | BindingFlags.Static);
        foreach (var registration in CK01ImportRegistry.GetRegistrations().Where(x => Keys.ContainsKey(x.TableName)))
        {
            var schema = JsonConvert.DeserializeObject<DataTableSchema>(File.ReadAllText(Path.Combine(root, "DataTables", registration.SchemaPath)));
            var csv = DataTableCsvParser.Parse(File.ReadAllText(Path.Combine(fixture, schema.csvPath))).Table;
            source.Add(schema.tableName, csv); var rows = new List<CK01GeneratedDataRowReference>();
            foreach (var row in csv.Rows)
            {
                var asset = (ScriptableObject)Activator.CreateInstance(typeof(CK01CharacterData).Assembly.GetType(schema.targetType));
                var args = new object[] { asset, schema, row, csv, false, null, null };
                Check((bool)apply.Invoke(null, args), "actual production TryApplyRow: " + args[5]);
                generatedRows.Add(asset); rows.Add(new CK01GeneratedDataRowReference(DataTableValidator.GetRowIdentity(schema, row, csv, false), asset));
            }
            tables.Add(new CK01GeneratedDataTable(schema.tableName, null, rows));
        }
        Check(source.Count == 8 && generatedRows.Count == 20, "eight permanent fixture tables, twenty actual generated rows");
        var catalog = new CK01GeneratedDataCatalog(); catalog.ReplaceTables(tables);
        INarrativeDataDirectory directory; IReadOnlyList<NarrativeDataDiagnostic> diagnostics;
        Check(NarrativeDataAdapter.TryCreate(catalog, out directory, out diagnostics) && diagnostics.Count == 0, "production catalog to immutable Core directory");
        int rowsCompared = VerifyAll(directory, source, expected); int firstComparisons = fieldComparisons;
        Check(rowsCompared == 20 && firstComparisons == 296, "all 296 non-comment fixture cells compared independently");
        NarrativeDialogue normal; NarrativeDialogueLine line; NarrativeAmbientPool pool; string diagnostic;
        Check(directory.TryGetDialogue("dlg_normal", out normal, out diagnostic) && normal.Cast.Count == 4 && normal.Level == NarrativeDialogueLevel.Normal, "generated four slots are data, not model capacity");
        Check(directory.TryGetDialogueLine("ln_normal", out line, out diagnostic) && line.Commands.Success && line.Commands.Commands.Count == 2, "commands parsed on adaptation");
        var cachedCommands = line.Commands; var cachedEnd = normal.OnEnd;
        NarrativeLineChain chain;
        Check(directory.TryGetLineChain("dlg_normal", out chain, out diagnostic) && chain.Lines.Count == 2 && chain.Lines[1].LineId == "ln_story", "actual fixture chain crosses a differing label");
        Check(chain.Diagnostics.Count == 1 && chain.Diagnostics[0].Kind == NarrativeDataDiagnosticKind.DialogueLabelMismatch, "actual label diagnostic retained");
        Check(directory.TryGetAmbientPool("amb_pool", out pool, out diagnostic), "fixture pool lookup");
        var poolList = pool.DialogueIds as IList<string>; bool listDenied = false;
        try { poolList[0] = "changed"; } catch (NotSupportedException) { listDenied = true; }
        Check(listDenied, "adapter does not expose generated mutable pool list");
        foreach (var row in generatedRows)
        foreach (var field in row.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            if (field.FieldType == typeof(string)) field.SetValue(row, "changed source");
            else if (field.FieldType == typeof(bool)) field.SetValue(row, !(bool)field.GetValue(row));
            else if (field.FieldType == typeof(CK01OptionalFloat)) field.SetValue(row, new CK01OptionalFloat(999));
            else if (field.FieldType == typeof(CK01OptionalInt)) field.SetValue(row, new CK01OptionalInt(999));
            else if (field.FieldType == typeof(CK01OptionalBool)) field.SetValue(row, new CK01OptionalBool(true));
            else if (field.FieldType == typeof(List<string>)) ((List<string>)field.GetValue(row)).Clear();
        }
        catalog.ReplaceTables(new CK01GeneratedDataTable[0]); tables.Clear();
        Check(VerifyAll(directory, source, expected) == 20 && fieldComparisons == 592, "all row field and source catalog mutations are isolated");
        Check(directory.TryGetDialogueLine("ln_normal", out line, out diagnostic) && ReferenceEquals(cachedCommands, line.Commands), "line command cache survives query/source mutation");
        Check(directory.TryGetDialogue("dlg_normal", out normal, out diagnostic) && ReferenceEquals(cachedEnd, normal.OnEnd), "on_end cache survives query/source mutation");

        var characterRow = new CK01CharacterData { character_id = "chr_defaults", portrait_offset_x = new CK01OptionalFloat { value = 123 }, portrait_offset_y = new CK01OptionalFloat { value = 234 }, portrait_scale = new CK01OptionalFloat { value = 345 } };
        var character = NarrativeDataAdapter.FromRow(characterRow);
        Check(character.PortraitOffsetX == 0 && character.PortraitOffsetY == 0 && character.PortraitScale == 1 && !character.ConfiguredPortraitScale.HasValue, "actual hasValue false ignores stored payload and applies only schema character defaults");
        characterRow.portrait_offset_x = new CK01OptionalFloat(0); characterRow.portrait_offset_y = new CK01OptionalFloat(0); characterRow.portrait_scale = new CK01OptionalFloat(2);
        character = NarrativeDataAdapter.FromRow(characterRow);
        Check(character.ConfiguredPortraitOffsetX.HasValue && character.PortraitOffsetX == 0 && character.ConfiguredPortraitOffsetY.HasValue && character.PortraitScale == 2, "explicit zero offsets retained");
        var cgRow = new CK01CGData { cg_id = "cg_nullable", cg_sprite = "raw sprite", ken_burns_pan_x = new CK01OptionalFloat(0), ken_burns_pan_y = new CK01OptionalFloat { value = 77 }, keep_box = new CK01OptionalBool(false), keep_portraits = new CK01OptionalBool { value = true }, keep_name = new CK01OptionalBool(true) };
        var cg = NarrativeDataAdapter.FromRow(cgRow);
        Check(cg.TransitionMode == null && cg.KenBurnsCurve == null && !cg.FadeIn.HasValue && !cg.FadeOut.HasValue && !cg.KenBurnsZoom.HasValue && !cg.KenBurnsDuration.HasValue, "all unspecified CG values remain unset for N008");
        Check(cg.KenBurnsPanX == 0 && !cg.KenBurnsPanY.HasValue && cg.KeepBox == false && !cg.KeepPortraits.HasValue && cg.KeepName == true, "independent axes/hasValue and explicit false");
        var badLine = NarrativeDataAdapter.FromRow(new CK01DialogueLineData { line_id = "ln_bad", commands = " wait(1);unknown() " });
        Check(!badLine.Commands.Success && badLine.Commands.Commands.Count == 0 && badLine.Commands.SourceText == " wait(1);unknown() " && badLine.Commands.Diagnostics.Count > 0, "actual line adapter blocks valid prefix on overall failure, preserves original positions/text");
        var badEnd = NarrativeDataAdapter.FromRow(new CK01DialogueData { dialogue_id = "dlg_bad", tier = "story", on_end = "wait(0);cg_show(", overrides = "unknown.group=raw", cg_profile = " unknown template " });
        Check(!badEnd.OnEnd.Success && badEnd.OnEnd.Commands.Count == 0 && badEnd.OnEnd.SourceText == "wait(0);cg_show(" && badEnd.OnEnd.Diagnostics.Count > 0, "actual on_end parser failure retained without execution");
        Check(badEnd.Profiles.Cg == " unknown template " && badEnd.Profiles.Tier == "" && badEnd.Overrides == "unknown.group=raw", "adapter neither resolves profile names nor parses overrides");
        bool tierDenied = false; try { NarrativeDataAdapter.FromRow(new CK01DialogueData { tier = "typo" }); } catch (FormatException) { tierDenied = true; }
        Check(tierDenied, "unknown tier does not silently become ambient");
        bool nullDenied = false; try { NarrativeDataAdapter.FromRow((CK01DialogueLineData)null); } catch (ArgumentNullException) { nullDenied = true; }
        Check(nullDenied, "null row diagnostic boundary");
        INarrativeDataDirectory failed;
        Check(!NarrativeDataAdapter.TryCreate(null, out failed, out diagnostics) && failed == null && diagnostics.Count == 1, "missing source catalog explicit failure");
        Check(!NarrativeDataAdapter.TryCreate(catalog, out failed, out diagnostics) && failed == null && diagnostics.Count > 0, "missing tables do not publish a partial directory");
        return JsonConvert.SerializeObject(new { passed = true, checks, fixtureTables = 8, convertedFixtureRows = generatedRows.Count,
            runtimeFieldComparisons = fieldComparisons, sourceMutationIsolation = true, commandColumnsParsedOncePerModel = true,
            host = "existing DataTables StaticCompile", unityNativeLaunched = false });
    }
}
