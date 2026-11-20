using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;
using EatWhat.Narrative.Unity;

internal static class NarrativeDialogueBoxTests
{
    static readonly List<string> details = new List<string>();
    static void Check(bool value, string name) { if (!value) throw new Exception("Dialogue box: " + name); details.Add(name); }
    static void Near(double value, double expected, string name) { Check(Math.Abs(value - expected) < 1e-5, name); }
    // An output recorder only: it cannot compute glyphs, wrapping, baselines or positions.
    sealed class Surface : INarrativeDialogueBoxSurface
    {
        public string Text, Name; public bool ShowName, Auto, Fast; public int End, Layouts;
        public readonly List<string> Calls = new List<string>();
        public void LayoutFullLine(string text, string name, bool show) { Text = text; Name = name; ShowName = show; Layouts++; Calls.Add("layout"); }
        public void SetVisiblePrefix(int end) { End = end; Calls.Add("visible"); }
        public void SetToggleStates(bool auto, bool fast) { Auto = auto; Fast = fast; }
    }
    sealed class Capabilities : INarrativeDialogueView, INarrativeDialogueLock, INarrativeTalkedRecord, INarrativeDialogueClock
    {
        public double NowSeconds { get { return 0; } }
        public void Acquire(string id, double now) { } public void Release(string id, double now) { }
        public void MarkTalked(string id) { }
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
    sealed class Rig
    {
        public readonly Surface Surface = new Surface();
        public readonly NarrativeAdvanceInputModel Input = new NarrativeAdvanceInputModel();
        public readonly DialoguePresentationData Config = new DialoguePresentationData();
        public readonly NarrativeDialogueStateMachine Machine;
        public readonly NarrativeDialogueBoxPresenter Presenter;
        public Rig(string name = "name.character", string nameOverride = "")
        {
            Config.text[0].charsPerSecond = 4; Config.text[0].punctuationPause = 0;
            var command = new NarrativeCommandSource(NarrativeCommandParser.Parse(""));
            var dialogue = new NarrativeDialogue("dlg_box", NarrativeDialogueLevel.Normal, null,
                new NarrativeProfileNames("", "", "", "", "", "", "", "", ""), "", "ln_box", "", command);
            var line = new NarrativeDialogueLine("ln_box", "dlg_box", "", "chr_box", nameOverride, "text.box", command, "", "", "", "");
            var directory = new NarrativeDataDirectory(characters: new[] { new NarrativeCharacter("chr_box", name, "", null, null, null, "", "", null, null) },
                dialogues: new[] { dialogue }, lines: new[] { line });
            var caps = new Capabilities();
            Machine = new NarrativeDialogueStateMachine(directory, d => DialoguePresentationResolver.Resolve(Config, d), caps, caps, caps, caps,
                new NarrativeCommandScheduler(c => null), Input);
            Presenter = new NarrativeDialogueBoxPresenter(Surface, Input);
            Check(Machine.Start("dlg_box") == NarrativeDialogueStartStatus.Started, "actual FSM start name=" + name + " override=" + nameOverride);
            Check(Machine.NotifyEntranceCompleted(Machine.Current), "actual FSM entrance accepted");
        }
        public void Begin(string text) { Presenter.BeginLine(Machine.CurrentLine, text); }
    }
    static int Main()
    {
        var table = new LocTableAsset(); table.entries.Add(new LocTableEntry { key = "name.character", zh_cn = "角色名" });
        table.entries.Add(new LocTableEntry { key = "name.override", zh_cn = "句上名字" });
        table.entries.Add(new LocTableEntry { key = "name.empty", zh_cn = "" }); LocService.Configure(table);
        var a = new Rig(); a.Begin("甲乙丙丁");
        Check(a.Machine.CurrentLine.NameKey == "name.character" && a.Surface.Name == "角色名" && a.Surface.ShowName, "FSM character name localized and whole name box shown");
        Check(a.Surface.Calls[0] == "layout" && a.Surface.Calls[1] == "visible" && a.Surface.Text == "甲乙丙丁" && a.Surface.End == 0, "entire final string submitted before first visible prefix");
        int completions = 0; NarrativeLinePresentation completed = null;
        a.Presenter.TextCompleted += value => { completions++; completed = value; a.Machine.NotifyLineCompleted(value); };
        a.Presenter.Tick(.25); Check(a.Surface.End == 1 && a.Presenter.VisibleCharacterCount == 1, "actual typewriter first visible prefix");
        a.Presenter.Tick(.25); Check(a.Surface.End == 2 && a.Surface.Layouts == 1 && a.Surface.Text == "甲乙丙丁", "progress changes prefix without resubmitting full string");
        a.Presenter.Tick(.5); Check(a.Surface.End == 4 && completions == 0 && a.Machine.Phase == NarrativeDialoguePhase.Typing, "natural completion queued outside originating Tick");
        a.Presenter.Tick(0); Check(completions == 1 && ReferenceEquals(completed, a.Machine.CurrentLine) && a.Machine.Phase == NarrativeDialoguePhase.LineComplete, "later Tick reports exact FSM correlation object");
        a.Presenter.Tick(100); a.Presenter.CompleteLine(); a.Presenter.Tick(0); Check(completions == 1, "completed line reported once");
        var renamed = new Rig(nameOverride: "name.override"); renamed.Begin("正文");
        Check(renamed.Machine.CurrentLine.NameKey == "name.override" && renamed.Surface.Name == "句上名字" && renamed.Surface.ShowName, "actual FSM line override has priority over character name");
        var narrator = new Rig(name: ""); narrator.Begin("旁白"); Check(!narrator.Surface.ShowName && narrator.Surface.Name == "", "empty FSM name hides whole name box");
        var blank = new Rig(name: "name.empty"); blank.Begin("旁白"); Check(!blank.Surface.ShowName, "localized empty value hides whole name box");
        var missing = new Rig(name: "name.absent"); missing.Begin("正文"); Check(missing.Surface.ShowName && missing.Surface.Name == "#name.absent#", "real localization missing-key fallback preserved");
        int advance = 0; renamed.Presenter.AdvanceRequested += () => advance++;
        renamed.Presenter.AutoClicked(); Check(renamed.Input.AutoEnabled && renamed.Surface.Auto && !renamed.Input.FastForwardEnabled && advance == 0, "AUTO toggles only input owner and display");
        renamed.Presenter.AutoClicked(); Check(!renamed.Input.AutoEnabled && !renamed.Surface.Auto && advance == 0, "AUTO toggles back without advancing");
        renamed.Presenter.FastForwardClicked(); Check(renamed.Input.FastForwardEnabled && renamed.Surface.Fast && advance == 0, "fast button toggles only its owner and display");
        renamed.Presenter.FastForwardClicked(); Check(!renamed.Input.FastForwardEnabled && !renamed.Surface.Fast && advance == 0, "fast toggles back without advancing");
        renamed.Presenter.ScreenClicked(); Check(advance == 1, "screen route emits one advance request");
        renamed.Presenter.InteractPressed(); Check(advance == 2, "Toolkit interaction route emits one advance request");
        renamed.Input.SetAuto(true); renamed.Input.SetFastForward(true); renamed.Presenter.Tick(0);
        Check(renamed.Surface.Auto && renamed.Surface.Fast, "external input-owner state refreshed without a second owner");
        renamed.Presenter.Hide(); renamed.Presenter.ScreenClicked(); renamed.Presenter.InteractPressed(); renamed.Presenter.AutoClicked();
        Check(advance == 2 && renamed.Input.AutoEnabled, "hidden view suppresses input forwarding and toggles");
        var empty = new Rig(); int emptyReports = 0; empty.Presenter.TextCompleted += value => emptyReports++;
        empty.Begin(""); Check(emptyReports == 0 && empty.Surface.End == 0, "empty Begin never calls back synchronously");
        empty.Presenter.Tick(0); empty.Presenter.Tick(0); Check(emptyReports == 1, "empty completion reported once on later Tick");
        empty.Begin(""); empty.Begin("下一句"); empty.Presenter.Tick(0); Check(emptyReports == 1, "replacement line cancels pending old completion");
        empty.Presenter.CompleteLine(); Check(emptyReports == 1 && empty.Surface.End == 3, "manual completion reveals all without synchronous callback");
        empty.Presenter.Hide(); empty.Presenter.Tick(0); Check(emptyReports == 1, "hide cancels queued completion");
        var unicode = new Rig(); unicode.Begin("\n😀e\u0301\n甲");
        Check(unicode.Surface.End == 1 && unicode.Presenter.VisibleCharacterCount == 0, "leading newline consumes no typed character");
        unicode.Presenter.Tick(.25); Check(unicode.Surface.End == 3 && unicode.Presenter.VisibleCharacterCount == 1, "surrogate pair uses real UTF16 prefix");
        unicode.Presenter.Tick(.25); Check(unicode.Surface.End == 6 && unicode.Presenter.VisibleCharacterCount == 2, "combining sequence and following free newline remain whole");
        foreach (var mode in new[] { DialogueBoxMode.SlideUp, DialogueBoxMode.ScalePop })
        {
            var config = new DialoguePresentationData(); config.entrance[0].boxMode = mode; config.entrance[0].boxSlideDistance = 231;
            var snapshot = DialoguePresentationResolver.Resolve(config, a.Machine.Current.Dialogue).Snapshot;
            var start = NarrativeDialogueBoxPresenter.Pose(snapshot, 0); var end = NarrativeDialogueBoxPresenter.Pose(snapshot, 1);
            Near(start.Alpha, 0, mode + " p0 alpha"); Near(end.Alpha, 1, mode + " p1 alpha");
            Near(start.Scale, mode == DialogueBoxMode.SlideUp ? 1 : .82, mode + " p0 scale"); Near(end.Scale, 1, mode + " p1 scale");
            Near(start.OffsetY, mode == DialogueBoxMode.SlideUp ? -231 : 0, mode + " p0 offset from actual snapshot"); Near(end.OffsetY, 0, mode + " p1 offset");
        }
        Console.WriteLine("DIALOGUE_BOX_RESULT={\"passed\":true,\"checks\":" + details.Count + ",\"actualCoreOwners\":true,\"actualFsmNames\":true,\"nativeUnity\":false,\"realTmpLineInfoVerified\":false,\"boundary\":\"surface output recorder only; no layout implementation\",\"details\":[\"" + string.Join("\",\"", details.ToArray()) + "\"]}");
        return 0;
    }
}
