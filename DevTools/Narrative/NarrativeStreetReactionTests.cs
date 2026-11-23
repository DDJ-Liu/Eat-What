using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;

internal static class NarrativeStreetReactionTests
{
    static readonly List<string> details = new List<string>();
    static void Check(bool value, string name)
    { if (!value) throw new Exception("Street reaction: " + name); details.Add(name); }
    static NarrativeStreetReactionActor Actor(string id, float x, bool right, bool visible = true)
    { return new NarrativeStreetReactionActor(id, x, right, visible); }
    static bool Has(IReadOnlyList<NarrativeStreetFacingCommand> commands, string id)
    { foreach (var c in commands) if (c.ActorId == id) return true; return false; }
    static void Facing(IReadOnlyList<NarrativeStreetFacingCommand> commands, string id, bool right, string name)
    {
        bool found = false; foreach (var c in commands) if (c.ActorId == id) { found = c.FacingRight == right; break; }
        Check(found, name);
    }
    sealed class Scene : INarrativeStreetReactionSource
    {
        public readonly Dictionary<string, NarrativeStreetReactionActor> Actors = new Dictionary<string, NarrativeStreetReactionActor>(StringComparer.Ordinal);
        readonly List<string> candidates = new List<string>();
        public readonly List<string> Requests = new List<string>();
        public int Queries;
        public void Put(NarrativeStreetReactionActor actor, bool candidate = true)
        { Actors[actor.ActorId] = actor; if (candidate && !candidates.Contains(actor.ActorId)) candidates.Add(actor.ActorId); }
        public bool TryGetActor(string id, out NarrativeStreetReactionActor actor)
        { Requests.Add(id); return Actors.TryGetValue(id, out actor); }
        public IEnumerable<NarrativeStreetReactionActor> GetCandidates()
        {
            Queries++; var result = new List<NarrativeStreetReactionActor>();
            foreach (var id in candidates) { NarrativeStreetReactionActor a; if (Actors.TryGetValue(id, out a)) result.Add(a); }
            return result;
        }
        public void Apply(IReadOnlyList<NarrativeStreetFacingCommand> commands)
        {
            foreach (var c in commands)
            { var a = Actors[c.ActorId]; Actors[c.ActorId] = Actor(a.ActorId, a.X, c.FacingRight, a.IsOnScreen); }
        }
    }
    static Scene Pair(float npcX = 0, float playerX = 10, bool npcRight = false, bool playerRight = true)
    { var s = new Scene(); s.Put(Actor("npc", npcX, npcRight)); s.Put(Actor("player", playerX, playerRight)); return s; }
    static DialoguePresentationSnapshot Snapshot(double seconds = 2, DialogueTurnMode mode = DialogueTurnMode.FaceSpeaker)
    {
        var data = new DialoguePresentationData(); data.reaction[0].turnDelay = (float)seconds; data.reaction[0].turnMode = mode;
        var dialogue = new NarrativeDialogue("dlg_reaction", NarrativeDialogueLevel.Normal, null,
            new NarrativeProfileNames("", "", "", "", "", "", "", "", ""), "", "", "",
            new NarrativeCommandSource(NarrativeCommandParser.Parse("")));
        var result = DialoguePresentationResolver.Resolve(data, dialogue);
        if (!result.Success) throw new Exception("Reaction tests require real valid parameter resolution.");
        return result.Snapshot;
    }
    static NarrativeStreetReactionRules Start(Scene scene, bool withNpc = true)
    {
        var r = new NarrativeStreetReactionRules(scene);
        scene.Apply(r.Begin(withNpc ? scene.Actors["npc"] : null, scene.Actors["player"])); return r;
    }
    static void InvalidTime(NarrativeStreetReactionRules rules, double value, string name)
    {
        bool rejected = false; try { rules.Advance(value); } catch (ArgumentOutOfRangeException) { rejected = true; }
        Check(rejected, name);
    }

    public static void Run()
    {
        details.Clear();
        var left = Pair(); var initial = new NarrativeStreetReactionRules(left);
        var begin = initial.Begin(left.Actors["npc"], left.Actors["player"]);
        Check(begin.Count == 2 && initial.IsActive, "left placement gives immediate mutual-facing commands");
        Facing(begin, "npc", true, "NPC on left faces right"); Facing(begin, "player", false, "player on right faces left");
        Check(left.Actors["npc"].FacingRight == false && left.Actors["player"].FacingRight, "rules only return commands and do not mutate source actors");
        Check(left.Queries == 0 && left.Requests.Count == 0, "mutual facing needs no candidate query");
        var restored = initial.End();
        Check(restored.Count == 1 && !Has(restored, "player"), "restoration includes initiating NPC and excludes player");
        Facing(restored, "npc", false, "initiator original direction retained");
        var right = Pair(10, 0, true, false); var rightRules = new NarrativeStreetReactionRules(right);
        var rightStart = rightRules.Begin(right.Actors["npc"], right.Actors["player"]);
        Facing(rightStart, "npc", false, "NPC on right faces left"); Facing(rightStart, "player", true, "player on left faces right");
        rightRules.End();
        foreach (float distance in new[] { 0f, .009f, -.009f })
        {
            var near = Pair(0, distance); var r = new NarrativeStreetReactionRules(near);
            Check(r.Begin(near.Actors["npc"], near.Actors["player"]).Count == 0, "strict tolerance leaves near pair unchanged " + distance);
            Check(r.End().Count == 1, "initiator still retained in restoration for near pair " + distance);
        }
        var edge = Pair(0, .01f); var edgeRules = new NarrativeStreetReactionRules(edge);
        Check(edgeRules.Begin(edge.Actors["npc"], edge.Actors["player"]).Count == 2, "exact positive .01 is outside tolerance"); edgeRules.End();
        var aligned = Pair(0, 10, true, false); var alignedRules = new NarrativeStreetReactionRules(aligned);
        Check(alignedRules.Begin(aligned.Actors["npc"], aligned.Actors["player"]).Count == 0 && alignedRules.End().Count == 1,
            "already-facing pair emits no redundant command while initiator remains remembered");
        var noNpc = Pair(); var noNpcRules = new NarrativeStreetReactionRules(noNpc);
        Check(noNpcRules.Begin(null, noNpc.Actors["player"]).Count == 0 && noNpcRules.End().Count == 0, "no initiating NPC skips initial facing and restoration");

        var fresh = Pair(); var rules = Start(fresh);
        fresh.Put(Actor("speaker", 20, true)); fresh.Put(Actor("watch", 5, true));
        fresh.Put(Actor("enters", 30, false, false)); fresh.Put(Actor("off", 0, true, false));
        fresh.Put(Actor("near", -19.995f, true)); fresh.Put(Actor("aligned", 0, false));
        Check(rules.SetSpeaker(fresh.Actors["speaker"], Snapshot()).Count == 0 && rules.IsWaitingForTurn, "speaker starts delayed turn without outputs");
        Check(rules.Advance(1.5).Count == 0 && fresh.Queries == 0 && fresh.Requests.Count == 0, "no candidate or position query before delay");
        fresh.Put(Actor("speaker", -20, true)); fresh.Put(Actor("enters", -30, false, true));
        fresh.Put(Actor("late", 30, true)); fresh.Put(Actor("player", 10, true));
        var due = rules.Advance(.5);
        Check(due.Count == 3 && fresh.Queries == 1 && fresh.Requests.Count == 1, "deadline requests one current actor and one fresh candidate list");
        Facing(due, "watch", false, "uses speaker current position rather than old notification position");
        Facing(due, "enters", true, "uses current candidate position and visibility");
        Facing(due, "late", false, "newly registered candidate appears at deadline");
        Check(!Has(due, "npc") && !Has(due, "player") && !Has(due, "speaker"), "initiator player and speaker are excluded");
        Check(!Has(due, "off") && !Has(due, "near") && !Has(due, "aligned"), "offscreen near and already-facing NPCs have no turn");
        Check(rules.RememberedNpcCount == 4, "only actual bystander changes plus initiator are remembered");
        fresh.Apply(due);
        Check(rules.Advance(100).Count == 0 && fresh.Queries == 1, "completed delayed turn is one-shot");
        var allRestore = rules.End();
        Check(allRestore.Count == 4 && !Has(allRestore, "player") && !Has(allRestore, "off"), "restore covers remembered NPCs only");
        Facing(allRestore, "watch", true, "bystander first original restored");
        Check(!rules.IsActive && !rules.IsWaitingForTurn && rules.RememberedNpcCount == 0 && rules.End().Count == 0, "End clears state and is idempotent");
        rules.Begin(null, fresh.Actors["player"]);
        Check(allRestore.Count == 4, "returned restoration is immutable across subsequent runs"); rules.End();

        var replacement = Pair(); replacement.Put(Actor("a", 20, true), false); replacement.Put(Actor("b", -20, true), false);
        replacement.Put(Actor("watch", 0, true)); var replaceRules = Start(replacement, false);
        replaceRules.SetSpeaker(replacement.Actors["a"], Snapshot()); replaceRules.Advance(1);
        replaceRules.SetSpeaker(replacement.Actors["b"], Snapshot());
        Check(replaceRules.Advance(1.5).Count == 0 && replacement.Queries == 0, "changing speaker discards old timer and restarts full delay");
        Facing(replaceRules.Advance(.5), "watch", false, "replacement turn targets only new speaker");
        Check(replacement.Requests.Count == 1 && replacement.Requests[0] == "b", "discarded speaker never queried"); replaceRules.End();
        var same = Start(replacement, false); same.SetSpeaker(replacement.Actors["b"], Snapshot()); same.Advance(1);
        same.SetSpeaker(Actor("b", 999, false), Snapshot(20));
        Check(same.RemainingDelaySeconds == 1, "same actual speaker does not restart even across segment snapshot changes");
        Check(same.Advance(1).Count != 0 && !same.IsWaitingForTurn, "same-speaker delay still finishes at original deadline");
        same.SetSpeaker(replacement.Actors["b"], Snapshot());
        Check(!same.IsWaitingForTurn, "same speaker after completed turn does not schedule another"); same.End();

        var cancelledScene = Pair(); cancelledScene.Put(Actor("speaker", 20, true), false);
        var cancelled = Start(cancelledScene); cancelled.SetSpeaker(cancelledScene.Actors["speaker"], Snapshot()); cancelled.Advance(1);
        cancelled.SetSpeaker(null, Snapshot());
        Check(!cancelled.IsWaitingForTurn && cancelled.Advance(100).Count == 0 && cancelledScene.Queries == 0, "no street actor cancels pending turn without queries");
        cancelled.SetSpeaker(cancelledScene.Actors["speaker"], Snapshot()); cancelledScene.Actors.Remove("speaker");
        Check(cancelled.Advance(2).Count == 0 && cancelledScene.Requests.Count == 1 && cancelledScene.Queries == 0,
            "speaker lost before deadline yields no candidates or turn"); cancelled.End();
        var noneScene = Pair(); noneScene.Put(Actor("speaker", 20, true), false);
        var noneRules = new NarrativeStreetReactionRules(noneScene);
        Check(noneRules.Begin(noneScene.Actors["npc"], noneScene.Actors["player"]).Count == 2, "turnMode None never suppresses initial mutual facing");
        noneRules.SetSpeaker(noneScene.Actors["speaker"], Snapshot(2, DialogueTurnMode.None));
        Check(!noneRules.IsWaitingForTurn && noneRules.Advance(100).Count == 0 && noneScene.Queries == 0, "None mode skips background reaction");
        noneRules.SetSpeaker(noneScene.Actors["speaker"], Snapshot());
        Check(noneRules.IsWaitingForTurn, "re-enabled mode can schedule a previously suppressed speaker");
        noneRules.SetSpeaker(noneScene.Actors["speaker"], Snapshot(2, DialogueTurnMode.None));
        Check(!noneRules.IsWaitingForTurn, "None also cancels an existing timer"); noneRules.End();

        var instantScene = Pair(); instantScene.Put(Actor("a", 10, true), false); instantScene.Put(Actor("b", -10, true), false);
        instantScene.Put(Actor("watch", 0, false)); var instant = Start(instantScene);
        var first = instant.SetSpeaker(instantScene.Actors["a"], Snapshot(0));
        Facing(first, "watch", true, "zero delay returns turn during notification call");
        Check(!instant.IsWaitingForTurn && instantScene.Queries == 1, "zero delay needs no subsequent tick"); instantScene.Apply(first);
        var second = instant.SetSpeaker(instantScene.Actors["b"], Snapshot(0));
        Facing(second, "watch", false, "continuation changes same NPC for second time"); instantScene.Apply(second);
        Check(instant.RememberedNpcCount == 2, "continuation retains first originals without duplicate records or restoring");
        var finalRestore = instant.End();
        Facing(finalRestore, "watch", false, "twice-turned NPC restores first original rather than second starting facing");
        Facing(finalRestore, "npc", false, "initiator restores after entire chained conversation");
        Check(finalRestore.Count == 2 && !Has(finalRestore, "player"), "chained restoration excludes player");

        var playerScene = Pair(); playerScene.Put(Actor("watch", -10, false)); var playerRules = Start(playerScene);
        Facing(playerRules.SetSpeaker(playerScene.Actors["player"], Snapshot(0)), "watch", true, "player speaker turns other visible NPCs toward player");
        Check(!Has(playerRules.End(), "player"), "player speaker still never enters restoration");
        var instances = Pair(); instances.Put(Actor("npc#2", -10, false)); var instanceRules = Start(instances);
        Facing(instanceRules.SetSpeaker(instances.Actors["player"], Snapshot(0)), "npc#2", true, "full instance suffix is preserved and not confused with initiator"); instanceRules.End();

        var abortScene = Pair(); abortScene.Put(Actor("speaker", 20, true), false);
        var aborted = Start(abortScene); aborted.SetSpeaker(abortScene.Actors["speaker"], Snapshot());
        var abortRestore = aborted.Abort();
        Check(abortRestore.Count == 1 && !aborted.IsActive && !aborted.IsWaitingForTurn, "abort returns restoration and clears pending time");
        Check(aborted.Advance(100).Count == 0 && abortScene.Queries == 0 && aborted.SetSpeaker(abortScene.Actors["speaker"], Snapshot(0)).Count == 0,
            "late notifications and time after abort cannot turn actors");
        var active = Start(abortScene); active.SetSpeaker(abortScene.Actors["speaker"], Snapshot()); active.Advance(1);
        bool nestedRejected = false; try { active.Begin(null, abortScene.Actors["player"]); } catch (InvalidOperationException) { nestedRejected = true; }
        Check(nestedRejected && active.RememberedNpcCount == 1 && active.RemainingDelaySeconds == 1, "accidental Begin cannot discard active originals or timer");
        InvalidTime(active, -1, "negative time rejected"); InvalidTime(active, double.NaN, "NaN time rejected");
        InvalidTime(active, double.PositiveInfinity, "infinite time rejected");
        Check(active.RemainingDelaySeconds == 1 && abortScene.Queries == 0, "invalid time leaves pending state unchanged"); active.Abort();
        Console.WriteLine("PASS street reaction tests: " + details.Count + " checks");
        Console.WriteLine("STREET_REACTION_RESULT {\"passed\":true,\"checks\":" + details.Count + ",\"details\":[" +
            string.Join(",", details.ConvertAll(x => "\"" + x + "\"")) + "]}");
    }
}
