using System;
using EatWhat.Narrative.Core;

// Independent expectations: N0-11 click table; D5 appendix C.7 rows95-97;
// Schema v2.5 rows553-554 (0.3s and 1.5s).
// D5 SHA256: 70265B7208EF31C915EB5E0F8CFD781F2EA111B9FFBCE3EBA6DF323A6B82011A
// Schema SHA256: 61CB6622AE5AE8AEC19C2A8B9B4ABAD5B72C5CBD593851932D5AF0A40F7E11C9
// Values below are literal expected data, not derived from the model's implementation/defaults.
internal static class NarrativeAdvanceInputTests
{
    private static int checks, clickCases;
    private static void Check(bool value, string label)
    {
        checks++;
        if (!value) throw new Exception(label);
    }
    private static void Intent(NarrativeAdvanceIntent actual, NarrativeAdvanceIntent expected, string label)
    { Check(actual == expected, label + ": expected=" + expected + " actual=" + actual); }

    private static void ClickCase(NarrativeAdvancePhase phase, bool fast,
        NarrativeAdvanceIntent expected, NarrativeAdvanceSettings settings = null)
    {
        clickCases++;
        var model = new NarrativeAdvanceInputModel(settings);
        model.SetFastForward(fast);
        Intent(model.TryClick(phase, 10), expected, "Independent click row " + clickCases);
    }

    private static void Matrix()
    {
        var defaults = new NarrativeAdvanceSettings();
        Check(defaults.ClickWhileTyping == NarrativeTypingClickAction.CompleteLine, "D5 regular click preset");
        Check(defaults.ClickWhileTypingFast == NarrativeTypingClickAction.SkipToNext, "D5 fast click preset");
        Check(defaults.AutoWait == 1.2, "D5 AUTO preset seconds");
        Check(defaults.AdvanceCooldown == 0.3, "Schema manual cooldown seconds");
        Check(defaults.CgAdvanceCooldown == 1.5, "Schema CG cooldown seconds");
        ClickCase(NarrativeAdvancePhase.Typing, false, NarrativeAdvanceIntent.CompleteLine);
        ClickCase(NarrativeAdvancePhase.Typing, true, NarrativeAdvanceIntent.SkipToNext);
        ClickCase(NarrativeAdvancePhase.LineComplete, false, NarrativeAdvanceIntent.SkipToNext);
        ClickCase(NarrativeAdvancePhase.Entering, false, NarrativeAdvanceIntent.SkipEntrance);
        ClickCase(NarrativeAdvancePhase.LineComplete, true, NarrativeAdvanceIntent.SkipToNext);
        ClickCase(NarrativeAdvancePhase.Entering, true, NarrativeAdvanceIntent.SkipEntrance);
        var alternate = new NarrativeAdvanceSettings(NarrativeTypingClickAction.SkipToNext,
            NarrativeTypingClickAction.CompleteLine);
        ClickCase(NarrativeAdvancePhase.Typing, false, NarrativeAdvanceIntent.SkipToNext, alternate);
        ClickCase(NarrativeAdvancePhase.Typing, true, NarrativeAdvanceIntent.CompleteLine, alternate);
        var inactive = new NarrativeAdvanceInputModel();
        Intent(inactive.TryClick(NarrativeAdvancePhase.Inactive, 10), NarrativeAdvanceIntent.None, "Inactive context ignores click");
        Intent(inactive.TryClick((NarrativeAdvancePhase)99, 10), NarrativeAdvanceIntent.None, "Unknown context ignores click");
        Intent(inactive.TryClick(NarrativeAdvancePhase.Typing, 10), NarrativeAdvanceIntent.CompleteLine, "Ignored context starts no cooldown");
    }

    private static void AutoAndFast()
    {
        for (int mask = 0; mask < 4; mask++)
        {
            var model = new NarrativeAdvanceInputModel();
            model.SetAuto((mask & 1) != 0); model.SetFastForward((mask & 2) != 0);
            Check(model.AutoEnabled == ((mask & 1) != 0) && model.FastForwardEnabled == ((mask & 2) != 0),
                "All four independent toggle combinations");
            Intent(model.EvaluateAuto(NarrativeAdvancePhase.Typing, 100), NarrativeAdvanceIntent.None, "No AUTO before full text");
            model.NotifyLineCompleted(100);
            Intent(model.EvaluateAuto(NarrativeAdvancePhase.LineComplete, 101.199999), NarrativeAdvanceIntent.None, "AUTO waits from completion");
            Intent(model.EvaluateAuto(NarrativeAdvancePhase.LineComplete, 101.2), (mask & 1) != 0
                ? NarrativeAdvanceIntent.SkipToNext : NarrativeAdvanceIntent.None, "AUTO at exact wait; fast alone never advances");
            model.NotifyLineCompleted(101.3); // Duplicate notification cannot rearm a consumed line wait.
            Intent(model.EvaluateAuto(NarrativeAdvancePhase.LineComplete, 200), NarrativeAdvanceIntent.None, "No repeated AUTO / no fast-only advance");
        }
        var toggles = new NarrativeAdvanceInputModel();
        toggles.ToggleAuto(); toggles.ToggleFastForward();
        Check(toggles.AutoEnabled && toggles.FastForwardEnabled, "Both can be toggled on");
        toggles.ToggleAuto(); Check(!toggles.AutoEnabled && toggles.FastForwardEnabled, "AUTO toggle preserves fast");
        toggles.ToggleFastForward(); Check(!toggles.AutoEnabled && !toggles.FastForwardEnabled, "Fast toggle preserves AUTO");

        var wait = new NarrativeAdvanceInputModel(new NarrativeAdvanceSettings(autoWait: 0.5));
        wait.SetAuto(true); wait.NotifyLineCompleted(10); wait.NotifyLineCompleted(10.2);
        Intent(wait.EvaluateAuto(NarrativeAdvancePhase.Entering, 11), NarrativeAdvanceIntent.None, "Entering cannot AUTO");
        Intent(wait.EvaluateAuto(NarrativeAdvancePhase.LineComplete, 10.499999), NarrativeAdvanceIntent.None, "Custom wait just before");
        Intent(wait.EvaluateAuto(NarrativeAdvancePhase.LineComplete, 10.5), NarrativeAdvanceIntent.SkipToNext, "Custom wait exact, duplicate completion does not restart");
        wait.NotifyLineStarted();
        Intent(wait.EvaluateAuto(NarrativeAdvancePhase.LineComplete, 30), NarrativeAdvanceIntent.None, "New line clears previous completion");
        wait.NotifyLineCompleted(30); wait.SetAuto(false);
        Intent(wait.EvaluateAuto(NarrativeAdvancePhase.LineComplete, 40), NarrativeAdvanceIntent.None, "AUTO off holds completion");
        wait.SetAuto(true);
        Intent(wait.EvaluateAuto(NarrativeAdvancePhase.LineComplete, 40), NarrativeAdvanceIntent.SkipToNext, "AUTO measures actual completion, not enabling time");

        var manual = new NarrativeAdvanceInputModel(new NarrativeAdvanceSettings(autoWait: 0.5));
        manual.SetAuto(true); manual.NotifyLineCompleted(10);
        Intent(manual.TryClick(NarrativeAdvancePhase.LineComplete, 10.1), NarrativeAdvanceIntent.SkipToNext, "Manual next consumes old AUTO wait");
        manual.NotifyLineCompleted(10.2);
        Intent(manual.EvaluateAuto(NarrativeAdvancePhase.LineComplete, 11), NarrativeAdvanceIntent.None, "No duplicate AUTO after manual next");
        var resetWait = new NarrativeAdvanceInputModel(); resetWait.SetAuto(true);
        resetWait.NotifyLineCompleted(0); resetWait.NotifyLineStarted();
        Intent(resetWait.EvaluateAuto(NarrativeAdvancePhase.LineComplete, 100), NarrativeAdvanceIntent.None, "New line cancels an unconsumed old wait");
        var fast = new NarrativeAdvanceInputModel(); fast.SetFastForward(true); fast.NotifyLineCompleted(0);
        Intent(fast.EvaluateAuto(NarrativeAdvancePhase.LineComplete, 1000), NarrativeAdvanceIntent.None, "Fast alone still waits for a click");
        Intent(fast.TryClick(NarrativeAdvancePhase.LineComplete, 1000), NarrativeAdvanceIntent.SkipToNext, "Fast-only complete line uses click");
    }

    private static void Cooldowns()
    {
        var manual = new NarrativeAdvanceInputModel();
        Intent(manual.TryClick(NarrativeAdvancePhase.Typing, 10), NarrativeAdvanceIntent.CompleteLine, "First click accepted");
        Intent(manual.TryClick(NarrativeAdvancePhase.LineComplete, 10.299999), NarrativeAdvanceIntent.None, "Manual just before boundary");
        Intent(manual.TryClick(NarrativeAdvancePhase.LineComplete, 10.3), NarrativeAdvanceIntent.SkipToNext, "Manual exact boundary accepted");
        manual.NotifyLineStarted(); manual.SetFastForward(true);
        Intent(manual.TryClick(NarrativeAdvancePhase.Typing, 10.599999), NarrativeAdvanceIntent.None, "Fast click respects ongoing cooldown across lines");
        Intent(manual.TryClick(NarrativeAdvancePhase.Typing, 10.3 + 0.3), NarrativeAdvanceIntent.SkipToNext, "Fast exact cooldown boundary");
        var entrance = new NarrativeAdvanceInputModel();
        Intent(entrance.TryClick(NarrativeAdvancePhase.Entering, 20), NarrativeAdvanceIntent.SkipEntrance, "Entrance click accepted");
        Intent(entrance.TryClick(NarrativeAdvancePhase.Typing, 20.299999), NarrativeAdvanceIntent.None, "Entrance skip starts manual cooldown");
        Intent(entrance.TryClick(NarrativeAdvancePhase.Typing, 20.3), NarrativeAdvanceIntent.CompleteLine, "Entrance cooldown exact boundary");
        var cg = new NarrativeAdvanceInputModel(); cg.NotifyCgChanged(30);
        Intent(cg.TryClick(NarrativeAdvancePhase.LineComplete, 31.499999), NarrativeAdvanceIntent.None, "CG just before boundary");
        Intent(cg.TryClick(NarrativeAdvancePhase.LineComplete, 31.5), NarrativeAdvanceIntent.SkipToNext, "CG exact boundary");
        Intent(cg.TryClick(NarrativeAdvancePhase.LineComplete, 31.799999), NarrativeAdvanceIntent.None, "Accepted CG-boundary click starts manual cooldown");
        var overlap = new NarrativeAdvanceInputModel(new NarrativeAdvanceSettings(advanceCooldown: 0.5));
        Intent(overlap.TryClick(NarrativeAdvancePhase.Typing, 10), NarrativeAdvanceIntent.CompleteLine, "Overlap first manual");
        overlap.NotifyCgChanged(10.1);
        Intent(overlap.TryClick(NarrativeAdvancePhase.LineComplete, 10.5), NarrativeAdvanceIntent.None, "Manual expiry does not clear CG");
        Intent(overlap.TryClick(NarrativeAdvancePhase.LineComplete, 11.6), NarrativeAdvanceIntent.SkipToNext, "Independent CG expiry");
        overlap.NotifyCgChanged(11.7);
        Intent(overlap.TryClick(NarrativeAdvancePhase.LineComplete, 12.1), NarrativeAdvanceIntent.None, "New CG refresh does not use manual deadline");
        Intent(overlap.TryClick(NarrativeAdvancePhase.LineComplete, 13.2), NarrativeAdvanceIntent.SkipToNext, "New CG refreshed deadline exact");

        var exempt = new NarrativeAdvanceInputModel(new NarrativeAdvanceSettings(autoWait: 0.5, advanceCooldown: 10, cgAdvanceCooldown: 20));
        exempt.SetAuto(true); exempt.SetFastForward(true);
        Intent(exempt.TryClick(NarrativeAdvancePhase.Typing, 0), NarrativeAdvanceIntent.SkipToNext, "Fast manual click starts cooldown");
        exempt.NotifyCgChanged(0); exempt.NotifyLineStarted(); exempt.NotifyLineCompleted(1);
        Intent(exempt.EvaluateAuto(NarrativeAdvancePhase.LineComplete, 1.499999), NarrativeAdvanceIntent.None, "Exempt AUTO still waits");
        Intent(exempt.EvaluateAuto(NarrativeAdvancePhase.LineComplete, 1.5), NarrativeAdvanceIntent.SkipToNext, "AUTO bypasses both active cooldowns while fast on");
        Intent(exempt.TryClick(NarrativeAdvancePhase.LineComplete, 1.5), NarrativeAdvanceIntent.None, "AUTO does not clear manual or CG cooldown");
        exempt.NotifyLineStarted(); exempt.NotifyLineCompleted(2);
        Intent(exempt.EvaluateAuto(NarrativeAdvancePhase.LineComplete, 2.5), NarrativeAdvanceIntent.SkipToNext, "Next completion has its own AUTO wait");
        Intent(exempt.TryClick(NarrativeAdvancePhase.LineComplete, 19.999999), NarrativeAdvanceIntent.None, "AUTO does not extend or clear CG cooldown");
        Intent(exempt.TryClick(NarrativeAdvancePhase.LineComplete, 20), NarrativeAdvanceIntent.SkipToNext, "Custom CG duration exact boundary");
        Check(exempt.AutoEnabled && exempt.FastForwardEnabled, "Line changes/cooldowns preserve both toggles");
        var noManualRestart = new NarrativeAdvanceInputModel(new NarrativeAdvanceSettings(autoWait: 0.5, advanceCooldown: 2));
        noManualRestart.SetAuto(true);
        Intent(noManualRestart.TryClick(NarrativeAdvancePhase.Typing, 0), NarrativeAdvanceIntent.CompleteLine, "Manual-only initial cooldown");
        noManualRestart.NotifyLineCompleted(0);
        Intent(noManualRestart.EvaluateAuto(NarrativeAdvancePhase.LineComplete, 0.5), NarrativeAdvanceIntent.SkipToNext, "AUTO ignores manual-only cooldown");
        Intent(noManualRestart.TryClick(NarrativeAdvancePhase.Typing, 1.999999), NarrativeAdvanceIntent.None, "AUTO preserves manual-only cooldown");
        Intent(noManualRestart.TryClick(NarrativeAdvancePhase.Typing, 2), NarrativeAdvanceIntent.CompleteLine, "AUTO did not restart manual deadline");
        var autoOnly = new NarrativeAdvanceInputModel(new NarrativeAdvanceSettings(autoWait: 0.5, advanceCooldown: 2));
        autoOnly.SetAuto(true); autoOnly.NotifyLineCompleted(0);
        Intent(autoOnly.EvaluateAuto(NarrativeAdvancePhase.LineComplete, 0.5), NarrativeAdvanceIntent.SkipToNext, "AUTO-only intent");
        Intent(autoOnly.TryClick(NarrativeAdvancePhase.Typing, 0.5), NarrativeAdvanceIntent.CompleteLine, "AUTO starts no manual cooldown");
        var zero = new NarrativeAdvanceInputModel(new NarrativeAdvanceSettings(autoWait: 0, advanceCooldown: 0, cgAdvanceCooldown: 0));
        zero.NotifyCgChanged(0); zero.SetAuto(true); zero.NotifyLineCompleted(0);
        Intent(zero.EvaluateAuto(NarrativeAdvancePhase.LineComplete, 0), NarrativeAdvanceIntent.SkipToNext, "Zero wait can advance immediately");
        Intent(zero.TryClick(NarrativeAdvancePhase.Entering, 0), NarrativeAdvanceIntent.SkipEntrance, "Zero cooldown accepts same instant");
        Intent(zero.TryClick(NarrativeAdvancePhase.Typing, 0), NarrativeAdvanceIntent.CompleteLine, "Zero cooldown not clamped to a new threshold");
    }

    private static void InvalidSettings()
    {
        Action[] invalid = {
            () => new NarrativeAdvanceSettings(autoWait: -1),
            () => new NarrativeAdvanceSettings(advanceCooldown: double.NaN),
            () => new NarrativeAdvanceSettings(cgAdvanceCooldown: double.PositiveInfinity),
            () => new NarrativeAdvanceSettings(clickWhileTyping: (NarrativeTypingClickAction)99)
        };
        foreach (var action in invalid)
        {
            bool rejected = false;
            try { action(); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected, "Invalid configuration rejected at construction");
        }
    }

    public static void Run()
    {
        Matrix(); AutoAndFast(); Cooldowns(); InvalidSettings();
        Console.WriteLine("PASS narrative advance input tests: " + checks + " checks; clickCases=" + clickCases);
    }
}
