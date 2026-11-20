using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using EatWhat.Narrative.Core;

namespace EatWhat.DataTables
{
    /// <summary>Schema v2.5 authoring rules. No scene loading, asset import or command execution.</summary>
    public static class NarrativeTableValidation
    {
        public static void EnableProject(DataTableValidationContext context, string projectRoot, string sceneProjectRoot = null)
        {
            Enable(context, null, (role, name) => FindFiles(projectRoot, role, name));
            var staticRules = context.NarrativeValidation;
            context.NarrativeValidation = (schema, table, sources, result) =>
            {
                staticRules(schema, table, sources, result);
                if (schema.tableName == "SceneStates")
                    NarrativeSceneContentAudit.ValidateProject(sources, result, sceneProjectRoot ?? projectRoot);
            };
        }

        public static void Enable(DataTableValidationContext context, DialoguePresentationData config = null,
            Func<string, string, IList<string>> files = null)
        {
            if (context == null) throw new ArgumentNullException("context");
            context.NarrativeValidation = (schema, table, sources, result) =>
                new Rules(schema, table, sources, result, config, files).Validate();
            context.NarrativeV25 = true;
        }

        // Same exact-basename policy as the importer; filesystem existence only, not Sprite import/type attestation.
        public static IList<string> FindFiles(string projectRoot, string role, string name)
        {
            var result = new List<string>();
            string[] roots = role == "portrait" ? CK01ImportRegistry.PortraitSearchDirectories :
                role == "cg" ? CK01ImportRegistry.CgSearchDirectories :
                role == "visual" ? CK01ImportRegistry.VisualSearchDirectories : new[] { "Assets/Scenes" };
            foreach (string relative in roots)
            {
                string directory = Path.Combine(projectRoot, relative);
                if (!Directory.Exists(directory)) continue;
                foreach (string path in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
                {
                    string extension = Path.GetExtension(path).ToLowerInvariant();
                    bool valid = role == "visual" ? extension == ".prefab" : role == "scene" ? extension == ".unity" :
                        extension == ".png" || extension == ".jpg" || extension == ".jpeg" || extension == ".tga" || extension == ".psd" || extension == ".tif" || extension == ".tiff" || extension == ".bmp";
                    if (valid && string.Equals(Path.GetFileNameWithoutExtension(path), name, StringComparison.Ordinal) && !result.Contains(path)) result.Add(path);
                }
            }
            return result;
        }

        sealed class Rules
        {
            readonly DataTableSchema schema; readonly DataTableCsvTable table;
            readonly DataTableValidationContext context; readonly DataTableValidationResult result;
            readonly DialoguePresentationData config; readonly Func<string, string, IList<string>> files;
            public Rules(DataTableSchema s, DataTableCsvTable t, DataTableValidationContext c, DataTableValidationResult r,
                DialoguePresentationData configuration, Func<string, string, IList<string>> lookup)
            { schema = s; table = t; context = c; result = r; config = configuration; files = lookup; }

            DataTableReferenceSource Source(string name)
            {
                foreach (var source in context.ReferenceTables.Values)
                    if (source.Schema != null && source.Schema.tableName == name) return source;
                return null;
            }
            static string Value(DataTableReferenceSource source, DataTableCsvRow row, string field)
            { return source == null || row == null ? "" : DataTableValidator.GetEffectiveValue(DataTableValidator.FindColumn(source.Schema, field), row, source.Table); }
            string V(DataTableCsvRow row, string field)
            { return DataTableValidator.GetEffectiveValue(DataTableValidator.FindColumn(schema, field), row, table); }
            DataTableCsvRow Find(string name, string field, string value, string scopeField = null, string scope = null)
            {
                var source = Source(name); if (source == null || string.IsNullOrEmpty(value)) return null;
                DataTableCsvRow found = null;
                foreach (var row in source.Table.Rows)
                    if (Value(source, row, field) == value && (scopeField == null || Value(source, row, scopeField) == scope))
                    { if (found != null) return null; found = row; }
                return found;
            }
            void Emit(string rule, DataTableCsvRow row, string field, string reason, bool info = false)
            {
                string message = "CSV line " + row.LineNumber + ": field '" + field + "' [" + rule + "] " + schema.tableName + ": " + reason;
                (info ? result.Infos : result.Warnings).Add(message);
            }
            void Unknown(DataTableCsvRow row, string field, string reason)
            { Emit("NARRATIVE_UNVERIFIED", row, field, reason, true); }
            void Ref(DataTableCsvRow row, string field, string target, string key, string rule, string scopeField = null, string scope = null)
            {
                string value = V(row, field); if (value.Length == 0) return;
                if (Source(target) == null) { Unknown(row, field, target + " input unavailable; reference not checked."); return; }
                if (Find(target, key, value, scopeField, scope) == null) Emit(rule, row, field, "Missing or ambiguous " + target + " reference '" + value + "'.");
            }
            void FileRef(DataTableCsvRow row, string field, string role, string rule)
            {
                string value = V(row, field); if (value.Length == 0) return;
                if (files == null) { Unknown(row, field, "File inventory unavailable; existence not checked."); return; }
                var found = NarrativeFilenameLookupPolicy.Resolve(role, value, name => files(role, name));
                if (!string.IsNullOrEmpty(found.Warning)) Emit(rule, row, field, found.Warning);
            }
            void Range(DataTableCsvRow row, string field, float min, float max, bool exclusive, string rule)
            {
                float number; string value = V(row, field);
                if (value.Length > 0 && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number) &&
                    !float.IsNaN(number) && !float.IsInfinity(number) && (number < min || number > max || exclusive && number == min))
                    Emit(rule, row, field, "Value outside " + (exclusive ? "(" : "[") + min.ToString(CultureInfo.InvariantCulture) + ", " + max.ToString(CultureInfo.InvariantCulture) + "].");
            }
            List<DataTableColumnSchema> CastColumns(DataTableSchema s)
            {
                var columns = new List<DataTableColumnSchema>();
                foreach (var column in s.columns) if (Regex.IsMatch(column.name, "^cast_[0-9]+$")) columns.Add(column);
                columns.Sort((a,b) => int.Parse(a.name.Substring(5), CultureInfo.InvariantCulture).CompareTo(int.Parse(b.name.Substring(5), CultureInfo.InvariantCulture)));
                return columns;
            }
            List<DataTableCsvRow> Chain(DataTableReferenceSource dialogues, DataTableCsvRow dialogue)
            {
                var rows = new List<DataTableCsvRow>(); var visited = new HashSet<string>(StringComparer.Ordinal);
                var lines = Source("DialogueLines"); string next = Value(dialogues, dialogue, "first_line");
                while (lines != null && next.Length > 0 && visited.Add(next))
                { var row = Find("DialogueLines", "line_id", next); if (row == null) break; rows.Add(row); next = Value(lines, row, "next_line"); }
                return rows;
            }
            List<DataTableCsvRow> Owners(DataTableCsvRow line)
            {
                var owners = new List<DataTableCsvRow>();
                var dialogues = Source("Dialogues"); var lines = Source("DialogueLines");
                if (dialogues == null || lines == null) return owners;
                foreach (var dialogue in dialogues.Table.Rows) if (Chain(dialogues, dialogue).Contains(line)) owners.Add(dialogue);
                return owners;
            }
            public void Validate()
            {
                foreach (var row in table.Rows)
                {
                    switch (schema.tableName)
                    {
                        case "Characters":
                            FileRef(row,"portrait_sprite","portrait","V-18"); FileRef(row,"street_visual_prefab","visual","V-18");
                            Range(row,"portrait_offset_x",-.5f,.5f,false,"V-18"); Range(row,"portrait_offset_y",0,1,false,"V-18"); Range(row,"portrait_scale",0,float.MaxValue,true,"V-18"); break;
                        case "Dialogues": Dialogue(row); break;
                        case "DialogueLines": Line(row); break;
                        case "CGs":
                            FileRef(row,"cg_sprite","cg","V-19"); foreach (string field in new[]{"fade_in","fade_out","ken_burns_duration"}) Range(row,field,0,float.MaxValue,false,"V-19"); break;
                        case "Scenes": Ref(row,"default_state","SceneStates","state_id","V-20","scene_id",V(row,"scene_id")); FileRef(row,"scene_asset","scene","V-20"); break;
                        case "SceneStates": Ref(row,"scene_id","Scenes","scene_id","NARRATIVE_REFERENCE"); CompleteState(row); break;
                        case "SceneStatePlacements": Placement(row); break;
                        case "AmbientPools": Pool(row); break;
                    }
                }
            }
            void Dialogue(DataTableCsvRow row)
            {
                string tier = V(row,"tier"); bool gap = false; var cast = CastColumns(schema);
                foreach (var column in cast)
                {
                    if (V(row,column.name+"_station").Length > 0 && (V(row,column.name+"_pos_x").Length > 0 || V(row,column.name+"_pos_y").Length > 0))
                        Emit("V-14b",row,column.name+"_station","Explicit position takes precedence; station is ignored.",true);
                    string value = V(row,column.name); if (value.Length == 0) { gap = true; continue; }
                    if (gap) Emit("V-14",row,column.name,"Cast slots must be filled continuously from 01.");
                    Ref(row,column.name,"Characters","character_id","V-14");
                    var character = Find("Characters","character_id",value);
                    if (character != null && Value(Source("Characters"),character,"portrait_sprite").Length == 0) Emit("V-14",row,column.name,"Cast character has no portrait.");
                }
                if (tier == "ambient")
                {
                    if (V(row,"next_dialogue").Length > 0) Emit("V-24",row,"next_dialogue","Ambient dialogue does not use continuation.");
                    foreach (var column in schema.columns) if (column.name.StartsWith("cast_",StringComparison.Ordinal) && V(row,column.name).Length > 0)
                        Emit("V-24",row,column.name,"Ambient dialogue does not use cast configuration.");
                }
                Ref(row,"next_dialogue","Dialogues","dialogue_id","NARRATIVE_REFERENCE");
                var lines = Source("DialogueLines");
                if (lines == null) Unknown(row,"first_line","DialogueLines input unavailable; chain not checked.");
                else
                {
                    string next = V(row,"first_line"); var seen = new HashSet<string>(StringComparer.Ordinal);
                    DataTableCsvRow previous = null;
                    while (next.Length > 0)
                    {
                        if (!seen.Add(next)) { Emit("V-13",row,"first_line","Chain contains a cycle at '"+next+"'."); break; }
                        var line = Find("DialogueLines","line_id",next);
                        if (line == null) { if(previous == null) Emit("V-13",row,"first_line","Chain entry is missing or ambiguous: '"+next+"'."); break; }
                        previous = line; next = Value(lines,line,"next_line");
                    }
                }
                Templates(row);
                Commands(row,"on_end",row,false);
            }
            void Line(DataTableCsvRow row)
            {
                Ref(row,"dialogue_id","Dialogues","dialogue_id","V-13"); Ref(row,"next_line","DialogueLines","line_id","V-13");
                var owners = Owners(row); var dialogues = Source("Dialogues");
                if (dialogues == null) Unknown(row,"dialogue_id","Dialogues input unavailable; ownership not checked.");
                else if (owners.Count == 0) Emit("V-13",row,"line_id","Line is not reachable from any dialogue first_line.");
                else foreach (var owner in owners) if (Value(dialogues,owner,"dialogue_id") != V(row,"dialogue_id"))
                    Emit("V-13",row,"dialogue_id","Tag disagrees with chain owner '"+Value(dialogues,owner,"dialogue_id")+"'.");
                var dialogue = owners.Count == 1 ? owners[0] : Find("Dialogues","dialogue_id",V(row,"dialogue_id"));
                string tier = Value(dialogues,dialogue,"tier"), speaker = V(row,"speaker");
                if ((tier == "normal" || tier == "story") && speaker.Length == 0) Emit("V-15",row,"speaker","Normal/story dialogue requires a speaker.");
                Ref(row,"speaker","Characters","character_id","V-15");
                var character = Find("Characters","character_id",speaker);
                if (tier != "ambient" && dialogue != null && character != null && Value(Source("Characters"),character,"portrait_sprite").Length > 0)
                {
                    bool present = false; foreach (var column in CastColumns(dialogues.Schema)) if (Value(dialogues,dialogue,column.name) == speaker) present = true;
                    if (!present) Emit("V-15",row,"speaker","Speaker has a portrait but is absent from the cast.");
                }
                Commands(row,"commands",dialogue,true);
            }
            void Templates(DataTableCsvRow row)
            {
                foreach (var item in DialogueOverrideParser.ParseItems(V(row,"overrides")).Diagnostics) Emit("V-17",row,"overrides",item.ToString());
                if (config == null) { Unknown(row,"tier_profile","Presentation configuration unavailable; templates and default command mode not checked."); return; }
                foreach (var column in schema.columns)
                {
                    if (!column.name.EndsWith("_profile",StringComparison.Ordinal)) continue;
                    string group = column.name.Substring(0,column.name.Length-8);
                    string method = "Find" + char.ToUpperInvariant(group[0]) + group.Substring(1);
                    var args = new object[]{V(row,column.name),null};
                    typeof(DialoguePresentationData).GetMethod(method).Invoke(config,args);
                    foreach (var issue in (IReadOnlyList<PresentationConfigDiagnostic>)args[1]) Emit("V-17",row,column.name,issue.Reason);
                }
            }
            void Commands(DataTableCsvRow row,string field,DataTableCsvRow dialogue,bool line)
            {
                var parsed = NarrativeCommandParser.Parse(V(row,field));
                foreach (var issue in parsed.Diagnostics) Emit("V-16",row,field,"At UTF-16 " + issue.Position + ": " + issue.Reason);
                if (!parsed.Success) return; // Invalid command batches are not semantically executable in N005 either.
                var dialogues = Source("Dialogues"); string tier = schema.tableName == "Dialogues" ? V(row,"tier") : Value(dialogues,dialogue,"tier");
                NarrativeDialogueLevel level; bool known = Enum.TryParse(tier,true,out level) && (tier == "normal" || tier == "story" || tier == "ambient");
                bool? parallel = null;
                if (config != null && dialogue != null)
                {
                    IReadOnlyList<PresentationConfigDiagnostic> issues;
                    var profile = config.FindTier(Value(dialogues,dialogue,"tier_profile"),out issues);
                    if (profile != null) parallel = profile.commandExecutionMode == NarrativeCommandExecutionMode.Parallel;
                }
                string overrides = schema.tableName == "Dialogues" ? V(row,"overrides") : Value(dialogues,dialogue,"overrides");
                foreach (var item in DialogueOverrideParser.ParseItems(overrides).Items)
                    if (item.Value != null && item.Value.Definition.Key == "tier.commandExecutionMode") parallel = (NarrativeCommandExecutionMode)item.Value.Value == NarrativeCommandExecutionMode.Parallel;
                foreach (var command in parsed.Commands)
                {
                    var signature = command.Signature; if (signature == null) continue;
                    if (!signature.IsAvailableInVersion) Emit("V-16b",row,field,command.Name + " is reserved for the full version.",true);
                    else if (known && !signature.Allows(level) || line && signature.OnlyOnEnd) Emit("V-16",row,field,command.Name + " is unavailable for this tier/location.");
                    if (command.Name == "wait")
                    { if (parallel == true) Emit("V-16b",row,field,"wait has no sequencing effect in parallel mode.",true); else if (!parallel.HasValue) Unknown(row,field,"Command mode unavailable; parallel wait not checked."); }
                    CommandReferences(row,field,command);
                }
            }
            void CommandReferences(DataTableCsvRow row,string field,NarrativeCommandInvocation command)
            {
                string scene = null;
                foreach (var argument in command.Arguments) if (argument.Parameter != null && argument.Parameter.Kind == NarrativeCommandParameterKind.SceneId) scene = argument.Value;
                foreach (var argument in command.Arguments)
                {
                    if (argument.Parameter == null || argument.TypedValue == null) continue;
                    string target = null,key = null;
                    switch (argument.Parameter.Kind)
                    {
                        case NarrativeCommandParameterKind.DialogueId: target="Dialogues";key="dialogue_id";break;
                        case NarrativeCommandParameterKind.CgId: target="CGs";key="cg_id";break;
                        case NarrativeCommandParameterKind.SceneId: target="Scenes";key="scene_id";break;
                        case NarrativeCommandParameterKind.SceneStateId: target="SceneStates";key="state_id";break;
                    }
                    if (target == null) continue;
                    if (target == "SceneStates" && string.IsNullOrEmpty(scene)) { Unknown(row,field,"Scene context missing; state not selected by first matching name."); continue; }
                    if (Source(target) == null) Unknown(row,field,target+" input unavailable; command reference not checked.");
                    else if (Find(target,key,argument.Value,target == "SceneStates" ? "scene_id" : null,scene) == null)
                        Emit("V-23",row,field,command.Name+" @"+argument.Position+" missing or ambiguous "+target+" reference '"+argument.Value+"'"+(target == "SceneStates" ? " in scene '"+scene+"'" : "")+".");
                }
            }
            void Placement(DataTableCsvRow row)
            {
                Ref(row,"scene_id","Scenes","scene_id","V-21"); Ref(row,"state_id","SceneStates","state_id","V-21","scene_id",V(row,"scene_id"));
                Ref(row,"dialogue_id","Dialogues","dialogue_id","V-21"); Ref(row,"ambient_pool","AmbientPools","pool_id","V-21");
                if (V(row,"kind") == "actor")
                {
                    NarrativeCommandTarget target; NarrativeCommandDiagnostic issue;
                    if (!NarrativeCommandTargetParser.TryParse("actor:"+V(row,"target"),out target,out issue)) Emit("V-21",row,"target",issue.Reason);
                    else if (Source("Characters") == null) Unknown(row,"target","Characters input unavailable.");
                    else if (Find("Characters","character_id",target.BaseId) == null) Emit("V-21",row,"target","Actor base character is missing or ambiguous.");
                    BubbleSources(row);
                }
                else if (V(row,"kind") == "object" || V(row,"kind") == "exit")
                    foreach (string field in new[]{"anchor_id","facing","dialogue_id","ambient_pool","marker"})
                        if (V(row,field).Length > 0) Emit("V-21",row,field,"This column is only used for actor placements.");
            }
            // V-26 is table-only. A suffixed initiator is still a distinct actor; recipients match the explicit base speaker exactly.
            void BubbleSources(DataTableCsvRow row)
            {
                bool active;
                string actor = V(row,"target"), pool = V(row,"ambient_pool"), scene = V(row,"scene_id"), state = V(row,"state_id");
                NarrativeCommandTarget parsed; NarrativeCommandDiagnostic issue;
                if (!bool.TryParse(V(row,"active"),out active) || !active || pool.Length == 0 ||
                    actor.IndexOf('#') >= 0 || !NarrativeCommandTargetParser.TryParse("actor:"+actor,out parsed,out issue) ||
                    Find("AmbientPools","pool_id",pool) == null) return;
                var placements = Source("SceneStatePlacements"); var pools = Source("AmbientPools");
                var dialogues = Source("Dialogues"); var lines = Source("DialogueLines");
                if (placements == null || pools == null || dialogues == null || lines == null) return;
                var conflicts = new List<string>();
                foreach (var other in placements.Table.Rows)
                {
                    string initiator = Value(placements,other,"target"), otherPool = Value(placements,other,"ambient_pool");
                    if (Value(placements,other,"kind") != "actor" || initiator == actor ||
                        Value(placements,other,"scene_id") != scene || Value(placements,other,"state_id") != state ||
                        !bool.TryParse(Value(placements,other,"active"),out active) || !active || otherPool.Length == 0 ||
                        !NarrativeCommandTargetParser.TryParse("actor:"+initiator,out parsed,out issue)) continue;
                    var sourcePool = Find("AmbientPools","pool_id",otherPool); if (sourcePool == null) continue;
                    string conflictingLine = "";
                    foreach (string raw in Value(pools,sourcePool,"dialogue_ids").Split(';'))
                    {
                        var dialogue = Find("Dialogues","dialogue_id",raw.Trim());
                        if (dialogue == null || Value(dialogues,dialogue,"tier") != "ambient") continue;
                        foreach (var line in Chain(dialogues,dialogue))
                            if (Value(lines,line,"speaker") == actor) { conflictingLine = Value(lines,line,"line_id"); break; }
                        if (conflictingLine.Length != 0) break;
                    }
                    if (conflictingLine.Length != 0)
                        conflicts.Add("actor '"+initiator+"', pool '"+otherPool+"', line '"+conflictingLine+"'");
                }
                if (conflicts.Count != 0) Emit("V-26",row,"ambient_pool","Actor '"+actor+"' in scene '"+scene+"', state '"+state+
                    "' has table pool '"+pool+"' and is an explicit speaker in another active actor's table pool: "+string.Join("; ",conflicts)+". Scene-instance pools are not inspected.");
            }
            void CompleteState(DataTableCsvRow row)
            {
                var placements = Source("SceneStatePlacements"); if (placements == null) { Unknown(row,"state_id","Placement input unavailable; full-state coverage not checked."); return; }
                var all = new HashSet<string>(StringComparer.Ordinal); var current = new HashSet<string>(StringComparer.Ordinal);
                foreach (var placement in placements.Table.Rows)
                    if (Value(placements,placement,"scene_id") == V(row,"scene_id"))
                    {
                        string identity = Value(placements,placement,"kind") + ":" + Value(placements,placement,"target"); all.Add(identity);
                        if (Value(placements,placement,"state_id") == V(row,"state_id")) current.Add(identity);
                    }
                foreach (string identity in all) if (!current.Contains(identity)) Emit("V-21",row,"state_id","Full state omits placement '"+identity+"'; states are not deltas.");
            }
            void Pool(DataTableCsvRow row)
            {
                foreach (string raw in V(row,"dialogue_ids").Split(';'))
                {
                    string id = raw.Trim(); if (id.Length == 0) continue;
                    if (Source("Dialogues") == null) { Unknown(row,"dialogue_ids","Dialogues input unavailable."); break; }
                    var dialogue = Find("Dialogues","dialogue_id",id);
                    if (dialogue == null || Value(Source("Dialogues"),dialogue,"tier") != "ambient") Emit("V-22",row,"dialogue_ids","Pool entry '"+id+"' must reference an existing ambient dialogue.");
                }
            }
        }
    }

    /// <summary>Read-only V-25 for the current Unity text representation. No Unity or alternate CSV loader.</summary>
    public static class NarrativeSceneContentAudit
    {
        public sealed class Inventory
        {
            public readonly HashSet<string> Actors = new HashSet<string>(StringComparer.Ordinal);
            public readonly HashSet<string> Anchors = new HashSet<string>(StringComparer.Ordinal);
            public readonly List<string> Issues = new List<string>();
            public bool Complete { get { return Issues.Count == 0; } }
        }
        public sealed class Placement
        {
            public string Kind, Target, Anchor;
            public Placement(string kind, string target, string anchor) { Kind = kind; Target = target; Anchor = anchor; }
        }
        sealed class Document { public string Class, Id, Body; public bool Stripped; }
        sealed class Marker { public string Id, Field, Value; public bool Enabled; }
        static readonly Regex Header = new Regex(@"^--- !u!(\d+) &(-?\d+)( stripped)?\s*$", RegexOptions.Multiline);
        static readonly Regex Pointer = new Regex(@"^\{fileID:\s*(-?\d+)(?:,\s*guid:\s*([a-fA-F0-9]{32}),\s*type:\s*\d+)?\}$");
        static readonly System.Text.UTF8Encoding Utf8 = new System.Text.UTF8Encoding(false, true);
        static string Read(string path) { return File.ReadAllText(path, Utf8); }
        static string LocalId(string text) { return long.Parse(text, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture); }
        static string Field(string body, string name)
        {
            var matches = Regex.Matches(body, @"^  " + Regex.Escape(name) + @":(?: (.*))?\r?$", RegexOptions.Multiline);
            if (matches.Count != 1) throw new FormatException("Missing/ambiguous field " + name + ".");
            return matches[0].Groups[1].Value.TrimEnd('\r');
        }
        static Match PPtr(string text)
        {
            var match = Pointer.Match(text.Trim());
            if (!match.Success) throw new FormatException("Unsupported object reference.");
            LocalId(match.Groups[1].Value); // Validate as an integer; never round localIDs through float/double.
            return match;
        }
        static string Scalar(string text)
        {
            text = text.Trim();
            if (text.StartsWith("'", StringComparison.Ordinal))
            {
                if (text.Length < 2 || !text.EndsWith("'", StringComparison.Ordinal)) throw new FormatException("Malformed quoted marker ID.");
                return text.Substring(1, text.Length - 2).Replace("''", "'");
            }
            if (text.StartsWith("\"", StringComparison.Ordinal))
            {
                if (text.Length < 2 || !text.EndsWith("\"", StringComparison.Ordinal)) throw new FormatException("Malformed quoted marker ID.");
                return Regex.Unescape(text.Substring(1, text.Length - 2));
            }
            // In chr_vendor#2 the hash is part of the ID, not a YAML comment.
            if (text.IndexOfAny(new[] {'{', '}', '[', ']', '|', '>'}) >= 0) throw new FormatException("Unsupported marker scalar.");
            return text;
        }
        static List<Document> Documents(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new FormatException("Empty Unity text.");
            if (!text.StartsWith("%YAML 1.1", StringComparison.Ordinal) || !text.Contains("%TAG !u! tag:unity3d.com,2011:"))
                throw new FormatException("Missing Unity YAML preamble.");
            var headers = Header.Matches(text); var documents = new List<Document>();
            if (headers.Count == 0 || Regex.Matches(text, @"^---", RegexOptions.Multiline).Count != headers.Count)
                throw new FormatException("Unsupported/malformed Unity document headers.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < headers.Count; i++)
            {
                var h = headers[i]; string id = LocalId(h.Groups[2].Value);
                if (!ids.Add(id)) throw new FormatException("Duplicate Unity localID " + id + ".");
                string body = text.Substring(h.Index + h.Length, (i + 1 < headers.Count ? headers[i+1].Index : text.Length) - h.Index - h.Length).TrimStart('\r','\n');
                if (!Regex.IsMatch(body, @"^[A-Za-z][A-Za-z0-9_]*:\r?\n")) throw new FormatException("Malformed Unity object body.");
                documents.Add(new Document { Class = h.Groups[1].Value, Id = id, Stripped = h.Groups[3].Success, Body = body });
            }
            return documents;
        }
        static List<Marker> Markers(List<Document> docs, string actorGuid, string anchorGuid)
        {
            var markers = new List<Marker>();
            foreach (var doc in docs)
            {
                if (doc.Class != "114") continue;
                if (!doc.Body.StartsWith("MonoBehaviour:", StringComparison.Ordinal)) throw new FormatException("Malformed MonoBehaviour.");
                if (doc.Stripped)
                {
                    // Inherited components are collected from the source PrefabInstance, once.
                    var reference = PPtr(Field(doc.Body, "m_PrefabInstance"));
                    bool found = false;
                    foreach (var instance in docs) if (instance.Class == "1001" && instance.Id == LocalId(reference.Groups[1].Value)) found = true;
                    if (!found) throw new FormatException("Unresolved stripped component.");
                    continue;
                }
                var script = PPtr(Field(doc.Body, "m_Script")); string guid = script.Groups[2].Value;
                if (guid.Length == 0) throw new FormatException("Missing MonoBehaviour script GUID.");
                string field = string.Equals(guid, actorGuid, StringComparison.OrdinalIgnoreCase) ? "characterId" :
                    string.Equals(guid, anchorGuid, StringComparison.OrdinalIgnoreCase) ? "anchorId" : null;
                if (field == null) continue;
                string enabled = Field(doc.Body, "m_Enabled");
                if (enabled != "0" && enabled != "1") throw new FormatException("Invalid marker m_Enabled.");
                markers.Add(new Marker { Id = doc.Id, Field = field, Value = Scalar(Field(doc.Body, field)), Enabled = enabled == "1" });
            }
            return markers;
        }
        static void Add(Inventory inventory, IEnumerable<Marker> markers)
        {
            foreach (var marker in markers)
            {
                if (!marker.Enabled) continue;
                NarrativeCommandTarget target; NarrativeCommandDiagnostic issue;
                if (!NarrativeCommandTargetParser.TryParse((marker.Field == "characterId" ? "actor:" : "anchor:") + marker.Value, out target, out issue))
                    inventory.Issues.Add("Empty/invalid " + marker.Field + " at component " + marker.Id + ".");
                else (marker.Field == "characterId" ? inventory.Actors : inventory.Anchors).Add(target.Identifier);
            }
        }
        static void Overrides(Document instance, string sourceGuid, List<Marker> markers)
        {
            foreach (string name in new[] { "m_RemovedComponents", "m_RemovedGameObjects", "m_AddedGameObjects", "m_AddedComponents" })
            {
                var match = Regex.Match(instance.Body, @"^    " + name + @":(.*)\r?$", RegexOptions.Multiline);
                if (!match.Success || match.Groups[1].Value.Trim() != "[]") throw new FormatException("Unsupported Prefab additions/removals: " + name + ".");
            }
            var blocks = Regex.Matches(instance.Body, @"^    - target: (.*)\r?\n(?<body>(?:(?!    - target:|    m_Removed|    m_Added|  m_SourcePrefab:).*(?:\n|$))*)", RegexOptions.Multiline);
            foreach (Match block in blocks)
            {
                string body = block.Groups["body"].Value;
                var property = Regex.Match(body, @"^      propertyPath: (.*)\r?$", RegexOptions.Multiline);
                if (!property.Success) throw new FormatException("Malformed Prefab modification.");
                string path = property.Groups[1].Value.Trim();
                if (path != "characterId" && path != "anchorId" && path != "m_Enabled") continue;
                var pointer = PPtr(block.Groups[1].Value.Trim()); Marker marker = null;
                if (string.Equals(pointer.Groups[2].Value, sourceGuid, StringComparison.OrdinalIgnoreCase))
                    foreach (var candidate in markers) if (candidate.Id == LocalId(pointer.Groups[1].Value)) marker = candidate;
                // m_Enabled on an unrelated component is not a marker override.
                if (marker == null && path == "m_Enabled") continue;
                if (marker == null || path != "m_Enabled" && marker.Field != path) throw new FormatException("Unresolved marker override target.");
                var value = Regex.Match(body, @"^      value:(?: (.*))?\r?$", RegexOptions.Multiline);
                if (!value.Success) throw new FormatException("Missing marker override value.");
                string scalar = Scalar(value.Groups[1].Value.TrimEnd('\r'));
                if (path == "m_Enabled")
                { if (scalar != "0" && scalar != "1") throw new FormatException("Invalid marker enabled override."); marker.Enabled = scalar == "1"; }
                else marker.Value = scalar;
            }
            int targets = Regex.Matches(instance.Body, @"^    - target:", RegexOptions.Multiline).Count;
            if (targets != blocks.Count || !Regex.IsMatch(instance.Body, @"^    m_Modifications:(?: \[\])?\r?$", RegexOptions.Multiline))
                throw new FormatException("Unsupported Prefab modification list.");
        }
        public static Inventory ReadInventory(string sceneText, string actorGuid, string anchorGuid, Func<string, string> readPrefabText)
        {
            var inventory = new Inventory();
            try
            {
                if (!Regex.IsMatch(actorGuid ?? "", "^[a-fA-F0-9]{32}$") || !Regex.IsMatch(anchorGuid ?? "", "^[a-fA-F0-9]{32}$") ||
                    string.Equals(actorGuid, anchorGuid, StringComparison.OrdinalIgnoreCase)) throw new FormatException("Missing/ambiguous marker script GUIDs.");
                var docs = Documents(sceneText); Add(inventory, Markers(docs, actorGuid, anchorGuid));
                foreach (var instance in docs)
                {
                    if (instance.Class != "1001") continue;
                    if (!instance.Body.StartsWith("PrefabInstance:", StringComparison.Ordinal)) throw new FormatException("Malformed PrefabInstance.");
                    var source = PPtr(Field(instance.Body, "m_SourcePrefab")); string guid = source.Groups[2].Value;
                    if (LocalId(source.Groups[1].Value) != "100100000") throw new FormatException("Unsupported Prefab source localID.");
                    if (guid.Length == 0 || readPrefabText == null) throw new FormatException("Unresolved source Prefab GUID.");
                    var prefab = Documents(readPrefabText(guid));
                    foreach (var doc in prefab) if (doc.Class == "1001" || doc.Stripped) throw new FormatException("Nested/variant Prefab is outside this audit's supported representation.");
                    var markers = Markers(prefab, actorGuid, anchorGuid);
                    Overrides(instance, guid, markers); Add(inventory, markers);
                }
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException || error is FormatException || error is OverflowException)
            { inventory.Issues.Add(error.Message); }
            return inventory;
        }
        public static IList<string> AuditState(Inventory inventory, string sceneId, string stateId, IEnumerable<Placement> placements)
        {
            var warnings = new List<string>(); string scope = " scene '" + sceneId + "', state '" + stateId + "': ";
            foreach (string issue in inventory.Issues) warnings.Add("[V-25][UNVERIFIED]" + scope + issue);
            // An incomplete inventory cannot prove absence. Do not manufacture missing or clean results.
            if (!inventory.Complete) return warnings;
            var listed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in placements)
            {
                if (row.Kind != "actor") continue;
                listed.Add(row.Target);
                if (!string.IsNullOrEmpty(row.Anchor))
                { if (!inventory.Anchors.Contains(row.Anchor)) warnings.Add("[V-25][MISSING_ANCHOR]" + scope + "actor '" + row.Target + "' uses missing anchor '" + row.Anchor + "'."); }
                else if (!inventory.Actors.Contains(row.Target)) warnings.Add("[V-25][NO_PREPLACED]" + scope + "actor '" + row.Target + "' has no anchor or exact preplaced instance.");
            }
            var actors = new List<string>(inventory.Actors); actors.Sort(StringComparer.Ordinal);
            foreach (string actor in actors) if (!listed.Contains(actor)) warnings.Add("[V-25][UNLISTED_ACTOR]" + scope + "preplaced actor '" + actor + "' has no placement.");
            return warnings;
        }
        static DataTableReferenceSource Source(DataTableValidationContext context, string name)
        { foreach (var source in context.ReferenceTables.Values) if (source.Schema != null && source.Schema.tableName == name) return source; return null; }
        static string Value(DataTableReferenceSource source, DataTableCsvRow row, string name)
        { return DataTableValidator.GetEffectiveValue(DataTableValidator.FindColumn(source.Schema, name), row, source.Table); }
        static string MetaGuid(string path)
        {
            var matches = Regex.Matches(Read(path), @"^guid: ([a-fA-F0-9]{32})\r?$", RegexOptions.Multiline);
            if (matches.Count != 1) throw new FormatException("Missing/ambiguous GUID in " + path + ".");
            return matches[0].Groups[1].Value;
        }
        public static void ValidateProject(DataTableValidationContext context, DataTableValidationResult result, string projectRoot)
        {
            var states = Source(context, "SceneStates");
            if (states == null || states.Table == null || states.Table.Rows.Count == 0) return; // No configured scope; not an attestation of scene contents.
            var scenes = Source(context, "Scenes"); var entries = Source(context, "SceneStatePlacements");
            var cache = new Dictionary<string, Inventory>(StringComparer.Ordinal);
            Dictionary<string, string> prefabs = null;
            Func<string, string> readPrefab = guid =>
            {
                if (prefabs == null)
                {
                    prefabs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    string root = Path.Combine(projectRoot, "Assets/Prefabs");
                    if (Directory.Exists(root)) foreach (string meta in Directory.GetFiles(root, "*.prefab.meta", SearchOption.AllDirectories))
                    { string key = MetaGuid(meta); if (prefabs.ContainsKey(key)) prefabs[key] = null; else prefabs.Add(key, meta.Substring(0, meta.Length - 5)); }
                }
                string path; if (!prefabs.TryGetValue(guid, out path) || path == null) throw new FormatException("Missing/ambiguous source Prefab " + guid + ".");
                return Read(path);
            };
            foreach (var state in states.Table.Rows)
            {
                string sceneId = Value(states, state, "scene_id"), stateId = Value(states, state, "state_id");
                Inventory inventory; var placements = new List<Placement>();
                if (!cache.TryGetValue(sceneId, out inventory))
                {
                    inventory = new Inventory();
                    try
                    {
                        if (scenes == null || scenes.Table == null || entries == null || entries.Table == null) throw new FormatException("Scene/placement input unavailable.");
                        string asset = null;
                        foreach (var row in scenes.Table.Rows) if (Value(scenes, row, "scene_id") == sceneId)
                        { if (asset != null) throw new FormatException("Ambiguous scene ID."); asset = Value(scenes, row, "scene_asset"); }
                        if (string.IsNullOrEmpty(asset)) throw new FormatException("Scene asset name unavailable.");
                        var paths = NarrativeTableValidation.FindFiles(projectRoot, "scene", asset);
                        if (paths.Count != 1) throw new FormatException("Missing/ambiguous scene file '" + asset + "'.");
                        inventory = ReadInventory(Read(paths[0]), MetaGuid(Path.Combine(projectRoot, "Assets/Scripts/LongLoop/NarrativeActorMarker.cs.meta")),
                            MetaGuid(Path.Combine(projectRoot, "Assets/Scripts/LongLoop/NarrativeAnchorMarker.cs.meta")), readPrefab);
                    }
                    catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException || error is FormatException)
                    { inventory.Issues.Add(error.Message); }
                    cache.Add(sceneId, inventory);
                }
                if (entries != null && entries.Table != null) foreach (var row in entries.Table.Rows)
                    if (Value(entries, row, "scene_id") == sceneId && Value(entries, row, "state_id") == stateId)
                        placements.Add(new Placement(Value(entries, row, "kind"), Value(entries, row, "target"), Value(entries, row, "anchor_id")));
                foreach (string warning in AuditState(inventory, sceneId, stateId, placements)) result.Warnings.Add("CSV line " + state.LineNumber + ": " + warning);
            }
        }
    }
}
