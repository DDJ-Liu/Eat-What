using System;
using System.Collections.Generic;
using System.Linq;
using EatWhat.Narrative.Core;

internal static class NarrativeTalkedRecordTests
{
    static readonly List<string> details = new List<string>();
    static void Check(bool value, string reason) { if (!value) throw new Exception("Talked record: " + reason); details.Add(reason); }
    static void Invalid(Action work, string reason)
    { bool failed = false; try { work(); } catch (ArgumentException) { failed = true; } Check(failed, reason); }
    // External presentation/clock/lock capabilities only. The actual FSM and actual record run below.
    sealed class Capabilities : INarrativeDialogueView, INarrativeDialogueClock, INarrativeDialogueLock
    {
        public double NowSeconds { get { return 0; } }
        public void Acquire(string id, double now) { } public void Release(string id, double now) { }
        public void DialogueStarted(NarrativeDialogueContext c, double now) { }
        public void SetLetterbox(NarrativeDialogueContext c, bool visible, double now) { }
        public void PrepareStoryCamera(NarrativeDialogueContext c, double now) { }
        public void Enter(NarrativeDialogueContext c, double now) { }
        public void CompleteEntrance(NarrativeDialogueContext c, double now) { }
        public void UpdateCast(NarrativeDialogueContext c, double now) { }
        public void SetSpeaker(NarrativeLinePresentation l, double now) { }
        public void ReactToLine(NarrativeLinePresentation l, double now) { }
        public void StartText(NarrativeLinePresentation l, double now) { }
        public void CompleteText(NarrativeLinePresentation l, double now) { }
        public void CanEnd(NarrativeDialogueContext c, double now) { }
        public void Exit(NarrativeDialogueContext c, double now) { }
        public void DialogueEnded(NarrativeDialogueContext c, double now) { }
    }
    static NarrativeDialogueStateMachine Machine(NarrativeTalkedRecord record, bool seamless)
    {
        var command = new NarrativeCommandSource(NarrativeCommandParser.Parse(""));
        var names = new NarrativeProfileNames("", "", "", "", "", "", "", "", "");
        var a = new NarrativeDialogue("dlg_a", NarrativeDialogueLevel.Normal, null, names, "", "ln_a", "dlg_b", command);
        var b = new NarrativeDialogue("dlg_b", NarrativeDialogueLevel.Normal, null, names, "", "ln_b", "", command);
        var lines = new[] { new NarrativeDialogueLine("ln_a", "dlg_a", "", "", "", "text_a", command, "", "", "", ""),
            new NarrativeDialogueLine("ln_b", "dlg_b", "", "", "", "", command, "", "", "", "") };
        var data = new NarrativeDataDirectory(dialogues: new[] { a, b }, lines: lines);
        var config = new DialoguePresentationData(); config.text[0].advanceCooldown = 0;
        if (seamless) config.exit[0].continuationMode = DialogueContinuationMode.Seamless;
        var caps = new Capabilities();
        return new NarrativeDialogueStateMachine(data, d => DialoguePresentationResolver.Resolve(config, d), caps, caps, caps,
            record, new NarrativeCommandScheduler(c => null), new NarrativeAdvanceInputModel());
    }
    static void Fsm(bool seamless)
    {
        var record = new NarrativeTalkedRecord(); var machine = Machine(record, seamless);
        Check(machine.Start("dlg_a") == NarrativeDialogueStartStatus.Started && record.Count == 0, "actual FSM start does not record; seamless=" + seamless);
        var a = machine.Current;
        Check(machine.NotifyEntranceCompleted(a) && record.Count == 0, "entrance does not record");
        var lineA = machine.CurrentLine;
        Check(machine.NotifyLineCompleted(lineA) && !record.HasTalked("dlg_a"), "line completion does not prematurely record");
        machine.Click();
        if (!seamless)
        {
            Check(machine.Phase == NarrativeDialoguePhase.Exiting && record.Count == 0, "full exit must finish before record");
            Check(machine.NotifyExitCompleted(a), "actual full exit accepted");
        }
        Check(record.HasTalked("dlg_a") && !record.HasTalked("dlg_b") && record.Count == 1, "FSM records finished segment only");
        Check(!machine.NotifyExitCompleted(a) && !machine.NotifyLineCompleted(lineA) && record.Count == 1, "stale callbacks cannot duplicate record");
        Check(machine.Current.Dialogue.DialogueId == "dlg_b", "actual continuation reached second dialogue");
        var b = machine.Current;
        if (!seamless) Check(machine.NotifyEntranceCompleted(b), "second full entrance accepted");
        Check(machine.NotifyLineCompleted(machine.CurrentLine), "second line completes"); machine.Click();
        Check(!record.HasTalked("dlg_b") && machine.Phase == NarrativeDialoguePhase.Exiting, "last segment awaits actual exit even with seamless setting");
        Check(machine.NotifyExitCompleted(b) && record.HasTalked("dlg_b") && record.Count == 2, "both actual FSM completion paths use formal record interface");
        Check(machine.Phase == NarrativeDialoguePhase.Inactive, "completed machine becomes inactive");
    }
    public static void Run()
    {
        details.Clear(); var record = new NarrativeTalkedRecord();
        Check(record.Count == 0 && !record.HasTalked("dlg_a") && !record.HasTalked(null), "new owner empty and missing query false");
        INarrativeTalkedRecord contract = record; contract.MarkTalked("dlg_a");
        Check(record.Count == 1 && record.HasTalked("dlg_a"), "actual existing FSM interface implemented");
        contract.MarkTalked("dlg_a"); Check(record.Count == 1, "duplicate mark stores one ID");
        record.MarkTalked("dlg_b"); record.MarkTalked("dlg_A");
        Check(record.Count == 3 && !record.HasTalked("DLG_A"), "ordinal identity has no case folding");
        var exported = record.Export(); Check(exported.SequenceEqual(new[] { "dlg_A", "dlg_a", "dlg_b" }), "export deterministic ordinal list");
        exported.Clear(); Check(record.Count == 3, "export is detached from owner");
        var other = new NarrativeTalkedRecord(); other.Restore(record.Export());
        Check(other.Export().SequenceEqual(record.Export()), "list export restore roundtrip");
        var source = new List<string> { "dlg_c", "dlg_c", "dlg_a" }; other.Restore(source); source.Clear();
        Check(other.Count == 2 && other.HasTalked("dlg_c") && other.HasTalked("dlg_a") && !other.HasTalked("dlg_b"), "restore replaces and deduplicates detached source");
        Check(other.Clear(new[] { "dlg_c", "dlg_missing", "dlg_c" }) == 1 && other.Count == 1 && other.HasTalked("dlg_a"), "specified clear idempotent and leaves unrelated IDs");
        Check(other.Clear(new string[0]) == 0 && other.Clear(new[] { "dlg_missing" }) == 0, "empty and unknown clear harmless");
        other.ClearAll(); other.ClearAll(); Check(other.Count == 0 && other.Export().Count == 0, "clear all idempotent");
        other.Restore(new string[0]); Check(other.Count == 0, "empty list restores empty state");
        other.MarkTalked("dlg_keep");
        Invalid(() => other.MarkTalked(null), "null mark rejected"); Invalid(() => other.MarkTalked(""), "empty mark rejected");
        Invalid(() => other.MarkTalked("  "), "blank mark rejected without guessing IDs");
        Invalid(() => other.Restore(null), "null restore is not implicit clear");
        Invalid(() => other.Restore(new[] { "dlg_new", "" }), "invalid restore rejected atomically");
        Check(other.Count == 1 && other.HasTalked("dlg_keep") && !other.HasTalked("dlg_new"), "failed restore preserves live owner");
        Invalid(() => other.Clear(new[] { "dlg_keep", "" }), "invalid specified clear rejected atomically");
        Check(other.HasTalked("dlg_keep"), "failed clear preserves live owner");
        other.MarkTalked("dlg_future_unknown"); Check(other.HasTalked("dlg_future_unknown"), "record does not discard nonblank ID based on current directory");
        Fsm(false); Fsm(true);
        Console.WriteLine("TALKED_RECORD_RESULT={\"passed\":true,\"checks\":" + details.Count + ",\"actualRecordAndFsm\":true,\"fileIO\":false,\"nativeUnity\":false,\"details\":[\"" + string.Join("\",\"", details.ToArray()) + "\"]}");
    }
}
