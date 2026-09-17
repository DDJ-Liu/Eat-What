using System;
using System.Collections.Generic;

namespace EatWhat.Cooking.ShortCycle
{
    public static class ShortCycleActionNames
    {
        public const string OpenCover = "shortcycle.open_cover";
        public const string CloseCover = "shortcycle.close_cover";
        public const string OpenCatalog = "shortcycle.open_catalog";
        public const string CloseCatalog = "shortcycle.close_catalog";
        public const string SelectRecipe = "shortcycle.select_recipe";
        public const string OpenClueBoard = "shortcycle.open_clue_board";
        public const string CloseClueBoard = "shortcycle.close_clue_board";
        public const string OpenStepDetail = "shortcycle.open_step_detail";
        public const string CloseStepDetail = "shortcycle.close_step_detail";
        public const string ToggleFavorite = "shortcycle.toggle_favorite";
        public const string StartCooking = "shortcycle.start_cooking";
        public const string Return = "shortcycle.return";
        public const string AdvanceToPhase2 = "shortcycle.advance_to_phase2";
        public const string Escape = "shortcycle.escape";
        public const string TrayHighlightEnter = "shortcycle.tray.highlight_enter";
        public const string TrayHighlightExit = "shortcycle.tray.highlight_exit";
        public const string TrayToggleDetail = "shortcycle.tray.toggle_detail";
        public const string FridgeFilter = "shortcycle.fridge.filter";
        public const string FridgeTidy = "shortcycle.fridge.tidy";
        public const string FridgeScroll = "shortcycle.fridge.scroll";
        public const string InventoryTake = "shortcycle.inventory.take";
        public const string InventoryPutBack = "shortcycle.inventory.put_back";
        public const string EquipmentOpenEntry = "shortcycle.equipment.open_entry";

        private static readonly IList<string> all = new List<string>
        {
            OpenCover,
            CloseCover,
            OpenCatalog,
            CloseCatalog,
            SelectRecipe,
            OpenClueBoard,
            CloseClueBoard,
            OpenStepDetail,
            CloseStepDetail,
            ToggleFavorite,
            StartCooking,
            Return,
            AdvanceToPhase2,
            Escape,
            TrayHighlightEnter,
            TrayHighlightExit,
            TrayToggleDetail,
            FridgeFilter,
            FridgeTidy,
            FridgeScroll,
            InventoryTake,
            InventoryPutBack,
            EquipmentOpenEntry
        }.AsReadOnly();

        public static IList<string> All { get { return all; } }
    }

    public sealed class ShortCycleActionRequest
    {
        public string ActionName;
        public string recipe_id;
        public string tag_id;
        public string item_id;
        public float delta;
    }

    public enum ShortCycleActionErrorCode
    {
        None,
        Rejected,
        NotImplemented,
        Locked
    }

    /// <summary>
    /// Pure navigation contract used by the action router and editor-external tests.
    /// open_cover means opening the physical book cover: Cover -> RecipeCatalog.
    /// </summary>
    public static class ShortCycleActionTransitionTable
    {
        public static bool TryGetTarget(
            string actionName,
            ShortCyclePhase0View currentView,
            out ShortCyclePhase0View targetView)
        {
            targetView = currentView;
            if (actionName == ShortCycleActionNames.OpenCover && currentView == ShortCyclePhase0View.Cover)
            {
                targetView = ShortCyclePhase0View.RecipeCatalog;
                return true;
            }
            if (actionName == ShortCycleActionNames.OpenCatalog && currentView == ShortCyclePhase0View.RecipeBrowse)
            {
                targetView = ShortCyclePhase0View.RecipeCatalog;
                return true;
            }
            if (actionName == ShortCycleActionNames.CloseCatalog && currentView == ShortCyclePhase0View.RecipeCatalog)
            {
                targetView = ShortCyclePhase0View.RecipeBrowse;
                return true;
            }
            if (actionName == ShortCycleActionNames.OpenClueBoard && currentView == ShortCyclePhase0View.RecipeBrowse)
            {
                targetView = ShortCyclePhase0View.ClueBoardShell;
                return true;
            }
            if (actionName == ShortCycleActionNames.CloseClueBoard && currentView == ShortCyclePhase0View.ClueBoardShell)
            {
                targetView = ShortCyclePhase0View.RecipeBrowse;
                return true;
            }
            if (actionName == ShortCycleActionNames.OpenStepDetail && currentView == ShortCyclePhase0View.RecipeBrowse)
            {
                targetView = ShortCyclePhase0View.StepDetail;
                return true;
            }
            if (actionName == ShortCycleActionNames.CloseStepDetail && currentView == ShortCyclePhase0View.StepDetail)
            {
                targetView = ShortCyclePhase0View.RecipeBrowse;
                return true;
            }
            if (actionName == ShortCycleActionNames.CloseStepDetail && currentView == ShortCyclePhase0View.RecipeBrowse)
            {
                targetView = ShortCyclePhase0View.RecipeBrowse;
                return true;
            }

            return false;
        }
    }

    public sealed class ShortCycleActionResult
    {
        public bool Succeeded;
        public string Error;
        public ShortCycleActionErrorCode ErrorCode;

        public static ShortCycleActionResult Success()
        {
            return new ShortCycleActionResult { Succeeded = true, ErrorCode = ShortCycleActionErrorCode.None };
        }

        public static ShortCycleActionResult Failure(string error)
        {
            return new ShortCycleActionResult
            {
                Succeeded = false,
                Error = error,
                ErrorCode = ShortCycleActionErrorCode.Rejected
            };
        }

        public static ShortCycleActionResult Locked(string error)
        {
            return new ShortCycleActionResult
            {
                Succeeded = false,
                Error = error,
                ErrorCode = ShortCycleActionErrorCode.Locked
            };
        }

        public static ShortCycleActionResult NotImplemented(string actionName)
        {
            return new ShortCycleActionResult
            {
                Succeeded = false,
                Error = "Action '" + (actionName ?? string.Empty) + "' is registered but not implemented.",
                ErrorCode = ShortCycleActionErrorCode.NotImplemented
            };
        }
    }

    public delegate ShortCycleActionResult ShortCycleActionHandler(ShortCycleActionRequest request);

    /// <summary>Common named-action entry for UI today and keyboard bindings later.</summary>
    public sealed class ShortCycleActionRegistry
    {
        private readonly Dictionary<string, ShortCycleActionHandler> handlers =
            new Dictionary<string, ShortCycleActionHandler>(StringComparer.Ordinal);

        public IList<string> RegisteredActionNames
        {
            get
            {
                var names = new List<string>(handlers.Keys);
                names.Sort(StringComparer.Ordinal);
                return names.AsReadOnly();
            }
        }

        public bool Register(string actionName, ShortCycleActionHandler handler)
        {
            if (string.IsNullOrEmpty(actionName) || handler == null || handlers.ContainsKey(actionName))
            {
                return false;
            }

            handlers.Add(actionName, handler);
            return true;
        }

        public bool Unregister(string actionName)
        {
            return !string.IsNullOrEmpty(actionName) && handlers.Remove(actionName);
        }

        public void Clear()
        {
            handlers.Clear();
        }

        public ShortCycleActionResult Dispatch(ShortCycleActionRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.ActionName))
            {
                return ShortCycleActionResult.Failure("A named action is required.");
            }

            ShortCycleActionHandler handler;
            if (!handlers.TryGetValue(request.ActionName, out handler))
            {
                return ShortCycleActionResult.Failure("No handler is registered for '" + request.ActionName + "'.");
            }

            return handler(request) ?? ShortCycleActionResult.Failure("Action handler returned no result.");
        }
    }

    public sealed class ShortCycleInputGate
    {
        private int lockCount;

        public bool IsLocked
        {
            get { return lockCount > 0; }
        }

        public IDisposable Acquire(string reason)
        {
            lockCount++;
            return new ReleaseToken(this);
        }

        private void Release()
        {
            if (lockCount > 0)
            {
                lockCount--;
            }
        }

        private sealed class ReleaseToken : IDisposable
        {
            private ShortCycleInputGate owner;

            public ReleaseToken(ShortCycleInputGate owner)
            {
                this.owner = owner;
            }

            public void Dispose()
            {
                if (owner == null)
                {
                    return;
                }

                owner.Release();
                owner = null;
            }
        }
    }

    public sealed class ShortCycleActionDispatcher
    {
        private readonly ShortCycleActionRegistry registry;
        private readonly ShortCycleInputGate inputGate;

        public ShortCycleActionDispatcher(ShortCycleActionRegistry registry, ShortCycleInputGate inputGate)
        {
            if (registry == null) throw new ArgumentNullException("registry");
            if (inputGate == null) throw new ArgumentNullException("inputGate");
            this.registry = registry;
            this.inputGate = inputGate;
        }

        public ShortCycleActionResult Dispatch(ShortCycleActionRequest request)
        {
            if (inputGate.IsLocked)
            {
                return ShortCycleActionResult.Locked("Short-cycle input is locked during camera movement.");
            }

            return registry.Dispatch(request);
        }
    }
}
