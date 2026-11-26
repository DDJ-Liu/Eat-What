using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;
using UnityEngine;

namespace EatWhat.Narrative.Unity
{
    // Takes a value snapshot. No generated row, catalog or Unity object escapes into Core models.
    public static class NarrativeDataAdapter
    {
        static float? Optional(CK01OptionalFloat value) { return value.hasValue ? (float?)value.value : null; }
        static int? Optional(CK01OptionalInt value) { return value.hasValue ? (int?)value.value : null; }
        static bool? Optional(CK01OptionalBool value) { return value.hasValue ? (bool?)value.value : null; }
        static void Required(object row) { if (row == null) throw new ArgumentNullException("row"); }

        /// <summary>Exact base character id, using the catalog's current source. False + null diagnostic means no table override.</summary>
        public static bool TryGetCharacterVisualPrefab(CK01GeneratedDataCatalog catalog, string characterId,
            out GameObject prefab, out string diagnostic)
        {
            prefab = null; diagnostic = null;
            if (catalog == null) { diagnostic = "Generated catalog is missing."; return false; }
            CK01CharacterData row;
            if (!catalog.TryGetCharacter(characterId, out row, out diagnostic)) return false;
            // An empty source column explicitly preserves the instance visual, even if a stale derived field was authored.
            if (string.IsNullOrEmpty(row.street_visual_prefab)) return false;
            if (row.street_visual_prefab_asset == null)
            { diagnostic = "Character '" + characterId + "' visual '" + row.street_visual_prefab + "' has no bound Prefab reference."; return false; }
            prefab = row.street_visual_prefab_asset; return true;
        }

        public static bool TryGetCharacterPortraitSprite(CK01GeneratedDataCatalog catalog, string characterId,
            out Sprite sprite, out string diagnostic)
        {
            sprite = null; diagnostic = null;
            if (catalog == null) { diagnostic = "Generated catalog is missing."; return false; }
            CK01CharacterData row;
            if (!catalog.TryGetCharacter(characterId, out row, out diagnostic) || string.IsNullOrEmpty(row.portrait_sprite)) return false;
            sprite = row.portrait_sprite_asset; return sprite != null;
        }
        public static NarrativeCharacter FromRow(CK01CharacterData row)
        {
            Required(row);
            return new NarrativeCharacter(row.character_id, row.name_key, row.portrait_sprite,
                Optional(row.portrait_offset_x), Optional(row.portrait_offset_y), Optional(row.portrait_scale),
                row.street_visual_prefab, row.expressions, Optional(row.emote_offset_x), Optional(row.emote_offset_y));
        }
        public static NarrativeDialogue FromRow(CK01DialogueData row)
        {
            Required(row); NarrativeDialogueLevel level;
            switch (row.tier)
            {
                case "ambient": level = NarrativeDialogueLevel.Ambient; break;
                case "normal": level = NarrativeDialogueLevel.Normal; break;
                case "story": level = NarrativeDialogueLevel.Story; break;
                default: throw new FormatException("Unknown dialogue tier: " + row.tier);
            }
            var slots = new[] {
                new NarrativeCastEntry(1, row.cast_01, row.cast_01_station, Optional(row.cast_01_pos_x), Optional(row.cast_01_pos_y), Optional(row.cast_01_scale), Optional(row.cast_01_order)),
                new NarrativeCastEntry(2, row.cast_02, row.cast_02_station, Optional(row.cast_02_pos_x), Optional(row.cast_02_pos_y), Optional(row.cast_02_scale), Optional(row.cast_02_order)),
                new NarrativeCastEntry(3, row.cast_03, row.cast_03_station, Optional(row.cast_03_pos_x), Optional(row.cast_03_pos_y), Optional(row.cast_03_scale), Optional(row.cast_03_order)),
                new NarrativeCastEntry(4, row.cast_04, row.cast_04_station, Optional(row.cast_04_pos_x), Optional(row.cast_04_pos_y), Optional(row.cast_04_scale), Optional(row.cast_04_order)) };
            var profiles = new NarrativeProfileNames(row.tier_profile, row.entrance_profile, row.exit_profile,
                row.layout_profile, row.speaker_profile, row.expression_profile, row.text_profile, row.reaction_profile, row.cg_profile);
            return new NarrativeDialogue(row.dialogue_id, level, slots, profiles, row.overrides, row.first_line,
                row.next_dialogue, new NarrativeCommandSource(NarrativeCommandParser.Parse(row.on_end)));
        }
        public static NarrativeDialogueLine FromRow(CK01DialogueLineData row)
        {
            Required(row);
            return new NarrativeDialogueLine(row.line_id, row.dialogue_id, row.next_line, row.speaker,
                row.name_override_key, row.text_key, new NarrativeCommandSource(NarrativeCommandParser.Parse(row.commands)),
                row.expression, row.emotion, row.emphasis, row.scene_emote);
        }
        public static NarrativeCg FromRow(CK01CGData row)
        {
            Required(row);
            return new NarrativeCg(row.cg_id, row.cg_sprite, row.transition_mode, Optional(row.fade_in), Optional(row.fade_out),
                Optional(row.ken_burns_zoom), Optional(row.ken_burns_pan_x), Optional(row.ken_burns_pan_y), Optional(row.ken_burns_duration),
                row.ken_burns_curve, Optional(row.keep_box), Optional(row.keep_portraits), Optional(row.keep_name));
        }
        public static NarrativeScene FromRow(CK01SceneData row)
        { Required(row); return new NarrativeScene(row.scene_id, row.name_key, row.scene_type, row.is_small_room, row.scene_asset, row.default_state); }
        public static NarrativeSceneState FromRow(CK01SceneStateData row)
        { Required(row); return new NarrativeSceneState(row.scene_id, row.state_id, row.story_stage, row.description, row.switch_source); }
        public static NarrativeSceneStatePlacement FromRow(CK01SceneStatePlacementData row)
        { Required(row); return new NarrativeSceneStatePlacement(row.scene_id, row.state_id, row.kind, row.target, row.anchor_id, row.facing, row.active, row.dialogue_id, row.ambient_pool, row.marker); }
        public static NarrativeAmbientPool FromRow(CK01AmbientPoolData row)
        { Required(row); return new NarrativeAmbientPool(row.pool_id, row.scope, row.dialogue_ids); }

        public static bool TryCreate(CK01GeneratedDataCatalog catalog, out INarrativeDataDirectory directory,
            out IReadOnlyList<NarrativeDataDiagnostic> diagnostics)
        {
            directory = null; var issues = new List<NarrativeDataDiagnostic>();
            if (catalog == null)
            {
                issues.Add(new NarrativeDataDiagnostic(NarrativeDataDiagnosticKind.InvalidRow, "Catalog", "", "Generated catalog is missing."));
                diagnostics = issues.AsReadOnly(); return false;
            }
            var characters = Read<CK01CharacterData, NarrativeCharacter>(catalog, "Characters", FromRow, issues);
            var dialogues = Read<CK01DialogueData, NarrativeDialogue>(catalog, "Dialogues", FromRow, issues);
            var lines = Read<CK01DialogueLineData, NarrativeDialogueLine>(catalog, "DialogueLines", FromRow, issues);
            var cgs = Read<CK01CGData, NarrativeCg>(catalog, "CGs", FromRow, issues);
            var scenes = Read<CK01SceneData, NarrativeScene>(catalog, "Scenes", FromRow, issues);
            var states = Read<CK01SceneStateData, NarrativeSceneState>(catalog, "SceneStates", FromRow, issues);
            var placements = Read<CK01SceneStatePlacementData, NarrativeSceneStatePlacement>(catalog, "SceneStatePlacements", FromRow, issues);
            var pools = Read<CK01AmbientPoolData, NarrativeAmbientPool>(catalog, "AmbientPools", FromRow, issues);
            if (issues.Count == 0)
            {
                var candidate = new NarrativeDataDirectory(characters, dialogues, lines, cgs, scenes, states, placements, pools);
                foreach (var issue in candidate.Diagnostics) issues.Add(issue);
                if (issues.Count == 0) directory = candidate;
            }
            diagnostics = issues.AsReadOnly(); return directory != null;
        }

        static List<TModel> Read<TRow, TModel>(CK01GeneratedDataCatalog catalog, string table,
            Func<TRow, TModel> convert, List<NarrativeDataDiagnostic> issues) where TRow : ScriptableObject
        {
            var result = new List<TModel>(); IList<TRow> rows; string diagnostic;
            if (!catalog.TryGetRows(table, out rows, out diagnostic))
            { issues.Add(new NarrativeDataDiagnostic(NarrativeDataDiagnosticKind.InvalidRow, table, "", diagnostic)); return result; }
            foreach (var row in rows)
            {
                if (row == null) { issues.Add(new NarrativeDataDiagnostic(NarrativeDataDiagnosticKind.InvalidRow, table, "", "Null generated row.")); continue; }
                try { result.Add(convert(row)); }
                catch (FormatException error) { issues.Add(new NarrativeDataDiagnostic(NarrativeDataDiagnosticKind.InvalidRow, table, "", error.Message)); }
            }
            return result;
        }
    }
}
