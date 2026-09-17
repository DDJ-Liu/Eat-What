using System;
using System.Collections.Generic;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    [Serializable]
    public sealed class ShortCycleViewSetEntry
    {
        public ShortCyclePhase0View view;
        public ShortCyclePresentationSet[] sets = new ShortCyclePresentationSet[0];
    }

    [DisallowMultipleComponent]
    public sealed class ShortCycleSpace : MonoBehaviour
    {
        [SerializeField] private ShortCycleSpaceId spaceId = ShortCycleSpaceId.Phase0;
        [SerializeField] private ShortCycleCameraSlot cameraSlot = ShortCycleCameraSlot.Phase0;
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

        internal bool WantsFocus(ShortCyclePresentationSet set) { return focused && WantsActive(set); }

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
