using System;
using System.Collections.Generic;
using EatWhat.Cooking.ShortCycle;

namespace EatWhat.Cooking.ShortCycle.Tests
{
    // Compiled into a separate DLL with ShortCycle.Core as its only project reference.
    public static class ShortCycleCoreAssemblyTests
    {
        private static void Check(bool value, string caseName)
        {
            if (!value) throw new InvalidOperationException("Core assembly: " + caseName);
        }

        private sealed class Names : IPhase2ProductNameSource
        {
            public bool TryGetItemName(string id, out string name)
            { name = id == "potato" ? "Potato" : id == "dish" ? "Dish" : id == "unknown" ? "Unknown" : null; return name != null; }
            public bool TryGetToolName(string id, out string name)
            { name = id == "pan" ? "Pan" : null; return name != null; }
            public bool TryGetActionName(string id, out string name)
            { name = id == "fry" ? "Fry" : null; return name != null; }
        }

        public static int Run()
        {
            Check((int)ShortCyclePhase0View.Cover == 0 && (int)ShortCycleSpaceId.Phase0 == 0,
                "presentation enum values");
            var whitelist = new RecipeWhitelist("recipe", new[] { "record" });
            var area = new AreaConstraint("kitchen", new[] { "fry" }, new[] { "basket" });
            var record = new ProcessingRecordView("record", "dish", new[] { "potato", "potato" }, "fry", "basket", true);
            var tool = new ProcessingToolView("pan", new[] { "fry" });
            Check(AreaConstraintValidator.Check(area, "fry", "basket", new[] { "basket" }) == ProcessingRejectionCode.None,
                "current-area constraint accepts present carrier");
            Check(AreaConstraintValidator.Check(area, "fry", "basket", new string[0]) == ProcessingRejectionCode.CarrierUnavailable,
                "current-area constraint rejects absent carrier");

            var carriers = new ShortCycleCarrierResolver(whitelist, area,
                new[] { new ShortCycleCarrierRecord("record", record.Inputs, "fry", "basket") });
            Check(carriers.Resolve("recipe", "kitchen", "potato", new[] { "basket" }).CarrierId == "basket",
                "carrier resolution");
            Check(carriers.Resolve("recipe", "other", "potato", new[] { "basket" }).Candidates.Count == 0,
                "carrier area isolation");

            var zone = new OperationZoneSession(whitelist, area, new[] { record }, new[] { "basket" });
            Check(zone.TryAdd("recipe", "kitchen", "potato").Accepted && zone.GetSnapshot().CarrierId == "basket",
                "operation locks carrier on first input");
            Check(zone.TryAdd("recipe", "other", "potato").RejectionCode == OperationZoneRejectionCode.WrongArea,
                "operation rejects cross-area input");
            Check(zone.TryAdd("recipe", "kitchen", "potato", 2).RejectionCode == OperationZoneRejectionCode.QuantityExceedsPath,
                "operation rejects excess quantity without mutation");
            Check(zone.GetSnapshot().Inputs.Count == 1 && zone.TryAdd("recipe", "kitchen", "potato").Accepted,
                "operation retains valid quantity path");
            var snapshot = zone.GetSnapshot();
            var matcher = new ProcessingMatcher(whitelist, area, new[] { record }, "unknown");
            var match = matcher.Match("recipe", "kitchen", snapshot.Inputs, tool, new[] { "basket" });
            Check(match.Matched && match.OutputItemId == "dish", "multiset match");
            Check(!matcher.Match("recipe", "kitchen", snapshot.Inputs, tool, new string[0]).Matched,
                "matching respects current-area carrier");
            Phase2ProductDraft draft;
            string diagnostic;
            Check(Phase2ProductProduction.TryCreate(match, snapshot, tool, "unknown", new Names(), out draft, out diagnostic)
                && draft.DisplayName == "Potato ×2 · Pan · Fry" && !draft.IsUnknown,
                "product naming and locked operation");
            Check(zone.Complete().Inputs.Count == 2 && !zone.GetSnapshot().HasInputs,
                "complete clears operation without inventory side effects");

            var countInputs = new List<ProcessingItemStack> {
                new ProcessingItemStack("potato", 2), new ProcessingItemStack("potato", 3) };
            var counted = ProcessingMatcher.Counts(countInputs);
            Check(counted["potato"] == 5 && countInputs.Count == 2 &&
                countInputs[0].Quantity == 2 && countInputs[1].Quantity == 3,
                "counts aggregates duplicates without mutating inputs");
            counted["potato"] = 99;
            Check(ProcessingMatcher.Counts(countInputs)["potato"] == 5,
                "counts returns a fresh dictionary");
            var rejectsNull = false;
            try { ProcessingMatcher.Counts(new ProcessingItemStack[] { null }); }
            catch (ArgumentException) { rejectsNull = true; }
            Check(rejectsNull, "counts rejects null stack");
            var rejectsOverflow = false;
            try { ProcessingMatcher.Counts(new[] {
                new ProcessingItemStack("potato", int.MaxValue), new ProcessingItemStack("potato") }); }
            catch (OverflowException) { rejectsOverflow = true; }
            Check(rejectsOverflow, "counts rejects checked overflow");

            var progress = new StepProgress("recipe", new[] {
                new RecipeStepRuleView("first", new[] { "potato" }, new string[0]),
                new RecipeStepRuleView("tail", new string[0], new[] { "dish" }) }, "unknown");
            var readyCount = 0;
            progress.FinalDishReady += _ => readyCount++;
            progress.OnPrepZoneChanged(new[] { new ProcessingItemStack("potato") });
            Check(progress.GetStates()[0].State == StepCompletionState.Dynamic, "dynamic step activates");
            progress.OnPrepZoneChanged(new ProcessingItemStack[0]);
            Check(progress.GetStates()[0].State == StepCompletionState.None, "dynamic step clears");
            progress.OnPrepZoneChanged(new[] { new ProcessingItemStack("dish") });
            progress.OnPrepZoneChanged(new ProcessingItemStack[0]);
            Check(progress.GetStates()[1].State == StepCompletionState.Permanent && readyCount == 1,
                "permanent tail and one-shot event");
            return 19;
        }
    }
}
