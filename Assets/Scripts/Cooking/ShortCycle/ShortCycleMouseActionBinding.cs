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
        private MousePressableObject gesturePressable;
        private bool originalDeferClick;
        private MouseDraggableObject gestureDrag;
        private DragContainer gestureContainer;
        private ShortCycleIngredientDragPreview dragPreview;

        public string ActionName { get { return actionName; } }
        public string RecipeId { get { return recipeId; } }

        public void Configure(ShortCycleActionRouter router, string configuredActionName)
        { Configure(router, configuredActionName, null); }

        public void Configure(ShortCycleActionRouter router, string configuredActionName, string configuredRecipeId)
        {
            actionRouter = router;
            actionName = configuredActionName;
            recipeId = configuredRecipeId;
            if (isActiveAndEnabled) ConfigureClickGesture();
        }

        private void OnEnable()
        { ConfigureClickGesture(); MouseManager.OnMouseCancel += HandleRightClick; }

        private void OnDisable()
        { RestoreClickGesture(); MouseManager.OnMouseCancel -= HandleRightClick; }

        private void ConfigureClickGesture()
        {
            RestoreClickGesture();
            if ((actionName != ShortCycleActionNames.InventoryTake || GetComponent<FridgeSlot>() == null) &&
                (actionName != ShortCycleActionNames.InventoryPutBack || GetComponent<TraySlot>() == null)) return;
            gesturePressable = GetComponent<MousePressableObject>();
            if (gesturePressable != null)
            {
                originalDeferClick = gesturePressable.DeferClickUntilDragRelease;
                gesturePressable.DeferClickUntilDragRelease = true;
            }
            gestureDrag = GetComponent<MouseDraggableObject>();
            if (gestureDrag != null) gestureDrag.PreparingDrag += PrepareDrag;
        }

        private void RestoreClickGesture()
        {
            if (gestureDrag != null) gestureDrag.PreparingDrag -= PrepareDrag;
            if (gestureContainer != null)
            {
                gestureContainer.CancelVisualDrag();
                gestureContainer.VisualDragStart = null;
                gestureContainer.VisualDragUpdate = null;
                gestureContainer.VisualDragEnd = null;
            }
            if (dragPreview != null) dragPreview.SourceRebinding();
            gestureContainer = null; gestureDrag = null;
            if (gesturePressable != null) gesturePressable.DeferClickUntilDragRelease = originalDeferClick;
            gesturePressable = null;
        }

        private void PrepareDrag(MouseDraggableObject drag)
        {
            var piece = GetComponent<ShortCycleGridPieceBase>();
            var owner = sessionManager != null ? sessionManager : piece != null ? piece.OwnerSession : null;
            var container = GetComponent<DragContainer>();
            if (piece == null || owner == null || owner.TrayInteraction == null || container == null) return;
            // The Return controller lives on TrayStateMachine, not on this slot.
            // Cancel only this slot's old return so an immediate re-grab cannot race it.
            if (rejectedDropTransition != null) rejectedDropTransition.CancelAll();
            if (dragPreview == null) dragPreview = GetComponent<ShortCycleIngredientDragPreview>();
            if (dragPreview == null) dragPreview = gameObject.AddComponent<ShortCycleIngredientDragPreview>();
            dragPreview.Prepare(owner, piece, drag);
            gestureContainer = container;
            container.VisualDragStart = dragPreview.Begin;
            container.VisualDragUpdate = dragPreview.Move;
            container.VisualDragEnd = dragPreview.End;
        }

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
        {
            if (actionName == ShortCycleActionNames.Escape || actionName == ShortCycleActionNames.Return)
            {
                var gate = GetComponent<ShortCycleStateGatedInteraction>();
                if (gate != null && !gate.InteractionAllowed)
                    return ShortCycleActionResult.Failure("The page navigation button is not focused.");
            }
            return DispatchFromSource(gameObject, 0f);
        }

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
        {
            if (gestureContainer != null) gestureContainer.CancelVisualDrag();
            if (dragPreview != null) dragPreview.Release();
            ReturnDraggedVisual();
        }

        private void ReturnDraggedVisual(GameObject source = null)
        {
            var sourceBinding = source == null ? null : source.GetComponent<ShortCycleMouseActionBinding>();
            var preview = (source == null ? gameObject : source).GetComponent<ShortCycleIngredientDragPreview>();
            if (preview != null && preview.HandledGesture) { preview.Release(); return; }
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
