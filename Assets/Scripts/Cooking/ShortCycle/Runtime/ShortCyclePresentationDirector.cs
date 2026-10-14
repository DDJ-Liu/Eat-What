using System;
using System.Collections.Generic;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    [Serializable]
    public sealed class ShortCycleModalEntry
    {
        public ShortCycleModalId modal;
        public ShortCycleModalSet set;
    }

    /// <summary>
    /// Single Unity implementation of the Core host. Cross-space completion is
    /// guarded by a generation number so cancelled camera callbacks cannot land late.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShortCyclePresentationDirector : MonoBehaviour, IShortCyclePresentationHost, IShortCyclePresentationBatchHost
    {
        [SerializeField] private ShortCycleSpace[] spaces = new ShortCycleSpace[0];
        [SerializeField] private ShortCycleSpace sharedSpace = null;
        [SerializeField] private ShortCycleTravelingElement[] travelingElements = new ShortCycleTravelingElement[0];
        [SerializeField] private ShortCycleCameraPanController cameraPanController = null;
        [SerializeField] private ShortCycleInteractionLayerStack interactionLayerStack = null;
        [SerializeField] private ShortCycleTrayPresentation trayPresentation = null;
        [SerializeField] private ShortCycleModalEntry[] modals = new ShortCycleModalEntry[0];

        private int generation;
        private ShortCycleSpaceId? pendingDeparture;
        private ShortCycleSpace pendingViewArrival;
        private bool missingCameraWarningIssued;
        private readonly Dictionary<ShortCyclePresentationSet, bool> activationRequests = new Dictionary<ShortCyclePresentationSet, bool>();
        private readonly HashSet<ShortCyclePresentationSet> focusRequests = new HashSet<ShortCyclePresentationSet>();
        private readonly HashSet<ShortCyclePresentationSet> preparingRequests = new HashSet<ShortCyclePresentationSet>();
        private readonly HashSet<ShortCyclePresentationSet> visualOnlyRequests = new HashSet<ShortCyclePresentationSet>();
        private struct VisibilityChange
        {
            public bool Active;
            public float AtProgress;
        }
        private readonly Dictionary<ShortCyclePresentationSet, VisibilityChange> visibilityChanges =
            new Dictionary<ShortCyclePresentationSet, VisibilityChange>();
        private int presentationChangeDepth;

        public int Generation { get { return generation; } }

        public void BeginPresentationChange()
        {
            if (presentationChangeDepth++ != 0) return;
            CancelTimedVisibility();
            if (cameraPanController != null) cameraPanController.CancelPan();
        }

        public void EndPresentationChange()
        {
            if (presentationChangeDepth == 0) return;
            if (--presentationChangeDepth != 0) return;
            if (cameraPanController != null && cameraPanController.IsPanning)
                BeginTimedVisibility();
            else MergeVisibility();
        }

        public void LeaveSpace(ShortCycleSpaceId space)
        {
            OwnVisibility();
            generation++;
            pendingDeparture = space;
            var source = ResolveSpace(space);
            if (pendingViewArrival == source) pendingViewArrival = null;
            if (source != null)
            {
                if (interactionLayerStack != null) interactionLayerStack.Pop(source.SpaceLayer);
                source.LeaveFocus();
            }
            MergeVisibility();
        }

        public void EnterSpace(ShortCycleSpaceId space)
        {
            OwnVisibility();
            var requestGeneration = ++generation;
            var target = ResolveSpace(space);
            var hadDeparture = pendingDeparture.HasValue;
            var isCrossSpace = pendingDeparture.HasValue && pendingDeparture.Value != space;
            pendingDeparture = null;

            if (target != null && interactionLayerStack != null)
                interactionLayerStack.Push(target.SpaceLayer);

            if (target == null)
            {
                MergeVisibility();
                return;
            }

            if (!hadDeparture)
                foreach (var traveler in travelingElements ?? new ShortCycleTravelingElement[0])
                    if (traveler != null) traveler.SnapTo(space);

            if (!isCrossSpace)
            {
                target.ArriveFocus();
                MergeVisibility();
                return;
            }

            target.PrepareVisuals();
            foreach (var traveler in travelingElements ?? new ShortCycleTravelingElement[0])
                if (traveler != null) traveler.MoveTo(space);

            // Phase0 view rows own the actual destination slot. Waiting for ShowView avoids
            // a redundant P0Center pan immediately followed by P0Left for Browse/ClueBoard.
            if (target.HasViewCameraRouting)
            {
                pendingViewArrival = target;
                MergeVisibility();
                return;
            }

            MergeVisibility();
            if (!RequestPan(target.CameraSlot, () => CompleteArrival(requestGeneration, target)))
                CompleteArrival(requestGeneration, target);
        }

        public void ShowView(ShortCycleSpaceId space, ShortCyclePhase0View view)
        {
            OwnVisibility();
            var requestGeneration = ++generation;
            var target = ResolveSpace(space);
            if (target == null)
            {
                MergeVisibility();
                return;
            }

            ShortCycleCameraSlot slot;
            target.TryResolveCameraSlot(view, out slot);
            if (cameraPanController == null || !cameraPanController.IsAtSlot(slot))
                target.PrepareVisuals();
            target.ShowView(view);
            MergeVisibility();
            if (!RequestPan(slot, () => CompleteViewArrival(requestGeneration, target, view)))
                CompleteViewArrival(requestGeneration, target, view);
        }

        public void HideView(ShortCycleSpaceId space, ShortCyclePhase0View view)
        {
            OwnVisibility();
            generation++;
            var target = ResolveSpace(space);
            if (target != null)
            {
                target.HideView(view);
                target.CancelVisualPreparation();
            }
            MergeVisibility();
        }

        public void PushModal(ShortCycleModalId modal)
        {
            var target = ResolveModal(modal);
            if (target == null) return;
            target.Activate();
            if (interactionLayerStack != null) interactionLayerStack.Push(target.ModalLayer);
        }

        public void PopModal(ShortCycleModalId modal)
        {
            var target = ResolveModal(modal);
            if (target == null) return;
            if (interactionLayerStack != null) interactionLayerStack.Pop(target.ModalLayer);
            target.Deactivate();
        }

        public void SetTrayView(ShortCycleTrayView view)
        {
            if (trayPresentation != null) trayPresentation.Apply(view);
        }

        public void ClearSessionPresentation()
        {
            generation++;
            CancelTimedVisibility();
            presentationChangeDepth = 0;
            if (cameraPanController != null) cameraPanController.CancelPan();
            pendingDeparture = null;
            pendingViewArrival = null;
            missingCameraWarningIssued = false;
            foreach (var traveler in travelingElements ?? new ShortCycleTravelingElement[0])
                if (traveler != null) traveler.Cancel();
            foreach (var space in spaces ?? new ShortCycleSpace[0])
            {
                if (space == null) continue;
                space.HideAllViews();
                space.LeaveFocus();
                space.RestoreTransientState();
            }
            if (sharedSpace != null) sharedSpace.RestoreTransientState();
            foreach (var row in modals ?? new ShortCycleModalEntry[0])
                if (row != null && row.set != null) row.set.RestoreTransientState();
            if (interactionLayerStack != null) interactionLayerStack.RestoreTransientState();
            activationRequests.Clear();
            focusRequests.Clear();
            preparingRequests.Clear();
            visualOnlyRequests.Clear();
        }

        private void CompleteArrival(int requestGeneration, ShortCycleSpace target)
        {
            if (requestGeneration != generation || target == null) return;
            CancelTimedVisibility();
            if (pendingViewArrival == target) pendingViewArrival = null;
            target.ArriveFocus();
            MergeVisibility();
        }

        private void CompleteViewArrival(int requestGeneration, ShortCycleSpace target, ShortCyclePhase0View view)
        {
            if (requestGeneration != generation || target == null) return;
            CancelTimedVisibility();
            if (pendingViewArrival == target)
            {
                pendingViewArrival = null;
            }
            target.ArriveFocus();
            MergeVisibility();
        }

        private bool RequestPan(ShortCycleCameraSlot slot, Action onComplete)
        {
            if (cameraPanController != null && cameraPanController.PanTo(slot, onComplete))
            {
                missingCameraWarningIssued = false;
                return true;
            }

            if (!missingCameraWarningIssued)
            {
                Debug.LogWarning("ShortCycle camera/marker is unavailable; applying logical presentation state without a pan.", this);
                missingCameraWarningIssued = true;
            }
            return false;
        }

        private void OnDisable() { ClearSessionPresentation(); }
        private void OnDestroy() { ClearSessionPresentation(); }

        private void OwnVisibility()
        {
            foreach (var candidate in spaces ?? new ShortCycleSpace[0])
                if (candidate != null) candidate.DirectorOwnsVisibility = true;
            if (sharedSpace != null) sharedSpace.DirectorOwnsVisibility = true;
        }

        private void MergeVisibility()
        {
            if (presentationChangeDepth != 0) return;
            if (visibilityChanges.Count != 0) CancelTimedVisibility();
            CollectVisibilityRequests();
            Rect cameraRect;
            var hasCameraRect = ShortCycleSpace.TryGetCameraWorldRect(
                cameraPanController == null ? null : cameraPanController.ControlledCamera, out cameraRect);
            if (!hasCameraRect && !missingCameraWarningIssued)
            {
                Debug.LogWarning("ShortCycle camera bounds are unavailable; preserving logical Set visibility.", this);
                missingCameraWarningIssued = true;
            }
            foreach (var request in activationRequests)
            {
                var persistentVisual = request.Key.KeepVisualActiveAcrossSpaces;
                var visible = (request.Value || persistentVisual) &&
                    (persistentVisual || visualOnlyRequests.Contains(request.Key) ||
                        preparingRequests.Contains(request.Key) ||
                        !hasCameraRect || request.Key.IsInView(cameraRect));
                request.Key.ApplyVisibility(visible, visible && focusRequests.Contains(request.Key));
            }
        }

        private void CollectVisibilityRequests()
        {
            activationRequests.Clear();
            focusRequests.Clear();
            preparingRequests.Clear();
            visualOnlyRequests.Clear();
            foreach (var candidate in spaces ?? new ShortCycleSpace[0]) CollectRequests(candidate);
            CollectRequests(sharedSpace);
        }

        private void BeginTimedVisibility()
        {
            CollectVisibilityRequests();
            visibilityChanges.Clear();
            foreach (var request in activationRequests)
            {
                var set = request.Key;
                var desired = request.Value || set.KeepVisualActiveAcrossSpaces;
                if (!set.HasVisibilityRequest || set.IsVisibilityRequested == desired)
                {
                    set.ApplyVisibility(desired, false);
                    continue;
                }
                var threshold = desired ? set.AppearAtProgress : set.DisappearAtProgress;
                if (threshold <= 0f) set.ApplyVisibility(desired, false);
                else
                {
                    set.SetFocus(false);
                    visibilityChanges[set] = new VisibilityChange { Active = desired, AtProgress = threshold };
                }
            }
            if (visibilityChanges.Count == 0) return;
            cameraPanController.PanProgressed += ApplyTimedVisibility;
            ApplyTimedVisibility(cameraPanController.NormalizedProgress);
        }

        private void ApplyTimedVisibility(float progress)
        {
            foreach (var request in visibilityChanges)
            {
                if (progress >= request.Value.AtProgress)
                    request.Key.ApplyVisibility(request.Value.Active, false);
            }
        }

        private void CancelTimedVisibility()
        {
            if (cameraPanController != null) cameraPanController.PanProgressed -= ApplyTimedVisibility;
            visibilityChanges.Clear();
        }

        private void CollectRequests(ShortCycleSpace space)
        {
            if (space == null) return;
            foreach (var set in space.ReferencedSets())
            {
                var wantsActive = space.WantsActive(set);
                bool previous;
                activationRequests.TryGetValue(set, out previous);
                activationRequests[set] = previous || wantsActive;
                if (space.WantsFocus(set)) focusRequests.Add(set);
                if (wantsActive && space.IsPreparingVisuals) preparingRequests.Add(set);
                if (space.WantsVisualOnly(set)) visualOnlyRequests.Add(set);
            }
        }

        private ShortCycleSpace ResolveSpace(ShortCycleSpaceId space)
        {
            if (space == ShortCycleSpaceId.Shared) return sharedSpace;
            foreach (var candidate in spaces ?? new ShortCycleSpace[0])
                if (candidate != null && candidate.SpaceId == space) return candidate;
            return null;
        }

        private ShortCycleModalSet ResolveModal(ShortCycleModalId modal)
        {
            foreach (var row in modals ?? new ShortCycleModalEntry[0])
                if (row != null && row.modal == modal) return row.set;
            return null;
        }
    }
}
