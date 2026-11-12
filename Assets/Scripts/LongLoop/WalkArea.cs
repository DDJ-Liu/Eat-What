using System;
using UnityEngine;

namespace EatWhat.LongLoop
{
    /// <summary>World-space union of enabled polygon regions for player foot positions.</summary>
    [DisallowMultipleComponent]
    public sealed class WalkArea : MonoBehaviour
    {
        [SerializeField] private PolygonCollider2D[] regions = Array.Empty<PolygonCollider2D>();

        public bool IsUsable
        {
            get
            {
                if (!isActiveAndEnabled || regions == null) return false;
                foreach (var region in regions)
                    if (Valid(region)) return true;
                return false;
            }
        }

        public void ConfigureRegions(params PolygonCollider2D[] values)
        {
            regions = values ?? Array.Empty<PolygonCollider2D>();
        }

        public bool Contains(Vector2 worldPoint)
        {
            if (!IsUsable) return false;
            foreach (var region in regions)
                if (Valid(region) && region.OverlapPoint(worldPoint)) return true;
            return false;
        }

        public Vector2 ClosestPoint(Vector2 worldPoint)
        {
            if (!IsUsable) throw new InvalidOperationException("WalkArea has no enabled polygon regions.");
            if (Contains(worldPoint)) return worldPoint;
            var best = Vector2.zero;
            var bestDistance = float.PositiveInfinity;
            foreach (var region in regions)
            {
                if (!Valid(region)) continue;
                var point = region.ClosestPoint(worldPoint);
                var distance = (point - worldPoint).sqrMagnitude;
                if (distance < bestDistance)
                {
                    best = point;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private static bool Valid(PolygonCollider2D region)
        {
            return region != null && region.isActiveAndEnabled && region.pathCount > 0;
        }
    }
}
