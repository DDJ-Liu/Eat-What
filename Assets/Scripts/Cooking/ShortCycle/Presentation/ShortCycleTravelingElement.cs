using System;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    [Serializable]
    public sealed class ShortCycleTravelAnchor
    {
        public ShortCycleSpaceId space;
        public Transform anchor;
    }

    [DisallowMultipleComponent]
    public sealed class ShortCycleTravelingElement : MonoBehaviour
    {
        [SerializeField] private Transform movingTransform = null;
        [SerializeField] private TransitionBehaviour_Position positionTransition = null;
        [SerializeField] private ShortCycleTravelAnchor[] anchors = new ShortCycleTravelAnchor[0];
        [SerializeField] private float duration = 0.45f;

        public bool MoveTo(ShortCycleSpaceId space, Action onComplete = null)
        {
            var anchor = ResolveAnchor(space);
            if (movingTransform == null || positionTransition == null || anchor == null) return false;
            var destinationSnapshot = anchor.position;
            return positionTransition.PlayTo(destinationSnapshot, duration, true, onComplete, null) != null;
        }

        /// <summary>Cancels any active move and places the existing traveler at its authored space anchor.</summary>
        public bool SnapTo(ShortCycleSpaceId space)
        {
            var anchor = ResolveAnchor(space);
            if (movingTransform == null || anchor == null) return false;
            Cancel();
            movingTransform.position = anchor.position;
            return true;
        }

        public void Cancel()
        {
            if (positionTransition != null) positionTransition.Cancel();
        }

        private Transform ResolveAnchor(ShortCycleSpaceId space)
        {
            foreach (var row in anchors ?? new ShortCycleTravelAnchor[0])
                if (row != null && row.space == space) return row.anchor;
            return null;
        }
    }
}
