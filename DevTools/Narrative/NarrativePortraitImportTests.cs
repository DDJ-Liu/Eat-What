using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using EatWhat.DataTables;
using EatWhat.Narrative.Unity;
using UnityEngine;
using UnityEditor;

// Actual shared lookup, formal validation and TryApplyRow; AssetDatabase/asset handles are external.
public static class NarrativePortraitImportTests
{
    static readonly List<string> details = new List<string>();
    static void Check(bool value, string name)
    { if (!value) throw new Exception("Portrait import: " + name); details.Add(name); }
    static readonly MethodInfo apply = typeof(DataTableImportService).GetMethod("TryApplyRow", BindingFlags.NonPublic | BindingFlags.Static);
    static DataTableCsvParseResult Csv(DataTableSchema schema, string role, string name)
    {
        var headers = new List<string>(); var cells = new List<string>();
        foreach (var c in schema.columns)
        {
            headers.Add(c.csvColumn);
            cells.Add(c.name == "character_id" ? "chr_fixture" : c.name == "cg_id" ? "cg_fixture" :
                c.name == (role == "portrait" ? "portrait_sprite" : "cg_sprite") ? name : "");
        }
        return DataTableCsvParser.Parse(string.Join(",", headers) + "\n" + string.Join(",", cells) + "\n");
    }
    static List<string> Apply(ScriptableObject asset, DataTableSchema schema, DataTableCsvParseResult csv, out bool changed)
    {
        var args = new object[] { asset, schema, csv.Table.Rows[0], csv.Table, false, null, null };
        Check((bool)apply.Invoke(null, args) && args[5] == null, "actual row import succeeds in memory");
        changed = (bool)args[4]; return (List<string>)args[6];
    }
    public static string Run(string repository, string output)
    {
        details.Clear(); Directory.CreateDirectory(output); AssetDatabase.ResetProofFixture();
        var registered = new List<string>();
        try
        {
            foreach (string role in new[] { "portrait", "cg" })
            {
                string folder = role == "portrait" ? "Portraits" : "CGs";
                for (int slices = 0; slices <= 2; slices++)
                {
                    string name = role + (slices == 0 ? "Single" : slices == 1 ? "One" : "Two");
                    string path = Path.Combine(output, "Assets/Sprites/Narrative", folder, name + ".png").Replace('\\', '/');
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    // File/metadata fixture only; no real texture is decoded or imported.
                    File.WriteAllText(path, "external sprite handle fixture");
                    string meta = "fileFormatVersion: 2\nTextureImporter:\n  spriteMode: " + (slices == 0 ? 1 : 2) + "\n  textureType: 8\n  spriteSheet:\n    sprites:\n";
                    for (int i = 0; i < slices; i++) meta += "    - name: " + name + "_" + i + "\n      rect: {x: 0, y: 0, width: 1, height: 1}\n";
                    File.WriteAllText(path + ".meta", meta);
                    AssetDatabase.ProofGuids[path] = (registered.Count + 1).ToString("x32"); registered.Add(path);
                    // External imported texture/Sprite handles; production lookup still reads the fixture's real mode metadata.
                    var texture = new Texture2D(1,1,TextureFormat.RGBA32,false);
                    AssetDatabase.ProofMain[path] = texture;
                    AssetDatabase.ProofAll[path] = new UnityEngine.Object[] { texture, new Sprite { name = name } };
                }
            }
            AssetDatabase.ProofFindAssets = (filter, folders) => {
                var guids = new List<string>();
                foreach (var path in registered) foreach (var folder in folders)
                    if (path.StartsWith(Path.Combine(output, folder).Replace('\\', '/') + "/", StringComparison.Ordinal))
                    { guids.Add(AssetDatabase.ProofGuids[path]); break; }
                return guids.ToArray();
            };
            foreach (string role in new[] { "portrait", "cg" })
            {
                string tableName = role == "portrait" ? "Characters" : "CGs";
                string field = role == "portrait" ? "portrait_sprite" : "cg_sprite";
                var schema = JsonConvert.DeserializeObject<DataTableSchema>(File.ReadAllText(Path.Combine(repository, "DataTables/Schemas/Narrative", tableName + ".schema.json")));
                for (int slices = 0; slices <= 2; slices++)
                {
                    string name = role + (slices == 0 ? "Single" : slices == 1 ? "One" : "Two");
                    var paths = NarrativeTableValidation.FindFiles(output, role, name);
                    var lookup = NarrativeFilenameLookupPolicy.Resolve(role, name, n => paths);
                    Check(paths.Count == 1, role + " physical exact candidate " + slices);
                    if (slices == 0)
                        Check(lookup.AssetPath == paths[0] && lookup.Warning == null && !lookup.RejectedMultipleSprite, role + " single import resolves whole file");
                    else
                        Check(lookup.AssetPath == null && lookup.RejectedMultipleSprite && lookup.Warning.Contains("导入方式是多张子图，请改为单张"), role + " Multiple rejected regardless of slice count " + slices);
                    string metaBefore = File.ReadAllText(paths[0] + ".meta");
                    var again = NarrativeFilenameLookupPolicy.Resolve(role, name, n => paths);
                    Check(again.AssetPath == lookup.AssetPath && again.Warning == lookup.Warning && again.RejectedMultipleSprite == lookup.RejectedMultipleSprite &&
                        File.ReadAllText(paths[0] + ".meta") == metaBefore, role + " repeated read is identical and does not edit metadata " + slices);
                    var csv = Csv(schema, role, name); Check(csv.Errors.Count == 0, role + " actual parser accepts source fixture " + slices);
                    var context = new DataTableValidationContext();
                    var locSchema = new DataTableSchema { tableName = "Localization" };
                    locSchema.columns.Add(new DataTableColumnSchema { name = "loc_key", csvColumn = "loc_key", type = "string" });
                    var locTable = DataTableCsvParser.Parse("loc_key\nloc_fixture\n").Table;
                    context.ReferenceTables["Localization/Localization.schema.json"] = new DataTableReferenceSource(locSchema, locTable);
                    context.ReferenceValues[DataTableValidator.ReferenceKey("Localization/Localization.schema.json", "loc_key")] =
                        new HashSet<string>(StringComparer.Ordinal) { "loc_fixture" };
                    NarrativeTableValidation.EnableProject(context, output);
                    var report = DataTableValidator.Validate(schema, csv, context);
                    string rule = role == "portrait" ? "[V-18]" : "[V-19]";
                    bool diagnosed = false; foreach (var w in report.Warnings)
                        if (w.Contains(rule) && w.Contains("导入方式是多张子图，请改为单张")) diagnosed = true;
                    Check(report.Errors.Count == 0 && diagnosed == (slices != 0), role + " formal warning is nonblocking and names import mode " + slices);
                    ScriptableObject row = role == "portrait" ? (ScriptableObject)new CK01CharacterData() : new CK01CGData();
                    bool changed; var warnings = Apply(row, schema, csv, out changed);
                    string imported = role == "portrait" ? ((CK01CharacterData)row).portrait_sprite : ((CK01CGData)row).cg_sprite;
                    Check(imported == (slices == 0 ? name : "") && warnings.Count == (slices == 0 ? 0 : 1), role + " actual import keeps Single and clears rejected Multiple derived value " + slices);
                    if (slices != 0) Check(warnings[0].Contains("导入方式是多张子图，请改为单张"), role + " actual importer gives explicit warning " + slices);
                    if (role == "portrait") Check(NarrativeDataAdapter.FromRow((CK01CharacterData)row).PortraitSprite == imported,
                        "actual runtime adapter receives no portrait for rejected Multiple " + slices);
                    Check(DataTableValidator.GetEffectiveValue(DataTableValidator.FindColumn(schema, field), csv.Table.Rows[0], csv.Table) == name,
                        role + " source cell remains unchanged " + slices);
                    Apply(row, schema, csv, out changed); Check(!changed, role + " repeated row import is idempotent " + slices);
                }
                string single = role + "Single";
                Check(NarrativeFilenameLookupPolicy.Resolve(role, single.ToLowerInvariant(), n => NarrativeTableValidation.FindFiles(output, role, n)).AssetPath == null,
                    role + " filename case is exact");
                Check(NarrativeFilenameLookupPolicy.Resolve(role, role + "One_0", n => NarrativeTableValidation.FindFiles(output, role, n)).AssetPath == null,
                    role + " sub-sprite display name is not a file lookup key");
                Check(NarrativeFilenameLookupPolicy.Resolve(role, single + ".png", n => new List<string>()).AssetPath == null,
                    role + " extensions remain invalid");
            }
            return JsonConvert.SerializeObject(new { passed = true, checks = details.Count, details,
                actualSharedLookupAndFormalValidationAndImport = true, assetDatabaseHandlesOnly = true,
                realTextureDecoded = false, productionAssetPersistence = false, originalAssertionsChanged = false });
        }
        finally { AssetDatabase.ResetProofFixture(); }
    }
}
