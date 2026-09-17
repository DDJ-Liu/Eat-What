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
    public sealed class ShortCyclePresentationDirector : MonoBehaviour, IShortCyclePresentationHost
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
        private readonly Dictionary<ShortCyclePresentationSet, bool> activationRequests = new Dictionary<ShortCyclePresentationSet, bool>();
        private readonly HashSet<ShortCyclePresentationSet> focusRequests = new HashSet<ShortCyclePresentationSet>();

        public int Generation { get { return generation; } }

        public void LeaveSpace(ShortCycleSpaceId space)
        {
            OwnVisibility();
            generation++;
            pendingDeparture = space;
            var source = ResolveSpace(space);
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

            foreach (var traveler in travelingElements ?? new ShortCycleTravelingElement[0])
                if (traveler != null) traveler.MoveTo(space);

            if (cameraPanController == null ||
                !cameraPanController.PanTo(target.CameraSlot, () => CompleteArrival(requestGeneration, target)))
                CompleteArrival(requestGeneration, target);
        }

        public void ShowView(ShortCycleSpaceId space, ShortCyclePhase0View view)
        {
            OwnVisibility();
            var target = ResolveSpace(space);
            if (space == ShortCycleSpaceId.Phase0 && view == ShortCyclePhase0View.ClueBoardShell)
                foreach (var traveler in travelingElements ?? new ShortCycleTravelingElement[0])
                    if (traveler != null) traveler.SnapTo(space);
            if (target != null) target.ShowView(view);
            MergeVisibility();
        }

        public void HideView(ShortCycleSpaceId space, ShortCyclePhase0View view)
        {
            OwnVisibility();
            var target = ResolveSpace(space);
            if (target != null) target.HideView(view);
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
            pendingDeparture = null;
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
        }

        private void CompleteArrival(int requestGeneration, ShortCycleSpace target)
        {
            if (requestGeneration != generation || target == null) return;
            target.ArriveFocus();
            MergeVisibility();
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
            activationRequests.Clear();
            focusRequests.Clear();
            foreach (var candidate in spaces ?? new ShortCycleSpace[0]) CollectRequests(candidate);
            CollectRequests(sharedSpace);
            foreach (var request in activationRequests)
                request.Key.ApplyVisibility(request.Value, focusRequests.Contains(request.Key));
        }

        private void CollectRequests(ShortCycleSpace space)
        {
            if (space == null) return;
            foreach (var set in space.ReferencedSets())
            {
                bool previous;
                activationRequests.TryGetValue(set, out previous);
                activationRequests[set] = previous || space.WantsActive(set);
                if (space.WantsFocus(set)) focusRequests.Add(set);
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
