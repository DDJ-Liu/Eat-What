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
        public static void EnableProject(DataTableValidationContext context, string projectRoot)
        { Enable(context, null, (role, name) => FindFiles(projectRoot, role, name)); }

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
                }
                else if (V(row,"kind") == "object" || V(row,"kind") == "exit")
                    foreach (string field in new[]{"anchor_id","facing","dialogue_id","ambient_pool","marker"})
                        if (V(row,field).Length > 0) Emit("V-21",row,field,"This column is only used for actor placements.");
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
}
