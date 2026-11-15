using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;

internal static class NarrativeDialogueStateMachineTests
{
    static int checks;
    static void Check(bool value, string message) { checks++; if (!value) throw new Exception("Dialogue FSM: " + message); }
    sealed class Event
    { public string Name, Id; public double Time; }
    sealed class Clock : INarrativeDialogueClock { public double Value; public double NowSeconds { get { return Value; } } }
    sealed class Capabilities : INarrativeDialogueView, INarrativeDialogueLock, INarrativeTalkedRecord
    {
        public readonly List<Event> Events = new List<Event>();
        public readonly List<NarrativeLinePresentation> Text = new List<NarrativeLinePresentation>();
        public int Locks, Unlocks, Records;
        void Add(string name, string id, double now) { Events.Add(new Event { Name = name, Id = id, Time = now }); }
        public void Acquire(string id, double now) { Locks++; Add("lock", id, now); }
        public void Release(string id, double now) { Unlocks++; Add("unlock", id, now); }
        public void MarkTalked(string id) { Records++; Add("record", id, 0); }
        public void DialogueStarted(NarrativeDialogueContext c, double now) { Add("start", c.Dialogue.DialogueId, now); }
        public void SetLetterbox(NarrativeDialogueContext c, bool visible, double now) { Add(visible ? "bars-on" : "bars-off", c.Dialogue.DialogueId, now); }
        public void PrepareStoryCamera(NarrativeDialogueContext c, double now) { Add("camera", c.Dialogue.DialogueId, now); }
        public void Enter(NarrativeDialogueContext c, double now) { Add("enter", c.Dialogue.DialogueId, now); }
        public void CompleteEntrance(NarrativeDialogueContext c, double now) { Add("skip-enter", c.Dialogue.DialogueId, now); }
        public void UpdateCast(NarrativeDialogueContext c, double now) { Add("cast", c.Dialogue.DialogueId, now); }
        public void SetSpeaker(NarrativeLinePresentation line, double now) { Add("speaker", line.Line.LineId, now); }
        public void ReactToLine(NarrativeLinePresentation line, double now) { Add("react", line.Line.LineId, now); }
        public void StartText(NarrativeLinePresentation line, double now) { Text.Add(line); Add("text", line.Line.LineId, now); }
        public void CompleteText(NarrativeLinePresentation line, double now) { Add("complete-text", line.Line.LineId, now); }
        public void CanEnd(NarrativeDialogueContext c, double now) { Add("can-end", c.Dialogue.DialogueId, now); }
        public void Exit(NarrativeDialogueContext c, double now) { Add("exit", c.Dialogue.DialogueId, now); }
        public void DialogueEnded(NarrativeDialogueContext c, double now) { Add("end", c.Dialogue.DialogueId, now); }
        public int Count(string name) { return Events.FindAll(x => x.Name == name).Count; }
        public int Position(string name, string id = null) { return Events.FindIndex(x => x.Name == name && (id == null || x.Id == id)); }
    }
    sealed class Handler : INarrativeCommandHandler
    {
        public NarrativeCommandInvocation Command; public Capabilities Log; public Clock Clock;
        public bool Instant, Begun, Done, Cancelled; public int Finals; public double BeganAt;
        public void Begin(double now) { Begun = true; BeganAt = now; Log.Events.Add(new Event { Name = "cmd:" + Command.Name, Id = Command.RawText, Time = now }); }
        public bool IsComplete { get { return Begun && (Instant || Done); } }
        public void CompleteImmediately(NarrativeCommandTruncation truncation, double now) { Finals++; Done = true; Log.Events.Add(new Event { Name = "truncate", Id = Command.Name, Time = now }); }
        public void Cancel(double now) { Cancelled = true; Done = true; Log.Events.Add(new Event { Name = "cancel", Id = Command.Name, Time = now }); }
    }
    sealed class Rig
    {
        public readonly Clock Clock = new Clock(); public readonly Capabilities View = new Capabilities();
        public readonly DialoguePresentationData Config = new DialoguePresentationData();
        public readonly NarrativeAdvanceInputModel Input = new NarrativeAdvanceInputModel();
        public readonly List<Handler> Handlers = new List<Handler>(); public readonly List<NarrativeCommandInvocation> Invocations = new List<NarrativeCommandInvocation>();
        public readonly NarrativeCommandScheduler Scheduler; public readonly NarrativeDialogueStateMachine Machine;
        public int Resolves; public bool FailSwitch;
        public Rig(IEnumerable<NarrativeDialogue> dialogues, IEnumerable<NarrativeDialogueLine> lines)
        {
            Config.text[0].advanceCooldown = 0; Config.text[0].autoWait = 0.5f;
            var directory = new NarrativeDataDirectory(characters: new[] { Character("chr_a", "name.a"), Character("chr_b", "name.b"), Character("chr_narrator", "") }, dialogues: dialogues, lines: lines);
            Scheduler = new NarrativeCommandScheduler(command => {
                Invocations.Add(command); if (FailSwitch && command.Name == "scene_switch") return null;
                var h = new Handler { Command = command, Log = View, Clock = Clock, Instant = command.Name == "set_flag" || command.Name == "scene_switch" };
                Handlers.Add(h); return h;
            });
            Machine = new NarrativeDialogueStateMachine(directory, d => { Resolves++; return DialoguePresentationResolver.Resolve(Config, d); }, View, View, Clock, View, Scheduler, Input);
        }
        public void Enter()
        {
            var context = Machine.Current;
            if (Machine.Phase == NarrativeDialoguePhase.StoryPreparing) { Machine.NotifyStoryBarsReady(context); Machine.NotifyStoryCameraReady(context); }
            Check(Machine.NotifyEntranceCompleted(context), "accepted entrance completion");
        }
        public void EndCurrent()
        {
            var context = Machine.Current; Check(Machine.NotifyLineCompleted(Machine.CurrentLine), "line complete"); Machine.Click();
            if (Machine.Phase == NarrativeDialoguePhase.Exiting) Check(Machine.NotifyExitCompleted(context), "exit complete");
        }
    }
    static NarrativeCharacter Character(string id, string name)
    { return new NarrativeCharacter(id, name, "sprite", null, null, null, "", "", null, null); }
    static NarrativeCommandSource Commands(string source = "") { return new NarrativeCommandSource(NarrativeCommandParser.Parse(source)); }
    static NarrativeDialogue Dialogue(string id, string first, NarrativeDialogueLevel level = NarrativeDialogueLevel.Normal, string next = "", string onEnd = "", string overrides = "")
    {
        return new NarrativeDialogue(id, level, new[] { new NarrativeCastEntry(1, "chr_a", "", null, null, null, null), new NarrativeCastEntry(2, "chr_a", "", null, null, null, null) },
            new NarrativeProfileNames("", "", "", "", "", "", "", "", ""), overrides, first, next, Commands(onEnd));
    }
    static NarrativeDialogueLine Line(string id, string next = "", string speaker = "chr_a", string nameOverride = "", string commands = "", string label = "dlg_a")
    { return new NarrativeDialogueLine(id, label, next, speaker, nameOverride, "text." + id, Commands(commands), "", "", "", ""); }

    static void NormalLifecycle()
    {
        var a = Line("ln_a", "ln_b", commands: "actor_move(chr_a#2,player)"); var b = Line("ln_b", speaker: "chr_narrator");
        var rig = new Rig(new[] { Dialogue("dlg_a", "ln_a", onEnd: "set_flag(done,true)") }, new[] { b, a });
        var m = rig.Machine; rig.Clock.Value = 5;
        Check(m.Start("dlg_a") == NarrativeDialogueStartStatus.Started && m.Phase == NarrativeDialoguePhase.Entering, "normal start then entrance wait");
        Check(rig.View.Events[0].Name == "lock" && rig.View.Events[1].Name == "start" && rig.View.Events[2].Name == "enter" && rig.Resolves == 1, "steps1/2/4 order");
        Check(m.Start("dlg_a") == NarrativeDialogueStartStatus.Busy && rig.View.Locks == 1, "busy does not acquire twice");
        var context = m.Current; rig.Enter(); var oldLine = m.CurrentLine;
        Check(ReferenceEquals(rig.Invocations[0], a.Commands.Commands[0]) && rig.Invocations[0].Arguments[0].Value == "chr_a#2", "cached invocation and full instance identity reused");
        int command = rig.View.Position("cmd:actor_move"), speaker = rig.View.Position("speaker"), reaction = rig.View.Position("react"), text = rig.View.Position("text");
        Check(command < speaker && speaker < reaction && reaction < text, "command/speaker/reaction/text order");
        foreach (int index in new[] { command, speaker, reaction, text }) Check(rig.View.Events[index].Time == 5 && oldLine.StartedAtSeconds == 5, "same caller timestamp for all line starts");
        Check(oldLine.NameKey == "name.a" && oldLine.ShowNameBox && !oldLine.AllPortraitsDim && oldLine.SpeakerId == "chr_a" && oldLine.TextKey == "text.ln_a", "raw name/text keys and in-cast speaker flags");
        Check(m.Click() == NarrativeAdvanceIntent.CompleteLine && m.Phase == NarrativeDialoguePhase.Typing && rig.View.Count("complete-text") == 1, "N011 completion request waits for view report");
        Check(m.NotifyLineCompleted(oldLine) && !m.NotifyLineCompleted(oldLine) && !m.CanEnd, "one completion per line, not final yet");
        rig.Clock.Value = 6; m.Click();
        Check(rig.Handlers[0].Finals == 1 && m.CurrentLine.Line.LineId == "ln_b" && rig.View.Position("truncate") < rig.View.Position("text", "ln_b"), "next line truncates previous before starting");
        Check(!m.CurrentLine.ShowNameBox && m.CurrentLine.AllPortraitsDim && m.CurrentLine.NameKey == "", "ordinary narrator has no name box and dims all portraits");
        Check(!m.NotifyLineCompleted(oldLine), "late old text completion rejected by message identity");
        Check(m.NotifyLineCompleted(m.CurrentLine) && m.CanEnd && rig.View.Count("can-end") == 1, "last line grants end signal");
        m.Click(); Check(m.Phase == NarrativeDialoguePhase.Exiting && rig.View.Records == 0, "exit waits before step9");
        Check(m.NotifyExitCompleted(context) && !m.NotifyExitCompleted(context), "exit completion consumed once");
        Check(m.Phase == NarrativeDialoguePhase.Inactive && rig.View.Records == 1 && rig.View.Locks == 1 && rig.View.Unlocks == 1, "record/release exactly once");
        Check(rig.View.Position("exit") < rig.View.Position("cmd:set_flag") && rig.View.Position("cmd:set_flag") < rig.View.Position("record") && rig.View.Position("record") < rig.View.Position("unlock") && rig.View.Position("unlock") < rig.View.Position("end"), "steps8/9 ordering");
    }

    static void StoryAndInput()
    {
        var rig = new Rig(new[] { Dialogue("dlg_a", "ln_a", NarrativeDialogueLevel.Story) }, new[] { Line("ln_a", commands: "camera_pan(player,4);actor_move(chr_a,player)") });
        var m = rig.Machine; m.Start("dlg_a"); var context = m.Current;
        Check(m.Phase == NarrativeDialoguePhase.StoryPreparing && rig.View.Count("bars-on") == 1 && rig.View.Count("camera") == 0 && rig.View.Count("enter") == 0, "story requests bars only before their completion");
        Check(m.Click() == NarrativeAdvanceIntent.None && !m.NotifyEntranceCompleted(context), "story preparation cannot be skipped as entrance");
        Check(m.NotifyStoryBarsReady(context) && !m.NotifyStoryBarsReady(context) && rig.View.Count("enter") == 0, "wait for both story capabilities");
        Check(m.NotifyStoryCameraReady(context) && m.Phase == NarrativeDialoguePhase.Entering, "camera completion releases entrance");
        Check(m.Click() == NarrativeAdvanceIntent.SkipEntrance && rig.View.Count("skip-enter") == 1 && m.Phase == NarrativeDialoguePhase.Typing, "N011 skips entrance");
        Check(!m.NotifyEntranceCompleted(context), "late entrance completion does not replay text");
        var line = m.CurrentLine; m.NotifyLineCompleted(line);
        Check(m.CanEnd && rig.View.Count("bars-off") == 1, "story last-line completion withdraws bars");
        m.Click();
        Check(rig.Handlers[0].Cancelled && !rig.Handlers[1].Cancelled && rig.Handlers[1].Finals == 0, "story exit cancels camera only, leaves actor background batch");
        m.NotifyExitCompleted(context); Check(rig.View.Records == 1 && rig.Scheduler.ActiveBatchCount > 0, "no background-completion unlock gate");
        rig.Handlers[1].Done = true; m.Tick(); Check(rig.Scheduler.ActiveBatchCount > 0, "FSM does not take over scheduler Tick");
        rig.Scheduler.Tick(rig.Clock.Value); Check(rig.Scheduler.ActiveBatchCount == 0, "external scheduler driver reaps background after dialogue end");

        var fast = new Rig(new[] { Dialogue("dlg_a", "ln_a") }, new[] { Line("ln_a") });
        fast.Input.SetAuto(true); fast.Input.SetFastForward(true); fast.Machine.Start("dlg_a"); fast.Enter();
        Check(fast.Input.AutoEnabled && fast.Input.FastForwardEnabled, "per-segment settings preserve N011 toggles");
        Check(fast.Machine.Click() == NarrativeAdvanceIntent.SkipToNext && fast.Machine.Phase == NarrativeDialoguePhase.Typing, "fast last-line skip asks completion before end");
        fast.Machine.NotifyLineCompleted(fast.Machine.CurrentLine); Check(fast.Machine.Phase == NarrativeDialoguePhase.Exiting && fast.View.Count("can-end") == 1, "last fast skip grants end then exits after report");
        var auto = new Rig(new[] { Dialogue("dlg_a", "ln_a", overrides: "text.autoWait=0.25") }, new[] { Line("ln_a") });
        auto.Input.SetAuto(true); auto.Machine.Start("dlg_a"); auto.Enter(); auto.Machine.NotifyLineCompleted(auto.Machine.CurrentLine);
        auto.Clock.Value = 0.249; auto.Machine.Tick(); Check(auto.Machine.Phase == NarrativeDialoguePhase.LineComplete, "N011 owns AUTO wait");
        auto.Clock.Value = 0.25; auto.Machine.Tick(); Check(auto.Machine.Phase == NarrativeDialoguePhase.Exiting, "snapshot parameters feed the same N011 model");
        var model = new NarrativeAdvanceInputModel(new NarrativeAdvanceSettings(advanceCooldown: 10, cgAdvanceCooldown: 20));
        model.SetAuto(true); model.SetFastForward(true); model.TryClick(NarrativeAdvancePhase.Entering, 0); model.NotifyCgChanged(0);
        model.ConfigureSettings(new NarrativeAdvanceSettings(advanceCooldown: 0, cgAdvanceCooldown: 0));
        Check(model.AutoEnabled && model.FastForwardEnabled && model.TryClick(NarrativeAdvancePhase.Typing, 19) == NarrativeAdvanceIntent.None && model.TryClick(NarrativeAdvancePhase.Typing, 20) == NarrativeAdvanceIntent.SkipToNext, "settings bridge preserves existing cooldown deadlines and toggles");
    }

    static void SerialStoryPreparation()
    {
        var rig = new Rig(new[] { Dialogue("dlg_a", "ln_a", NarrativeDialogueLevel.Story) }, new[] { Line("ln_a") });
        var m = rig.Machine; rig.Clock.Value = 10; m.Start("dlg_a"); var context = m.Current;
        Check(m.Phase == NarrativeDialoguePhase.StoryPreparing && rig.View.Count("bars-on") == 1 && rig.View.Count("camera") == 0 && rig.View.Count("enter") == 0, "P1 bars must complete before any camera request");
        Check(!m.NotifyStoryCameraReady(context) && rig.View.Count("camera") == 0 && rig.View.Count("enter") == 0, "P1 early camera completion is rejected without consumption");
        var foreign = new Rig(new[] { Dialogue("dlg_a", "ln_a", NarrativeDialogueLevel.Story) }, new[] { Line("ln_a") }); foreign.Machine.Start("dlg_a");
        Check(!m.NotifyStoryBarsReady(foreign.Machine.Current) && !m.NotifyStoryCameraReady(foreign.Machine.Current) && rig.View.Count("camera") == 0, "P1 foreign preparation message cannot release a stage");
        rig.Clock.Value = 10.5;
        Check(m.NotifyStoryBarsReady(context) && rig.View.Count("camera") == 1 && rig.View.Count("enter") == 0 && m.Phase == NarrativeDialoguePhase.StoryPreparing, "P1 bars completion immediately requests camera and still waits");
        Check(rig.View.Events[rig.View.Position("camera")].Time == 10.5 && rig.View.Position("bars-on") < rig.View.Position("camera"), "P1 camera request uses actual bars completion time");
        Check(!m.NotifyStoryBarsReady(context) && rig.View.Count("camera") == 1, "P1 duplicate bars completion cannot request camera twice");
        rig.Clock.Value = 11;
        Check(m.NotifyStoryCameraReady(context) && m.Phase == NarrativeDialoguePhase.Entering && rig.View.Count("enter") == 1 && rig.View.Events[rig.View.Position("enter")].Time == 11, "P1 actual camera completion alone releases entrance after bars");
        Check(!m.NotifyStoryCameraReady(context) && !m.NotifyStoryBarsReady(context) && rig.View.Count("enter") == 1, "P1 duplicate or late preparation completion cannot enter twice");
        m.NotifyEntranceCompleted(context);
        Check(!m.NotifyStoryCameraReady(context) && !m.NotifyStoryBarsReady(context), "P1 preparation messages remain late after text starts");
        rig.EndCurrent(); rig.Clock.Value = 12; m.Start("dlg_a");
        Check(!ReferenceEquals(m.Current, context) && !m.NotifyStoryBarsReady(context) && !m.NotifyStoryCameraReady(context) && rig.View.Count("camera") == 1, "P1 stale context cannot prepare a new segment");

        var seamless = new Rig(new[] { Dialogue("dlg_a", "ln_a", NarrativeDialogueLevel.Story, "dlg_b", overrides: "exit.continuationMode=Seamless"), Dialogue("dlg_b", "ln_b", NarrativeDialogueLevel.Story) }, new[] { Line("ln_a"), Line("ln_b", label: "dlg_b") });
        seamless.Machine.Start("dlg_a"); seamless.Enter(); seamless.EndCurrent();
        Check(seamless.Machine.Current.Dialogue.DialogueId == "dlg_b" && seamless.Machine.Phase == NarrativeDialoguePhase.Typing && seamless.View.Count("bars-on") == 1 && seamless.View.Count("camera") == 1 && seamless.View.Count("enter") == 1 && seamless.View.Count("cast") == 1, "P1 same-level story seamless still skips all preparation requests");
    }

    static void FlagsAndFollowUps()
    {
        var lines = new[] { Line("ln_a", "ln_b", "chr_narrator", "line.override"), Line("ln_b", speaker: "chr_b"), Line("ln_c", label: "dlg_c") };
        var rig = new Rig(new[] { Dialogue("dlg_a", "ln_a", next: "dlg_c"), Dialogue("dlg_c", "ln_c") }, lines);
        rig.Machine.Start("dlg_a"); rig.Enter();
        Check(rig.Machine.CurrentLine.ShowNameBox && rig.Machine.CurrentLine.NameKey == "line.override" && rig.Machine.CurrentLine.AllPortraitsDim, "per-line name overrides unnamed narrator without localization service");
        rig.Machine.NotifyLineCompleted(rig.Machine.CurrentLine); rig.Machine.Click();
        Check(rig.Machine.CurrentLine.ShowNameBox && rig.Machine.CurrentLine.NameKey == "name.b" && rig.Machine.CurrentLine.AllPortraitsDim, "named off-cast speaker dims all");
        rig.EndCurrent();
        Check(rig.Machine.Current.Dialogue.DialogueId == "dlg_c" && rig.Machine.Phase == NarrativeDialoguePhase.Entering && rig.View.Count("exit") == 1 && rig.View.Count("enter") == 2 && rig.View.Unlocks == 1 && rig.View.Locks == 2 && rig.Resolves == 2, "full follow-up starts from fresh data/snapshot after unlock");
        Check(rig.View.Position("end", "dlg_a") < rig.View.Position("lock", "dlg_c"), "complete release/end precedes next start");
        rig.Enter(); rig.EndCurrent(); Check(rig.View.Records == 2 && rig.View.Unlocks == 2, "one record per segment");

        var seamless = new Rig(new[] { Dialogue("dlg_a", "ln_a", next: "dlg_b", overrides: "exit.continuationMode=Seamless"), Dialogue("dlg_b", "ln_b") }, new[] { Line("ln_a"), Line("ln_b", label: "dlg_b") });
        seamless.Machine.Start("dlg_a"); seamless.Enter(); var prior = seamless.Machine.Current; seamless.EndCurrent();
        Check(seamless.View.Count("exit") == 0 && seamless.View.Count("enter") == 1 && seamless.View.Count("cast") == 1 && seamless.Machine.Current.Dialogue.DialogueId == "dlg_b" && seamless.Machine.Phase == NarrativeDialoguePhase.Typing, "same-level seamless skips both visual transitions and updates cast");
        Check(seamless.View.Records == 1 && seamless.View.Unlocks == 1 && seamless.View.Locks == 2 && !seamless.Machine.NotifyExitCompleted(prior), "seamless retains per-segment step9/2 and rejects stale context");
        var mixed = new Rig(new[] { Dialogue("dlg_a", "ln_a", next: "dlg_b", overrides: "exit.continuationMode=Seamless"), Dialogue("dlg_b", "ln_b", NarrativeDialogueLevel.Story) }, new[] { Line("ln_a"), Line("ln_b", label: "dlg_b") });
        mixed.Machine.Start("dlg_a"); mixed.Enter(); mixed.EndCurrent();
        Check(mixed.View.Count("exit") == 1 && mixed.View.Count("cast") == 0 && mixed.Machine.Phase == NarrativeDialoguePhase.StoryPreparing && mixed.View.Count("bars-on") == 1, "different levels force full flow");
    }

    static void SceneSwitchAndFailures()
    {
        var rig = new Rig(new[] { Dialogue("dlg_a", "ln_a", next: "dlg_b"), Dialogue("dlg_b", "ln_b") }, new[] { Line("ln_a", "ln_unused", commands: "scene_switch(scn_a,door)"), Line("ln_unused"), Line("ln_b", label: "dlg_b") });
        rig.Machine.Start("dlg_a"); rig.Enter(); var context = rig.Machine.Current;
        Check(rig.Machine.Phase == NarrativeDialoguePhase.Exiting && rig.View.Text.Count == 1 && rig.Machine.PendingSceneSwitches.Count == 1, "actual scheduler signal exits and discards remaining lines");
        var command = rig.Machine.PendingSceneSwitches[0]; rig.Machine.NotifyExitCompleted(context);
        Check(rig.Machine.Phase == NarrativeDialoguePhase.WaitingForSceneSwitch && rig.View.Unlocks == 1 && rig.View.Count("start") == 1, "unlock independent of scene-completion continuation gate");
        rig.Machine.Tick(); Check(rig.View.Count("start") == 1, "no fabricated scene completion");
        Check(!rig.Machine.NotifySceneSwitchCompleted(Commands("scene_switch(scn_a,door)").Commands[0]), "unrelated completion invocation rejected");
        Check(rig.Machine.NotifySceneSwitchCompleted(command) && rig.Machine.Current.Dialogue.DialogueId == "dlg_b" && !rig.Machine.NotifySceneSwitchCompleted(command), "external exact completion starts next once");

        var delayed = new Rig(new[] { Dialogue("dlg_a", "ln_a", next: "dlg_b", onEnd: "wait(2);scene_switch(scn_a,door)", overrides: "tier.commandExecutionMode=Sequential;exit.continuationMode=Seamless"), Dialogue("dlg_b", "ln_b") }, new[] { Line("ln_a"), Line("ln_b", label: "dlg_b") });
        delayed.Machine.Start("dlg_a"); delayed.Enter(); delayed.EndCurrent();
        Check(delayed.Machine.Phase == NarrativeDialoguePhase.WaitingForSceneSwitch && delayed.Machine.PendingSceneSwitches.Count == 0 && delayed.View.Count("cast") == 0, "sequential on_end future switch prevents premature seamless/start");
        delayed.Clock.Value = 2; delayed.Machine.Tick(); Check(delayed.Machine.PendingSceneSwitches.Count == 0, "FSM does not execute scheduler wait");
        delayed.Scheduler.Tick(2); delayed.Machine.Tick();
        Check(delayed.Machine.PendingSceneSwitches.Count == 1 && delayed.View.Count("start") == 1, "actual later signal still needs external completion");
        delayed.Machine.NotifySceneSwitchCompleted(delayed.Machine.PendingSceneSwitches[0]);
        Check(delayed.Machine.Current.Dialogue.DialogueId == "dlg_b" && delayed.Machine.Phase == NarrativeDialoguePhase.Entering, "scene continuation always uses full flow");

        var badCommand = new Rig(new[] { Dialogue("dlg_a", "ln_a", onEnd: "set_flag(end,true);unknown()") }, new[] { Line("ln_a", commands: "set_flag(line,true);unknown()") });
        badCommand.Machine.Start("dlg_a"); badCommand.Enter(); badCommand.EndCurrent();
        Check(badCommand.Invocations.Count == 0 && badCommand.Machine.Diagnostics.Count >= 2 && badCommand.View.Records == 1, "cached failed columns retain diagnostics and never execute valid prefixes");
        var failedSwitch = new Rig(new[] { Dialogue("dlg_a", "ln_a", next: "dlg_b", onEnd: "scene_switch(scn_a,door)") , Dialogue("dlg_b", "ln_b") }, new[] { Line("ln_a"), Line("ln_b", label: "dlg_b") }) { FailSwitch = true };
        failedSwitch.Machine.Start("dlg_a"); failedSwitch.Enter(); failedSwitch.EndCurrent();
        Check(failedSwitch.Machine.Phase == NarrativeDialoguePhase.Inactive && failedSwitch.View.Count("start") == 1 && failedSwitch.View.Unlocks == 1 && failedSwitch.Machine.Diagnostics.Count > 0, "failed switch explicit diagnostic, no false completion or retained lock");
        var ambient = new Rig(new[] { Dialogue("dlg_a", "ln_a", NarrativeDialogueLevel.Ambient) }, new[] { Line("ln_a") });
        Check(ambient.Machine.Start("dlg_a") == NarrativeDialogueStartStatus.NotApplicable && ambient.View.Locks == 0 && ambient.Resolves == 0 && ambient.View.Events.Count == 0, "ambient explicitly not applicable without effects/lock");
        Check(ambient.Machine.Start("missing") == NarrativeDialogueStartStatus.Failed && ambient.View.Locks == 0, "missing dialogue explicit failure");
        var broken = new Rig(new[] { Dialogue("dlg_a", "ln_a") }, new[] { Line("ln_a", "missing") });
        Check(broken.Machine.Start("dlg_a") == NarrativeDialogueStartStatus.Failed && broken.View.Locks == 0 && broken.Machine.Diagnostics.Count > 0, "chain failure never executes ambiguous prefix");
        var invalidConfig = new Rig(new[] { Dialogue("dlg_a", "ln_a") }, new[] { Line("ln_a") }); invalidConfig.Config.text = new DialogueTextConfig[0];
        Check(invalidConfig.Machine.Start("dlg_a") == NarrativeDialogueStartStatus.Failed && invalidConfig.View.Locks == 0, "resolver fatal configuration fails before lock");
    }

    public static void Run()
    {
        checks = 0; NormalLifecycle(); StoryAndInput(); SerialStoryPreparation(); FlagsAndFollowUps(); SceneSwitchAndFailures();
        Console.WriteLine("PASS narrative dialogue FSM: " + checks + " checks; injected fake capabilities only; ten-step normal/story lifecycle");
    }
}
