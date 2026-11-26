using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    [DisallowMultipleComponent]
    public sealed class ShortCycleOutlineGroupBinding : MonoBehaviour
    {
        [SerializeField] private ShortCycleOutlineRole role = ShortCycleOutlineRole.FridgeSlots;
        [SerializeField] private SpriteOutlineGroup2D primaryGroup = null;
        [SerializeField] private SpriteOutlineGroup2D secondaryGroup = null;
        [SerializeField] private Material outlineMaterial = null;

        [SerializeField] private SpriteOutline2D primaryOutline = null;
        [SerializeField] private SpriteOutline2D secondaryOutline = null;

        private TargetSnapshot primarySnapshot;
        private TargetSnapshot secondarySnapshot;
        private bool hoverApplied;

        public ShortCycleOutlineRole Role { get { return role; } }
        public Material LegacyOutlineMaterial { get { return outlineMaterial; } }

        public void ApplyDefault()
        {
            ApplyHover(false);
        }

        public void ApplyHover(bool highlighted)
        {
            if (highlighted == hoverApplied)
                return;

            if (highlighted)
            {
                primarySnapshot = TargetSnapshot.Capture(primaryGroup, primaryOutline);
                secondarySnapshot = TargetSnapshot.Capture(secondaryGroup, secondaryOutline);
                primarySnapshot.ApplyHover();
                secondarySnapshot.ApplyHover();
                hoverApplied = true;
                return;
            }

            primarySnapshot.Restore();
            secondarySnapshot.Restore();
            hoverApplied = false;
        }

        private void OnDisable()
        {
            if (hoverApplied)
                ApplyHover(false);
        }

        private struct TargetSnapshot
        {
            private SpriteOutlineGroup2D group;
            private SpriteOutline2D outline;
            private Color color;
            private float thickness;
            private bool valid;

            public static TargetSnapshot Capture(SpriteOutlineGroup2D configuredGroup, SpriteOutline2D configuredOutline)
            {
                var snapshot = new TargetSnapshot();
                snapshot.group = configuredGroup;
                snapshot.outline = configuredGroup == null ? configuredOutline : null;
                snapshot.valid = snapshot.group != null || snapshot.outline != null;
                if (snapshot.group != null)
                {
                    snapshot.color = snapshot.group.OutlineColor;
                    snapshot.thickness = snapshot.group.Thickness;
                }
                else if (snapshot.outline != null)
                {
                    snapshot.color = snapshot.outline.OutlineColor;
                    snapshot.thickness = snapshot.outline.Thickness;
                }
                return snapshot;
            }

            public void ApplyHover()
            {
                if (!valid)
                    return;
                Set(ShortCycleOutlineContract.HoverColor,
                    ShortCycleOutlineContract.GetHoverThickness(thickness));
            }

            public void Restore()
            {
                if (!valid)
                    return;
                Set(color, thickness);
            }

            private void Set(Color configuredColor, float configuredThickness)
            {
                if (group != null)
                    group.Configure(group.OutlineMaterial, configuredColor, configuredThickness);
                else if (outline != null)
                    outline.Configure(outline.OutlineMaterial, configuredColor, configuredThickness);
            }
        }
    }
}
