using System;
using System.Collections.Generic;
using System.Reflection;
using EatWhat.Narrative.Core;

internal static class NarrativeDataDirectoryTests
{
    static int checks;
    static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    static NarrativeCommandSource Commands(string raw = "") { return new NarrativeCommandSource(NarrativeCommandParser.Parse(raw)); }
    static NarrativeProfileNames Profiles() { return new NarrativeProfileNames("", "", "", "", "", "", "", "", ""); }
    static NarrativeDialogue Dialogue(string id, string first)
    { return new NarrativeDialogue(id, NarrativeDialogueLevel.Normal, null, Profiles(), "", first, "", Commands()); }
    static NarrativeDialogueLine Line(string id, string next, string label = "dlg_a")
    { return new NarrativeDialogueLine(id, label, next, "", "", "text", Commands(), "", "", "", ""); }
    static void ReadOnly<T>(IReadOnlyList<T> values, T extra)
    {
        var list = values as IList<T>; Check(list != null && list.IsReadOnly, "read-only collection wrapper");
        bool denied = false; try { list.Add(extra); } catch (NotSupportedException) { denied = true; }
        Check(denied, "external list mutation refused");
    }

    public static void Run()
    {
        checks = 0;
        var sourceCast = new List<NarrativeCastEntry>();
        for (int i = 1; i <= 5; i++) sourceCast.Add(new NarrativeCastEntry(i, "chr_a", "station" + i, i, null, null, 0));
        var dialogue = new NarrativeDialogue("dlg_a", NarrativeDialogueLevel.Normal, sourceCast, Profiles(), "unknown.key=raw", "ln_a", "dlg_next", Commands("wait(0)"));
        sourceCast.Clear(); Check(dialogue.Cast.Count == 5 && dialogue.CastSlots.Count == 5, "copied cast list and no permanent four-person limit");
        Check(dialogue.Cast[4].Slot == 5 && dialogue.Cast[4].PosX == 5 && !dialogue.Cast[4].PosY.HasValue && dialogue.Cast[4].Order == 0, "nested cast fields and nullable zero");
        ReadOnly(dialogue.Cast, dialogue.Cast[0]); ReadOnly(dialogue.CastSlots, dialogue.Cast[0]);
        var ids = new List<string> { "dlg_a", "dlg_b" }; var pool = new NarrativeAmbientPool("amb_a", "raw scope", ids);
        ids[0] = "changed"; ids.Clear(); Check(pool.DialogueIds.Count == 2 && pool.DialogueIds[0] == "dlg_a", "pool input list isolated"); ReadOnly(pool.DialogueIds, "changed");
        var character = new NarrativeCharacter("chr_a", "", "", null, null, null, "", "reserved", null, null);
        Check(character.PortraitOffsetX == 0 && character.PortraitOffsetY == 0 && character.PortraitScale == 1, "schema character defaults");
        Check(!character.ConfiguredPortraitOffsetX.HasValue && !character.ConfiguredPortraitScale.HasValue && !character.EmoteOffsetX.HasValue, "presence retained apart from defaults");
        var explicitZero = new NarrativeCharacter("chr_zero", "", "", 0, 0, 0, "", "", 0, 0);
        Check(explicitZero.ConfiguredPortraitOffsetX.HasValue && explicitZero.PortraitScale == 0 && explicitZero.EmoteOffsetY == 0, "explicit zero is not treated as blank or clamped");
        var cg = new NarrativeCg("cg_a", "sprite", "", null, 0, null, 0, null, null, "", false, null, true);
        Check(cg.TransitionMode == null && cg.KenBurnsCurve == null && !cg.FadeIn.HasValue && cg.FadeOut == 0 && cg.KenBurnsPanX == 0 && !cg.KenBurnsPanY.HasValue, "independent nullable CG values");
        Check(cg.KeepBox.HasValue && !cg.KeepBox.Value && !cg.KeepPortraits.HasValue && cg.KeepName == true, "CG blank versus false versus true");
        Check(dialogue.Profiles.Tier == "" && dialogue.Profiles.Cg == "" && dialogue.Overrides == "unknown.key=raw", "profile and override original text retained for N008");

        var characters = new List<NarrativeCharacter> { character };
        var scene = new NarrativeScene("scn_a", "name", "street", false, "Street", "morning");
        var stateA = new NarrativeSceneState("scn_a", "morning", "stage", "description", "source");
        var stateB = new NarrativeSceneState("scn_b", "morning", "", "", "");
        var actor = new NarrativeSceneStatePlacement("scn_a", "morning", "actor", "chr_a#2", "", "left", false, "dlg_a", "amb_a", "none");
        var sameTargetOtherKind = new NarrativeSceneStatePlacement("scn_a", "morning", "object", "chr_a#2", "", "", true, "", "", "");
        var sameTargetOtherState = new NarrativeSceneStatePlacement("scn_a", "night", "actor", "chr_a#2", "", "", true, "", "", "");
        var sameTargetOtherScene = new NarrativeSceneStatePlacement("scn_b", "morning", "actor", "chr_a#2", "", "", true, "", "", "");
        var a = Line("ln_a", "ln_b"); var b = Line("ln_b", "ln_c", "dlg_other"); var c = Line("ln_c", "");
        var lineSource = new List<NarrativeDialogueLine> { c, Line("ln_unlinked", ""), b, a };
        var directory = new NarrativeDataDirectory(characters, new[] { dialogue }, lineSource, new[] { cg }, new[] { scene },
            new[] { stateA, stateB }, new[] { actor, sameTargetOtherKind, sameTargetOtherState, sameTargetOtherScene }, new[] { pool });
        characters.Clear(); lineSource.Clear(); string diagnostic;
        NarrativeCharacter foundCharacter; NarrativeDialogue foundDialogue; NarrativeDialogueLine foundLine; NarrativeCg foundCg;
        NarrativeScene foundScene; NarrativeSceneState foundState; NarrativeSceneStatePlacement foundPlacement; NarrativeAmbientPool foundPool;
        Check(directory.TryGetCharacter("chr_a", out foundCharacter, out diagnostic) && ReferenceEquals(character, foundCharacter) && diagnostic == null, "character snapshot query");
        Check(directory.TryGetDialogue("dlg_a", out foundDialogue, out diagnostic) && ReferenceEquals(dialogue, foundDialogue), "dialogue query");
        Check(directory.TryGetDialogueLine("ln_a", out foundLine, out diagnostic) && ReferenceEquals(a, foundLine), "line query");
        Check(directory.TryGetCg("cg_a", out foundCg, out diagnostic) && ReferenceEquals(cg, foundCg), "CG query");
        Check(directory.TryGetScene("scn_a", out foundScene, out diagnostic) && ReferenceEquals(scene, foundScene), "scene query");
        Check(directory.TryGetSceneState("scn_a", "morning", out foundState, out diagnostic) && ReferenceEquals(stateA, foundState), "first complete state key");
        Check(directory.TryGetSceneState("scn_b", "morning", out foundState, out diagnostic) && ReferenceEquals(stateB, foundState), "same state across scenes");
        foreach (var entry in new[] { actor, sameTargetOtherKind, sameTargetOtherState, sameTargetOtherScene })
            Check(directory.TryGetSceneStatePlacement(entry.SceneId, entry.StateId, entry.Kind, entry.Target, out foundPlacement, out diagnostic) && ReferenceEquals(entry, foundPlacement), "complete four-part placement key");
        Check(directory.TryGetAmbientPool("amb_a", out foundPool, out diagnostic) && ReferenceEquals(pool, foundPool), "pool query");
        NarrativeLineChain chain;
        Check(directory.TryGetLineChain("dlg_a", out chain, out diagnostic) && diagnostic == null && chain.Lines.Count == 3, "link authority over input order and labels");
        Check(chain.Lines[0].LineId == "ln_a" && chain.Lines[1].LineId == "ln_b" && chain.Lines[2].LineId == "ln_c", "first/next order, excludes unlinked row");
        Check(chain.Diagnostics.Count == 1 && chain.Diagnostics[0].Kind == NarrativeDataDiagnosticKind.DialogueLabelMismatch && !chain.Diagnostics[0].IsError, "label conflict retained as warning");
        ReadOnly(chain.Lines, a); ReadOnly(chain.Diagnostics, chain.Diagnostics[0]);
        var firstCommands = chain.Lines[0].Commands;
        NarrativeLineChain repeat; Check(directory.TryGetLineChain("dlg_a", out repeat, out diagnostic) && ReferenceEquals(firstCommands, repeat.Lines[0].Commands), "commands reused rather than parsed per query");

        var duplicates = new NarrativeDataDirectory(new[] { character, character }, new[] { dialogue, dialogue }, new[] { a, a }, new[] { cg, cg },
            new[] { scene, scene }, new[] { stateA, stateA }, new[] { actor, actor }, new[] { pool, pool });
        Check(duplicates.Diagnostics.Count == 8, "all eight duplicate-key diagnostics"); ReadOnly(duplicates.Diagnostics, duplicates.Diagnostics[0]);
        Check(!duplicates.TryGetCharacter("chr_a", out foundCharacter, out diagnostic) && foundCharacter == null && diagnostic != null, "duplicate character never picks first");
        Check(!duplicates.TryGetDialogue("dlg_a", out foundDialogue, out diagnostic) && foundDialogue == null, "duplicate dialogue");
        Check(!duplicates.TryGetDialogueLine("ln_a", out foundLine, out diagnostic) && foundLine == null, "duplicate line");
        Check(!duplicates.TryGetCg("cg_a", out foundCg, out diagnostic) && foundCg == null, "duplicate CG");
        Check(!duplicates.TryGetScene("scn_a", out foundScene, out diagnostic) && foundScene == null, "duplicate scene");
        Check(!duplicates.TryGetSceneState("scn_a", "morning", out foundState, out diagnostic) && foundState == null, "duplicate state");
        Check(!duplicates.TryGetSceneStatePlacement("scn_a", "morning", "actor", "chr_a#2", out foundPlacement, out diagnostic) && foundPlacement == null, "duplicate placement");
        Check(!duplicates.TryGetAmbientPool("amb_a", out foundPool, out diagnostic) && foundPool == null, "duplicate pool");
        var empty = new NarrativeDataDirectory();
        Check(!empty.TryGetCharacter(null, out foundCharacter, out diagnostic) && foundCharacter == null && diagnostic != null, "missing/null character key");
        Check(!empty.TryGetDialogue("missing", out foundDialogue, out diagnostic) && foundDialogue == null && diagnostic != null, "missing dialogue");
        Check(!empty.TryGetDialogueLine("missing", out foundLine, out diagnostic) && foundLine == null && diagnostic != null, "missing line");
        Check(!empty.TryGetCg("missing", out foundCg, out diagnostic) && foundCg == null && diagnostic != null, "missing CG");
        Check(!empty.TryGetScene("missing", out foundScene, out diagnostic) && foundScene == null && diagnostic != null, "missing scene");
        Check(!empty.TryGetSceneState(null, "missing", out foundState, out diagnostic) && foundState == null && diagnostic != null, "missing state");
        Check(!empty.TryGetSceneStatePlacement("", "", "", "", out foundPlacement, out diagnostic) && foundPlacement == null && diagnostic != null, "missing placement");
        Check(!empty.TryGetAmbientPool("missing", out foundPool, out diagnostic) && foundPool == null && diagnostic != null, "missing pool");
        Check(!directory.TryGetCharacter("CHR_A", out foundCharacter, out diagnostic), "ordinal ID lookup");
        Check(!empty.TryGetLineChain("missing", out chain, out diagnostic) && chain.Lines.Count == 0 && chain.Diagnostics[0].Kind == NarrativeDataDiagnosticKind.MissingKey, "missing chain parent");
        var invalid = new NarrativeDataDirectory(characters: new NarrativeCharacter[] { null, new NarrativeCharacter("", "", "", null, null, null, "", "", null, null) });
        Check(invalid.Diagnostics.Count == 2, "null row and incomplete key are explicit");
        var blankFirst = new NarrativeDataDirectory(dialogues: new[] { Dialogue("dlg_a", "") });
        Check(!blankFirst.TryGetLineChain("dlg_a", out chain, out diagnostic) && chain.Diagnostics[0].Kind == NarrativeDataDiagnosticKind.InvalidRow, "blank first line diagnostic");
        var selfLoop = new NarrativeDataDirectory(dialogues: new[] { Dialogue("dlg_a", "ln_a") }, lines: new[] { Line("ln_a", "ln_a") });
        Check(!selfLoop.TryGetLineChain("dlg_a", out chain, out diagnostic) && chain.Lines.Count == 1 && chain.Diagnostics[0].Kind == NarrativeDataDiagnosticKind.LineCycle, "self loop truncated once");
        var loop = new NarrativeDataDirectory(dialogues: new[] { Dialogue("dlg_a", "ln_a") }, lines: new[] { Line("ln_b", "ln_a", "wrong"), Line("ln_a", "ln_b") });
        Check(!loop.TryGetLineChain("dlg_a", out chain, out diagnostic) && chain.Lines.Count == 2 && chain.Diagnostics.Count == 2 && chain.Diagnostics[1].Kind == NarrativeDataDiagnosticKind.LineCycle, "cycle and label diagnostics both retained");
        var broken = new NarrativeDataDirectory(dialogues: new[] { Dialogue("dlg_a", "ln_a") }, lines: new[] { Line("ln_a", "ln_missing") });
        Check(!broken.TryGetLineChain("dlg_a", out chain, out diagnostic) && chain.Lines.Count == 1 && chain.Diagnostics[0].Kind == NarrativeDataDiagnosticKind.MissingKey, "broken successor retains prefix");
        var duplicateLine = new NarrativeDataDirectory(dialogues: new[] { Dialogue("dlg_a", "ln_a") }, lines: new[] { a, a });
        Check(!duplicateLine.TryGetLineChain("dlg_a", out chain, out diagnostic) && chain.Lines.Count == 0 && chain.Diagnostics[0].Kind == NarrativeDataDiagnosticKind.DuplicateKey, "ambiguous first row never selected");
        var collision = new NarrativeDataDirectory(states: new[] { new NarrativeSceneState("a|b", "c", "", "", ""), new NarrativeSceneState("a", "b|c", "", "", "") });
        Check(collision.TryGetSceneState("a|b", "c", out foundState, out diagnostic) && foundState.SceneId == "a|b", "composite keys cannot collide through delimiters");
        Check(collision.TryGetSceneState("a", "b|c", out foundState, out diagnostic) && foundState.SceneId == "a", "second non-concatenated key");
        var failure = Commands("wait(1);unknown()");
        Check(!failure.Success && failure.Commands.Count == 0 && failure.Diagnostics.Count > 0 && failure.SourceText == "wait(1);unknown()", "whole failed column blocks valid prefix");
        ReadOnly(failure.Commands, null); ReadOnly(failure.Diagnostics, failure.Diagnostics[0]);
        foreach (var type in new[] { typeof(NarrativeCharacter), typeof(NarrativeDialogue), typeof(NarrativeDialogueLine), typeof(NarrativeCg), typeof(NarrativeScene), typeof(NarrativeSceneState), typeof(NarrativeSceneStatePlacement), typeof(NarrativeAmbientPool), typeof(NarrativeCastEntry), typeof(NarrativeProfileNames), typeof(NarrativeCommandSource), typeof(NarrativeLineChain), typeof(NarrativeDataDiagnostic) })
        {
            Check(type.IsSealed && type.GetFields(BindingFlags.Instance | BindingFlags.Public).Length == 0, "sealed model without public fields: " + type.Name);
            foreach (var property in type.GetProperties()) Check(property.GetSetMethod() == null, "no public property setter: " + type.Name + "." + property.Name);
        }
        Console.WriteLine("PASS narrative data directory: " + checks + " checks; eight model kinds; complete keys; immutable chains and diagnostics");
    }
}
