using UnityEngine;
using UnityEngine.Rendering;

namespace EatWhat.LongLoop
{
    public enum YSorterStatus { Inactive, WaitingForStart, MissingTarget, MissingFoot, InvalidInput,
        ExternalChange, Applied, Saturated }

    /// <summary>Writes one explicit target's layer-local order from an independent foot reference.</summary>
    [DisallowMultipleComponent]
    public class YSorter : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private SortingGroup targetSortingGroup;
        [SerializeField] private Transform foot;
        [SerializeField] private int baseOrder;
        [SerializeField, Min(0f)] private float precision_TUNE = 100f;
        [SerializeField] private float depthOffset;
        [SerializeField] private bool bakeOnStart;

        private Renderer capturedRenderer;
        private SortingGroup capturedGroup;
        private int originalOrder;
        private int originalLayer;
        private int lastOrder;
        private bool acquired;
        private bool applied;
        private bool ownershipLost;
        private bool started;
        private bool baked;
        private bool lastBakeOnStart;
        private bool lastSaturated;
        private int lastBaseOrder;
        private float lastFootY;
        private float lastPrecision;
        private float lastOffset;

        public YSorterStatus Status { get; private set; }
        public int AppliedOrder => lastOrder;
        public int BaseOrder { get => baseOrder; set { baseOrder = value; RefreshIfRunning(); } }
        public float Precision { get => precision_TUNE; set { precision_TUNE = value; RefreshIfRunning(); } }
        public float DepthOffset { get => depthOffset; set { depthOffset = value; RefreshIfRunning(); } }
        public bool BakeOnStart
        {
            get => bakeOnStart;
            set { if (bakeOnStart == value) return; bakeOnStart = value; baked = false; RefreshIfRunning(); }
        }

        public void ConfigureTargets(Renderer renderer, SortingGroup group, Transform footReference)
        {
            targetRenderer = renderer;
            targetSortingGroup = group;
            foot = footReference;
            baked = false;
            if (isActiveAndEnabled) { EnsureSelection(); RefreshIfRunning(); }
        }

        protected virtual void OnEnable()
        {
            baked = false;
            EnsureSelection();
            if (started) Refresh();
            else Status = acquired ? YSorterStatus.WaitingForStart : YSorterStatus.MissingTarget;
        }
        protected virtual void Start() { started = true; Refresh(); }
        protected virtual void LateUpdate() { RefreshIfRunning(); }
        protected virtual void OnDisable() { Release(); Status = YSorterStatus.Inactive; }
        protected virtual void OnDestroy() { Release(); }
        private void RefreshIfRunning() { if (isActiveAndEnabled && started) Refresh(); }

        public bool Rebake() { baked = false; return Refresh(); }

        public bool Refresh()
        {
            if (!isActiveAndEnabled) return false;
            if (!started) { Status = YSorterStatus.WaitingForStart; return false; }
            EnsureSelection();
            if (!acquired) { Status = YSorterStatus.MissingTarget; return false; }
            if (ownershipLost || CurrentLayer != originalLayer ||
                CurrentOrder != (applied ? lastOrder : originalOrder))
            {
                ownershipLost = true;
                Status = YSorterStatus.ExternalChange;
                return false;
            }
            if (bakeOnStart != lastBakeOnStart) { baked = false; lastBakeOnStart = bakeOnStart; }
            if (bakeOnStart && baked) return true;
            if (foot == null) { Status = YSorterStatus.MissingFoot; return false; }
            float y = foot.position.y;
            if (applied && !bakeOnStart && y == lastFootY && baseOrder == lastBaseOrder &&
                precision_TUNE == lastPrecision && depthOffset == lastOffset)
            {
                Status = lastSaturated ? YSorterStatus.Saturated : YSorterStatus.Applied;
                return true;
            }
            int order;
            bool saturated;
            if (!YSortMath.TryOrder(baseOrder, y, precision_TUNE, depthOffset, out order, out saturated))
            {
                Status = YSorterStatus.InvalidInput;
                return false;
            }
            if (CurrentOrder != order) WriteOrder(order);
            lastOrder = order;
            lastFootY = y;
            lastBaseOrder = baseOrder;
            lastPrecision = precision_TUNE;
            lastOffset = depthOffset;
            lastSaturated = saturated;
            applied = true;
            baked = bakeOnStart;
            Status = saturated ? YSorterStatus.Saturated : YSorterStatus.Applied;
            return true;
        }

        private int CurrentOrder => capturedGroup != null ? capturedGroup.sortingOrder : capturedRenderer.sortingOrder;
        private int CurrentLayer => capturedGroup != null ? capturedGroup.sortingLayerID : capturedRenderer.sortingLayerID;
        private void WriteOrder(int order)
        {
            if (capturedGroup != null) capturedGroup.sortingOrder = order;
            else capturedRenderer.sortingOrder = order;
        }
        private void EnsureSelection()
        {
            var group = targetSortingGroup != null ? targetSortingGroup : null;
            var renderer = group == null && targetRenderer != null ? targetRenderer : null;
            if (ReferenceEquals(group, capturedGroup) && ReferenceEquals(renderer, capturedRenderer) && acquired)
                return;
            Release();
            baked = false;
            capturedGroup = group;
            capturedRenderer = renderer;
            acquired = group != null || renderer != null;
            if (!acquired) return;
            originalOrder = CurrentOrder;
            originalLayer = CurrentLayer;
            lastOrder = originalOrder;
            lastBakeOnStart = bakeOnStart;
        }
        private void Release()
        {
            bool alive = capturedGroup != null || capturedRenderer != null;
            if (acquired && alive && applied && !ownershipLost &&
                CurrentOrder == lastOrder && CurrentLayer == originalLayer && CurrentOrder != originalOrder)
                WriteOrder(originalOrder);
            capturedGroup = null;
            capturedRenderer = null;
            acquired = false;
            applied = false;
            ownershipLost = false;
        }
    }
}
