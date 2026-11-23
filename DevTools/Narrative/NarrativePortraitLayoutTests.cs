using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;

internal static class NarrativePortraitLayoutTests
{
    static readonly List<string> details = new List<string>();
    static void Check(bool value, string name)
    { if (!value) throw new Exception("Portrait layout: " + name); details.Add(name); }
    static void Near(float actual, float expected, string name)
    { Check(Math.Abs(actual - expected) < 0.0001f, name); }
    static NarrativePortraitCastInput Actor(string id, int slot, string station = "",
        float? x = null, float? y = null, float? scale = null, int? order = null,
        bool portrait = true, float? offsetX = null, float? offsetY = null, float? characterScale = null)
    {
        return new NarrativePortraitCastInput(new NarrativeCastEntry(slot, id, station, x, y, scale, order),
            new NarrativeCharacter(id, "", portrait ? "portrait_test" : "", offsetX, offsetY, characterScale,
                "", "", null, null), portrait);
    }
    static DialoguePresentationSnapshot Snapshot(DialoguePresentationData data)
    {
        var dialogue = new NarrativeDialogue("dlg_layout", NarrativeDialogueLevel.Normal, null,
            new NarrativeProfileNames("", "", "", "", "", "", "", "", ""), "", "", "",
            new NarrativeCommandSource(NarrativeCommandParser.Parse("")));
        var resolved = DialoguePresentationResolver.Resolve(data, dialogue);
        if (!resolved.Success) throw new Exception("Portrait test requires a real valid presentation snapshot.");
        return resolved.Snapshot;
    }
    static NarrativePortraitLayoutResult Resolve(NarrativePortraitCastInput[] actors, DialoguePresentationData data = null)
    { return NarrativePortraitLayoutResolver.Resolve(actors, Snapshot(data ?? new DialoguePresentationData())); }
    static int Count(NarrativePortraitLayoutResult result, NarrativePortraitLayoutDiagnosticKind kind)
    { int n = 0; foreach (var item in result.Diagnostics) if (item.Kind == kind) n++; return n; }

    public static void Run()
    {
        details.Clear();
        // Expected positions are the card's current authored default values, not another resolver.
        float[][] xs = { new[] { 0f }, new[] { -747f, 747f }, new[] { -853f, 0f, 853f },
            new[] { -960f, -400f, 400f, 960f } };
        for (int n = 1; n <= 4; n++)
        {
            var actors = new NarrativePortraitCastInput[n];
            for (int i = 0; i < n; i++) actors[i] = Actor("actor" + i, i + 1);
            var result = Resolve(actors);
            Check(result.Portraits.Count == n && result.Diagnostics.Count == 0, n + " visible actors, no diagnostics");
            for (int i = 0; i < n; i++)
            {
                var p = result.Portraits[i];
                Check(p.CharacterId == "actor" + i && p.Slot == i + 1, n + " identity and cast order " + i);
                Near(p.DisplayAnchor.X, xs[n - 1][i], n + " default x " + i);
                Near(p.DisplayAnchor.Y, 0f, n + " default y " + i);
                Near(p.AlignmentPoint.X, .5f, n + " blank character offset x " + i);
                Near(p.AlignmentPoint.Y, 0f, n + " blank character offset y " + i);
                Near(p.Scale, n == 4 && (i == 0 || i == 3) ? .9f : 1f, n + " default scale " + i);
                Check(p.Order == (n == 3 && i == 1 || n == 4 && (i == 1 || i == 2) ? 1 : 0), n + " station layer " + i);
            }
        }

        var priority = Resolve(new[] {
            Actor("a", 1, "s3-left", 123f, 45f), Actor("b", 2, "s3-center") });
        Near(priority.Portraits[0].DisplayAnchor.X, 123, "direct position wins over named station");
        Near(priority.Portraits[0].DisplayAnchor.Y, 45, "direct y retained");
        Near(priority.Portraits[0].Scale, 1, "direct position uses station scale one");
        Check(priority.Portraits[0].Order == 0, "direct position without order uses zero");
        Near(priority.Portraits[1].DisplayAnchor.X, 0, "two-person dialogue can use three-person named centre");
        Check(priority.Portraits[1].Order == 1, "named station layer overrides two-person default");

        var factors = new DialoguePresentationData();
        factors.layout[0].stations[0].entries[0] = new PortraitStation("scaled", -50, 7, 2f, 8);
        var product = Resolve(new[] { Actor("a", 3, scale: 4, characterScale: 3, offsetX: .2f, offsetY: .75f) }, factors).Portraits[0];
        Near(product.Scale, 24, "station x character x segment scale");
        Near(product.AlignmentPoint.X, .7f, "alignment x is half plus character fraction");
        Near(product.AlignmentPoint.Y, .75f, "alignment y is character fraction");
        Near(product.DisplayAnchor.X, -50, "alignment offset never moves display anchor");
        Near(product.DisplayAnchor.Y, 7, "station vertical pixels remain separate");
        var namedScale = Resolve(new[] { Actor("a", 1, "s4-left-outer", scale: 2, characterScale: 3) }).Portraits[0];
        Near(namedScale.Scale, 5.4f, "named cross-count station contributes scale");

        var layers = Resolve(new[] { Actor("a", 4, "s3-center", order: -3),
            Actor("b", 2, "s3-center"), Actor("c", 7, x: 1, order: 1) });
        Check(layers.Portraits[0].Order == -3 && layers.Portraits[1].Order == 1 && layers.Portraits[2].Order == 1,
            "explicit, station and direct-explicit layer priorities");
        Check(layers.Portraits[0].Slot == 4 && layers.Portraits[1].Slot == 2 && layers.Portraits[2].Slot == 7,
            "output retains enumeration order rather than depth order");
        Check(layers.BackToFront[0].CharacterId == "a" && layers.BackToFront[1].CharacterId == "b" &&
            layers.BackToFront[2].CharacterId == "c", "larger order wins; larger original slot wins a tie");

        var filtered = Resolve(new[] { Actor("a", 1), Actor("hidden", 2, "missing", scale: 0, portrait: false), Actor("c", 3) });
        Check(filtered.Portraits.Count == 2 && filtered.Portraits[1].Slot == 3, "middle actor without portrait omitted, original slot retained");
        Near(filtered.Portraits[0].DisplayAnchor.X, -747, "filtered list uses two-person left");
        Near(filtered.Portraits[1].DisplayAnchor.X, 747, "filtered list uses two-person right without a hole");
        Check(filtered.Diagnostics.Count == 1 && Count(filtered, NarrativePortraitLayoutDiagnosticKind.MissingPortrait) == 1 &&
            filtered.Diagnostics[0].CharacterId == "hidden" && filtered.Diagnostics[0].Slot == 2, "one precise missing-portrait diagnostic; no unused-field warnings");
        var none = Resolve(new[] { Actor("a", 1, portrait: false), Actor("b", 2, portrait: false) });
        Check(none.Portraits.Count == 0 && none.BackToFront.Count == 0 && none.Diagnostics.Count == 2, "all portraits absent gives empty layout and one diagnostic each");
        Check(Resolve(new NarrativePortraitCastInput[0]).Portraits.Count == 0, "empty cast is supported");
        Check(Resolve(new[] { new NarrativePortraitCastInput(new NarrativeCastEntry(1, "", "", null, null, null, null), null, false) }).Diagnostics.Count == 0,
            "unassigned optional column is not a missing portrait");
        var available = Actor("a", 1);
        var unavailable = new NarrativePortraitCastInput(available.Cast, available.Character, false);
        Check(Resolve(new[] { unavailable }).Portraits.Count == 0, "availability is supplied, not guessed from a sprite filename");

        var unknown = Resolve(new[] { Actor("a", 1, "not-a-station"), Actor("b", 2) });
        Near(unknown.Portraits[0].DisplayAnchor.X, -747, "unknown name falls back to visible index");
        Check(Count(unknown, NarrativePortraitLayoutDiagnosticKind.UnknownStation) == 1, "unknown name diagnosed once");
        var five = Resolve(new[] { Actor("a", 1), Actor("b", 2), Actor("c", 3), Actor("d", 4), Actor("e", 5) });
        Check(five.Portraits.Count == 5 && Count(five, NarrativePortraitLayoutDiagnosticKind.MissingDefaultStation) == 5, "five people do not truncate and each missing default is diagnosed");
        foreach (var p in five.Portraits)
            Check(p.DisplayAnchor.X == 0 && p.DisplayAnchor.Y == 0 && p.Scale == 1 && p.Order == 0 && p.Side == NarrativePortraitSide.Center,
                "missing default uses bottom centre/unit factor/zero order " + p.Slot);
        var fiveMixed = Resolve(new[] { Actor("a", 1, x: 20), Actor("b", 2, "s3-left"), Actor("c", 3), Actor("d", 4), Actor("e", 5) });
        Check(Count(fiveMixed, NarrativePortraitLayoutDiagnosticKind.MissingDefaultStation) == 3, "five-person direct and valid names do not require defaults");
        Near(fiveMixed.Portraits[1].DisplayAnchor.X, -853, "named station works beyond four people");

        var zero = new DialoguePresentationData(); zero.layout[0].stations[0].entries[0].scale = 0;
        var invalid = Resolve(new[] { Actor("a", 1, scale: 0, characterScale: -2) }, zero);
        Near(invalid.Portraits[0].Scale, 1, "each nonpositive scale factor falls back independently");
        Check(Count(invalid, NarrativePortraitLayoutDiagnosticKind.InvalidScale) == 3, "each used invalid factor diagnosed once");
        var half = Resolve(new[] { Actor("a", 1, "s3-left", x: 42), Actor("b", 2, "s3-right", y: 23) });
        Check(half.Portraits[0].DisplayAnchor.X == 42 && half.Portraits[0].DisplayAnchor.Y == 0 &&
            half.Portraits[1].DisplayAnchor.X == 0 && half.Portraits[1].DisplayAnchor.Y == 23, "either direct coordinate overrides station; other coordinate defaults zero");
        var zeroX = Resolve(new[] { Actor("a", 1, "s3-left", x: 0) });
        Near(zeroX.Portraits[0].DisplayAnchor.X, 0, "configured zero position is present");
        var unclamped = Resolve(new[] { Actor("a", 1, offsetX: 2, offsetY: -3) }).Portraits[0];
        Check(unclamped.AlignmentPoint.X == 2.5f && unclamped.AlignmentPoint.Y == -3, "offsets outside recommendations are not clamped");

        var sides = Resolve(new[] { Actor("a", 1, x: -1), Actor("b", 2, x: -.999f), Actor("c", 3, x: 0),
            Actor("d", 4, x: .999f), Actor("e", 5, x: 1) });
        Check(sides.Portraits[0].Side == NarrativePortraitSide.Left && sides.Portraits[1].Side == NarrativePortraitSide.Center &&
            sides.Portraits[2].Side == NarrativePortraitSide.Center && sides.Portraits[3].Side == NarrativePortraitSide.Center &&
            sides.Portraits[4].Side == NarrativePortraitSide.Right, "strict one-reference-pixel centre tolerance and signed boundaries");

        var duplicate = new DialoguePresentationData();
        duplicate.layout[0].stations = new[] {
            new PortraitStationSet(3, new PortraitStation("same", -300, 0, 1, 3), new PortraitStation("three-b", 0, 0, 1, 0), new PortraitStation("three-c", 300, 0, 1, 0)),
            new PortraitStationSet(1, new PortraitStation("same", -100, 0, 2, 1)),
            new PortraitStationSet(2, new PortraitStation("same", -200, 0, 1, 2), new PortraitStation("two-b", 200, 0, 1, 0)) };
        var dup = Resolve(new[] { Actor("a", 1, "same"), Actor("b", 2, "same") }, duplicate);
        Check(dup.Portraits[0].DisplayAnchor.X == -100 && dup.Portraits[1].DisplayAnchor.X == -100 &&
            dup.Portraits[0].Scale == 2 && dup.Portraits[0].Order == 1, "duplicate station chooses smallest count regardless of group order");
        Check(Count(dup, NarrativePortraitLayoutDiagnosticKind.DuplicateStation) == 1, "duplicate name diagnosed once across three groups and two users");

        var data = new DialoguePresentationData(); var snapshot = Snapshot(data);
        var inputs = new List<NarrativePortraitCastInput> { Actor("a", 1), Actor("b", 2) };
        var first = NarrativePortraitLayoutResolver.Resolve(inputs, snapshot);
        data.layout[0].stations[1].entries[0].position = new PresentationPoint(999, 999);
        data.layout[0].portraitBottomAlign = false;
        var second = NarrativePortraitLayoutResolver.Resolve(inputs, snapshot);
        Check(first.Portraits[0].DisplayAnchor.X == second.Portraits[0].DisplayAnchor.X && second.Portraits[0].DisplayAnchor.X == -747,
            "segment snapshot remains frozen after authoring edits; repeat call deterministic");
        inputs.Clear();
        Check(first.Portraits.Count == 2 && first.Portraits[0].CharacterId == "a", "returned immutable records do not retain mutable input list");
        var alignment = new DialoguePresentationData(); alignment.layout[0].portraitBottomAlign = false;
        var bottomOff = Resolve(new[] { Actor("a", 1, offsetY: .4f) }, alignment).Portraits[0];
        Check(bottomOff.AlignmentPoint.Y == .4f && bottomOff.DisplayAnchor.Y == 0, "portraitBottomAlign is not consumed");
        Check(available.Cast.Station == "" && available.Cast.Scale == null && available.Character.ConfiguredPortraitScale == null,
            "calculation never writes authored cast or character defaults");
        Console.WriteLine("PASS portrait layout tests: " + details.Count + " checks");
        Console.WriteLine("PORTRAIT_LAYOUT_RESULT {\"passed\":true,\"checks\":" + details.Count + ",\"details\":[" +
            string.Join(",", details.ConvertAll(x => "\"" + x + "\"")) + "]}");
    }
}
