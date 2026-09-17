using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>MouseInteract scroll-step bridge for the Phase1 fridge shelves.</summary>
    [DisallowMultipleComponent]
    public sealed class ShortCycleScrollAdapter : MonoBehaviour
    {
        [SerializeField] private MouseScrollableObject scrollableObject = null;
        [SerializeField] private ShortCycleActionRouter actionRouter = null;
        [SerializeField] private ShortCycleSessionManager sessionManager = null;
        [SerializeField] private bool invertPhysicalScrollDirection = false;

        public int DispatchCount { get; private set; }
        public ShortCycleActionResult LastResult { get; private set; }
        public int ReceivedStepCount { get; private set; }
        public float LastReceivedDelta { get; private set; }
        public float LastMappedDelta { get; private set; }
        public ShortCycleActionResult LastAttemptResult { get; private set; }
        public MouseScrollableObject ScrollableObject { get { return scrollableObject; } }
        public ShortCycleActionRouter ActionRouter { get { return actionRouter; } }
        public ShortCycleSessionManager SessionManager { get { return sessionManager; } }
        public bool IsSubscribed { get; private set; }
        public bool InvertPhysicalScrollDirection { get { return invertPhysicalScrollDirection; } }

        private void OnEnable()
        {
            IsSubscribed = false;
            if (scrollableObject == null || scrollableObject.scrollStepEvent == null) return;
            scrollableObject.scrollStepEvent.RemoveListener(HandleScrollStep);
            scrollableObject.scrollStepEvent.AddListener(HandleScrollStep);
            IsSubscribed = true;
        }

        private void OnDisable()
        {
            if (scrollableObject != null && scrollableObject.scrollStepEvent != null)
                scrollableObject.scrollStepEvent.RemoveListener(HandleScrollStep);
            IsSubscribed = false;
        }

        public ShortCycleActionResult Dispatch(float delta)
        {
            if (delta == 0f) return null;
            if (sessionManager == null || sessionManager.CurrentPhase != ShortCyclePhase.Phase1)
                return ShortCycleActionResult.Locked("Fridge scrolling requires the active Phase1 interaction layer.");
            if (actionRouter == null)
                return ShortCycleActionResult.Failure("Short-cycle action router is not connected.");

            var result = actionRouter.DispatchAction(new ShortCycleActionRequest
            {
                ActionName = ShortCycleActionNames.FridgeScroll,
                delta = delta < 0f ? -1f : 1f
            });
            DispatchCount++;
            LastResult = result;
            return result;
        }

        /// <summary>
        /// Maps only the physical MouseScrollableObject event into the established business
        /// contract. Dispatch(+1) remains next tier and Dispatch(-1) remains previous tier.
        /// </summary>
        public float MapPhysicalStep(float delta)
        {
            if (delta == 0f) return 0f;
            var normalized = delta < 0f ? -1f : 1f;
            return invertPhysicalScrollDirection ? -normalized : normalized;
        }

        private void HandleScrollStep(float delta)
        {
            ReceivedStepCount++;
            LastReceivedDelta = delta;
            LastMappedDelta = MapPhysicalStep(delta);
            LastAttemptResult = Dispatch(LastMappedDelta);
        }
    }
}
