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

        private ShortCycleSpace phase2Owner;
        private ShortCyclePhase2View phase2BaseBeforeModal;
        private bool phase2StapleBeforeModal;
        public bool Phase2TransitionActive { get; private set; }
        public bool Phase2ViewActive { get { return phase2Owner != null; } }
        public ShortCyclePhase2View Phase2View { get { return phase2Owner == null ? ShortCyclePhase2View.Overview : phase2Owner.Phase2View; } }
        public ShortCyclePhase2Overlay Phase2Overlay { get { return phase2Owner == null ? ShortCyclePhase2Overlay.None : phase2Owner.Phase2Overlay; } }
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
            if (space == ShortCycleSpaceId.Phase2 && phase2Owner != null)
            {
                if (interactionLayerStack != null && interactionLayerStack.IsTop(phase2Owner.Phase2ModalLayer))
                    interactionLayerStack.Pop(phase2Owner.Phase2ModalLayer);
                phase2Owner.ClearPhase2State();
                phase2Owner = null;
            }
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

            if (target != null && interactionLayerStack != null &&
                !interactionLayerStack.Contains(target.SpaceLayer))
                interactionLayerStack.Push(target.SpaceLayer);

            if (target == null)
            {
                MergeVisibility();
                return;
            }

            if (!hadDeparture)
                foreach (var traveler in travelingElements ?? new ShortCycleTravelingElement[0])
                    if (traveler != null) traveler.SnapTo(space);

            if (space == ShortCycleSpaceId.Phase2)
            {
                // P2 is a separate one-camera scene; its view changes never request a P0 slot.
                target.ArriveFocus();
                MergeVisibility();
                return;
            }

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

        public bool TryValidatePhase2Configuration(ShortCyclePresentationSet detailSet, out string error)
        {
            ShortCycleSpace target = null;
            foreach (var candidate in spaces ?? new ShortCycleSpace[0])
            {
                if (candidate == null || candidate.SpaceId != ShortCycleSpaceId.Phase2) continue;
                if (target != null) { error = "P2 has duplicate Space bindings."; return false; }
                target = candidate;
            }
            if (target == null) { error = "P2 Space binding is missing."; return false; }
            if (!target.TryValidatePhase2Table(out error)) return false;
            ShortCyclePresentationSet configuredDetail;
            if (!target.TryGetPhase2Set(ShortCyclePhase2SetRole.AreaDetail, out configuredDetail) ||
                configuredDetail != detailSet)
            { error = "P2 AreaDetail binding does not match the single viewTable Set."; return false; }
            if (interactionLayerStack == null || interactionLayerStack.Count < 1 ||
                interactionLayerStack.Top == null)
            { error = "P2 input layer stack requires a live base layer."; return false; }
            error = null; return true;
        }

        public bool TryEnterPhase2(out string error)
        {
            if (phase2Owner != null)
            { error = "P2 presentation is already active."; return false; }
            ShortCycleSpace target = null;
            foreach (var candidate in spaces ?? new ShortCycleSpace[0])
            {
                if (candidate == null || candidate.SpaceId != ShortCycleSpaceId.Phase2) continue;
                if (target != null) { error = "P2 has duplicate Space bindings."; return false; }
                target = candidate;
            }
            if (target == null) { error = "P2 Space binding is missing."; return false; }
            if (!target.TryValidatePhase2Table(out error)) return false;
            if (interactionLayerStack == null || interactionLayerStack.Count < 1 ||
                !interactionLayerStack.Push(target.SpaceLayer))
            { error = "P2 input layer is unavailable or already stacked."; return false; }
            phase2Owner = target;
            target.SetPhase2State(ShortCyclePhase2View.Overview, ShortCyclePhase2Overlay.None, false, false);
            EnterSpace(ShortCycleSpaceId.Phase2);
            error = null; return true;
        }

        public bool TrySetPhase2View(ShortCyclePhase2View next, out string error)
        {
            if (!Phase2Ready(out error)) return false;
            if (!Enum.IsDefined(typeof(ShortCyclePhase2View), next))
            { error = "Invalid P2 view."; return false; }
            var current = phase2Owner.Phase2View;
            if (current == next)
            { error = "P2 view is already active."; return false; }
            if (next == ShortCyclePhase2View.Interaction || next == ShortCyclePhase2View.MiniGame)
            {
                if (current != ShortCyclePhase2View.AreaDetail ||
                    !interactionLayerStack.Push(phase2Owner.Phase2ModalLayer))
                { error = "P2 processing modal requires AreaDetail and a free modal layer."; return false; }
                phase2BaseBeforeModal = current;
                phase2StapleBeforeModal = phase2Owner.Phase2Staple;
            }
            else if (current == ShortCyclePhase2View.Interaction || current == ShortCyclePhase2View.MiniGame)
            {
                if (next != phase2BaseBeforeModal ||
                    !interactionLayerStack.Pop(phase2Owner.Phase2ModalLayer))
                { error = "P2 processing modal must finish to its original base view."; return false; }
            }
            else if (current != ShortCyclePhase2View.Overview && current != ShortCyclePhase2View.AreaDetail)
            { error = "P2 view cannot change from an invalid base."; return false; }
            OwnVisibility();
            phase2Owner.SetPhase2State(next, ShortCyclePhase2Overlay.None, false,
                next == ShortCyclePhase2View.AreaDetail &&
                    (current == ShortCyclePhase2View.Interaction || current == ShortCyclePhase2View.MiniGame) &&
                    phase2StapleBeforeModal);
            MergeVisibility();
            error = null; return true;
        }

        public bool TrySetPhase2Overlay(ShortCyclePhase2Overlay overlay, out string error)
        {
            if (!Phase2Ready(out error, true)) return false;
            if (!Enum.IsDefined(typeof(ShortCyclePhase2Overlay), overlay))
            { error = "Invalid P2 overlay."; return false; }
            var current = phase2Owner.Phase2Overlay;
            if (overlay == current)
            { error = "P2 overlay is already in that state."; return false; }
            if (overlay != ShortCyclePhase2Overlay.None)
            {
                if (current != ShortCyclePhase2Overlay.None ||
                    !interactionLayerStack.Push(phase2Owner.Phase2ModalLayer))
                { error = "P2 overlay requires a free modal layer."; return false; }
            }
            else if (!interactionLayerStack.Pop(phase2Owner.Phase2ModalLayer))
            { error = "P2 overlay is not the top input layer."; return false; }
            OwnVisibility();
            phase2Owner.SetPhase2State(phase2Owner.Phase2View, overlay, false, phase2Owner.Phase2Staple);
            MergeVisibility();
            error = null; return true;
        }

        public bool TrySetPhase2Transition(bool active, out string error)
        {
            if (phase2Owner == null || interactionLayerStack == null)
            { error = "P2 presentation is not active."; return false; }
            if (!phase2Owner.TryValidatePhase2Table(out error)) return false;
            if (phase2Owner.Phase2Transition == active ||
                phase2Owner.Phase2Overlay != ShortCyclePhase2Overlay.None ||
                phase2Owner.Phase2View == ShortCyclePhase2View.Interaction ||
                phase2Owner.Phase2View == ShortCyclePhase2View.MiniGame)
            { error = "P2 transition is reentrant or blocked by a modal."; return false; }
            if (active ? !interactionLayerStack.Push(phase2Owner.Phase2ModalLayer) :
                !interactionLayerStack.Pop(phase2Owner.Phase2ModalLayer))
            { error = "P2 transition input layer is unavailable."; return false; }
            OwnVisibility();
            phase2Owner.SetPhase2State(phase2Owner.Phase2View, ShortCyclePhase2Overlay.None, active, phase2Owner.Phase2Staple);
            MergeVisibility();
            error = null; return true;
        }

        public bool TrySetPhase2StapleBubble(bool visible, out string error)
        {
            if (!Phase2Ready(out error)) return false;
            if (phase2Owner.Phase2View != ShortCyclePhase2View.AreaDetail)
            { error = "P2 staple condition is only valid in AreaDetail."; return false; }
            OwnVisibility();
            phase2Owner.SetPhase2State(phase2Owner.Phase2View, ShortCyclePhase2Overlay.None, false, visible);
            MergeVisibility();
            error = null; return true;
        }

        public bool TryCanNavigatePhase2(out string error)
        {
            return Phase2Ready(out error);
        }

        private bool Phase2Ready(out string error, bool allowOverlay = false)
        {
            if (phase2Owner == null || interactionLayerStack == null)
            { error = "P2 presentation is not active."; return false; }
            if (!phase2Owner.TryValidatePhase2Table(out error)) return false;
            if (phase2Owner.Phase2Transition ||
                (!allowOverlay && phase2Owner.Phase2Overlay != ShortCyclePhase2Overlay.None) ||
                !interactionLayerStack.Contains(phase2Owner.SpaceLayer) ||
                (phase2Owner.Phase2Overlay == ShortCyclePhase2Overlay.None &&
                 (phase2Owner.Phase2View == ShortCyclePhase2View.Overview ||
                  phase2Owner.Phase2View == ShortCyclePhase2View.AreaDetail) &&
                 !interactionLayerStack.IsTop(phase2Owner.SpaceLayer)))
            { error = "P2 view is blocked by a transition, overlay or lost input layer."; return false; }
            error = null; return true;
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
            Phase2TransitionActive = false;
            if (phase2Owner != null) { phase2Owner.ClearPhase2State(); phase2Owner = null; }
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
                var persistentVisual = request.Key.KeepVisualActiveAcrossSpaces &&
                    (phase2Owner == null || !phase2Owner.IsPhase2Set(request.Key));
                var visible = !Phase2TransitionActive && (request.Value || persistentVisual) &&
                    (persistentVisual || visualOnlyRequests.Contains(request.Key) ||
                        preparingRequests.Contains(request.Key) ||
                        !hasCameraRect || request.Key.IsInView(cameraRect));
                request.Key.ApplyVisibility(visible, visible && focusRequests.Contains(request.Key));
            }
        }

        public void SetPhase2Transition(bool active)
        {
            OwnVisibility(); generation++; CancelTimedVisibility();
            if (cameraPanController != null) cameraPanController.CancelPan();
            Phase2TransitionActive = active;
            foreach (var space in spaces ?? new ShortCycleSpace[0])
                if (space != null) { if (active) space.ShowView(ShortCyclePhase0View.Phase2Transition); else space.HideView(ShortCyclePhase0View.Phase2Transition); }
            if (sharedSpace != null) { if (active) sharedSpace.ShowView(ShortCyclePhase0View.Phase2Transition); else sharedSpace.HideView(ShortCyclePhase0View.Phase2Transition); }
            MergeVisibility();
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
                var desired = request.Value || (set.KeepVisualActiveAcrossSpaces &&
                    (phase2Owner == null || !phase2Owner.IsPhase2Set(set)));
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
