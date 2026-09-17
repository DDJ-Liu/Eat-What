using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>
    /// UnityEvent-compatible bridge for project MouseInteract framework events.
    /// It deliberately contains no P1 drag/drop/hover/scroll business: future
    /// adapters bind the matching framework event to one of these overloads and
    /// still enter the same named-action router used by the buttons and G5 data.
    /// </summary>
    public sealed class ShortCycleMouseActionBinding : MonoBehaviour
    {
        [SerializeField] private ShortCycleActionRouter actionRouter = null;
        [SerializeField] private string actionName = null;
        [SerializeField] private string recipeId = null;

        public string ActionName
        {
            get { return actionName; }
        }

        public string RecipeId
        {
            get { return recipeId; }
        }

        public void Configure(ShortCycleActionRouter router, string configuredActionName)
        {
            Configure(router, configuredActionName, null);
        }

        public void Configure(ShortCycleActionRouter router, string configuredActionName, string configuredRecipeId)
        {
            actionRouter = router;
            actionName = configuredActionName;
            recipeId = configuredRecipeId;
        }

        public ShortCycleActionResult Dispatch()
        {
            return actionRouter == null
                ? ShortCycleActionResult.Failure("Short-cycle action router is not connected.")
                : actionRouter.DispatchAction(new ShortCycleActionRequest
                {
                    ActionName = actionName,
                    recipe_id = recipeId
                });
        }

        // Button_MouseInteract.selectEvent, MouseDraggableObject no-arg events,
        // and HoverTrigger over/out events bind to this entry.
        public void DispatchFromMouseEvent()
        {
            var result = Dispatch();
            Debug.Log("CK01-C-INPUT MouseInteractionLayer -> MouseActionBinding -> ActionRouter" +
                " object=" + gameObject.name + " action=" + actionName +
                " succeeded=" + result.Succeeded, this);
        }

        // DropZone.onDropEvent / onDropRejectedEvent example.
        public void DispatchFromDropEvent(MouseDraggableObject source)
        {
            Dispatch();
        }

        // MouseDraggableObject.onDropHitEvent example.
        public void DispatchFromDropHitEvent(DropZone zone)
        {
            Dispatch();
        }

        // MouseScrollableObject.scrollStepEvent example.
        public void DispatchFromScrollEvent(float direction)
        {
            Dispatch();
        }

        // ScrollArea_Controller.onScrollPositionChanged example.
        public void DispatchFromScrollPositionEvent(Vector2 normalizedPosition)
        {
            Dispatch();
        }
    }

}
