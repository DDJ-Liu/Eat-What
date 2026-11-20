using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;

internal static class NarrativeTypewriterTests
{
    static readonly List<string> details = new List<string>();
    static void Check(bool value, string name)
    { if (!value) throw new Exception("Typewriter: " + name); details.Add(name); }
    static void Count(NarrativeTypewriterModel model, int expected, string name)
    { Check(model.VisibleCharacterCount == expected, name + " visible=" + expected); }
    static void Near(double value, double expected, string name)
    { Check(Math.Abs(value - expected) < 1e-9, name); }
    static DialoguePresentationSnapshot Resolve(DialoguePresentationData data, string overrides = "")
    {
        var dialogue = new NarrativeDialogue("dlg_typewriter", NarrativeDialogueLevel.Normal, null,
            new NarrativeProfileNames("", "", "", "", "", "", "", "", ""), overrides, "ln_typewriter", "",
            new NarrativeCommandSource(NarrativeCommandParser.Parse("")));
        var result = DialoguePresentationResolver.Resolve(data, dialogue);
        if (!result.Success) throw new Exception("Typewriter fixture must use an actual valid resolver snapshot.");
        return result.Snapshot;
    }
    static DialoguePresentationSnapshot Snapshot(float speed = 4, float pause = .5f, string punctuation = "，。？！", float multiplier = 2)
    {
        var data = new DialoguePresentationData();
        data.text[0].charsPerSecond = speed; data.text[0].punctuationPause = pause;
        data.text[0].punctuationSet = punctuation; data.text[0].fastForwardMultiplier = multiplier;
        return Resolve(data);
    }
    static void Invalid(Action work, string name)
    { bool rejected = false; try { work(); } catch (ArgumentException) { rejected = true; } Check(rejected, name); }

    public static void Run()
    {
        details.Clear(); var model = new NarrativeTypewriterModel(); var normal = Snapshot();
        Check(!model.Complete() && !model.Advance(0, false), "unstarted model has no completion pulse");
        Check(!model.Begin("甲乙丙丁", normal), "nonempty Begin starts at zero"); Count(model, 0, "Begin");
        Check(!model.Advance(.125, false), "fractional tick not complete"); Count(model, 0, "half character");
        model.Advance(.125, false); Count(model, 1, "quarter second");
        model.Advance(.5, false); Count(model, 3, "three quarter second");
        Check(model.Advance(.25, false) && model.IsComplete, "last character emits completion immediately"); Count(model, 4, "one second");
        Check(!model.Advance(100, false) && !model.Complete(), "completion pulse cannot repeat");
        model.Begin("abcdefgh", Snapshot(10, 0)); model.Advance(.3, false); Count(model, 3, "decimal boundary roundoff");

        model.Begin("甲，乙", normal); model.Advance(.5, false); Count(model, 2, "punctuation revealed");
        Near(model.PauseRemainingSeconds, .5, "pause starts after punctuation");
        model.Advance(.25, false); Count(model, 2, "inside pause"); Near(model.PauseRemainingSeconds, .25, "pause consumes external time");
        model.Advance(.25, false); Count(model, 2, "pause boundary does not grant a free character");
        model.Advance(.249, false); Count(model, 2, "following character still needs its own time");
        Check(model.Advance(.001, false), "following character finishes at its boundary");
        model.Begin("甲，？！乙", normal); model.Advance(1, false); Count(model, 4, "consecutive punctuation consumes character time");
        Near(model.PauseRemainingSeconds, .5, "consecutive punctuation only pauses after the last");
        model.Advance(.5, false); Count(model, 4, "single consecutive pause"); Check(model.Advance(.25, false), "no extra pauses in run");
        model.Begin("甲？！\r\n", normal); Check(model.Advance(.75, false), "terminal punctuation and trailing newline finish immediately");
        Near(model.PauseRemainingSeconds, 0, "terminal punctuation no pause"); Count(model, 3, "newline excluded after punctuation");
        Check(model.VisibleText == "甲？！\r\n", "completed prefix preserves exact trailing newline");
        model.Begin("甲，\n！乙", normal); model.Advance(.75, false); Count(model, 3, "free newline between punctuation");
        Near(model.PauseRemainingSeconds, .5, "newline does not create another punctuation pause");
        model.Begin("甲，乙", Snapshot(4, .5f, "")); Check(model.Advance(.75, false), "empty punctuation set has no pauses");
        model.Begin("甲，乙", Snapshot(4, .5f, "！")); Check(model.Advance(.75, false), "snapshot punctuation membership controls pause");
        model.Begin("甲，乙", Snapshot(4, 0)); Check(model.Advance(.75, false), "zero punctuation pause stays instantaneous");

        model.Begin("甲，乙。丙", normal); model.Advance(.5, false);
        Check(!model.Advance(0, true), "fast on during a pause does not complete by itself"); Near(model.PauseRemainingSeconds, 0, "fast on cancels pending pause immediately");
        model.Advance(.125, true); Count(model, 3, "fast multiplier applied");
        model.Advance(.125, false); Count(model, 3, "switch off uses normal rate immediately");
        model.Advance(.0625, true); Count(model, 4, "fractional progress survives switches"); Near(model.PauseRemainingSeconds, 0, "fast punctuation does not pause");
        Check(model.Advance(.25, false), "off again continues without resurrecting old pause");
        model.Begin("甲，乙", normal); model.Advance(.25, true); Count(model, 2, "fast from beginning");
        Check(model.Advance(.25, false), "off after fast punctuation has no retroactive pause");
        model.Begin("甲乙丙", Snapshot(4, 0, "", 4)); model.Advance(.125, true); Count(model, 2, "different multiplier snapshot");
        model.Begin("甲乙丙", Snapshot(4, 0, "", 1)); model.Advance(.25, true); Count(model, 1, "multiplier one");

        model.Begin("甲，乙", normal); model.Advance(.5, false);
        Check(model.Complete() && model.VisibleText == "甲，乙" && model.IsComplete, "manual complete clears pause and reveals all");
        Near(model.PauseRemainingSeconds, 0, "manual complete clears remaining pause");
        Check(!model.Complete() && !model.Advance(1, true), "manual completion only reports once");
        Check(model.Begin("", normal) && model.IsComplete && model.VisibleText == "", "empty line complete at Begin");
        Check(!model.Complete() && !model.Advance(0, false), "empty completion does not repeat");
        Check(model.Begin(null, normal), "null text treated as empty");
        Check(model.Begin("\r\n\u2028\n", normal) && model.TotalCharacterCount == 0, "newline-only text completes at Begin");
        model.Begin("\r\n甲\n乙\r", normal); Count(model, 0, "leading newline costs no characters");
        Check(model.VisibleText == "\r\n" && model.TotalCharacterCount == 2, "leading newline visible without time");
        model.Advance(.25, false); Check(model.VisibleText == "\r\n甲\n", "embedded newline revealed with preceding character");
        Check(model.Advance(.25, false) && model.VisibleText == "\r\n甲\n乙\r", "CR LF and CRLF preserve text without extra time");

        model.Begin("A😀e\u0301B", normal); Check(model.TotalCharacterCount == 4, "Unicode text elements rather than UTF16 units");
        model.Advance(.5, false); Check(model.VisibleCharacterCount == 2 && model.VisibleText == "A😀" && model.VisibleTextLength == 3, "surrogate pair never split");
        model.Advance(.25, false); Check(model.VisibleCharacterCount == 3 && model.VisibleText == "A😀e\u0301", "combining sequence never split");
        Check(model.Advance(.25, false), "Unicode line completes");

        var once = new NarrativeTypewriterModel(); var split = new NarrativeTypewriterModel();
        once.Begin("甲，。乙！丙\n丁", normal); split.Begin("甲，。乙！丙\n丁", normal);
        once.Advance(1.5, false); for (int i = 0; i < 6; i++) split.Advance(.25, false);
        Check(once.VisibleText == split.VisibleText && once.VisibleCharacterCount == 4, "one large tick equals partitioned ticks around pauses");
        Near(once.PauseRemainingSeconds, split.PauseRemainingSeconds, "partitioned pause remainder");
        Check(once.Advance(1.25, false), "large tick handles multiple punctuation pauses and finishes");
        int reports = 0; for (int i = 0; i < 20; i++) if (split.Advance(.25, false)) reports++;
        Check(reports == 1 && split.VisibleText == once.VisibleText, "many ticks emit exactly one completion");

        model.Begin("甲，乙", normal); model.Advance(.5, false); model.Begin("XYZ", Snapshot(2));
        Count(model, 0, "new line resets revealed count"); Near(model.PauseRemainingSeconds, 0, "new line resets punctuation pause");
        model.Advance(.25, false); Count(model, 0, "new line has no inherited character credit");
        model.Advance(.25, false); Count(model, 1, "changed speed snapshot applied");
        model.Begin("甲乙", Snapshot(0)); Check(!model.Advance(100, false) && model.VisibleCharacterCount == 0, "zero speed does not guess instant mode");
        Check(model.Complete(), "zero speed still manually completes");
        model.Begin("甲乙", Snapshot(4, 0, "", 0)); model.Advance(100, true); Count(model, 0, "zero fast multiplier stops reveal");
        Check(model.Advance(.5, false), "normal speed resumes after zero fast multiplier");

        var data = new DialoguePresentationData(); data.text[0].charsPerSecond = 4;
        var frozen = Resolve(data); data.text[0].charsPerSecond = 8;
        model.Begin("甲乙丙丁", frozen); model.Advance(.25, false); Count(model, 1, "captured actual snapshot immune to authoring mutation");
        model.Begin("甲乙丙丁", Resolve(data)); model.Advance(.25, false); Count(model, 2, "newly resolved parameters change result");
        model.Begin("甲乙丙丁", Resolve(data, "text.charsPerSecond=12")); model.Advance(.25, false); Count(model, 3, "actual resolver override snapshot consumed");
        model.Begin("甲乙", normal); model.Advance(.125, false);
        Invalid(() => model.Advance(-1, false), "negative delta rejected"); Invalid(() => model.Advance(double.NaN, false), "NaN delta rejected");
        Invalid(() => model.Advance(double.PositiveInfinity, false), "infinite delta rejected");
        Invalid(() => model.Begin("new", null), "missing snapshot rejected before resetting line");
        model.Advance(.125, false); Check(model.VisibleText == "甲", "invalid input leaves prior progress intact");
        Console.WriteLine("TYPEWRITER_RESULT={\"passed\":true,\"checks\":" + details.Count + ",\"actualModel\":true,\"actualResolverSnapshots\":true,\"details\":[\"" + string.Join("\",\"", details.ToArray()) + "\"]}");
    }
}
