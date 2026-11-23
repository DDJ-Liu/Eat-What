using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EatWhat.Narrative.Core;
using EatWhat.Narrative.Unity;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;

// Actual MonoBehaviour service -> Session -> FSM/07/08/BoxView; existing external Unity/TMP handles only.
internal static class NarrativeDialogueNotificationTests
{
    static readonly List<string> details = new List<string>();
    static void Check(bool value, string why) { if (!value) throw new Exception("Line notification: " + why); details.Add(why); }
    static void Set(object target, string field, object value)
    { target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value); }
    static object Get(object target, string field)
    { return target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target); }
    static void Life(object target, string method)
    { target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null); }
    public sealed class Lock : MonoBehaviour, INarrativeDialogueLock
    { public void Acquire(string id, double time) { } public void Release(string id, double time) { } }
    static readonly NarrativeCommandSource Empty = new NarrativeCommandSource(NarrativeCommandParser.Parse(""));
    sealed class Rig
    {
        public readonly NarrativeDialogueService Service;
        public readonly NarrativeDialogueBoxView Box;
        public readonly List<NarrativeDialogueLineEvent> Lines = new List<NarrativeDialogueLineEvent>();
        public readonly List<NarrativeDialogueEvent> Starts = new List<NarrativeDialogueEvent>(), Ends = new List<NarrativeDialogueEvent>(), Whole = new List<NarrativeDialogueEvent>();
        public readonly List<string> Trace = new List<string>();
        public readonly List<int> LineFrames = new List<int>();
        public NarrativeDialogueSession Session { get { return (NarrativeDialogueSession)Get(Service, "session"); } }
        public Rig(string saveRoot, string next = "", bool several = false)
        {
            Application.persistentDataPath = saveRoot;
            var hud = new GameObject("Notification HUD", typeof(RectTransform));
            var canvas = hud.AddComponent<NarrativeDialogueCanvas>(); canvas.ConfigureFont(new TMP_FontAsset { material = new Material() });
            Box = hud.AddComponent<NarrativeDialogueBoxView>(); Set(Box, "dialogueCanvas", canvas);
            var dimmer = hud.AddComponent<NarrativeSceneDimmer>(); Set(dimmer, "dialogueCanvas", canvas);
            var root = new GameObject("Notification service"); var runtime = root.AddComponent<NarrativeRuntimeEntry>();
            var config = new DialoguePresentationData();
            config.entrance[0].dimTime = config.entrance[0].boxTime = config.exit[0].boxTime = config.exit[0].undimTime = 0;
            config.text[0].charsPerSecond = config.text[0].advanceCooldown = 0;
            var profiles = new NarrativeProfileNames("", "", "", "", "", "", "", "", "");
            var cast = new[] { new NarrativeCastEntry(1, "chr_a", "", null, null, null, null) };
            runtime.Directory = new NarrativeDataDirectory(
                characters: new[] {
                    new NarrativeCharacter("chr_a", "notify_name", "", null, null, null, "", "", null, null),
                    new NarrativeCharacter("chr_b", "notify_name", "", null, null, null, "", "", null, null),
                    new NarrativeCharacter("chr_off_cast", "notify_name", "", null, null, null, "", "", null, null) },
                dialogues: new[] {
                    new NarrativeDialogue("a", NarrativeDialogueLevel.Normal, cast, profiles, "reaction.turnDelay=0.25", "a1", next, Empty),
                    new NarrativeDialogue("b", NarrativeDialogueLevel.Normal, cast, profiles, "reaction.turnDelay=0.75", "b1", "", Empty),
                    new NarrativeDialogue("bad", NarrativeDialogueLevel.Normal, cast, profiles, "", "bad1", "", Empty),
                    new NarrativeDialogue("ambient", NarrativeDialogueLevel.Ambient, cast, profiles, "", "ambient1", "", Empty),
                    new NarrativeDialogue("story", NarrativeDialogueLevel.Story, cast, profiles, "", "story1", "", Empty) },
                lines: new[] {
                    new NarrativeDialogueLine("a1", "a", several ? "a2" : "", "chr_a", "", "notify_body", Empty, "", "", "", ""),
                    new NarrativeDialogueLine("a2", "a", "a3", "chr_a", "", "notify_body", Empty, "", "", "", ""),
                    new NarrativeDialogueLine("a3", "a", "a4", "", "", "notify_body", Empty, "", "", "", ""),
                    new NarrativeDialogueLine("a4", "a", "", "chr_off_cast", "", "notify_body", Empty, "", "", "", ""),
                    new NarrativeDialogueLine("b1", "b", "", "chr_b", "", "notify_body", Empty, "", "", "", ""),
                    new NarrativeDialogueLine("bad1", "bad", "bad1", "chr_a", "", "notify_body", Empty, "", "", "", ""),
                    new NarrativeDialogueLine("ambient1", "ambient", "", "chr_a", "", "notify_body", Empty, "", "", "", ""),
                    new NarrativeDialogueLine("story1", "story", "", "chr_a", "", "notify_body", Empty, "", "", "", "") });
            runtime.Resolve = (dialogue, policy) => policy.Resolve(config, dialogue);
            Service = root.AddComponent<NarrativeDialogueService>(); Set(Service, "runtimeEntry", runtime); Set(Service, "boxView", Box);
            Set(Service, "sceneDimmer", dimmer); Set(Service, "operationLockProvider", root.AddComponent<Lock>());
            Service.DialogueStarted += item => { Starts.Add(item); Trace.Add("start:" + item.DialogueId); };
            Service.DialogueEnded += item => { Ends.Add(item); Trace.Add("end:" + item.DialogueId); };
            Service.LineStarted += item =>
            {
                Lines.Add(item); LineFrames.Add(Time.frameCount); Trace.Add("line:" + item.DialogueId + "/" + item.LineId);
                Check(Service.CurrentSnapshot != null && ReferenceEquals(Service.CurrentSnapshot, Session.CurrentSnapshot), "line callback exposes the actual current snapshot");
                Check(Service.CurrentSnapshot.Get<float>("reaction.turnDelay") == (item.DialogueId == "a" ? .25f : .75f), "query follows this segment's resolved snapshot rather than the previous segment");
                Check(Service.CurrentSpeakerId == item.SpeakerId && item.SpeakerId == Session.CurrentSpeakerId, "line callback speaker is actual FSM line speaker");
                Check(((TextMeshProUGUI)Box.BodyTextComponent).text == "通知正文", "notification follows actual body presentation in same call");
            };
            Service.ConversationEnded += item => { Whole.Add(item); Trace.Add("whole:" + item.DialogueId); };
        }
        public NarrativeServiceStartStatus Start(string id = "a")
        { string issue; return Service.TryStart(id, "chr_initiator#2", out issue); }
        public void Step() { Time.frameCount++; Time.deltaTime = 0; Life(Service, "Update"); }
        public void Finish()
        {
            for (int i = 0; i < 40 && Service.IsRunning; i++)
            {
                Step();
                if (Session.Phase == NarrativeDialoguePhase.Typing || Session.Phase == NarrativeDialoguePhase.LineComplete)
                    Session.RequestAdvance(Time.frameCount);
            }
            Check(!Service.IsRunning, "fixture completes with actual FSM input and presentation callbacks");
        }
        public void Destroy() { Life(Service, "OnDestroy"); Life(Box, "OnDisable"); }
    }
    public static void Run(string root)
    {
        string originalPath = Application.persistentDataPath;
        try
        {
            var localization = new LocTableAsset(); localization.entries.Add(new LocTableEntry { key = "notify_body", zh_cn = "通知正文" });
            localization.entries.Add(new LocTableEntry { key = "notify_name", zh_cn = "通知姓名" }); LocService.Configure(localization);
            var normal = new Rig(Path.Combine(root, "normal"), several: true);
            Check(normal.Service.CurrentSnapshot == null && normal.Service.CurrentSpeakerId == null, "queries empty before session creation");
            Check(normal.Start() == NarrativeServiceStartStatus.Started, "actual MonoBehaviour start entry preserved");
            Check(normal.Service.CurrentSnapshot != null && normal.Service.CurrentSpeakerId == null && normal.Lines.Count == 0, "entrance has snapshot but no current speaker or early line event");
            var snapshot = normal.Service.CurrentSnapshot; int before = Time.frameCount; normal.Step();
            Check(normal.Lines.Count == 1 && normal.LineFrames[0] == before + 1, "first line is notified during presentation pump without a later frame");
            var first = normal.Lines[0];
            Check(first.DialogueId == "a" && first.LineId == "a1" && first.SpeakerId == "chr_a", "all three immutable line identities are correct");
            var line = ((NarrativeDialogueStateMachine)Get(normal.Session, "machine")).CurrentLine;
            ((INarrativeDialogueView)normal.Session).StartText(line, normal.Session.NowSeconds);
            Check(normal.Lines.Count == 1, "duplicate presentation request for current line does not notify twice");
            normal.Session.RequestAdvance(Time.frameCount); normal.Step();
            Check(normal.Session.Phase == NarrativeDialoguePhase.LineComplete && normal.Service.CurrentSpeakerId == "chr_a" && ReferenceEquals(normal.Service.CurrentSnapshot, snapshot), "queries remain valid after text completes until segment end");
            normal.Finish();
            Check(normal.Lines.Count == 4 && normal.Lines[1].LineId == "a2" && normal.Lines[1].SpeakerId == "chr_a", "consecutive lines by same speaker each notify once");
            Check(normal.Lines[2].SpeakerId == "" && normal.Lines[3].SpeakerId == "chr_off_cast", "narration and off-cast role IDs pass through FSM unchanged");
            Check(normal.Whole.Count == 1 && !normal.Whole[0].Aborted && ReferenceEquals(normal.Whole[0], normal.Ends[0]), "real final end has one separate notification and unchanged segment payload");
            Check(normal.Service.CurrentSnapshot == null && normal.Service.CurrentSpeakerId == null, "queries clear after normal end");
            Check(snapshot.Get<float>("reaction.turnDelay") == .25f && first.LineId == "a1", "retained snapshot and event remain unchanged after end");
            int count = normal.Lines.Count; normal.Step(); normal.Step(); Check(normal.Lines.Count == count && normal.Whole.Count == 1, "idle pumps do not repeat line or final events");
            var disposedSession = normal.Session; normal.Destroy();
            Check(Get(disposedSession, "LineStarted") == null && Get(disposedSession, "ConversationEnded") == null, "service destruction detaches both new session forwarding subscriptions");

            var chain = new Rig(Path.Combine(root, "chain"), "b"); chain.Start();
            chain.Service.DialogueEnded += item =>
            { Check(chain.Service.CurrentSnapshot == null && chain.Service.CurrentSpeakerId == null, "segment end clears queries before existing end callback"); };
            chain.Finish();
            Check(chain.Lines.Count == 2 && chain.Lines[0].DialogueId == "a" && chain.Lines[1].DialogueId == "b" && chain.Lines[1].LineId == "b1", "follow-up lines carry their different segment IDs");
            Check(chain.Starts.Count == 2 && chain.Ends.Count == 2 && chain.Whole.Count == 1 && chain.Whole[0].DialogueId == "b", "segment transition does not emit whole-conversation end");
            Check(string.Join(",", chain.Trace) == "start:a,line:a/a1,end:a,start:b,line:b/b1,end:b,whole:b", "old start/end order retained and final signal follows actual continuation");
            Check(chain.Whole[0].InitiatorId == "chr_initiator#2" && chain.Service.CurrentSnapshot == null && chain.Service.CurrentSpeakerId == null, "final signal keeps original initiator and clears queries"); chain.Destroy();

            var abort = new Rig(Path.Combine(root, "abort"), several: true); abort.Start(); abort.Step();
            abort.Session.Abort(); int stopped = abort.Lines.Count; abort.Step(); abort.Session.RequestAdvance(Time.frameCount); abort.Step();
            Check(stopped == 1 && abort.Lines.Count == stopped, "abort discards FSM and prevents further line notifications");
            Check(abort.Whole.Count == 1 && abort.Whole[0].Aborted && abort.Ends.Count == 1, "abort emits segment and whole end once");
            Check(abort.Service.CurrentSnapshot == null && abort.Service.CurrentSpeakerId == null, "abort clears both queries"); abort.Session.Abort();
            Check(abort.Whole.Count == 1, "repeated abort does not repeat whole end"); abort.Destroy();

            var early = new Rig(Path.Combine(root, "early")); early.Start(); Life(early.Service, "OnDisable"); early.Step();
            Check(early.Lines.Count == 0 && early.Whole.Count == 1 && early.Whole[0].Aborted, "disable during entrance emits no line and finishes once"); early.Destroy();
            var nested = new Rig(Path.Combine(root, "nested"), several: true); nested.Service.LineStarted += item => nested.Session.Abort(); nested.Start(); nested.Step(); nested.Step();
            Check(nested.Lines.Count == 1 && nested.Whole.Count == 1 && nested.Whole[0].Aborted, "abort from line listener is deferred safely and no later line appears"); nested.Destroy();

            foreach (string next in new[] { "missing", "bad", "ambient", "story" })
            {
                var invalid = new Rig(Path.Combine(root, next), next); invalid.Start(); invalid.Finish();
                Check(invalid.Starts.Count == 1 && invalid.Lines.Count == 1 && invalid.Whole.Count == 1 && invalid.Whole[0].DialogueId == "a", "non-starting follow-up " + next + " is a real end, not a predicted continuation"); invalid.Destroy();
            }
            var rejected = new Rig(Path.Combine(root, "rejected")); Check(rejected.Start("missing") == NarrativeServiceStartStatus.MissingDialogue, "missing external start remains rejected");
            Check(rejected.Lines.Count == 0 && rejected.Whole.Count == 0 && rejected.Service.CurrentSnapshot == null && rejected.Service.CurrentSpeakerId == null, "rejected start creates no notifications or query state"); rejected.Destroy();
            Console.WriteLine(JsonConvert.SerializeObject(new { result = "DIALOGUE_NOTIFICATIONS_PASS", checks = details.Count, details, nativeUnity = false, realTmpLayoutVerified = false }, Formatting.Indented));
        }
        finally { Application.persistentDataPath = originalPath; }
    }
}
