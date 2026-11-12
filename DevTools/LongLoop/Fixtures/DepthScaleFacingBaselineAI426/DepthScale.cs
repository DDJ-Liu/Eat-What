using EatWhat.LongLoop.Configuration;
using UnityEngine;

namespace EatWhat.LongLoop
{
    public enum DepthScaleStatus { Applied, MissingGroundLine, MissingConfig, InvalidInput, ExternalChange }

    /// <summary>Independent visual scale around a baseline-local foot point; never moves the parent.</summary>
    [DisallowMultipleComponent]
    public class DepthScale : MonoBehaviour
    {
        [SerializeField] private Transform groundLine;
        [SerializeField] private DepthScaleConfig localConfig;
        [SerializeField] private Vector3 localFootOffset;
        [SerializeField] private bool useOverrideFactor;
        [SerializeField] private float overrideFactor = 0.03f;
        [SerializeField] private MonoBehaviour depthOverrideProvider;

        private Vector3 baselineScale;
        private Vector3 lastScale;
        private Vector3 lastPosition;
        private Vector3 basePosition;
        private Vector3 ownedOffset;
        private bool captured;
        private bool applied;
        private bool ownershipLost;
        private bool subscribed;
        private bool hasFacing;
        private bool facingRight;
        private DepthScaleConfig subscribedLocal;

        public DepthScaleStatus Status { get; private set; }
        public float AppliedFactor { get; private set; } = 1f;
        public Vector3 FootWorldPosition { get; private set; }
        public bool UseOverrideFactor
        {
            get => useOverrideFactor;
            set { useOverrideFactor = value; Refresh(); }
        }
        public float OverrideFactor
        {
            get => overrideFactor;
            set { overrideFactor = value; Refresh(); }
        }

        public void ConfigureSources(Transform line, DepthScaleConfig config, MonoBehaviour provider = null)
        {
            groundLine = line;
            localConfig = config;
            depthOverrideProvider = provider;
            RebindLocalSubscription();
            Refresh();
        }

        public void ConfigureFootOffset(Vector3 offset)
        {
            ReleaseOwned();
            localFootOffset = offset;
            if (isActiveAndEnabled) { Capture(); Refresh(); }
        }

        protected virtual void OnEnable()
        {
            Capture();
            if (!subscribed) { StreetConfigs.Changed += OnConfigurationChanged; subscribed = true; }
            RebindLocalSubscription();
            Refresh();
        }
        protected virtual void LateUpdate() { Refresh(); }
        protected virtual void OnDisable() { Unsubscribe(); ReleaseOwned(); }
        protected virtual void OnDestroy() { Unsubscribe(); ReleaseOwned(); }

        private void Capture()
        {
            if (captured) return;
            baselineScale = transform.localScale;
            basePosition = transform.localPosition;
            ownedOffset = Vector3.zero;
            captured = true;
            ownershipLost = false;
            applied = false;
        }

        private Vector3 FacingBaseline
        {
            get
            {
                var value = baselineScale;
                if (hasFacing) value.x = Mathf.Abs(value.x) * (facingRight ? 1f : -1f);
                return value;
            }
        }

        public static bool TrySetFacing(Transform visual, bool right)
        {
            if (visual == null) return false;
            var depth = visual.GetComponent<DepthScale>();
            if (depth == null || !depth.isActiveAndEnabled) return false;
            depth.Capture();
            depth.hasFacing = true;
            depth.facingRight = right;
            depth.Refresh();
            return true;
        }

        public bool Refresh()
        {
            if (!isActiveAndEnabled) return false;
            Capture();
            if (ownershipLost || (applied && (!transform.localScale.Equals(lastScale) ||
                (!ownedOffset.Equals(Vector3.zero) && !transform.localPosition.Equals(lastPosition)))))
            {
                ownershipLost = true;
                Status = DepthScaleStatus.ExternalChange;
                return false;
            }
            var pivot = applied && !ownedOffset.Equals(Vector3.zero) ? basePosition : transform.localPosition;
            var localFoot = pivot + transform.localRotation * Vector3.Scale(baselineScale, localFootOffset);
            var foot = transform.parent != null ? transform.parent.TransformPoint(localFoot) : localFoot;
            FootWorldPosition = foot;
            var current = StreetConfigs.Current;
            var line = groundLine != null ? groundLine : current?.GroundLine;
            var config = localConfig != null ? localConfig : current?.DepthScale;
            float coefficient = 0f;
            bool overridden = useOverrideFactor;
            if (overridden) coefficient = overrideFactor;
            else if (depthOverrideProvider != null && depthOverrideProvider.isActiveAndEnabled &&
                depthOverrideProvider is IDepthOverride provider)
                overridden = provider.TryGetDepthScaleFactor(foot, out coefficient);
            if (!overridden && config != null) coefficient = config.DepthScaleFactor;
            float factor = 1f;
            Status = line == null ? DepthScaleStatus.MissingGroundLine :
                !overridden && config == null ? DepthScaleStatus.MissingConfig : DepthScaleStatus.Applied;
            if (Status == DepthScaleStatus.Applied &&
                (!DepthScaleMath.Finite(foot) || !DepthScaleMath.Finite(localFootOffset) ||
                !DepthScaleMath.TryFactor(line.position.y, foot.y, coefficient, out factor)))
                Status = DepthScaleStatus.InvalidInput;
            if (Status != DepthScaleStatus.Applied) factor = 1f;
            Vector3 next;
            if (!DepthScaleMath.TryScale(FacingBaseline, factor, out next))
            {
                Status = DepthScaleStatus.InvalidInput;
                factor = 1f;
                if (!DepthScaleMath.TryScale(FacingBaseline, factor, out next)) return false;
            }
            var offset = transform.localRotation *
                (Vector3.Scale(baselineScale, localFootOffset) - Vector3.Scale(next, localFootOffset));
            var position = pivot + offset;
            if (!DepthScaleMath.Finite(position)) { Status = DepthScaleStatus.InvalidInput; return false; }
            transform.localScale = next;
            transform.localPosition = position;
            lastScale = next;
            lastPosition = position;
            basePosition = pivot;
            ownedOffset = offset;
            applied = true;
            AppliedFactor = factor;
            return Status == DepthScaleStatus.Applied;
        }

        private void ReleaseOwned()
        {
            if (!captured) return;
            if (applied && !ownershipLost && transform.localScale.Equals(lastScale))
            {
                var restored = FacingBaseline;
                transform.localScale = restored;
                if (transform.localPosition.Equals(lastPosition))
                    transform.localPosition = basePosition + transform.localRotation *
                        (Vector3.Scale(baselineScale, localFootOffset) - Vector3.Scale(restored, localFootOffset));
            }
            AppliedFactor = 1f;
            captured = false;
            applied = false;
            ownedOffset = Vector3.zero;
        }

        private void OnConfigurationChanged() { RebindLocalSubscription(); Refresh(); }
        private void RebindLocalSubscription()
        {
            var next = subscribed && localConfig != null &&
                !ReferenceEquals(localConfig, StreetConfigs.Current?.DepthScale) ? localConfig : null;
            if (ReferenceEquals(next, subscribedLocal)) return;
            if (subscribedLocal != null) subscribedLocal.Changed -= OnConfigurationChanged;
            subscribedLocal = next;
            if (subscribedLocal != null) subscribedLocal.Changed += OnConfigurationChanged;
        }
        private void Unsubscribe()
        {
            if (subscribed) StreetConfigs.Changed -= OnConfigurationChanged;
            subscribed = false;
            if (subscribedLocal != null) subscribedLocal.Changed -= OnConfigurationChanged;
            subscribedLocal = null;
        }
    }
}
