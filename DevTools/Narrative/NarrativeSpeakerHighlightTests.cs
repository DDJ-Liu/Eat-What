using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;

internal static class NarrativeSpeakerHighlightTests
{
    static readonly List<string> details = new List<string>();
    static void Check(bool value, string name)
    { if (!value) throw new Exception("Speaker highlight: " + name); details.Add(name); }
    static void Near(float actual, float expected, string name)
    { Check(Math.Abs(actual - expected) < .00001f, name); }
    static void Pose(NarrativeSpeakerHighlightState s, float brightness, float scale, float rise,
        float shift, bool front, string name)
    {
        Near(s.Brightness, brightness, name + " brightness"); Near(s.Scale, scale, name + " scale");
        Near(s.Rise, rise, name + " rise"); Near(s.ShiftX, shift, name + " shift");
        Check(s.IsFront == front, name + " front");
    }
    static DialoguePresentationData Data(DialogueSpeakerTransition transition = DialogueSpeakerTransition.Fade, float seconds = 2)
    {
        var data = new DialoguePresentationData(); var s = data.speaker[0];
        s.listenerDim = .2f; s.speakerScale = 1.2f; s.speakerRise = 20; s.speakerShiftInward = 100;
        s.transition = transition; s.transitionTime = seconds; return data;
    }
    static DialoguePresentationSnapshot Snapshot(DialoguePresentationData data)
    {
        var dialogue = new NarrativeDialogue("dlg_highlight", NarrativeDialogueLevel.Normal, null,
            new NarrativeProfileNames("", "", "", "", "", "", "", "", ""), "", "", "",
            new NarrativeCommandSource(NarrativeCommandParser.Parse("")));
        var result = DialoguePresentationResolver.Resolve(data, dialogue);
        if (!result.Success) throw new Exception("Highlight tests require real valid parameter resolution.");
        return result.Snapshot;
    }
    static NarrativeSpeakerHighlightInput Input(string id, NarrativePortraitSide side)
    { return new NarrativeSpeakerHighlightInput(id, side); }
    static NarrativeSpeakerHighlightModel Model(DialoguePresentationData data = null)
    { return new NarrativeSpeakerHighlightModel(new[] { Input("left", NarrativePortraitSide.Left),
        Input("middle", NarrativePortraitSide.Center), Input("right", NarrativePortraitSide.Right) }, Snapshot(data ?? Data())); }
    static void InvalidTime(NarrativeSpeakerHighlightModel model, double seconds, string name)
    {
        bool rejected = false; try { model.Advance(seconds); } catch (ArgumentOutOfRangeException) { rejected = true; }
        Check(rejected, name);
    }

    public static void Run()
    {
        details.Clear(); var model = Model();
        Check(model.States.Count == 3 && !model.IsTransitioning && model.CurrentSpeakerId == "", "new segment has no received speaker");
        for (int i = 0; i < 3; i++) Pose(model.States[i], 1, 1, 0, 0, false, "neutral before first line " + i);
        model.Advance(100);
        Pose(model.States[0], 1, 1, 0, 0, false, "time alone never selects a speaker");
        Check(model.SetSpeaker("left") && model.IsTransitioning, "first speaker starts a fade");
        Pose(model.States[0], 1, 1, 0, 0, true, "front changes immediately while numeric values stay current");
        model.Advance(1);
        Pose(model.States[0], 1, 1.1f, 10, 50, true, "left halfway four quantities");
        Pose(model.States[1], .6f, 1, 0, 0, false, "listener halfway from neutral");
        model.Advance(1);
        Pose(model.States[0], 1, 1.2f, 20, 100, true, "left target shifts inward positive");
        Pose(model.States[2], .2f, 1, 0, 0, false, "listener target");
        Check(!model.IsTransitioning, "exact duration completes fade");
        model.SetSpeaker("right"); model.Advance(2);
        Pose(model.States[2], 1, 1.2f, 20, -100, true, "right target shifts inward negative");
        Pose(model.States[0], .2f, 1, 0, 0, false, "previous speaker returns to listener target");
        model.SetSpeaker("middle"); model.Advance(2);
        Pose(model.States[1], 1, 1.2f, 20, 0, true, "centre speaker never shifts sideways");

        var repeat = Model(); repeat.SetSpeaker("left"); repeat.Advance(.5);
        Check(!repeat.SetSpeaker("left") && repeat.IsTransitioning, "same speaker halfway does not restart");
        repeat.Advance(.5);
        Pose(repeat.States[0], 1, 1.1f, 10, 50, true, "same speaker keeps original elapsed time");
        Near(repeat.States[1].Brightness, .6f, "listener also keeps original elapsed time");
        repeat.Advance(1);
        Check(!repeat.SetSpeaker("left") && !repeat.IsTransitioning, "same speaker after completion remains settled");

        var interrupted = Model(); interrupted.SetSpeaker("left"); interrupted.Advance(1);
        var heldState = interrupted.States[0];
        Check(interrupted.SetSpeaker("right"), "different speaker restarts from current values");
        Pose(interrupted.States[0], 1, 1.1f, 10, 50, false, "old speaker loses front without a numeric jump");
        Pose(interrupted.States[2], .6f, 1, 0, 0, true, "new speaker gains front without a numeric jump");
        interrupted.Advance(0);
        Pose(interrupted.States[0], 1, 1.1f, 10, 50, false, "zero delta preserves current pose");
        interrupted.Advance(1);
        Pose(interrupted.States[0], .6f, 1.05f, 5, 25, false, "old speaker halfway to new target from its current values");
        Pose(interrupted.States[2], .8f, 1.1f, 10, -50, true, "new speaker halfway from its current values");
        interrupted.Advance(100);
        Pose(interrupted.States[0], .2f, 1, 0, 0, false, "oversized delta clamps old speaker at target");
        Pose(interrupted.States[2], 1, 1.2f, 20, -100, true, "oversized delta clamps new speaker at target");
        Check(object.ReferenceEquals(heldState, interrupted.States[0]), "stage can retain a live read-only state reference");

        var narration = Model();
        Check(narration.SetSpeaker(null), "first narration changes neutral even though initial id was empty");
        narration.Advance(1);
        for (int i = 0; i < 3; i++) Pose(narration.States[i], .6f, 1, 0, 0, false, "narration dims all halfway " + i);
        Check(!narration.SetSpeaker(""), "consecutive null and empty narration do not restart");
        narration.Advance(1);
        for (int i = 0; i < 3; i++) Pose(narration.States[i], .2f, 1, 0, 0, false, "narration listener target " + i);
        narration.SetSpeaker("not-in-cast"); narration.Advance(2);
        Pose(narration.States[0], .2f, 1, 0, 0, false, "unknown speaker leaves everybody a listener");
        narration.SetSpeaker("LEFT"); narration.Advance(2);
        Check(!narration.States[0].IsFront, "character ids compare ordinally");

        var duplicate = new NarrativeSpeakerHighlightModel(new[] { Input("hero", NarrativePortraitSide.Left),
            Input("hero", NarrativePortraitSide.Right), Input("other", NarrativePortraitSide.Center) }, Snapshot(Data(DialogueSpeakerTransition.Cut)));
        duplicate.SetSpeaker("hero");
        Pose(duplicate.States[0], 1, 1.2f, 20, 100, true, "duplicate character first portrait is a speaker");
        Pose(duplicate.States[1], 1, 1.2f, 20, -100, true, "duplicate character second portrait is also a speaker");
        Pose(duplicate.States[2], .2f, 1, 0, 0, false, "unrelated duplicate-list portrait is listener");
        Check(!duplicate.IsTransitioning, "cut mode never waits");

        var cut = Model(Data(DialogueSpeakerTransition.Cut)); cut.SetSpeaker("right");
        Pose(cut.States[2], 1, 1.2f, 20, -100, true, "cut target is immediate without Advance");
        cut.SetSpeaker(null);
        Pose(cut.States[2], .2f, 1, 0, 0, false, "cut narration immediately clears front and numeric highlight");
        var zero = Model(Data(seconds: 0)); zero.SetSpeaker("left");
        Pose(zero.States[0], 1, 1.2f, 20, 100, true, "zero duration fade is immediate");
        Check(!zero.IsTransitioning, "zero duration has no pending transition");

        // Actual layout output supplies only visible portraits, not a fake copy of its filtering rule.
        var visibleCharacter = new NarrativeCharacter("shown", "", "portrait_test", null, null, null, "", "", null, null);
        var hiddenCharacter = new NarrativeCharacter("hidden", "", "", null, null, null, "", "", null, null);
        var layout = NarrativePortraitLayoutResolver.Resolve(new[] {
            new NarrativePortraitCastInput(new NarrativeCastEntry(1, "shown", "", null, null, null, null), visibleCharacter, true),
            new NarrativePortraitCastInput(new NarrativeCastEntry(2, "hidden", "", null, null, null, null), hiddenCharacter, false) }, Snapshot(Data()));
        var inputs = new List<NarrativeSpeakerHighlightInput>();
        foreach (var portrait in layout.Portraits) inputs.Add(Input(portrait.CharacterId, portrait.Side));
        var filtered = new NarrativeSpeakerHighlightModel(inputs, Snapshot(Data()));
        filtered.SetSpeaker("hidden"); filtered.Advance(2);
        Check(filtered.States.Count == 1 && layout.Diagnostics.Count == 1, "missing portrait already omitted by actual layout");
        Pose(filtered.States[0], .2f, 1, 0, 0, false, "speaker with no portrait dims all displayed portraits");
        inputs.Clear(); Check(filtered.States.Count == 1, "model snapshots input list rather than retaining it");
        var empty = new NarrativeSpeakerHighlightModel(new NarrativeSpeakerHighlightInput[0], Snapshot(Data()));
        empty.SetSpeaker("none"); empty.Advance(10);
        Check(empty.States.Count == 0 && !empty.IsTransitioning, "empty displayed list is safe and has no phantom transition");

        var authored = Data(); var frozen = Model(authored);
        authored.speaker[0].listenerDim = .9f; authored.speaker[0].speakerScale = 2;
        authored.speaker[0].transitionTime = 0; authored.speaker[0].transition = DialogueSpeakerTransition.Cut;
        frozen.SetSpeaker("left"); frozen.Advance(1);
        Pose(frozen.States[0], 1, 1.1f, 10, 50, true, "model retains its segment snapshot and duration");
        Near(frozen.States[1].Brightness, .6f, "later author edits do not alter current listener target");
        var independent = Model();
        Pose(independent.States[0], 1, 1, 0, 0, false, "a new model starts neutral independently");
        InvalidTime(frozen, -1, "negative external time rejected");
        InvalidTime(frozen, double.NaN, "NaN external time rejected");
        InvalidTime(frozen, double.PositiveInfinity, "infinite external time rejected");
        Pose(frozen.States[0], 1, 1.1f, 10, 50, true, "invalid time cannot mutate progress");
        frozen.Advance(1e300);
        Pose(frozen.States[0], 1, 1.2f, 20, 100, true, "large finite time reaches exact target");
        frozen.Advance(1);
        Check(!frozen.IsTransitioning, "completed model stays settled on later ticks");
        Console.WriteLine("PASS speaker highlight tests: " + details.Count + " checks");
        Console.WriteLine("SPEAKER_HIGHLIGHT_RESULT {\"passed\":true,\"checks\":" + details.Count + ",\"details\":[" +
            string.Join(",", details.ConvertAll(x => "\"" + x + "\"")) + "]}");
    }
}
