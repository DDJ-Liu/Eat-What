using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using OfficeOpenXml;
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "DataTableParser", menuName = "Scriptable Objects/DataTableParser")]
public class DataTableParser : SerializedScriptableObject
{
#if UNITY_EDITOR
    [SerializeField, AssetsOnly, Required, LabelText("表格 (.xlsx)")]
    [Tooltip("拖入要校验的数据源表格。每页前四行是表头（字段名、类型、约束、说明），第五行起是业务数据。无需另配规则文件。")]
    private DefaultAsset workbook;

    [ShowInInspector, ReadOnly, MultiLineProperty(9), LabelText("最近一次校验")]
    private string Report { get; set; } = "尚未校验。";

    private const string ParserSettingKey = "EatWhat.DataTables.DefaultParser";
    private const string ParserSettingsPath = "Project/数据表";

    [SettingsProvider]
    private static SettingsProvider CreateParserSettings() => new SettingsProvider(ParserSettingsPath, SettingsScope.Project)
    {
        keywords = new HashSet<string> { "数据表", "默认解析器", "DataTableParser", "Excel" },
        guiHandler = _ =>
        {
            EditorBuildSettings.TryGetConfigObject(ParserSettingKey, out DataTableParser current);
            EditorGUI.BeginChangeCheck();
            var next = (DataTableParser)EditorGUILayout.ObjectField("默认解析器", current, typeof(DataTableParser), false);
            if (EditorGUI.EndChangeCheck() && next != current)
            {
                if (next == null) EditorBuildSettings.RemoveConfigObject(ParserSettingKey);
                else if (AssetDatabase.Contains(next)) EditorBuildSettings.AddConfigObject(ParserSettingKey, next, true);
                else Debug.LogWarning("默认解析器必须是已保存的 DataTableParser 资产。");
            }
            EditorGUILayout.HelpBox("Tools → 数据表 菜单使用此解析器。Excel 工作簿仍在解析器资产上设置。", MessageType.Info);
        }
    };

    private static DataTableParser GetProjectParser()
    {
        if (EditorBuildSettings.TryGetConfigObject(ParserSettingKey, out DataTableParser parser) && parser != null) return parser;
        Debug.LogWarning("未配置默认解析器，未执行配表操作。请在 Project Settings → 数据表 中指定 DataTableParser 资产。");
        SettingsService.OpenProjectSettings(ParserSettingsPath);
        return null;
    }

    [MenuItem("Tools/数据表/校验表格", false, 100)]
    private static void ValidateFromMenu() => GetProjectParser()?.ValidateWorkbook();

    [MenuItem("Tools/数据表/解析并生成 SO", false, 101)]
    private static void GenerateFromMenu() => GetProjectParser()?.GenerateTables();

    [Button("校验表格")]
    private void ValidateWorkbook()
    {
        Report = ValidateFile(AssetDatabase.GetAssetPath(workbook), out bool valid);
        if (valid) Debug.Log(Report, this);
        else Debug.LogError(Report, this);
    }

    [Button("解析并生成 SO")]
    private void GenerateTables()
    {
        Report = GenerateFile(AssetDatabase.GetAssetPath(workbook), AssetDatabase.GetAssetPath(this), out bool generated);
        if (generated) Debug.Log($"数据表生成完毕：{workbook.name}\n{Report}", this);
        else Debug.LogError(Report, this);
    }

    private sealed class Diagnostics
    {
        readonly List<string> details = new List<string>();
        public int Errors;
        public void Add(string at, string message)
        {
            Errors++;
            if (details.Count < 100) details.Add($"错误 {at}：{message}");
        }
        public string Summary(int sheets, int rows)
        {
            string status = Errors > 0 ? "未通过" : "表内规则通过";
            return $"{status}。{sheets} 张工作表，{rows} 条数据，{Errors} 个错误。"
                + (details.Count == 0 ? "" : "\n" + string.Join("\n", details))
                + (Errors > details.Count ? "\n仅显示前 100 项。" : "");
        }
    }

    private sealed class Column
    {
        public int Index;
        public string Name, Type, Rule;
    }
    private sealed class Sheet
    {
        public ExcelWorksheet Page;
        public string Id;
        public readonly List<Column> Columns = new List<Column>();
        public readonly Dictionary<string, int> Fields = new Dictionary<string, int>();
        public readonly SortedSet<int> Rows = new SortedSet<int>();
        public readonly Dictionary<string, int> Keys = new Dictionary<string, int>();
        public Column[] KeyColumns = Array.Empty<Column>();
        public string At(int row, int col) => Page.Name + "!" + Page.Cells[row, col].Address;
        public string Raw(int row, int col) => Convert.ToString(Page.Cells[row, col].Value, CultureInfo.InvariantCulture) ?? "";
        public string Value(int row, int col) => Raw(row, col).Trim();
    }

    internal static string ValidateFile(string path, out bool valid) => ProcessFile(path, null, out valid);
    internal static string GenerateFile(string path, string parserAssetPath, out bool generated) => ProcessFile(path, parserAssetPath ?? string.Empty, out generated);

    // Null selects validation only. Generation validates the same snapshot before any output I/O.
    private static string ProcessFile(string path, string parserAssetPath, out bool valid)
    {
        bool generate = parserAssetPath != null;
        valid = false;
        if (string.IsNullOrEmpty(path)) return "未通过：请先拖入要校验的 .xlsx 表格。";
        if (!string.Equals(Path.GetExtension(path), ".xlsx", StringComparison.OrdinalIgnoreCase)) return "未通过：只接受 .xlsx 文件。";
        var report = new Diagnostics();
        var sheets = new Dictionary<string, Sheet>();
        try
        {
            using (var input = OpenWorkbookCopy(path))
            using (var package = new ExcelPackage(input))
            {
                foreach (var page in package.Workbook.Worksheets)
                {
                    var name = Regex.Match(page.Name, @"^(T[1-9]\d*)(?:_.+)?$");
                    if (!name.Success) { report.Add(page.Name, "工作表名应以 T 和正整数编号标识，可用下划线接名称。"); continue; }
                    var s = new Sheet { Page = page, Id = name.Groups[1].Value };
                    if (sheets.ContainsKey(s.Id)) { report.Add(page.Name, "表编号重复：" + s.Id); continue; }
                    sheets.Add(s.Id,s);
                    ReadSheet(s,report);
                    IndexKeys(s,report);
                }
                if (sheets.Count == 0) report.Add("工作簿", "没有可校验的数据工作表。");
                foreach (var s in sheets.Values) ValidateRows(s,sheets,report);
                string summary = report.Summary(sheets.Count, sheets.Values.Sum(s => s.Rows.Count));
                if (report.Errors != 0) return generate ? summary + "\n未写入任何表资产。" : summary;
                if (generate) summary += "\n" + WriteTables(path, parserAssetPath, sheets.Values);
                valid = true;
                return summary;
            }
        }
        catch (Exception ex) { return (generate ? "生成失败：" : "未通过：无法读取或解析工作簿。") + ex.GetBaseException().Message; }
    }

    private sealed class Output
    {
        public string Path, PreviousPath, PreviousGuid, CaseRenamePath;
        public Sheet Source;
        public ParsedDataTable Existing;
        public ParsedDataTable.Column[] Columns;
        public ParsedDataTable.Row[] Rows;
    }

    // Old text assets stored ownership in the SO. Read only the main object's legacy header
    // for migration; never modify YAML, or adopt an unmarked asset by name alone.
    private static bool HasLegacyOwner(string path, ParsedDataTable asset, string guid, string id)
    {
        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string assetGuid, out long fileId) ||
            assetGuid != AssetDatabase.AssetPathToGUID(path)) return false;
        string header = "--- !u!114 &" + fileId.ToString(CultureInfo.InvariantCulture);
        using (var reader = File.OpenText(path))
        {
            if (!(reader.ReadLine() ?? "").StartsWith("%YAML", StringComparison.Ordinal)) return false;
            bool main = false, matchGuid = false, matchId = false;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith("--- ", StringComparison.Ordinal))
                {
                    if (main) break;
                    main = line == header;
                }
                else if (main)
                {
                    if (line == "  columns:" || line == "  rows:") break;
                    if (line == "  sourceWorkbookGuid: " + guid) matchGuid = true;
                    if (line == "  tableId: " + id) matchId = true;
                }
            }
            return matchGuid && matchId;
        }
    }

    private static string OwnerStamp(string guid, string id) => "DataTableParser/v1:" + guid + ":" + id;

    private static void RecordOwner(string path, string stamp)
    {
        var importer = AssetImporter.GetAtPath(path);
        if (importer == null) throw new IOException("目标缺少导入器：" + path);
        if (importer.userData == stamp) return;
        if (!string.IsNullOrEmpty(importer.userData)) throw new IOException("拒绝覆盖目标已有的导入器标记：" + path);
        var previous = importer.userData;
        try
        {
            importer.userData = stamp;
            if (!AssetDatabase.WriteImportSettingsIfDirty(path)) throw new IOException("未能保存生成归属标记：" + path);
        }
        catch
        {
            importer.userData = previous;
            throw;
        }
    }

    private static void CheckOutputName(string name)
    {
        // Reject, never sanitize: two distinct page names must not collapse to one file.
        if (string.IsNullOrEmpty(name) || name.Any(c => char.IsControl(c) || "<>:\"/\\|?*".IndexOf(c) >= 0) ||
            name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.EndsWith(".", StringComparison.Ordinal) ||
            name.EndsWith(" ", StringComparison.Ordinal))
            throw new IOException("不能用作输出文件名：" + name);
    }

    private static bool OutputOccupied(string path) => File.Exists(path) || Directory.Exists(path) ||
        File.Exists(path + ".meta") || Directory.Exists(path + ".meta") || AssetDatabase.LoadMainAssetAtPath(path) != null;

    private static void CheckOutputLink(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("拒绝使用链接作为输出路径：" + path);
    }

    private static void PlanExisting(Output output, IEnumerable<string> paths, string folder, string guid)
    {
        string id = output.Source.Id, legacy = folder + "/" + id + ".asset";
        foreach (string candidate in paths)
        {
            bool exact = string.Equals(candidate, output.Path, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(candidate, legacy, StringComparison.OrdinalIgnoreCase);
            // Other sheet suffixes are discovery hints only; a filename never proves ownership.
            if (!exact && !Path.GetFileName(candidate).StartsWith(id + "_", StringComparison.OrdinalIgnoreCase)) continue;
            var table = AssetDatabase.LoadMainAssetAtPath(candidate) as ParsedDataTable;
            var importer = AssetImporter.GetAtPath(candidate);
            if (table == null || importer == null || (importer.userData != OwnerStamp(guid, id) &&
                !(string.IsNullOrEmpty(importer.userData) && HasLegacyOwner(candidate, table, guid, id))))
            {
                if (exact) throw new IOException("无法确认此资产归属，拒绝覆盖：" + candidate);
                continue;
            }
            if (output.Existing != null) throw new IOException($"{id} 有多个同源表资产：{output.PreviousPath}、{candidate}");
            if (!File.Exists(candidate) || AssetDatabase.GetAssetPath(table) != candidate)
                throw new IOException("目标的磁盘路径与导入路径不一致：" + candidate);
            CheckOutputLink(candidate);
            if (File.Exists(candidate + ".meta")) CheckOutputLink(candidate + ".meta");
            if (EditorUtility.IsDirty(table) || EditorUtility.IsDirty(importer)) throw new IOException("目标有未保存修改：" + candidate);
            if (AssetDatabase.LoadAllAssetsAtPath(candidate).Any(asset => asset != table))
                throw new IOException("目标包含不属于生成表的子资产：" + candidate);
            output.Existing = table;
            output.PreviousPath = candidate;
            output.PreviousGuid = AssetDatabase.AssetPathToGUID(candidate);
            if (string.IsNullOrEmpty(output.PreviousGuid)) throw new IOException("目标缺少 GUID：" + candidate);
        }
        if (output.PreviousPath == output.Path) return;
        if (string.Equals(output.PreviousPath, output.Path, StringComparison.OrdinalIgnoreCase))
        {
            // A direct case-only move is not portable on case-insensitive filesystems.
            output.CaseRenamePath = folder + "/Rename_" + Guid.NewGuid().ToString("N") + ".asset";
            if (OutputOccupied(output.CaseRenamePath)) throw new IOException("临时改名路径被占用：" + output.CaseRenamePath);
        }
        else if (OutputOccupied(output.Path)) throw new IOException("目标路径被占用：" + output.Path);
        if (output.PreviousPath != null)
        {
            string error = AssetDatabase.ValidateMoveAsset(output.PreviousPath, output.CaseRenamePath ?? output.Path);
            if (!string.IsNullOrEmpty(error)) throw new IOException($"无法从 {output.PreviousPath} 改名：{error}");
        }
    }

    private static void MoveOutput(string from, string to, string guid)
    {
        if (OutputOccupied(to)) throw new IOException("改名目标被占用：" + to);
        string error = AssetDatabase.MoveAsset(from, to);
        if (!string.IsNullOrEmpty(error) || AssetDatabase.AssetPathToGUID(to) != guid)
            throw new IOException($"表资产改名失败 {from} → {to}：{error}");
    }

    private static string WriteTables(string sourcePath, string parserAssetPath, IEnumerable<Sheet> sheets)
    {
        // Inspector references resolve to project-relative paths; never emit assets elsewhere.
        string path = sourcePath.Replace('\\', '/');
        if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.Split('/').Any(p => p == ".." || p == "." || p.Length == 0))
            throw new IOException("请引用 Assets 目录中的工作簿。");
        string guid = AssetDatabase.AssetPathToGUID(path);
        if (string.IsNullOrEmpty(guid)) throw new IOException("找不到工作簿的 Unity GUID：" + path);
        string parserPath = parserAssetPath.Replace('\\', '/');
        if (!parserPath.StartsWith("Assets/", StringComparison.Ordinal) || parserPath.Split('/').Any(p => p == ".." || p == "." || p.Length == 0) ||
            !string.Equals(Path.GetExtension(parserPath), ".asset", StringComparison.OrdinalIgnoreCase))
            throw new IOException("请先将 DataTableParser 资产保存到 Assets 目录，再生成表资产。");
        string parent = parserPath.Substring(0, parserPath.LastIndexOf('/'));
        string folderName = Path.GetFileNameWithoutExtension(path) + "_Parsed";
        CheckOutputName(folderName);
        string folder = parent + "/" + folderName;
        if (!AssetDatabase.IsValidFolder(parent)) throw new IOException("解析器目录不存在：" + parent);
        for (string ancestor = parent; ; ancestor = ancestor.Substring(0, ancestor.LastIndexOf('/')))
        {
            CheckOutputLink(ancestor);
            if (ancestor == "Assets") break;
        }
        bool createFolder = !AssetDatabase.IsValidFolder(folder);
        if (createFolder && OutputOccupied(folder)) throw new IOException("目标目录被占用：" + folder);
        if (!createFolder) CheckOutputLink(folder);

        // Include unimported files, directories and orphan .meta files in the collision check.
        var paths = createFolder ? Array.Empty<string>() : Directory.GetFileSystemEntries(folder)
            .Select(p => p.Replace('\\', '/'))
            .Select(p => p.EndsWith(".meta", StringComparison.OrdinalIgnoreCase) ? p.Substring(0, p.Length - 5) : p)
            .Where(p => p.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)).Distinct(StringComparer.Ordinal).ToArray();
        var outputs = new List<Output>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Sheet s in sheets)
        {
            CheckOutputName(s.Page.Name);
            string outputPath = folder + "/" + s.Page.Name + ".asset";
            if (!names.Add(outputPath)) throw new IOException("输出文件名重复：" + outputPath);
            var output = new Output {
                Path = outputPath, Source = s,
                Columns = s.Columns.Select(c => new ParsedDataTable.Column(c.Name, c.Type, c.Rule == "id")).ToArray(),
                Rows = s.Rows.Select(row => new ParsedDataTable.Row(s.Columns.Select(c => s.Raw(row, c.Index)).ToArray())).ToArray()
            };
            PlanExisting(output, paths, folder, guid);
            outputs.Add(output);
        }
        if (createFolder)
        {
            string created = AssetDatabase.CreateFolder(parent, folderName);
            if (string.IsNullOrEmpty(created) || AssetDatabase.GUIDToAssetPath(created) != folder)
                throw new IOException("未能创建指定输出目录：" + folder);
        }
        int saved = 0;
        foreach (var output in outputs)
        {
            var table = output.Existing;
            bool isNew = table == null;
            try
            {
                string stamp = OwnerStamp(guid, output.Source.Id);
                if (isNew)
                {
                    if (OutputOccupied(output.Path)) throw new IOException("新建目标路径被占用。");
                    table = ScriptableObject.CreateInstance<ParsedDataTable>();
                }
                else
                {
                    // Persist proof before the legacy serialized header is replaced. Move the
                    // asset itself (including its .meta); never recreate it at the new name.
                    RecordOwner(output.PreviousPath, stamp);
                    if (output.PreviousPath != output.Path)
                    {
                        string from = output.PreviousPath;
                        if (output.CaseRenamePath != null)
                        {
                            MoveOutput(from, output.CaseRenamePath, output.PreviousGuid);
                            from = output.CaseRenamePath;
                        }
                        try { MoveOutput(from, output.Path, output.PreviousGuid); }
                        catch when (output.CaseRenamePath != null && AssetDatabase.AssetPathToGUID(from) == output.PreviousGuid)
                        {
                            MoveOutput(from, output.PreviousPath, output.PreviousGuid);
                            throw;
                        }
                        // A move may reimport the main object. Do not write through a stale handle.
                        table = AssetDatabase.LoadMainAssetAtPath(output.Path) as ParsedDataTable;
                        if (table == null || AssetImporter.GetAtPath(output.Path)?.userData != stamp || EditorUtility.IsDirty(table))
                            throw new IOException("改名后无法确认表资产、归属或未保存状态：" + output.Path);
                    }
                }
                table.Import(output.Columns, output.Rows);
                if (isNew) AssetDatabase.CreateAsset(table, output.Path);
                else EditorUtility.SetDirty(table);
                AssetDatabase.SaveAssetIfDirty(table);
                if (AssetDatabase.GetAssetPath(table) != output.Path || EditorUtility.IsDirty(table))
                    throw new IOException("资产未保存至目标路径。");
                if (isNew) RecordOwner(output.Path, stamp);
                saved++;
            }
            catch (Exception ex)
            {
                if (isNew && table != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(table))) UnityEngine.Object.DestroyImmediate(table);
                throw new IOException($"{output.Path} 写入失败；此前已保存 {saved}/{outputs.Count} 张表。{ex.Message}");
            }
        }
        return $"已写入 {saved} 个表资产（新建 {outputs.Count(o => o.Existing == null)} 个），目录：{folder}";
    }

    // EPPlus 4 expects eight hex digits in differential-format colors. Some exporters
    // write six. Normalize only that non-data metadata on an expandable in-memory copy.
    private static MemoryStream OpenWorkbookCopy(string path)
    {
        var copy = new MemoryStream();
        try
        {
            using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) input.CopyTo(copy);
            copy.Position = 0;
            using (var zip = new ZipArchive(copy, ZipArchiveMode.Update, true))
            {
                var styles = zip.GetEntry("xl/styles.xml");
                if (styles != null)
                {
                    XDocument xml;
                    using (var stream = styles.Open()) xml = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
                    var dxfs = xml.Root?.Element(xml.Root.Name.Namespace + "dxfs");
                    var colors = dxfs?.Descendants().Attributes("rgb").Where(a => Regex.IsMatch(a.Value, @"^[0-9A-Fa-f]{6}$")).ToArray();
                    if (colors != null && colors.Length > 0)
                    {
                        foreach (var color in colors) color.Value = "FF" + color.Value;
                        using (var stream = styles.Open())
                        {
                            stream.SetLength(0);
                            xml.Save(stream, SaveOptions.DisableFormatting);
                        }
                    }
                }
            }
            copy.Position = 0;
            return copy;
        }
        catch { copy.Dispose(); throw; }
    }

    private static void ReadSheet(Sheet s, Diagnostics report)
    {
        var used = new SortedSet<int>();
        if (s.Page.Dimension != null)
            foreach (var cell in s.Page.Cells[s.Page.Dimension.Address])
            {
                bool formula = !string.IsNullOrEmpty(cell.Formula);
                if (!formula && string.IsNullOrWhiteSpace(Convert.ToString(cell.Value,CultureInfo.InvariantCulture))) continue;
                used.Add(cell.Start.Column);
                if (cell.Start.Row >= 5) s.Rows.Add(cell.Start.Row);
                if (formula || cell.Value is ExcelErrorValue) report.Add(s.Page.Name + "!" + cell.Address,"不能用公式缓存或 Excel 错误值作判定。");
            }
        if (used.Count == 0) report.Add(s.Page.Name,"缺少数据表头（字段名、类型、约束、说明）。");
        foreach (int i in used)
        {
            var c = new Column { Index = i, Name = s.Value(1,i), Type = s.Value(2,i), Rule = s.Value(3,i) };
            if (c.Name == "" || s.Fields.ContainsKey(c.Name)) { report.Add(s.At(1,i),"字段名为空或重复：" + c.Name); continue; }
            s.Fields.Add(c.Name,i);
            s.Columns.Add(c);
            if (s.Value(4,i) == "") report.Add(s.At(4,i),"缺少字段说明。");
        }
    }

    private static void IndexKeys(Sheet s, Diagnostics report)
    {
        // Only columns explicitly marked 'id' participate. No implicit first-column key.
        s.KeyColumns = s.Columns.Where(c => c.Rule == "id").ToArray();
        if (s.KeyColumns.Length == 0) return;
        foreach (int row in s.Rows)
        {
            var parts = s.KeyColumns.Select(c => s.Value(row, c.Index)).ToArray();
            if (parts.Any(v => v.Length == 0)) continue; // The 'id' rule reports the exact empty cell.
            string key = ParsedDataTable.ComposeKey(parts);
            if (s.Keys.TryGetValue(key, out int first))
                report.Add(s.At(row, s.KeyColumns[0].Index), $"主键 {key} 与第 {first} 行重复。");
            else s.Keys.Add(key, row);
        }
    }

    // Bind declarations once against the complete workbook, including sheets with no data rows.
    private static Sheet ReferenceTarget(Dictionary<string, Sheet> tables, string id)
    {
        if (!tables.TryGetValue(id, out var target)) throw new FormatException("引用的目标表不存在：" + id);
        if (target.KeyColumns.Length == 0) throw new FormatException(target.Page.Name + " 未声明 id 列，不能按键引用。");
        return target;
    }

    // A reference may contain the full key. For a component column, also accept split
    // references when the source supplies every other component under its declared name.
    private static Func<int, string, string> ReferenceKey(Sheet target, Sheet source, string sourceField)
    {
        if (target.KeyColumns.Length == 1) return (_, value) => value;
        var keys = target.KeyColumns;
        int component = Array.FindIndex(keys, k => k.Name == sourceField);
        if (component < 0 || keys.Any(k => !source.Fields.ContainsKey(k.Name))) return (_, value) => value;
        var cols = keys.Select(k => source.Fields[k.Name]).ToArray();
        return (row, value) =>
        {
            if (target.Keys.ContainsKey(value)) return value;
            var parts = new string[cols.Length];
            for (int i = 0; i < cols.Length; i++) parts[i] = i == component ? value : source.Value(row, cols[i]);
            return ParsedDataTable.ComposeKey(parts) ?? value;
        };
    }

    private static Func<int, string, string> BindType(string type, Dictionary<string, Sheet> tables, Sheet source, string sourceField)
    {
        switch (type)
        {
            case "string": return (row, v) => null;
            case "int": return (row, v) => int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) ? null : "不是整数：" + v;
            case "float": return (row, v) => float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) && !float.IsNaN(n) && !float.IsInfinity(n) ? null : "不是有限浮点数：" + v;
            case "bool": return (row, v) => bool.TryParse(v, out _) ? null : "应为 true 或 false：" + v;
        }
        var list = Regex.Match(type, @"^list<([^<>]+)>$");
        if (list.Success)
        {
            var check = BindType(list.Groups[1].Value.Trim(), tables, source, sourceField);
            return (row, v) =>
            {
                foreach (var raw in v.Split(';'))
                {
                    var item = raw.Trim();
                    if (item.Length == 0) return "列表含空元素。";
                    var error = check(row, item);
                    if (error != null) return error;
                }
                return null;
            };
        }
        var enumeration = Regex.Match(type, @"^enum\(([^()]*)\)$");
        if (enumeration.Success)
        {
            var choices = enumeration.Groups[1].Value.Split(',').Select(v => v.Trim()).ToArray();
            var set = new HashSet<string>(choices, StringComparer.Ordinal);
            if (set.Contains("") || set.Count != choices.Length) throw new FormatException("枚举选项为空或重复：" + type);
            return (row, v) => set.Contains(v) ? null : "不在 " + type + " 中：" + v;
        }
        var reference = Regex.Match(type, @"^ref:(T[1-9]\d*)$");
        if (reference.Success)
        {
            var target = ReferenceTarget(tables, reference.Groups[1].Value);
            var resolve = ReferenceKey(target, source, sourceField);
            return (row, v) =>
            {
                string key = resolve(row, v);
                return target.Keys.ContainsKey(key) ? null : $"在 {target.Page.Name} 中找不到引用：{key}";
            };
        }
        throw new FormatException("未知或已停用的字段类型：" + type);
    }

    // conditional: <field or lookup(table,key).field> (== | !=) "text" [ || ... ]
    // Consume a complete comparison at a time so || inside a quoted value is not a separator.
    private const string Identifier = @"[\p{L}_]\w*";
    private static readonly Regex Comparison = new Regex(
        @"\G\s*(?:lookup\s*\(\s*(?<table>T[1-9]\d*)\s*,\s*(?<key>" + Identifier + @")\s*\)\s*\.\s*)?" +
        @"(?<field>" + Identifier + @")\s*(?<op>==|!=)\s*""(?<literal>(?:[^""\\]|\\[""\\])*)""\s*");

    private static int RequireField(Sheet sheet, string name)
    {
        if (!sheet.Fields.TryGetValue(name, out var column)) throw new FormatException(sheet.Page.Name + " 没有字段：" + name);
        return column;
    }

    private static Func<int, bool> BindRule(string rule, Sheet source, Dictionary<string, Sheet> tables)
    {
        if (rule == "required" || rule == "id") return _ => true;
        if (rule == "optional") return _ => false;
        const string prefix = "conditional:";
        if (!rule.StartsWith(prefix, StringComparison.Ordinal)) throw new FormatException("未知或已停用的约束：" + rule);
        var tests = new List<Func<int, bool>>();
        int pos = prefix.Length;
        while (true)
        {
            var m = Comparison.Match(rule, pos);
            if (!m.Success) throw new FormatException($"条件第 {pos + 1} 个字符处应为 字段或 lookup(...) 的 == / != 比较。");
            string field = m.Groups["field"].Value;
            Func<int, string> read;
            if (m.Groups["table"].Success)
            {
                var target = ReferenceTarget(tables, m.Groups["table"].Value);
                string keyField = m.Groups["key"].Value;
                int keyColumn = RequireField(source, keyField);
                var resolve = ReferenceKey(target, source, keyField);
                int valueColumn = RequireField(target, field);
                read = row =>
                {
                    var key = resolve(row, source.Value(row, keyColumn));
                    if (!target.Keys.TryGetValue(key, out int found)) throw new FormatException($"lookup 在 {target.Page.Name} 找不到主键“{key}”。");
                    return target.Value(found, valueColumn);
                };
            }
            else
            {
                int column = RequireField(source, field);
                read = row => source.Value(row, column);
            }
            string literal = Regex.Unescape(m.Groups["literal"].Value);
            bool equal = m.Groups["op"].Value == "==";
            tests.Add(row => string.Equals(read(row), literal, StringComparison.Ordinal) == equal);
            pos += m.Length;
            if (pos == rule.Length) break;
            if (pos + 2 > rule.Length || rule[pos] != '|' || rule[pos + 1] != '|') throw new FormatException($"条件第 {pos + 1} 个字符处应为 || 或表达式结束。");
            pos += 2;
        }
        return row => tests.Any(test => test(row));
    }

    private static void ValidateRows(Sheet sheet, Dictionary<string, Sheet> tables, Diagnostics report)
    {
        foreach (var c in sheet.Columns)
        {
            Func<int, string, string> check = null;
            Func<int, bool> required = null;
            try { check = BindType(c.Type, tables, sheet, c.Name); }
            catch (FormatException ex) { report.Add(sheet.At(2,c.Index), ex.Message); }
            try { required = BindRule(c.Rule, sheet, tables); }
            catch (FormatException ex) { report.Add(sheet.At(3,c.Index), ex.Message); }
            foreach (int row in sheet.Rows)
            {
                string value = sheet.Value(row,c.Index);
                if (value.Length == 0)
                {
                    try { if (required != null && required(row)) report.Add(sheet.At(row,c.Index), c.Name + " 必填。"); }
                    catch (FormatException ex) { report.Add(sheet.At(row,c.Index), ex.Message); }
                }
                else
                {
                    var error = check?.Invoke(row, value);
                    if (error != null) report.Add(sheet.At(row,c.Index), c.Name + "：" + error);
                }
            }
        }
    }
#endif
}
