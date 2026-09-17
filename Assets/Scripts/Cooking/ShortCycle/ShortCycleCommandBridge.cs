using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>
    /// One-way Command system bridge: commands can enter the existing named
    /// action router, while ShortCycle never depends on or modifies the command
    /// system implementation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShortCycleCommandBridge : MonoBehaviour
    {
        [SerializeField] private ShortCycleActionRouter actionRouter = null;

        [Command("ShortCycle Open Cover", Group = "ShortCycle")]
        public void OpenCover(CommandContext context)
        {
            Dispatch(context, ShortCycleActionNames.OpenCover);
        }

        [Command("ShortCycle Close Cover", Group = "ShortCycle")]
        public void CloseCover(CommandContext context)
        {
            Dispatch(context, ShortCycleActionNames.CloseCover);
        }

        [Command("ShortCycle Open Catalog", Group = "ShortCycle")]
        public void OpenCatalog(CommandContext context)
        {
            Dispatch(context, ShortCycleActionNames.OpenCatalog);
        }

        [Command("ShortCycle Close Catalog", Group = "ShortCycle")]
        public void CloseCatalog(CommandContext context)
        {
            Dispatch(context, ShortCycleActionNames.CloseCatalog);
        }

        [Command("ShortCycle Select Recipe", Group = "ShortCycle")]
        public void SelectRecipe(CommandContext context)
        {
            var request = new ShortCycleActionRequest
            {
                ActionName = ShortCycleActionNames.SelectRecipe,
                recipe_id = context == null ? null : context.GetArg<string>("recipe_id", null)
            };
            Dispatch(context, request);
        }

        [Command("ShortCycle Open Clue Board", Group = "ShortCycle")]
        public void OpenClueBoard(CommandContext context)
        {
            Dispatch(context, ShortCycleActionNames.OpenClueBoard);
        }

        [Command("ShortCycle Close Clue Board", Group = "ShortCycle")]
        public void CloseClueBoard(CommandContext context)
        {
            Dispatch(context, ShortCycleActionNames.CloseClueBoard);
        }

        [Command("ShortCycle Open Step Detail", Group = "ShortCycle")]
        public void OpenStepDetail(CommandContext context)
        {
            Dispatch(context, ShortCycleActionNames.OpenStepDetail);
        }

        [Command("ShortCycle Close Step Detail", Group = "ShortCycle")]
        public void CloseStepDetail(CommandContext context)
        {
            Dispatch(context, ShortCycleActionNames.CloseStepDetail);
        }

        [Command("ShortCycle Toggle Favorite", Group = "ShortCycle")]
        public void ToggleFavorite(CommandContext context)
        {
            Dispatch(context, ShortCycleActionNames.ToggleFavorite);
        }

        [Command("ShortCycle Start Cooking", Group = "ShortCycle")]
        public void StartCooking(CommandContext context)
        {
            Dispatch(context, ShortCycleActionNames.StartCooking);
        }

        [Command("ShortCycle Return", Group = "ShortCycle")]
        public void ReturnToPhase0(CommandContext context)
        {
            Dispatch(context, ShortCycleActionNames.Return);
        }

        [Command("ShortCycle Advance To Phase2", Group = "ShortCycle")]
        public void AdvanceToPhase2(CommandContext context)
        {
            Dispatch(context, ShortCycleActionNames.AdvanceToPhase2);
        }

        [Command("ShortCycle Escape", Group = "ShortCycle")]
        public void Escape(CommandContext context)
        {
            Dispatch(context, ShortCycleActionNames.Escape);
        }

        [Command("ShortCycle Tray Highlight Enter", Group = "ShortCycle")]
        public void TrayHighlightEnter(CommandContext context) { Dispatch(context, ShortCycleActionNames.TrayHighlightEnter); }

        [Command("ShortCycle Tray Highlight Exit", Group = "ShortCycle")]
        public void TrayHighlightExit(CommandContext context) { Dispatch(context, ShortCycleActionNames.TrayHighlightExit); }

        [Command("ShortCycle Tray Toggle Detail", Group = "ShortCycle")]
        public void TrayToggleDetail(CommandContext context) { Dispatch(context, ShortCycleActionNames.TrayToggleDetail); }

        [Command("ShortCycle Fridge Filter", Group = "ShortCycle")]
        public void FridgeFilter(CommandContext context)
        {
            Dispatch(context, new ShortCycleActionRequest
            {
                ActionName = ShortCycleActionNames.FridgeFilter,
                tag_id = context == null ? null : context.GetArg<string>("tag_id", null)
            });
        }

        [Command("ShortCycle Fridge Tidy", Group = "ShortCycle")]
        public void FridgeTidy(CommandContext context) { Dispatch(context, ShortCycleActionNames.FridgeTidy); }

        [Command("ShortCycle Fridge Scroll", Group = "ShortCycle")]
        public void FridgeScroll(CommandContext context)
        {
            Dispatch(context, new ShortCycleActionRequest
            {
                ActionName = ShortCycleActionNames.FridgeScroll,
                delta = context == null ? 0f : context.GetArg<float>("delta", 0f)
            });
        }

        [Command("ShortCycle Inventory Take", Group = "ShortCycle")]
        public void InventoryTake(CommandContext context)
        {
            Dispatch(context, new ShortCycleActionRequest
            {
                ActionName = ShortCycleActionNames.InventoryTake,
                item_id = context == null ? null : context.GetArg<string>("item_id", null)
            });
        }

        [Command("ShortCycle Inventory Put Back", Group = "ShortCycle")]
        public void InventoryPutBack(CommandContext context)
        {
            Dispatch(context, new ShortCycleActionRequest
            {
                ActionName = ShortCycleActionNames.InventoryPutBack,
                item_id = context == null ? null : context.GetArg<string>("item_id", null)
            });
        }

        [Command("ShortCycle Equipment Open Entry", Group = "ShortCycle")]
        public void EquipmentOpenEntry(CommandContext context) { Dispatch(context, ShortCycleActionNames.EquipmentOpenEntry); }

        private void Dispatch(CommandContext context, string actionName)
        {
            Dispatch(context, new ShortCycleActionRequest { ActionName = actionName });
        }

        private void Dispatch(CommandContext context, ShortCycleActionRequest request)
        {
            var result = actionRouter == null
                ? ShortCycleActionResult.Failure("Short-cycle action router is not connected.")
                : actionRouter.DispatchAction(request);

            if (context != null)
            {
                context.SetResult(result.Succeeded);
                context.SetArg("shortcycle.error", result.Error);
                context.SetArg("shortcycle.error_code", result.ErrorCode.ToString());
            }
        }
    }
}
