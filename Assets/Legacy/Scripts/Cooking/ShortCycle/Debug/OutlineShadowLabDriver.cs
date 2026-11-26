// VERIFY-TEMP: VisualEffectsLab outline/shadow merge prototype. Keep until final review closes.
using System;
using System.Text;
using EatWhat.Tools.Rendering;
using TMPro;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle.Debugging
{
    /// <summary>Thin Lab adapter: changes explicit samples only; owns no render algorithm.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Eat What/Debug/Outline Shadow Lab Driver")]
    public sealed class OutlineShadowLabDriver : MonoBehaviour
    {
        [SerializeField] private SpriteOutlineMergeRenderer2D primaryGroup = null;
        [SerializeField] private SpriteOutlineMergeRenderer2D crossingGroup = null;
        [SerializeField] private SpriteOutlineMergeMember2D[] forwardMembers = Array.Empty<SpriteOutlineMergeMember2D>();
        [SerializeField] private SpriteOutlineMergeMember2D[] reverseMembers = Array.Empty<SpriteOutlineMergeMember2D>();
        [SerializeField] private GameObject originalSingleItemComparison = null;
        [SerializeField] private GameObject catRigSeamComparison = null;
        [SerializeField] private SpriteOutlineGroup2D formalLocalFollowGroup = null;
        [SerializeField] private SpriteOutlineGroup2D formalGroupForceOnGroup = null;
        [SerializeField] private GameObject isolatedFormalCatFixtures = null;
        [SerializeField] private Camera forward2ReviewCamera = null;
        [SerializeField] private Transform forward2CameraMarker = null;
        [SerializeField, Min(0.1f)] private float forward2OrthographicSize = 2.5f;
        [SerializeField] private TextMeshPro statusText = null;

        private readonly StringBuilder builder = new StringBuilder(512);
        private bool hasForward2CameraSnapshot;
        private Vector3 savedCameraPosition;
        private Quaternion savedCameraRotation;
        private float savedOrthographicSize;

        public SpriteOutlineMergeRenderer2D PrimaryGroup { get { return primaryGroup; } }
        public SpriteOutlineMergeRenderer2D CrossingGroup { get { return crossingGroup; } }
        public SpriteOutlineMergeMember2D[] ForwardMembers { get { return forwardMembers; } }
        public SpriteOutlineMergeMember2D[] ReverseMembers { get { return reverseMembers; } }
        public GameObject OriginalSingleItemComparison { get { return originalSingleItemComparison; } }
        public GameObject CatRigSeamComparison { get { return catRigSeamComparison; } }
        public SpriteOutlineGroup2D FormalLocalFollowGroup { get { return formalLocalFollowGroup; } }
        public SpriteOutlineGroup2D FormalGroupForceOnGroup { get { return formalGroupForceOnGroup; } }
        public GameObject IsolatedFormalCatFixtures { get { return isolatedFormalCatFixtures; } }
        public Camera Forward2ReviewCamera { get { return forward2ReviewCamera; } }
        public Transform Forward2CameraMarker { get { return forward2CameraMarker; } }
        public float Forward2OrthographicSize { get { return forward2OrthographicSize; } }
        public bool HasForward2CameraSnapshot { get { return hasForward2CameraSnapshot; } }

        public void Configure(SpriteOutlineMergeRenderer2D primary, SpriteOutlineMergeRenderer2D crossing,
            SpriteOutlineMergeMember2D[] forward, SpriteOutlineMergeMember2D[] reverse,
            GameObject originalComparison, GameObject catRigComparison, TextMeshPro status)
        {
            primaryGroup = primary;
            crossingGroup = crossing;
            forwardMembers = forward ?? Array.Empty<SpriteOutlineMergeMember2D>();
            reverseMembers = reverse ?? Array.Empty<SpriteOutlineMergeMember2D>();
            originalSingleItemComparison = originalComparison;
            catRigSeamComparison = catRigComparison;
            statusText = status;
            RefreshStatus();
        }

        public void ConfigureFormal(SpriteOutlineGroup2D localFollowGroup,
            SpriteOutlineGroup2D groupForceOnGroup, GameObject isolatedCatFixtures)
        {
            formalLocalFollowGroup = localFollowGroup;
            formalGroupForceOnGroup = groupForceOnGroup;
            isolatedFormalCatFixtures = isolatedCatFixtures;
            RefreshStatus();
        }

        public bool ConfigureForward2Review(Camera camera, Transform marker, float orthographicSize)
        {
            float sanitizedSize = Mathf.Max(0.1f, orthographicSize);
            if (forward2ReviewCamera == camera && forward2CameraMarker == marker &&
                Mathf.Approximately(forward2OrthographicSize, sanitizedSize))
                return false;

            forward2ReviewCamera = camera;
            forward2CameraMarker = marker;
            forward2OrthographicSize = sanitizedSize;
            return true;
        }

        [ContextMenu("Toggle Primary Group")]
        public void TogglePrimaryGroup()
        {
            if (primaryGroup != null) primaryGroup.enabled = !primaryGroup.enabled;
            RefreshStatus();
        }

        [ContextMenu("Toggle Original Single Comparison")]
        public void ToggleOriginalComparison()
        {
            if (originalSingleItemComparison != null)
                originalSingleItemComparison.SetActive(!originalSingleItemComparison.activeSelf);
            RefreshStatus();
        }

        [ContextMenu("Swap Forward And Reverse Samples")]
        public void SwapOrderingSamples()
        {
            SetMembersActive(forwardMembers, !AnyActive(forwardMembers));
            SetMembersActive(reverseMembers, !AnyActive(reverseMembers));
            if (primaryGroup != null) primaryGroup.Invalidate("LabOrderSwap");
            RefreshStatus();
        }

        [ContextMenu("Invalidate Merge Cache")]
        public void InvalidateCache()
        {
            if (primaryGroup != null) primaryGroup.Invalidate("LabManualInvalidation");
            if (crossingGroup != null) crossingGroup.Invalidate("LabManualInvalidation");
            RefreshStatus();
        }

        [ContextMenu("Toggle Formal Parameter Matrix")]
        public void ToggleFormalParameterMatrix()
        {
            if (formalLocalFollowGroup != null)
                formalLocalFollowGroup.gameObject.SetActive(!formalLocalFollowGroup.gameObject.activeSelf);
            if (formalGroupForceOnGroup != null)
                formalGroupForceOnGroup.gameObject.SetActive(!formalGroupForceOnGroup.gameObject.activeSelf);
            RefreshStatus();
        }

        [ContextMenu("Toggle Isolated Formal Cat Fixtures")]
        public void ToggleIsolatedFormalCatFixtures()
        {
            if (isolatedFormalCatFixtures != null)
                isolatedFormalCatFixtures.SetActive(!isolatedFormalCatFixtures.activeSelf);
            RefreshStatus();
        }

        [ContextMenu("Focus Forward_2 Review Camera (Play Mode Only)")]
        public bool FocusForward2ReviewCamera()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("FORWARD-2-CAMERA focus is Play Mode only; the saved Lab camera was not changed.", this);
                return false;
            }
            if (forward2ReviewCamera == null || forward2CameraMarker == null ||
                !forward2ReviewCamera.orthographic)
            {
                Debug.LogWarning("FORWARD-2-CAMERA missing orthographic camera or marker.", this);
                return false;
            }

            if (!hasForward2CameraSnapshot)
            {
                savedCameraPosition = forward2ReviewCamera.transform.position;
                savedCameraRotation = forward2ReviewCamera.transform.rotation;
                savedOrthographicSize = forward2ReviewCamera.orthographicSize;
                hasForward2CameraSnapshot = true;
            }

            forward2ReviewCamera.transform.position = forward2CameraMarker.position;
            forward2ReviewCamera.transform.rotation = forward2CameraMarker.rotation;
            forward2ReviewCamera.orthographicSize = forward2OrthographicSize;
            RefreshStatus();
            return true;
        }

        [ContextMenu("Restore Camera Before Forward_2 Review")]
        public bool RestoreForward2ReviewCamera()
        {
            if (!hasForward2CameraSnapshot || forward2ReviewCamera == null)
                return false;

            forward2ReviewCamera.transform.position = savedCameraPosition;
            forward2ReviewCamera.transform.rotation = savedCameraRotation;
            forward2ReviewCamera.orthographicSize = savedOrthographicSize;
            hasForward2CameraSnapshot = false;
            RefreshStatus();
            return true;
        }

        [ContextMenu("Refresh Budget Status")]
        public void RefreshStatus()
        {
            if (statusText == null) return;
            builder.Length = 0;
            builder.AppendLine("OUTLINE / SHADOW MERGE LAB · VERIFY-TEMP");
            AppendGroup("Primary", primaryGroup);
            AppendGroup("Crossing", crossingGroup);
            AppendFormalGroup("Formal local + FollowGroup", formalLocalFollowGroup);
            AppendFormalGroup("Formal group + ForceOn", formalGroupForceOnGroup);
            builder.Append("Isolated real Cat fixtures: ");
            builder.AppendLine(isolatedFormalCatFixtures == null ? "MISSING" :
                (isolatedFormalCatFixtures.activeSelf ? "ACTIVE" : "SAFE/OFF"));
            builder.Append("Forward_2 review camera: ");
            builder.AppendLine(forward2ReviewCamera == null || forward2CameraMarker == null
                ? "MISSING"
                : (hasForward2CameraSnapshot ? "FOCUSED / RESTORE AVAILABLE" : "READY"));
            builder.AppendLine("Human GPU review deferred; this display is not an approval record.");
            statusText.text = builder.ToString();
        }

        private void OnDisable()
        {
            RestoreForward2ReviewCamera();
        }

        public string ExportBudgetState()
        {
            return "primary={" + (primaryGroup == null ? "missing" : primaryGroup.ExportBudgetState()) +
                "} crossing={" + (crossingGroup == null ? "missing" : crossingGroup.ExportBudgetState()) + "}";
        }

        private void AppendGroup(string label, SpriteOutlineMergeRenderer2D group)
        {
            builder.Append(label);
            builder.Append(": ");
            builder.AppendLine(group == null ? "MISSING" : group.ExportBudgetState());
        }

        private void AppendFormalGroup(string label, SpriteOutlineGroup2D group)
        {
            builder.Append(label);
            builder.Append(": ");
            builder.AppendLine(group == null || group.MergeBackend == null
                ? "MISSING"
                : group.MergeBackend.ExportBudgetState());
        }

        private static bool AnyActive(SpriteOutlineMergeMember2D[] values)
        {
            for (var index = 0; index < values.Length; index++)
                if (values[index] != null && values[index].gameObject.activeSelf) return true;
            return false;
        }

        private static void SetMembersActive(SpriteOutlineMergeMember2D[] values, bool active)
        {
            for (var index = 0; index < values.Length; index++)
                if (values[index] != null) values[index].gameObject.SetActive(active);
        }
    }
}
