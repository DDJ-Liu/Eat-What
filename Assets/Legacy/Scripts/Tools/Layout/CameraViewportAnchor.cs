using UnityEngine;

namespace EatWhat.Tools.Layout
{
    /// <summary>
    /// Keeps one explicit world-space root at a camera viewport position.
    /// Put renderers and colliders below the same anchored root so they cannot drift apart.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    [DisallowMultipleComponent]
    public sealed class CameraViewportAnchor : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera = null;
        [SerializeField] private Transform anchoredRoot = null;
        [SerializeField] private Vector2 viewportAnchor = Vector2.zero;
        [SerializeField] private Vector2 inwardViewportInset = Vector2.zero;
        [SerializeField] private bool preserveInitialWorldZ = true;
        [SerializeField] private float worldPlaneZ = 0f;

        private bool planeInitialized;
        private float resolvedWorldZ;

        public Camera TargetCamera { get { return targetCamera; } }
        public Transform AnchoredRoot { get { return anchoredRoot == null ? transform : anchoredRoot; } }
        public Vector2 ViewportAnchor { get { return viewportAnchor; } }
        public Vector2 InwardViewportInset { get { return inwardViewportInset; } }
        public bool PreserveInitialWorldZ { get { return preserveInitialWorldZ; } }
        public float WorldPlaneZ { get { return worldPlaneZ; } }

        private void OnEnable()
        {
            InitializePlane();
            ApplyNow();
        }

        // ShortCycle camera pans are advanced by coroutines before LateUpdate. A high execution
        // order keeps camera-relative controls on the final camera pose of the same rendered frame.
        private void LateUpdate()
        {
            ApplyNow();
        }

        private void OnValidate()
        {
            viewportAnchor.x = Mathf.Clamp01(viewportAnchor.x);
            viewportAnchor.y = Mathf.Clamp01(viewportAnchor.y);
            inwardViewportInset.x = Mathf.Clamp01(inwardViewportInset.x);
            inwardViewportInset.y = Mathf.Clamp01(inwardViewportInset.y);
            if (!preserveInitialWorldZ) resolvedWorldZ = worldPlaneZ;
        }

        /// <summary>Explicit runtime/test configuration; never searches for a camera or scene object.</summary>
        public void Configure(
            Camera camera,
            Transform root,
            Vector2 anchor,
            Vector2 inwardInset,
            bool keepInitialWorldZ,
            float planeZ)
        {
            targetCamera = camera;
            anchoredRoot = root;
            viewportAnchor = anchor;
            inwardViewportInset = inwardInset;
            preserveInitialWorldZ = keepInitialWorldZ;
            worldPlaneZ = planeZ;
            planeInitialized = false;
            InitializePlane();
            ApplyNow();
        }

        /// <summary>Applies the current camera-relative position and preserves scale and rotation.</summary>
        public bool ApplyNow()
        {
            var root = AnchoredRoot;
            if (targetCamera == null || root == null) return false;

            InitializePlane();
            var x = ResolveViewportCoordinate(viewportAnchor.x, inwardViewportInset.x);
            var y = ResolveViewportCoordinate(viewportAnchor.y, inwardViewportInset.y);
            var depth = resolvedWorldZ - targetCamera.transform.position.z;
            var position = targetCamera.ViewportToWorldPoint(new Vector3(x, y, depth));
            position.z = resolvedWorldZ;
            if (Vector3.Distance(root.position, position) > 0.0001f) root.position = position;
            return true;
        }

        /// <summary>
        /// Bounds owners use this to exclude controls that follow the camera from world-content bounds.
        /// The component must live on the anchored root or one of its ancestors.
        /// </summary>
        public static bool IsAnchoredTransform(Transform candidate)
        {
            for (var current = candidate; current != null; current = current.parent)
                if (current.GetComponent<CameraViewportAnchor>() != null) return true;
            return false;
        }

        private void InitializePlane()
        {
            if (planeInitialized) return;
            var root = AnchoredRoot;
            resolvedWorldZ = preserveInitialWorldZ && root != null ? root.position.z : worldPlaneZ;
            planeInitialized = true;
        }

        private static float ResolveViewportCoordinate(float anchor, float inset)
        {
            var clampedAnchor = Mathf.Clamp01(anchor);
            var clampedInset = Mathf.Clamp01(inset);
            return Mathf.Clamp01(clampedAnchor <= 0.5f
                ? clampedAnchor + clampedInset
                : clampedAnchor - clampedInset);
        }
    }
}
