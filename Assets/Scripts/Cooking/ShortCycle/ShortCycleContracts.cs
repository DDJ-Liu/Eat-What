using System;
using System.Collections.Generic;

namespace EatWhat.Cooking.ShortCycle
{
    public enum ShortCyclePhase
    {
        None,
        Phase0,
        Phase1,
        Phase2
    }

    public enum ShortCyclePhase0View
    {
        Cover,
        RecipeBrowse,
        RecipeCatalog,
        ClueBoardShell,
        StepDetail
    }

    /// <summary>
    /// Observable marker for the explicit tray sub-state classes. Presentation
    /// is owned by the state objects; this enum is only a wiring/query value.
    /// </summary>
    public enum ShortCycleTrayView
    {
        Collapsed,
        Highlight,
        Detail
    }

    public enum ShortCycleSpaceId
    {
        Phase0,
        Phase1,
        Shared
    }

    public enum ShortCycleModalId
    {
        StepDetail,
        Generic,
        HoverCard
    }

    public enum ShortCycleTriggerContext
    {
        None,
        FreeTry,
        CustomerOrder,
        NpcRequest
    }

    public enum ShortCycleSavePoint
    {
        EnterPhase0 = 1,
        Phase1ToPhase2 = 2
    }

    public enum ShortCycleProcessStep
    {
        InventoryConsumption = 1,
        SaveBoundary = 2,
        Phase2Handoff = 3,
        ContextFinalization = 4
    }

    [Serializable]
    public sealed class PrepZoneItem
    {
        public string item_id;
        public int quantity = 1;
        public int drag_order;
        public string state;

        public PrepZoneItem Clone()
        {
            return new PrepZoneItem
            {
                item_id = item_id,
                quantity = quantity,
                drag_order = drag_order,
                state = state
            };
        }
    }

    [Serializable]
    public sealed class PlaceholderState
    {
        // instance_id is the key required by CK01-B v2. item_id is the runtime
        // bridge back to a generated CK01ItemData asset for the later deduction.
        public string instance_id;
        public string item_id;
        public string stack_type;
        public int taken_count;
        public bool is_taken;

        public int GetCommittedQuantity()
        {
            if (taken_count > 0)
            {
                return taken_count;
            }

            return is_taken ? 1 : 0;
        }

        public PlaceholderState Clone()
        {
            return new PlaceholderState
            {
                instance_id = instance_id,
                item_id = item_id,
                stack_type = stack_type,
                taken_count = taken_count,
                is_taken = is_taken
            };
        }
    }

    public sealed class ShortCycleDataHandoff
    {
        public readonly string CurrentRecipeId;
        public readonly IList<PrepZoneItem> PrepZoneContents;

        public ShortCycleDataHandoff(string currentRecipeId, IList<PrepZoneItem> prepZoneContents)
        {
            CurrentRecipeId = currentRecipeId;
            PrepZoneContents = prepZoneContents ?? new List<PrepZoneItem>();
        }
    }

    public sealed class InventoryCommitResult
    {
        public bool Succeeded;
        public string Error;
        public int ConsumedQuantity;

        public static InventoryCommitResult Success(int consumedQuantity)
        {
            return new InventoryCommitResult { Succeeded = true, ConsumedQuantity = consumedQuantity };
        }

        public static InventoryCommitResult Failure(string error)
        {
            return new InventoryCommitResult { Succeeded = false, Error = error ?? "Unknown inventory commit failure." };
        }
    }

    public interface ISaveBoundary
    {
        void Reach(ShortCycleSavePoint savePoint, ShortCycleContext context);
    }

    public interface IInventoryConsumptionGateway
    {
        InventoryCommitResult TryCommitPlaceholderStates(IList<PlaceholderState> placeholderStates);
    }

    public interface IInventoryTightReflow
    {
        void RequestTightReflow();
    }

    public interface IShortCyclePhase2HandoffSink
    {
        bool IsAvailable { get; }
        bool TryReceive(ShortCycleDataHandoff handoff, out string error);
    }

    public interface IShortCycleUpstreamExit
    {
        void ExitToUpstream(ShortCycleTriggerContext triggerContext);
    }

    /// <summary>
    /// Implemented by world-space prefab pieces whose MouseInteract components
    /// are enabled or disabled by the explicit short-cycle FSM.
    /// </summary>
    public interface IShortCycleStateGatedInteraction
    {
        void SetInteractionAllowed(bool allowed);
    }

    public interface IShortCycleInventoryQuery
    {
        bool TryGetGeneratedItem(string itemId, out CK01ItemData item);
        int GetQuantity(string itemId);
    }

    public sealed class ObservableNoOpSaveBoundary : ISaveBoundary
    {
        private readonly List<ShortCycleSavePoint> reachedSavePoints = new List<ShortCycleSavePoint>();

        public event Action<ShortCycleSavePoint, ShortCycleContext> Reached;

        public IList<ShortCycleSavePoint> ReachedSavePoints
        {
            get { return reachedSavePoints.AsReadOnly(); }
        }

        public void Reach(ShortCycleSavePoint savePoint, ShortCycleContext context)
        {
            reachedSavePoints.Add(savePoint);
            var callback = Reached;
            if (callback != null)
            {
                callback(savePoint, context);
            }
        }
    }

    public sealed class UnavailableInventoryConsumptionGateway : IInventoryConsumptionGateway
    {
        private readonly string error;

        public UnavailableInventoryConsumptionGateway(string error)
        {
            this.error = error;
        }

        public InventoryCommitResult TryCommitPlaceholderStates(IList<PlaceholderState> placeholderStates)
        {
            return InventoryCommitResult.Failure(error);
        }
    }

    public sealed class UnavailablePhase2HandoffSink : IShortCyclePhase2HandoffSink
    {
        private readonly string error;

        public UnavailablePhase2HandoffSink(string error)
        {
            this.error = error;
        }

        public bool IsAvailable
        {
            get { return false; }
        }

        public bool TryReceive(ShortCycleDataHandoff handoff, out string failure)
        {
            failure = error;
            return false;
        }
    }
}
