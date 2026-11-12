using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>UnityEvent bridge; all item actions still pass through the named-action input gate.</summary>
    public sealed class ShortCycleMouseActionBinding : MonoBehaviour
    {
        [SerializeField] private ShortCycleActionRouter actionRouter = null;
        [SerializeField] private string actionName = null;
        [SerializeField] private string recipeId = null;
        [SerializeField] private string tagId = null;
        [SerializeField] private ShortCycleSessionManager sessionManager = null;
        [SerializeField] private Collider2D trayDropRegion = null;
        [SerializeField] private TransitionController rejectedDropTransition = null;
        [SerializeField] private string rejectedDropPreset = "Return";

        public string ActionName { get { return actionName; } }
        public string RecipeId { get { return recipeId; } }

        public void Configure(ShortCycleActionRouter router, string configuredActionName)
        { Configure(router, configuredActionName, null); }

        public void Configure(ShortCycleActionRouter router, string configuredActionName, string configuredRecipeId)
        {
            actionRouter = router;
            actionName = configuredActionName;
            recipeId = configuredRecipeId;
        }

        private void OnEnable()
        { MouseManager.OnMouseCancel += HandleRightClick; }

        private void OnDisable()
        { MouseManager.OnMouseCancel -= HandleRightClick; }

        private void HandleRightClick()
        {
            var tray = GetComponent<TraySlot>();
            var fridge = GetComponent<FridgeSlot>();
            if ((tray == null || actionName != ShortCycleActionNames.InventoryPutBack) &&
                (fridge == null || actionName != ShortCycleActionNames.InventoryTake || !HasReservedFridgeInstance(fridge)))
                return;
            var mouse = MouseManager.Instance;
            if (mouse == null || mouse.currentMouseLayer == null || mouse.dragStarted || mouse.dragPerforming)
                return;
            var hoveredDrag = mouse.currentDraggableObject;
            var hoveredPress = mouse.currentPressableHoverTarget;
            if ((hoveredDrag != null && hoveredDrag.transform.IsChildOf(transform)) ||
                (hoveredPress != null && hoveredPress.transform.IsChildOf(transform)))
            {
                if (tray != null) Dispatch();
                else DispatchFromSource(gameObject, 0f, ShortCycleActionNames.InventoryPutBack);
            }
        }

        private bool HasReservedFridgeInstance(FridgeSlot fridge)
        {
            if (sessionManager == null || sessionManager.CurrentPhase != ShortCyclePhase.Phase1 ||
                sessionManager.Context == null || sessionManager.Context.placeholder_states == null ||
                string.IsNullOrEmpty(fridge.ItemId) || string.IsNullOrEmpty(fridge.InstanceId) ||
                fridge.Display == null || !fridge.Display.is_placeholder) return false;
            foreach (var state in sessionManager.Context.placeholder_states)
                if (state != null && state.item_id == fridge.ItemId && state.instance_id == fridge.InstanceId &&
                    state.is_taken && state.taken_count > 0) return true;
            return false;
        }
        public ShortCycleActionResult Dispatch()
        { return DispatchFromSource(gameObject, 0f); }

        private ShortCycleActionResult DispatchFromSource(GameObject source, float delta)
        { return DispatchFromSource(source, delta, actionName); }

        private ShortCycleActionResult DispatchFromSource(GameObject source, float delta, string requestedAction)
        {
            if (actionRouter == null)
                return ShortCycleActionResult.Failure("Short-cycle action router is not connected.");
            var fridge = source == null ? null : source.GetComponent<FridgeSlot>();
            var tray = source == null ? null : source.GetComponent<TraySlot>();
            return actionRouter.DispatchAction(new ShortCycleActionRequest
            {
                ActionName = requestedAction,
                recipe_id = recipeId,
                tag_id = tagId,
                item_id = fridge != null ? fridge.ItemId : tray != null ? tray.ItemId : null,
                instance_id = fridge != null ? fridge.InstanceId : tray != null ? tray.InstanceId : null,
                delta = delta
            });
        }

        // Button.selectEvent, MouseDraggableObject no-arg events, HoverTrigger over/out.
        // A tray slot right-click binds this method with inventory.put_back.
        public void DispatchFromMouseEvent()
        {
            var result = Dispatch();
            Debug.Log("CK01-C-INPUT MouseInteractionLayer -> MouseActionBinding -> ActionRouter" +
                " object=" + gameObject.name + " action=" + actionName +
                " succeeded=" + result.Succeeded, this);
        }

        // The target zone owns only an accepted fridge take; tray data is source-owned.
        public void DispatchFromDropEvent(MouseDraggableObject source)
        {
            if (actionName != ShortCycleActionNames.InventoryTake || source == null ||
                source.GetComponent<FridgeSlot>() == null || source.GetComponent<TraySlot>() != null) return;
            DispatchFromSource(source.gameObject, 0f);
            ReturnDraggedVisual(source.gameObject);
        }
        // The source hit fires after zone.OnDrop, even when that zone rejects the source.
        // The explicit collider decides tray inside/outside independently of zone tags/order.
        public void DispatchFromDropHitEvent(DropZone zone)
        {
            if (IsTraySourceReleasedOutside()) Dispatch();
            ReturnDraggedVisual();
        }

        private bool IsTraySourceReleasedOutside()
        {
            if (actionName != ShortCycleActionNames.InventoryPutBack || GetComponent<TraySlot>() == null ||
                trayDropRegion == null) return false;
            Vector3 pointer;
            return global::Tools.TryGetMousePos(out pointer) && !trayDropRegion.OverlapPoint(pointer);
        }
        // A rejected fridge source bounces; a tray source is handled by its hit event.
        public void DispatchFromRejectedDropEvent(MouseDraggableObject source)
        {
            if (source != null && source.GetComponent<FridgeSlot>() != null &&
                source.GetComponent<TraySlot>() == null)
                ReturnDraggedVisual(source.gameObject);
        }
        public void DispatchFromMissedDropEvent()
        {
            if (IsTraySourceReleasedOutside()) Dispatch();
            ReturnDraggedVisual();
        }
        // MouseDraggableObject.cancelDraggingEvent: return only the source visual.
        public void CancelDragVisual()
        { ReturnDraggedVisual(); }

        private void ReturnDraggedVisual(GameObject source = null)
        {
            var sourceBinding = source == null ? null : source.GetComponent<ShortCycleMouseActionBinding>();
            var transition = sourceBinding != null && sourceBinding.rejectedDropTransition != null
                ? sourceBinding.rejectedDropTransition : rejectedDropTransition;
            var preset = sourceBinding != null && sourceBinding.rejectedDropTransition != null
                ? sourceBinding.rejectedDropPreset : rejectedDropPreset;
            if (transition != null) transition.GoTo(preset);
        }

        public void DispatchFromScrollEvent(float direction)
        { DispatchFromSource(gameObject, direction); }

        public void DispatchFromScrollPositionEvent(Vector2 normalizedPosition)
        { Dispatch(); }
    }
}
