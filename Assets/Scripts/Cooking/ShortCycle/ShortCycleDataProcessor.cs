using System.Collections.Generic;

namespace EatWhat.Cooking.ShortCycle
{
    public sealed class ShortCycleProcessResult
    {
        public bool Succeeded;
        public string Error;
        public IList<ShortCycleProcessStep> ExecutedSteps = new List<ShortCycleProcessStep>();
    }

    /// <summary>
    /// The only P0/P1 boundary allowed to change inventory. The four calls mirror
    /// CK01 v1.2 section 4.3 and deliberately do not emulate missing P2 entities.
    /// </summary>
    public sealed class ShortCycleDataProcessor
    {
        private readonly IInventoryConsumptionGateway inventory;
        private readonly ISaveBoundary saveBoundary;
        private readonly IShortCyclePhase2HandoffSink phase2Handoff;

        public ShortCycleDataProcessor(
            IInventoryConsumptionGateway inventory,
            ISaveBoundary saveBoundary,
            IShortCyclePhase2HandoffSink phase2Handoff)
        {
            this.inventory = inventory;
            this.saveBoundary = saveBoundary;
            this.phase2Handoff = phase2Handoff;
        }

        public ShortCycleProcessResult TryProcess(ShortCycleContext context)
        {
            var result = new ShortCycleProcessResult();
            if (context == null || context.current_phase != ShortCyclePhase.Phase1)
            {
                result.Error = "P1 -> P2 processing requires a Phase 1 context.";
                return result;
            }

            if (inventory == null || saveBoundary == null || phase2Handoff == null || !phase2Handoff.IsAvailable)
            {
                result.Error = "P1 -> P2 processing is not wired to its required boundary interfaces.";
                return result;
            }

            // 1. Apply actual deduction through the inventory gateway, then let
            // it perform its own tight reflow/view update when implemented.
            var commit = inventory.TryCommitPlaceholderStates(context.CopyPlaceholderStates());
            result.ExecutedSteps.Add(ShortCycleProcessStep.InventoryConsumption);
            if (commit == null || !commit.Succeeded)
            {
                result.Error = commit == null ? "Inventory gateway returned no result." : commit.Error;
                return result;
            }

            // 2. Save point two remains an observable no-op boundary in CK01.
            saveBoundary.Reach(ShortCycleSavePoint.Phase1ToPhase2, context);
            result.ExecutedSteps.Add(ShortCycleProcessStep.SaveBoundary);

            // 3. Hand off the complete prep data without inventing a P2 entity.
            string handoffError;
            if (!phase2Handoff.TryReceive(context.CreatePhase2Handoff(), out handoffError))
            {
                result.Error = handoffError ?? "Phase 2 handoff was rejected.";
                return result;
            }
            result.ExecutedSteps.Add(ShortCycleProcessStep.Phase2Handoff);

            // 4. Finalize the context only after handoff; keep prep contents.
            context.CompletePhase2();
            result.ExecutedSteps.Add(ShortCycleProcessStep.ContextFinalization);
            result.Succeeded = true;
            return result;
        }
    }
}
