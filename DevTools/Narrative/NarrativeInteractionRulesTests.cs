using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;

internal static class NarrativeInteractionRulesTests
{
    static readonly List<string> details = new List<string>();
    static void Check(bool value, string name)
    { if (!value) throw new Exception("Interaction rules: " + name); details.Add(name); }
    static void Invalid(Action work, string name)
    { bool rejected = false; try { work(); } catch (ArgumentException) { rejected = true; } Check(rejected, name); }
    static NarrativeInteractionCandidate Candidate(string id, double distance, double radius = 2, bool available = true)
    { return new NarrativeInteractionCandidate(id, distance, radius, available); }
    static NarrativeInteractionCandidate Select(string previous, params NarrativeInteractionCandidate[] candidates)
    { return NarrativeInteractionRules.SelectNearest(candidates, previous); }

    public static void Run()
    {
        details.Clear();
        var levels = new NarrativeDialogueLevel?[] { null, NarrativeDialogueLevel.Normal, NarrativeDialogueLevel.Story };
        var markers = new[] { "none", "talk", "story", "", null };
        var expected = new[] {
            new[] { NarrativeMarkerKind.None, NarrativeMarkerKind.None, NarrativeMarkerKind.None },
            new[] { NarrativeMarkerKind.Talk, NarrativeMarkerKind.Talk, NarrativeMarkerKind.Talk },
            new[] { NarrativeMarkerKind.Story, NarrativeMarkerKind.Story, NarrativeMarkerKind.Story },
            new[] { NarrativeMarkerKind.None, NarrativeMarkerKind.Talk, NarrativeMarkerKind.Story },
            new[] { NarrativeMarkerKind.None, NarrativeMarkerKind.Talk, NarrativeMarkerKind.Story }
        };
        for (int m = 0; m < markers.Length; m++)
            for (int level = 0; level < levels.Length; level++)
                foreach (bool talked in new[] { false, true })
                {
                    var result = NarrativeInteractionRules.ResolveMarker(markers[m], levels[level], talked);
                    string name = "marker row=" + m + " level=" + level + " talked=" + talked;
                    Check(result.Kind == expected[m][level], name + " kind");
                    Check(result.IsGrey == (expected[m][level] == NarrativeMarkerKind.Talk && level != 0 && talked), name + " grey only for bound talked ellipsis");
                }
        Check(NarrativeInteractionRules.ResolveMarker("none", NarrativeDialogueLevel.Story, true).Kind !=
            NarrativeInteractionRules.ResolveMarker("", NarrativeDialogueLevel.Story, true).Kind, "none is not empty inference");
        Check(!NarrativeInteractionRules.ResolveMarker("story", NarrativeDialogueLevel.Normal, true).IsGrey, "explicit gold marker overrides normal and never greys");
        Check(NarrativeInteractionRules.ResolveMarker("talk", NarrativeDialogueLevel.Story, true).IsGrey, "explicit ellipsis overrides story and can grey");
        Check(!NarrativeInteractionRules.ResolveMarker("talk", null, true).IsGrey, "no bound dialogue has no talked grey state");

        Check(Select(null) == null, "empty candidate set selects none");
        Check(Select(null, Candidate("boundary", 1, 1)) == null, "exact radius boundary excluded");
        var inside = Candidate("inside", 1 - 1e-12, 1);
        Check(Select(null, inside) == inside, "just inside boundary included without epsilon");
        Check(Select(null, Candidate("outside", 1 + 1e-12, 1)) == null, "just outside excluded");
        Check(Select(null, Candidate("zero-radius", 0, 0)) == null, "zero radius excludes even distance zero");
        var atOrigin = Candidate("origin", 0, 1);
        Check(Select(null, atOrigin) == atOrigin, "zero distance inside positive radius");
        var far = Candidate("far", 1.5); var near = Candidate("near", .5); var middle = Candidate("middle", 1);
        Check(Select(null, far, middle, near) == near, "nearest eligible candidate wins");
        Check(Select("far", far, near) == near, "previous choice cannot beat strictly nearer target");
        Check(Select(null, Candidate("unavailable", 0, 3, false), far) == far, "unavailable near target ignored");
        Check(Select("unavailable", Candidate("unavailable", .1, 2, false)) == null, "all unavailable selects none including previous");
        Check(Select(null, Candidate("out-near", .1, .1), far) == far, "each candidate uses its own radius");
        Check(Select("outside", Candidate("outside", 2), Candidate("other-outside", 5)) == null, "all out of range selects none");
        var a = Candidate("a", 1); var b = Candidate("b", 1); var c = Candidate("c", 1);
        Check(Select("a", a, b, c) == a, "tie preserves first previous choice");
        Check(Select("b", a, b, c) == b, "tie preserves middle previous choice");
        Check(Select("c", a, b, c) == c, "tie preserves last previous choice");
        Check(Select(null, b, a, c) == b, "tie without previous uses first input");
        Check(Select("missing", b, a, c) == b, "missing previous tie falls back to first input");
        Check(Select("a", a, b, near) == near, "later strictly nearer beats a preserved tie");
        Check(Select("far", near, far, Candidate("near-tie", .5)) == near, "previous farther is not a tie keeper");
        var sameIdentityNewObject = Candidate("b", 1);
        Check(Select("b", c, sameIdentityNewObject, a) == sameIdentityNewObject, "previous identity survives rebuilt candidate objects");
        var upper = Candidate("A", 1); var lower = Candidate("a", 1);
        Check(Select("A", lower, upper) == upper, "tie identities compare ordinal exact case");
        Check(Select("B", a, b) == a, "different case previous is not the same target");
        var list = new List<NarrativeInteractionCandidate> { far, near, middle };
        Check(NarrativeInteractionRules.SelectNearest(list, "far") == near && list[0] == far && list[1] == near && list.Count == 3, "selection never sorts or mutates input");
        Check(near.GroundDistance == .5 && near.InteractRadius == 2 && near.IsAvailable, "candidate snapshot unchanged");
        Check(Select("b", a, b) == b && Select("a", a, b) == a && Select(null, a, b) == a, "selection has no retained previous state");
        Check(NarrativeInteractionRules.ResolveMarker("", NarrativeDialogueLevel.Normal, true).IsGrey &&
            !NarrativeInteractionRules.ResolveMarker("", NarrativeDialogueLevel.Normal, false).IsGrey, "marker has no retained talked state");
        var tiny = Candidate("tiny", double.Epsilon, 1);
        Check(Select(null, tiny, atOrigin) == atOrigin, "distance comparison uses actual nearest value");

        Invalid(() => NarrativeInteractionRules.ResolveMarker("Talk", NarrativeDialogueLevel.Normal, false), "invalid marker not silently normalized");
        Invalid(() => NarrativeInteractionRules.ResolveMarker(" ", null, false), "whitespace is not configured empty");
        Invalid(() => NarrativeInteractionRules.ResolveMarker("", NarrativeDialogueLevel.Ambient, false), "ambient is outside bound interaction dialogue levels");
        Invalid(() => NarrativeInteractionRules.ResolveMarker("", (NarrativeDialogueLevel)999, false), "unknown bound dialogue level rejected");
        Invalid(() => NarrativeInteractionRules.SelectNearest(null, null), "null candidate collection rejected");
        Invalid(() => Select(null, new NarrativeInteractionCandidate[] { null }), "null candidate entry rejected");
        Invalid(() => Candidate("", 0), "empty identity rejected");
        Invalid(() => Candidate("bad-distance", -1), "negative ground distance rejected");
        Invalid(() => Candidate("bad-radius", 0, -1), "negative radius rejected");
        Invalid(() => Candidate("bad-distance", double.NaN), "NaN ground distance rejected");
        Invalid(() => Candidate("bad-radius", 0, double.NaN), "NaN radius rejected");
        Invalid(() => Candidate("bad-distance", double.PositiveInfinity), "infinite ground distance rejected");
        Invalid(() => Candidate("bad-radius", 0, double.PositiveInfinity), "infinite radius rejected");
        Console.WriteLine("MARKER_SELECTION_RESULT={\"passed\":true,\"checks\":" + details.Count + ",\"actualFunctions\":true,\"markerMatrixCases\":30,\"details\":[\"" + string.Join("\",\"", details.ToArray()) + "\"]}");
    }
}
