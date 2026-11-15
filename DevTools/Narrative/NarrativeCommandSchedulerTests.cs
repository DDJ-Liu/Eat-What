using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;

// Independent expectations: N0-10 rule table; Schema v2.5 command rows423-436,
// execution rules440-445 and V-16/V-16b. Fakes own all effect/timing behavior.
internal static class NarrativeCommandSchedulerTests
{
    private static int checks;
    private sealed class Clock { public double Now; }
    private sealed class Call
    {
        public string Action, Name;
        public double Time;
        public NarrativeCommandTruncation Truncation;
    }
    private sealed class FakeHandler : INarrativeCommandHandler
    {
        public NarrativeCommandInvocation Invocation;
        public Clock Clock;
        public List<Call> Log;
        public double Duration, Started;
        public bool Begun, Final, Cancelled, ThrowOnBegin;
        public void Begin(double now)
        {
            Begun = true; Started = now; Log.Add(new Call { Action = "begin", Name = Invocation.Name, Time = now });
            if (ThrowOnBegin) throw new InvalidOperationException("synthetic handler fault");
        }
        public bool IsComplete { get { return Begun && (Final || Clock.Now >= Started + Duration); } }
        public void CompleteImmediately(NarrativeCommandTruncation truncation, double now)
        { Final = true; Log.Add(new Call { Action = "final", Name = Invocation.Name, Time = now, Truncation = truncation }); }
        public void Cancel(double now)
        { Cancelled = true; Log.Add(new Call { Action = "cancel", Name = Invocation.Name, Time = now }); }
    }
    private sealed class Fakes
    {
        public readonly Clock Clock = new Clock();
        public readonly List<Call> Log = new List<Call>();
        public readonly List<FakeHandler> Handles = new List<FakeHandler>();
        public readonly Dictionary<string, double> Durations = new Dictionary<string, double>();
        public double DefaultDuration = 10;
        public string ThrowBegin, ThrowFactory;
        public INarrativeCommandHandler Create(NarrativeCommandInvocation command)
        {
            if (command.Name == "wait") throw new Exception("wait must be owned by the scheduler");
            if (command.Name == ThrowFactory) throw new InvalidOperationException("synthetic factory fault");
            double duration;
            if (!Durations.TryGetValue(command.Name, out duration)) duration = DefaultDuration;
            var handle = new FakeHandler { Invocation = command, Clock = Clock, Log = Log,
                Duration = duration, ThrowOnBegin = command.Name == ThrowBegin };
            Handles.Add(handle); return handle;
        }
        public int Count(string action) { return Log.FindAll(x => x.Action == action).Count; }
        public FakeHandler Handle(string name) { return Handles.Find(x => x.Invocation.Name == name); }
    }
    private static void Check(bool value, string label)
    { checks++; if (!value) throw new Exception(label); }
    private static NarrativeCommandParseResult Text(string source)
    {
        var parsed = NarrativeCommandParser.Parse(source);
        if (!parsed.Success) throw new Exception("Invalid authored fixture: " + source);
        return parsed;
    }
    private static void State(NarrativeCommandBatch batch, int index, NarrativeCommandExecutionState expected, string label)
    { Check(batch.Commands[index].State == expected, label); }

    private static void StartAndTiming()
    {
        var f = new Fakes { DefaultDuration = 3 }; var s = new NarrativeCommandScheduler(f.Create);
        var b = s.StartLine(Text("actor_move(chr_a,player);actor_face(chr_b,player);wait(2);object_active(gate,true)"), NarrativeDialogueLevel.Story, 0);
        bool textStarted = true; // Caller continues immediately; no await/sleep is involved.
        Check(textStarted && !b.IsComplete && f.Count("begin") == 3, "Text can start while parallel handlers remain unfinished");
        foreach (var e in b.Commands) Check(e.StartedAtSeconds == 0, "All parallel commands share supplied timestamp");
        foreach (var call in f.Log) Check(call.Action == "begin" && call.Time == 0, "Parallel begins at same time");
        f.Clock.Now = 1.999999; s.Tick(f.Clock.Now); State(b, 2, NarrativeCommandExecutionState.Running, "wait just before boundary");
        f.Clock.Now = 2; s.Tick(f.Clock.Now); State(b, 2, NarrativeCommandExecutionState.Completed, "wait at exact boundary");
        State(b, 0, NarrativeCommandExecutionState.Running, "Parallel wait does not serialize other commands");
        f.Clock.Now = 3; s.Tick(f.Clock.Now); Check(b.IsComplete && s.ActiveBatchCount == 0, "Parallel completion releases active batch");

        f = new Fakes { DefaultDuration = 1 }; f.Durations["set_flag"] = 0; f.Clock.Now = 10;
        s = new NarrativeCommandScheduler(f.Create);
        b = s.StartLine(Text("actor_move(chr_a,player);wait(2);actor_face(chr_a,anchor:door);set_flag(done,1)"), NarrativeDialogueLevel.Story, 10, NarrativeCommandExecutionMode.Sequential);
        Check(f.Count("begin") == 1 && b.Commands[1].StartedAtSeconds == null, "Sequential starts only first command");
        f.Clock.Now = 10.999999; s.Tick(f.Clock.Now); Check(f.Count("begin") == 1, "Sequential waits for unfinished first handler");
        f.Clock.Now = 11; s.Tick(f.Clock.Now); Check(b.Commands[1].StartedAtSeconds == 11 && f.Count("begin") == 1, "Scheduler starts its own wait after previous completes");
        f.Clock.Now = 12.999999; s.Tick(f.Clock.Now); Check(!f.Handle("actor_face").Begun, "Sequential wait prevents later begin before deadline");
        f.Clock.Now = 13; s.Tick(f.Clock.Now); Check(f.Handle("actor_face").Begun && f.Handle("actor_face").Started == 13, "Next begins at observed wait completion");
        f.Clock.Now = 14; s.Tick(f.Clock.Now); Check(b.IsComplete && f.Handle("set_flag").Started == 14, "Immediate tail completes in same tick");
        Check(f.Log[0].Name == "actor_move" && f.Log[1].Name == "actor_face" && f.Log[2].Name == "set_flag", "Sequential begin order retained");
        Check(f.Handles.Count == 3, "No fake handler created for wait");

        f = new Fakes(); f.Durations["set_flag"] = 0; f.Clock.Now = 20; s = new NarrativeCommandScheduler(f.Create);
        b = s.StartLine(Text("wait(0);wait(-1);set_flag(x,1)"), NarrativeDialogueLevel.Story, 20, NarrativeCommandExecutionMode.Sequential);
        Check(b.IsComplete && f.Handle("set_flag").Started == 20, "Nonpositive waits require no future clock time, without new numeric validation");
        f = new Fakes(); f.Clock.Now = 0; s = new NarrativeCommandScheduler(f.Create);
        b = s.StartLine(Text("wait(1);actor_move(chr_a,player)"), NarrativeDialogueLevel.Story, 0, NarrativeCommandExecutionMode.Sequential);
        f.Clock.Now = 5; s.Tick(5); Check(f.Handle("actor_move").Started == 5, "Sparse tick observes completion now; no backdated handler start");
    }

    private static void CutoffAndLifecycle()
    {
        string[] commands = { "set_flag(k,v)", "actor_move(chr_a,player)", "actor_face(chr_a,player)",
            "object_active(gate,true)", "shake(light)", "start_dialogue(dlg_next)",
            "set_scene_state(scn_x,morning)", "scene_switch(scn_x,entry)", "camera_pan(player,3)",
            "camera_return()", "cg_show(cg_x)", "cg_hide()" };
        NarrativeCommandTruncation[] endings = { NarrativeCommandTruncation.Instant,
            NarrativeCommandTruncation.SnapActorPosition, NarrativeCommandTruncation.SnapActorFacing,
            NarrativeCommandTruncation.ApplyObjectActiveFinal, NarrativeCommandTruncation.StopShake,
            NarrativeCommandTruncation.Instant, NarrativeCommandTruncation.Instant, NarrativeCommandTruncation.Instant,
            NarrativeCommandTruncation.SnapCamera, NarrativeCommandTruncation.SnapCamera,
            NarrativeCommandTruncation.FinishTransition, NarrativeCommandTruncation.FinishTransition };
        var f = new Fakes(); var s = new NarrativeCommandScheduler(f.Create);
        var b = s.StartLine(Text("wait(99);" + string.Join(";", commands)), NarrativeDialogueLevel.Story, 0, NarrativeCommandExecutionMode.Sequential);
        Check(f.Count("begin") == 0, "Sequential tail has no effect begin before wait");
        f.Clock.Now = 0.25; s.TruncateCurrentLine(0.25);
        Check(b.IsComplete && f.Count("final") == 12 && f.Count("begin") == 0, "Pending tail directly applies final results without begin");
        State(b, 0, NarrativeCommandExecutionState.Truncated, "Cutoff stops running wait");
        for (int i = 0; i < commands.Length; i++)
        {
            Check(f.Log[i].Name == commands[i].Substring(0, commands[i].IndexOf('(')), "Pending final result order follows authored order");
            Check(f.Log[i].Truncation == endings[i] && f.Log[i].Time == 0.25, "All signature cutoff categories passed exactly");
            Check(b.Commands[i + 1].StartedAtSeconds == null && b.Commands[i + 1].CompletedAtSeconds == 0.25, "Pending final result has no fictitious begin timestamp");
        }
        NarrativeCommandInvocation signal;
        Check(s.TryTakeSceneSwitch(out signal) && signal.Name == "scene_switch", "Truncated pending scene switch emits execution signal");
        s.TruncateCurrentLine(0.25); f.Clock.Now = 100; s.Tick(100);
        Check(f.Count("final") == 12 && !s.TryTakeSceneSwitch(out signal), "Repeated cutoff/tick cannot repeat final results or scene signal");

        f = new Fakes(); s = new NarrativeCommandScheduler(f.Create);
        b = s.StartLine(Text("actor_move(chr_a,player);actor_face(chr_a,player);object_active(gate,true);shake(light);camera_pan(player,3);camera_return();cg_show(cg_x);cg_hide()"), NarrativeDialogueLevel.Story, 0);
        f.Clock.Now = 1; s.StartLine(Text(""), NarrativeDialogueLevel.Story, 1);
        Check(b.IsComplete && f.Count("final") == 8 && f.Count("begin") == 8, "New text start truncates all unfinished old categories");
        f = new Fakes { DefaultDuration = 1 }; s = new NarrativeCommandScheduler(f.Create);
        b = s.StartLine(Text("actor_move(chr_a,player)"), NarrativeDialogueLevel.Story, 0);
        f.Clock.Now = 1; s.TruncateCurrentLine(1);
        State(b, 0, NarrativeCommandExecutionState.Completed, "Cutoff first observes naturally completed handler");
        Check(f.Count("final") == 0, "No duplicate final callback for already completed effect");

        f = new Fakes { DefaultDuration = 2 }; f.Durations["set_flag"] = 0; f.Durations["object_active"] = 3;
        s = new NarrativeCommandScheduler(f.Create);
        var line = s.StartLine(Text("actor_move(chr_a,player);wait(1);set_flag(bg,1)"), NarrativeDialogueLevel.Story, 0, NarrativeCommandExecutionMode.Sequential);
        s.EndDialogue(); Check(line.IsDetached && !line.IsComplete && f.Count("final") == 0, "Dialogue end detaches without truncation");
        f.Clock.Now = 0.25; var ending = s.StartOnEnd(Text("object_active(gate,false)"), NarrativeDialogueLevel.Story, 0.25);
        f.Clock.Now = 0.5; var next = s.StartLine(Text("wait(5)"), NarrativeDialogueLevel.Story, 0.5);
        f.Clock.Now = 0.6; s.TruncateCurrentLine(0.6);
        Check(next.IsComplete && !line.IsComplete && !ending.IsComplete && f.Count("final") == 0, "Only current line cut; old/on_end batches continue in background");
        f.Clock.Now = 2; s.Tick(2); Check(line.Commands[1].StartedAtSeconds == 2, "Detached sequential tail still starts");
        f.Clock.Now = 3; s.Tick(3); Check(line.IsComplete && f.Handle("set_flag").Started == 3 && !ending.IsComplete, "Detached tail reaches final command normally");
        f.Clock.Now = 3.25; s.Tick(3.25); Check(ending.IsComplete && s.ActiveBatchCount == 0, "On_end work also completes and releases batch");

        f = new Fakes(); s = new NarrativeCommandScheduler(f.Create);
        b = s.StartLine(Text("camera_pan(player,3);actor_move(chr_a,player);camera_return();shake(light)"), NarrativeDialogueLevel.Story, 0);
        f.Clock.Now = 1; s.CancelCameraCommands(1); s.CancelCameraCommands(1);
        State(b, 0, NarrativeCommandExecutionState.Cancelled, "Pan cancelled for camera takeover");
        State(b, 2, NarrativeCommandExecutionState.Cancelled, "Return cancelled for camera takeover");
        Check(f.Count("cancel") == 2 && f.Count("final") == 0, "Camera takeover cancels once, never applies old final camera position");
        Check(!f.Handle("actor_move").Cancelled && !f.Handle("shake").Cancelled, "Noncamera effects remain untouched");
        f.Clock.Now = 10; s.Tick(10); Check(b.IsComplete, "Noncamera effects continue to normal completion");
        f = new Fakes { DefaultDuration = 1 }; s = new NarrativeCommandScheduler(f.Create);
        b = s.StartLine(Text("actor_move(chr_a,player);camera_pan(player,3);object_active(gate,true);camera_return()"), NarrativeDialogueLevel.Story, 0, NarrativeCommandExecutionMode.Sequential);
        f.Clock.Now = 0.2; s.CancelCameraCommands(0.2);
        Check(!f.Handle("camera_pan").Begun && !f.Handle("camera_return").Begun && f.Count("cancel") == 2, "Pending sequential cameras cancelled without begin");
        f.Clock.Now = 1; s.Tick(1); Check(f.Handle("object_active").Started == 1, "Cancelled camera slot does not block noncamera successor");
        f.Clock.Now = 2; s.Tick(2); Check(b.IsComplete, "Tail completes past cancelled camera slots");
        f = new Fakes { DefaultDuration = 1 }; s = new NarrativeCommandScheduler(f.Create);
        b = s.StartLine(Text("camera_pan(player,3)"), NarrativeDialogueLevel.Story, 0);
        f.Clock.Now = 1; s.CancelCameraCommands(1); Check(f.Count("cancel") == 0 && b.IsComplete, "Already completed camera needs no cancellation");

        f = new Fakes(); f.Durations["scene_switch"] = 0; s = new NarrativeCommandScheduler(f.Create);
        b = s.StartOnEnd(Text("scene_switch(scn_next,entry)"), NarrativeDialogueLevel.Story, 0);
        Check(b.IsComplete && s.TryTakeSceneSwitch(out signal), "Scene execution signals caller to end segment");
        f.Clock.Now = 1; s.Tick(1); Check(!s.TryTakeSceneSwitch(out signal), "Scene signal consumed exactly once");
        b = s.StartLine(Text("scene_switch(scn_next,entry)"), NarrativeDialogueLevel.Normal, 1);
        Check(b.Diagnostics.Count == 1 && b.Diagnostics[0].Reason.Contains("on_end") && s.TryTakeSceneSwitch(out signal), "Off-on_end placement remains Schema V-16 warning, not runtime rejection");
    }

    private static void AvailabilityAndFailures()
    {
        string all = "wait(1);set_flag(k,v);actor_move(chr_a,player);actor_face(chr_a,player);object_active(gate,true);shake(light);start_dialogue(dlg_x);set_scene_state(scn_x,morning);scene_switch(scn_x,entry);camera_pan(player,3);camera_return();cg_show(cg_x);cg_hide();actor_emote(chr_a,smile)";
        int[] skips = { 8, 5, 1 }, handlers = { 5, 8, 12 };
        var levels = new[] { NarrativeDialogueLevel.Ambient, NarrativeDialogueLevel.Normal, NarrativeDialogueLevel.Story };
        for (int i = 0; i < levels.Length; i++)
        {
            var f = new Fakes(); var s = new NarrativeCommandScheduler(f.Create);
            var b = s.StartOnEnd(Text(all), levels[i], 0);
            Check(b.Diagnostics.Count == skips[i] && f.Handles.Count == handlers[i], "Independent runtime level matrix and no factory calls for skipped commands");
            State(b, 13, NarrativeCommandExecutionState.Skipped, "Reserved actor_emote skipped at every level");
            foreach (var diagnostic in b.Diagnostics)
                Check(diagnostic.Position == all.IndexOf(diagnostic.Reason.Substring(0, diagnostic.Reason.IndexOf(':')), StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(diagnostic.Reason), "Availability diagnostic retains original command position and reason");
        }
        var missing = new NarrativeCommandScheduler(null);
        var unavailable = missing.StartLine(Text("set_flag(k,v);wait(1)"), NarrativeDialogueLevel.Story, 0, NarrativeCommandExecutionMode.Sequential);
        Check(unavailable.Diagnostics.Count == 1 && unavailable.Commands[0].State == NarrativeCommandExecutionState.Skipped, "Missing handler skips with diagnostic instead of inventing effect");
        missing.Tick(1); Check(unavailable.IsComplete, "Scheduler-owned wait works with no effect handlers");
        var fakes = new Fakes(); var scheduler = new NarrativeCommandScheduler(fakes.Create);
        var invalid = scheduler.StartLine(NarrativeCommandParser.Parse("set_flag(k,v);unknown()"), NarrativeDialogueLevel.Story, 0);
        Check(invalid.Diagnostics.Count == 1 && fakes.Handles.Count == 0 && invalid.IsComplete, "Failed parse never dispatches retained valid prefix");
        fakes = new Fakes { ThrowBegin = "actor_move" }; scheduler = new NarrativeCommandScheduler(fakes.Create);
        var failed = scheduler.StartLine(Text("actor_move(chr_a,player);object_active(gate,true)"), NarrativeDialogueLevel.Story, 0);
        State(failed, 0, NarrativeCommandExecutionState.Failed, "Handler callback failure becomes per-command failed state");
        Check(failed.Diagnostics.Count == 1 && fakes.Handle("actor_move").Cancelled && fakes.Handle("object_active").Begun, "Failed handler cleaned up while other parallel work proceeds");
        fakes = new Fakes { ThrowFactory = "actor_move" }; scheduler = new NarrativeCommandScheduler(fakes.Create);
        failed = scheduler.StartLine(Text("actor_move(chr_a,player);wait(1)"), NarrativeDialogueLevel.Story, 0, NarrativeCommandExecutionMode.Sequential);
        Check(failed.Diagnostics.Count == 1 && failed.Commands[1].StartedAtSeconds == 0, "Factory failure does not stall sequential tail");
        foreach (double bad in new[] { double.NaN, double.PositiveInfinity })
        {
            bool rejected = false; try { scheduler.Tick(bad); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected, "Nonfinite caller time rejected as programming contract error");
        }
    }
    public static void Run()
    {
        StartAndTiming(); CutoffAndLifecycle(); AvailabilityAndFailures();
        Console.WriteLine("PASS narrative scheduler tests: " + checks + " checks; contracts=3");
    }
}
