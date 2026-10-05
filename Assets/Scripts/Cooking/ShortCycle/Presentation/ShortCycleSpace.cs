using System;
using System.Collections.Generic;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    [Serializable]
    public sealed class ShortCycleViewSetEntry
    {
        public ShortCyclePhase0View view;
        public ShortCycleCameraSlot cameraSlot = ShortCycleCameraSlot.P0Center;
        public ShortCyclePresentationSet[] sets = new ShortCyclePresentationSet[0];
    }

    [DisallowMultipleComponent]
    public sealed class ShortCycleSpace : MonoBehaviour
    {
        [SerializeField] private ShortCycleSpaceId spaceId = ShortCycleSpaceId.Phase0;
        [SerializeField] private ShortCycleCameraSlot cameraSlot = ShortCycleCameraSlot.P0Center;
        [SerializeField] private MouseInteractionLayer spaceLayer = null;
        [SerializeField] private ShortCyclePresentationSet[] structureSets = new ShortCyclePresentationSet[0];
        [SerializeField] private ShortCycleViewSetEntry[] viewTable = new ShortCycleViewSetEntry[0];
        private readonly HashSet<ShortCyclePhase0View> visibleViews = new HashSet<ShortCyclePhase0View>();
        private bool focused;
        private bool arrived;
        internal bool DirectorOwnsVisibility { get; set; }

        public ShortCycleSpaceId SpaceId { get { return spaceId; } }
        public ShortCycleCameraSlot CameraSlot { get { return cameraSlot; } }
        public MouseInteractionLayer SpaceLayer { get { return spaceLayer; } }
        internal bool HasViewCameraRouting { get { return viewTable != null && viewTable.Length > 0; } }

        public bool TryResolveCameraSlot(ShortCyclePhase0View view, out ShortCycleCameraSlot slot)
        {
            foreach (var row in viewTable ?? new ShortCycleViewSetEntry[0])
            {
                if (row == null || row.view != view) continue;
                slot = row.cameraSlot;
                return true;
            }

            slot = cameraSlot;
            return false;
        }

        public static bool TryGetCameraWorldRect(Camera camera, out Rect cameraRect)
        {
            if (camera == null || !camera.orthographic || camera.orthographicSize <= 0f ||
                camera.aspect <= 0f || float.IsNaN(camera.orthographicSize) ||
                float.IsInfinity(camera.orthographicSize) || float.IsNaN(camera.aspect) ||
                float.IsInfinity(camera.aspect))
            {
                cameraRect = default(Rect);
                return false;
            }

            var height = camera.orthographicSize * 2f;
            var width = height * camera.aspect;
            var center = camera.transform.position;
            cameraRect = new Rect(center.x - (width * 0.5f), center.y - (height * 0.5f), width, height);
            return true;
        }

        /// <summary>Computes the world-space union of every Renderer below an explicit root.</summary>
        public static bool ComputeSetBounds(Transform root, out Bounds bounds)
        {
            bounds = default(Bounds);
            if (root == null) return false;
            return ComputeSetBounds(root.GetComponentsInChildren<Renderer>(true), null, out bounds);
        }

        internal static bool ComputeSetBounds(Renderer[] renderers, Bounds[] snapshots, out Bounds bounds)
        {
            bounds = default(Bounds);
            if (renderers == null) return false;
            var found = false;
            for (var index = 0; index < renderers.Length; index++)
            {
                var renderer = renderers[index];
                if (renderer == null) continue;
                var rendererBounds = renderer.bounds;
                if (snapshots != null && index < snapshots.Length) snapshots[index] = rendererBounds;
                if (!found)
                {
                    bounds = rendererBounds;
                    found = true;
                }
                else bounds.Encapsulate(rendererBounds);
            }
            return found;
        }

        public void ArriveFocus()
        {
            focused = true;
            arrived = true;
            ApplyStandaloneVisibility();
        }

        public void LeaveFocus()
        {
            focused = false;
            ApplyStandaloneVisibility();
        }

        public void ShowView(ShortCyclePhase0View view) { visibleViews.Add(view); ApplyStandaloneVisibility(); }
        public void HideView(ShortCyclePhase0View view) { visibleViews.Remove(view); ApplyStandaloneVisibility(); }

        public void HideAllViews()
        {
            visibleViews.Clear();
            ApplyStandaloneVisibility();
        }

        public void RestoreTransientState()
        {
            focused = false;
            arrived = false;
            visibleViews.Clear();
            foreach (var set in structureSets ?? new ShortCyclePresentationSet[0])
                if (set != null) set.RestoreTransientState();
            foreach (var row in viewTable ?? new ShortCycleViewSetEntry[0])
                foreach (var set in row == null || row.sets == null ? new ShortCyclePresentationSet[0] : row.sets)
                    if (set != null) set.RestoreTransientState();
        }

        internal IEnumerable<ShortCyclePresentationSet> ReferencedSets()
        {
            foreach (var set in structureSets ?? new ShortCyclePresentationSet[0])
                if (set != null) yield return set;
            foreach (var row in viewTable ?? new ShortCycleViewSetEntry[0])
                foreach (var set in row == null || row.sets == null ? new ShortCyclePresentationSet[0] : row.sets)
                    if (set != null) yield return set;
        }

        internal bool WantsActive(ShortCyclePresentationSet candidate)
        {
            if (candidate != null && candidate.KeepVisualActiveAcrossSpaces) return true;
            return WantsLogicalActive(candidate);
        }

        private bool WantsLogicalActive(ShortCyclePresentationSet candidate)
        {
            foreach (var set in structureSets ?? new ShortCyclePresentationSet[0])
                if (set == candidate && (focused || (arrived && candidate.ContainerTransform.IsChildOf(transform))))
                    return true;
            foreach (var row in viewTable ?? new ShortCycleViewSetEntry[0])
            {
                if (!focused || row == null || !visibleViews.Contains(row.view)) continue;
                foreach (var set in row.sets ?? new ShortCyclePresentationSet[0])
                    if (set == candidate) return true;
            }
            return false;
        }

        internal bool WantsFocus(ShortCyclePresentationSet set) { return focused && WantsLogicalActive(set); }

        private void ApplyStandaloneVisibility()
        {
            if (DirectorOwnsVisibility) return;
            foreach (var set in ReferencedSets()) set.ApplyVisibility(WantsActive(set), WantsFocus(set));
        }

        private void OnValidate()
        {
            foreach (var set in ReferencedSets())
                if (!set.ContainerTransform.IsChildOf(transform))
                    Debug.LogWarning("Shared Set reference is allowed; visibility is merged by the Director: " + set.name, this);
        }
    }
}
