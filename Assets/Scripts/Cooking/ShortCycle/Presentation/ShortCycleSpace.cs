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

    public enum ShortCyclePhase2View { Overview, AreaDetail, Interaction, MiniGame }
    public enum ShortCyclePhase2Overlay { None, StepDetail, ExitConfirm }
    public enum ShortCyclePhase2SetRole
    {
        TopBar, Overview, AreaDetail, PrepTray, HandTool, InteractionModal,
        MiniGameModal, StapleBubble, Escape, StepDetailEntry, RestockEntry,
        StepDetailModal, ExitConfirmModal
    }

    [Serializable]
    public sealed class ShortCyclePhase2ViewSetEntry
    {
        public ShortCyclePhase2SetRole role;
        public ShortCyclePresentationSet set;
    }

    [DisallowMultipleComponent]
    public sealed class ShortCycleSpace : MonoBehaviour
    {
        [SerializeField] private ShortCycleSpaceId spaceId = ShortCycleSpaceId.Phase0;
        [SerializeField] private ShortCycleCameraSlot cameraSlot = ShortCycleCameraSlot.P0Center;
        [SerializeField] private MouseInteractionLayer spaceLayer = null;
        [SerializeField] private ShortCyclePresentationSet[] structureSets = new ShortCyclePresentationSet[0];
        [SerializeField] private ShortCyclePresentationSet[] visualOnlySets = new ShortCyclePresentationSet[0];
        [SerializeField] private ShortCycleViewSetEntry[] viewTable = new ShortCycleViewSetEntry[0];
        [SerializeField] private ShortCyclePhase2ViewSetEntry[] phase2ViewTable = new ShortCyclePhase2ViewSetEntry[0];
        [SerializeField] private MouseInteractionLayer phase2ModalLayer = null;
        private ShortCyclePhase2View phase2View = ShortCyclePhase2View.Overview;
        private ShortCyclePhase2Overlay phase2Overlay;
        private bool phase2Configured;
        private bool phase2Transition;
        private bool phase2Staple;
        private readonly HashSet<ShortCyclePhase0View> visibleViews = new HashSet<ShortCyclePhase0View>();
        private bool focused;
        private bool arrived;
        private bool preparingVisuals;
        internal bool DirectorOwnsVisibility { get; set; }

        public ShortCycleSpaceId SpaceId { get { return spaceId; } }
        public ShortCycleCameraSlot CameraSlot { get { return cameraSlot; } }
        public MouseInteractionLayer SpaceLayer { get { return spaceLayer; } }
        public MouseInteractionLayer Phase2ModalLayer { get { return phase2ModalLayer; } }
        public ShortCyclePhase2View Phase2View { get { return phase2View; } }
        public ShortCyclePhase2Overlay Phase2Overlay { get { return phase2Overlay; } }
        public bool Phase2Transition { get { return phase2Transition; } }
        public bool Phase2Configured { get { return phase2Configured; } }
        public bool Phase2Staple { get { return phase2Staple; } }
        internal bool HasViewCameraRouting { get { return viewTable != null && viewTable.Length > 0; } }
        internal bool IsPreparingVisuals { get { return preparingVisuals; } }

        public void PrepareVisuals()
        {
            preparingVisuals = true;
            focused = false;
            ApplyStandaloneVisibility();
        }

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
            preparingVisuals = false;
            ApplyStandaloneVisibility();
        }

        public void LeaveFocus()
        {
            focused = false;
            preparingVisuals = false;
            ApplyStandaloneVisibility();
        }

        public void CancelVisualPreparation()
        {
            if (!preparingVisuals) return;
            preparingVisuals = false;
            focused = arrived;
            ApplyStandaloneVisibility();
        }

        public void ShowView(ShortCyclePhase0View view) { visibleViews.Add(view); ApplyStandaloneVisibility(); }
        public void HideView(ShortCyclePhase0View view) { visibleViews.Remove(view); ApplyStandaloneVisibility(); }

        public void HideAllViews()
        {
            visibleViews.Clear();
            ApplyStandaloneVisibility();
        }

        public bool TryValidatePhase2Table(out string error)
        {
            if (spaceId != ShortCycleSpaceId.Phase2 || spaceLayer == null || phase2ModalLayer == null ||
                ReferenceEquals(spaceLayer, phase2ModalLayer))
            { error = "P2 Space or input layers are missing or mismatched."; return false; }
            if ((structureSets != null && structureSets.Length != 0) ||
                (visualOnlySets != null && visualOnlySets.Length != 0) ||
                (viewTable != null && viewTable.Length != 0))
            { error = "P2 Space cannot mix legacy view rows with its own viewTable."; return false; }
            var roles = new HashSet<ShortCyclePhase2SetRole>();
            var sets = new HashSet<ShortCyclePresentationSet>();
            foreach (var row in phase2ViewTable ?? new ShortCyclePhase2ViewSetEntry[0])
            {
                if (row == null || row.set == null || !Enum.IsDefined(typeof(ShortCyclePhase2SetRole), row.role) ||
                    !roles.Add(row.role) || !sets.Add(row.set) ||
                    !row.set.transform.IsChildOf(transform) ||
                    !row.set.ContainerTransform.IsChildOf(transform))
                { error = "P2 viewTable contains a missing, external, invalid or duplicate Set."; return false; }
            }
            if (roles.Count != Enum.GetValues(typeof(ShortCyclePhase2SetRole)).Length)
            { error = "P2 viewTable is incomplete."; return false; }
            error = null; return true;
        }

        public bool TryGetPhase2Set(ShortCyclePhase2SetRole role, out ShortCyclePresentationSet set)
        {
            foreach (var row in phase2ViewTable ?? new ShortCyclePhase2ViewSetEntry[0])
                if (row != null && row.role == role) { set = row.set; return set != null; }
            set = null; return false;
        }

        internal void SetPhase2State(ShortCyclePhase2View view, ShortCyclePhase2Overlay overlay,
            bool transition, bool staple)
        {
            phase2Configured = true;
            phase2View = view; phase2Overlay = overlay;
            phase2Transition = transition; phase2Staple = staple;
            ApplyStandaloneVisibility();
        }

        internal void ClearPhase2State()
        {
            phase2Configured = false; phase2View = ShortCyclePhase2View.Overview;
            phase2Overlay = ShortCyclePhase2Overlay.None; phase2Transition = false; phase2Staple = false;
            ApplyStandaloneVisibility();
        }

        public void RestoreTransientState()
        {
            focused = false;
            arrived = false;
            preparingVisuals = false;
            visibleViews.Clear();
            ClearPhase2State();
            foreach (var set in structureSets ?? new ShortCyclePresentationSet[0])
                if (set != null) set.RestoreTransientState();
            foreach (var set in visualOnlySets ?? new ShortCyclePresentationSet[0])
                if (set != null) set.RestoreTransientState();
            foreach (var row in viewTable ?? new ShortCycleViewSetEntry[0])
                foreach (var set in row == null || row.sets == null ? new ShortCyclePresentationSet[0] : row.sets)
                    if (set != null) set.RestoreTransientState();
            foreach (var row in phase2ViewTable ?? new ShortCyclePhase2ViewSetEntry[0])
                if (row != null && row.set != null) row.set.RestoreTransientState();
        }

        internal IEnumerable<ShortCyclePresentationSet> ReferencedSets()
        {
            foreach (var set in structureSets ?? new ShortCyclePresentationSet[0])
                if (set != null) yield return set;
            foreach (var set in visualOnlySets ?? new ShortCyclePresentationSet[0])
                if (set != null) yield return set;
            foreach (var row in viewTable ?? new ShortCycleViewSetEntry[0])
                foreach (var set in row == null || row.sets == null ? new ShortCyclePresentationSet[0] : row.sets)
                    if (set != null) yield return set;
            foreach (var row in phase2ViewTable ?? new ShortCyclePhase2ViewSetEntry[0])
                if (row != null && row.set != null) yield return row.set;
        }

        internal bool WantsActive(ShortCyclePresentationSet candidate)
        {
            if (visibleViews.Contains(ShortCyclePhase0View.Phase2Transition)) return false;
            if (IsPhase2Set(candidate)) return Phase2WantsActive(candidate);
            if (candidate != null && candidate.KeepVisualActiveAcrossSpaces) return true;
            return WantsLogicalActive(candidate);
        }

        private bool WantsLogicalActive(ShortCyclePresentationSet candidate)
        {
            foreach (var set in visualOnlySets ?? new ShortCyclePresentationSet[0])
                if (set == candidate && (focused || preparingVisuals)) return true;
            foreach (var set in structureSets ?? new ShortCyclePresentationSet[0])
                if (set == candidate && (focused || preparingVisuals ||
                    (arrived && candidate.ContainerTransform.IsChildOf(transform))))
                    return true;
            foreach (var row in viewTable ?? new ShortCycleViewSetEntry[0])
            {
                if ((!focused && !preparingVisuals) || row == null || !visibleViews.Contains(row.view)) continue;
                foreach (var set in row.sets ?? new ShortCyclePresentationSet[0])
                    if (set == candidate) return true;
            }
            return false;
        }

        internal bool WantsFocus(ShortCyclePresentationSet set)
        {
            if (IsPhase2Set(set)) return Phase2WantsFocus(set);
            foreach (var visualOnly in visualOnlySets ?? new ShortCyclePresentationSet[0])
                if (visualOnly == set) return false;
            return !visibleViews.Contains(ShortCyclePhase0View.Phase2Transition) && focused && WantsLogicalActive(set);
        }

        internal bool WantsVisualOnly(ShortCyclePresentationSet candidate)
        {
            if (IsPhase2Set(candidate)) return true;
            if (visibleViews.Contains(ShortCyclePhase0View.Phase2Transition)) return false;
            foreach (var set in visualOnlySets ?? new ShortCyclePresentationSet[0])
                if (set == candidate && (focused || preparingVisuals)) return true;
            return false;
        }

        internal bool IsPhase2Set(ShortCyclePresentationSet set)
        {
            if (set == null || spaceId != ShortCycleSpaceId.Phase2) return false;
            foreach (var row in phase2ViewTable ?? new ShortCyclePhase2ViewSetEntry[0])
                if (row != null && row.set == set) return true;
            return false;
        }

        private bool Phase2WantsActive(ShortCyclePresentationSet set)
        {
            if (!phase2Configured || phase2Transition || (!focused && !preparingVisuals)) return false;
            foreach (var row in phase2ViewTable ?? new ShortCyclePhase2ViewSetEntry[0])
            {
                if (row == null || row.set != set) continue;
                switch (row.role)
                {
                    case ShortCyclePhase2SetRole.TopBar:
                    case ShortCyclePhase2SetRole.PrepTray: return true;
                    case ShortCyclePhase2SetRole.Overview:
                    case ShortCyclePhase2SetRole.RestockEntry: return phase2View == ShortCyclePhase2View.Overview;
                    case ShortCyclePhase2SetRole.AreaDetail: return phase2View != ShortCyclePhase2View.Overview;
                    case ShortCyclePhase2SetRole.HandTool: return phase2View == ShortCyclePhase2View.AreaDetail;
                    case ShortCyclePhase2SetRole.InteractionModal: return phase2View == ShortCyclePhase2View.Interaction;
                    case ShortCyclePhase2SetRole.MiniGameModal: return phase2View == ShortCyclePhase2View.MiniGame;
                    case ShortCyclePhase2SetRole.StapleBubble: return phase2View == ShortCyclePhase2View.AreaDetail && phase2Staple;
                    case ShortCyclePhase2SetRole.Escape:
                    case ShortCyclePhase2SetRole.StepDetailEntry:
                        return phase2View == ShortCyclePhase2View.Overview || phase2View == ShortCyclePhase2View.AreaDetail;
                    case ShortCyclePhase2SetRole.StepDetailModal: return phase2Overlay == ShortCyclePhase2Overlay.StepDetail;
                    case ShortCyclePhase2SetRole.ExitConfirmModal: return phase2Overlay == ShortCyclePhase2Overlay.ExitConfirm;
                }
            }
            return false;
        }

        private bool Phase2WantsFocus(ShortCyclePresentationSet set)
        {
            if (!focused || !Phase2WantsActive(set)) return false;
            foreach (var row in phase2ViewTable ?? new ShortCyclePhase2ViewSetEntry[0])
            {
                if (row == null || row.set != set) continue;
                if (phase2Overlay != ShortCyclePhase2Overlay.None)
                    return row.role == ShortCyclePhase2SetRole.StepDetailModal && phase2Overlay == ShortCyclePhase2Overlay.StepDetail ||
                        row.role == ShortCyclePhase2SetRole.ExitConfirmModal && phase2Overlay == ShortCyclePhase2Overlay.ExitConfirm;
                if (phase2View == ShortCyclePhase2View.Interaction)
                    return row.role == ShortCyclePhase2SetRole.InteractionModal;
                if (phase2View == ShortCyclePhase2View.MiniGame)
                    return row.role == ShortCyclePhase2SetRole.MiniGameModal;
                return true;
            }
            return false;
        }

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
