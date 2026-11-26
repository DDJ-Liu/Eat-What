using System;
using UnityEngine;

namespace EatWhat.Tools.Rendering
{
    public enum OutlineMergeOverride
    {
        FollowGroup = 0,
        ForceOn = 1,
        ForceOff = 2
    }

    /// <summary>
    /// Explicit data contract consumed by SpriteOutlineMergeRenderer2D. It carries
    /// no business ownership: the nearest enabled merge renderer is authoritative.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    [AddComponentMenu("Eat What/Rendering/Sprite Outline Merge Member 2D")]
    public sealed class SpriteOutlineMergeMember2D : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sourceRenderer = null;
        [SerializeField] private OutlineMergeOverride mergeOutline = OutlineMergeOverride.FollowGroup;
        [SerializeField] private OutlineMergeOverride mergeShadow = OutlineMergeOverride.FollowGroup;
        [SerializeField, Tooltip("描边色/宽是否来自正式 SpriteOutlineGroup2D；与合并三态相互独立。")]
        private bool useGroupOutlineParameters = true;
        [SerializeField] private Color outlineColor = Color.white;
        [SerializeField, Min(0f)] private float outlineThicknessInSourcePixels = 1f;
        [SerializeField] private Color shadowColor = Color.black;
        [SerializeField] private Vector2 shadowOffsetInSourcePixels = new Vector2(1f, -1f);
        [SerializeField, Range(0f, 1f)] private float shadowOpacity = 0.5f;
        [SerializeField, Min(0f)] private float shadowSoftnessInSourcePixels = 1f;
        private bool formalSourceActive = true;

        public SpriteRenderer SourceRenderer { get { return sourceRenderer; } }
        public OutlineMergeOverride MergeOutline { get { return mergeOutline; } }
        public OutlineMergeOverride MergeShadow { get { return mergeShadow; } }
        public bool UseGroupOutlineParameters { get { return useGroupOutlineParameters; } }
        public Color OutlineColor { get { return outlineColor; } }
        public float OutlineThicknessInSourcePixels { get { return outlineThicknessInSourcePixels; } }
        public Color ShadowColor { get { return shadowColor; } }
        public Vector2 ShadowOffsetInSourcePixels { get { return shadowOffsetInSourcePixels; } }
        public float ShadowOpacity { get { return shadowOpacity; } }
        public float ShadowSoftnessInSourcePixels { get { return shadowSoftnessInSourcePixels; } }

        public SpriteOutlineMergeRenderer2D NearestOwner
        {
            get
            {
                for (var current = transform.parent; current != null; current = current.parent)
                {
                    var candidate = current.GetComponent<SpriteOutlineMergeRenderer2D>();
                    if (candidate != null && candidate.isActiveAndEnabled) return candidate;
                }
                return null;
            }
        }

        public void Configure(
            SpriteRenderer renderer,
            OutlineMergeOverride outlineMode,
            OutlineMergeOverride shadowMode,
            Color configuredOutlineColor,
            float configuredOutlineThickness,
            Color configuredShadowColor,
            Vector2 configuredShadowOffset,
            float configuredShadowOpacity,
            float configuredShadowSoftness)
        {
            ApplyConfiguration(renderer, outlineMode, shadowMode,
                outlineMode == OutlineMergeOverride.FollowGroup, configuredOutlineColor,
                configuredOutlineThickness, configuredShadowColor, configuredShadowOffset,
                configuredShadowOpacity, configuredShadowSoftness);
        }

        /// <summary>
        /// Formal-component adapter. Parameter ownership is deliberately separate from
        /// the two merge participation modes.
        /// </summary>
        public bool ConfigureFormal(
            SpriteRenderer renderer,
            OutlineMergeOverride outlineMode,
            OutlineMergeOverride shadowMode,
            bool followGroupOutlineParameters,
            Color configuredOutlineColor,
            float configuredOutlineThickness,
            Color configuredShadowColor,
            Vector2 configuredShadowOffset,
            float configuredShadowOpacity,
            float configuredShadowSoftness)
        {
            return ApplyConfiguration(renderer, outlineMode, shadowMode,
                followGroupOutlineParameters, configuredOutlineColor, configuredOutlineThickness,
                configuredShadowColor, configuredShadowOffset, configuredShadowOpacity,
                configuredShadowSoftness);
        }

        public void SetFormalSourceActive(bool active)
        {
            if (formalSourceActive == active) return;
            formalSourceActive = active;
            SpriteOutlineMergeRenderer2D owner = NearestOwner;
            if (owner != null) owner.Invalidate("FormalMemberLifecycle");
        }

        public bool ResolveOutlineEnabled(bool groupEnabled)
        {
            return Resolve(mergeOutline, groupEnabled);
        }

        public bool ResolveShadowEnabled(bool groupEnabled)
        {
            return Resolve(mergeShadow, groupEnabled);
        }

        public Color ResolveOutlineColor(Color groupColor)
        {
            return useGroupOutlineParameters ? groupColor : outlineColor;
        }

        public float ResolveOutlineThickness(float groupThickness)
        {
            return useGroupOutlineParameters ? groupThickness : outlineThicknessInSourcePixels;
        }

        internal MemberSnapshot CaptureSnapshot(SpriteOutlineMergeRenderer2D owner)
        {
            var renderer = sourceRenderer;
            return new MemberSnapshot(
                GetInstanceID(),
                renderer == null ? 0 : renderer.GetInstanceID(),
                renderer == null || renderer.sprite == null ? 0 : renderer.sprite.GetInstanceID(),
                formalSourceActive && isActiveAndEnabled && renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy,
                NearestOwner == owner,
                renderer == null ? 0 : renderer.sortingLayerID,
                renderer == null ? 0 : renderer.sortingOrder,
                transform.localToWorldMatrix,
                renderer == null ? Color.clear : renderer.color,
                mergeOutline,
                mergeShadow,
                useGroupOutlineParameters,
                outlineColor,
                outlineThicknessInSourcePixels,
                shadowColor,
                shadowOffsetInSourcePixels,
                shadowOpacity,
                shadowSoftnessInSourcePixels);
        }

        private void Reset()
        {
            sourceRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnValidate()
        {
            if (sourceRenderer == null) sourceRenderer = GetComponent<SpriteRenderer>();
            outlineColor = SanitizeColor(outlineColor, Color.white);
            outlineThicknessInSourcePixels = SanitizeNonNegative(outlineThicknessInSourcePixels, 1f);
            shadowColor = SanitizeColor(shadowColor, Color.black);
            shadowOffsetInSourcePixels = SanitizeVector(shadowOffsetInSourcePixels);
            shadowOpacity = SanitizeRange(shadowOpacity, 0f, 1f, 0.5f);
            shadowSoftnessInSourcePixels = SanitizeNonNegative(shadowSoftnessInSourcePixels, 1f);
        }

        private static bool Resolve(OutlineMergeOverride mode, bool groupEnabled)
        {
            if (mode == OutlineMergeOverride.ForceOn) return true;
            if (mode == OutlineMergeOverride.ForceOff) return false;
            return groupEnabled;
        }

        private bool ApplyConfiguration(SpriteRenderer renderer, OutlineMergeOverride outlineMode,
            OutlineMergeOverride shadowMode, bool followGroupOutlineParameters,
            Color configuredOutlineColor, float configuredOutlineThickness,
            Color configuredShadowColor, Vector2 configuredShadowOffset,
            float configuredShadowOpacity, float configuredShadowSoftness)
        {
            Color sanitizedOutlineColor = SanitizeColor(configuredOutlineColor, Color.white);
            float sanitizedOutlineThickness = SanitizeNonNegative(configuredOutlineThickness, 1f);
            Color sanitizedShadowColor = SanitizeColor(configuredShadowColor, Color.black);
            Vector2 sanitizedShadowOffset = SanitizeVector(configuredShadowOffset);
            float sanitizedShadowOpacity = SanitizeRange(configuredShadowOpacity, 0f, 1f, 0.5f);
            float sanitizedShadowSoftness = SanitizeNonNegative(configuredShadowSoftness, 1f);
            if (sourceRenderer == renderer && mergeOutline == outlineMode && mergeShadow == shadowMode &&
                useGroupOutlineParameters == followGroupOutlineParameters &&
                ColorsEqual(outlineColor, sanitizedOutlineColor) &&
                Mathf.Approximately(outlineThicknessInSourcePixels, sanitizedOutlineThickness) &&
                ColorsEqual(shadowColor, sanitizedShadowColor) && shadowOffsetInSourcePixels == sanitizedShadowOffset &&
                Mathf.Approximately(shadowOpacity, sanitizedShadowOpacity) &&
                Mathf.Approximately(shadowSoftnessInSourcePixels, sanitizedShadowSoftness))
                return false;

            sourceRenderer = renderer;
            mergeOutline = outlineMode;
            mergeShadow = shadowMode;
            useGroupOutlineParameters = followGroupOutlineParameters;
            outlineColor = sanitizedOutlineColor;
            outlineThicknessInSourcePixels = sanitizedOutlineThickness;
            shadowColor = sanitizedShadowColor;
            shadowOffsetInSourcePixels = sanitizedShadowOffset;
            shadowOpacity = sanitizedShadowOpacity;
            shadowSoftnessInSourcePixels = sanitizedShadowSoftness;
            SpriteOutlineMergeRenderer2D owner = NearestOwner;
            if (owner != null) owner.Invalidate("MemberConfiguration");
            return true;
        }

        private static bool ColorsEqual(Color left, Color right)
        {
            return Mathf.Approximately(left.r, right.r) && Mathf.Approximately(left.g, right.g) &&
                Mathf.Approximately(left.b, right.b) && Mathf.Approximately(left.a, right.a);
        }

        internal static float SanitizeNonNegative(float value, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Max(0f, value);
        }

        internal static float SanitizeRange(float value, float minimum, float maximum, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, minimum, maximum);
        }

        internal static Vector2 SanitizeVector(Vector2 value)
        {
            return new Vector2(
                float.IsNaN(value.x) || float.IsInfinity(value.x) ? 0f : value.x,
                float.IsNaN(value.y) || float.IsInfinity(value.y) ? 0f : value.y);
        }

        internal static Color SanitizeColor(Color value, Color fallback)
        {
            return new Color(
                SanitizeRange(value.r, 0f, 8f, fallback.r),
                SanitizeRange(value.g, 0f, 8f, fallback.g),
                SanitizeRange(value.b, 0f, 8f, fallback.b),
                SanitizeRange(value.a, 0f, 1f, fallback.a));
        }
    }

    internal struct MemberSnapshot : IEquatable<MemberSnapshot>
    {
        public readonly int MemberId;
        public readonly int RendererId;
        public readonly int SpriteId;
        public readonly bool Visible;
        public readonly bool CorrectOwner;
        public readonly int SortingLayerId;
        public readonly int SortingOrder;
        public readonly Matrix4x4 LocalToWorld;
        public readonly Color RendererColor;
        public readonly OutlineMergeOverride MergeOutline;
        public readonly OutlineMergeOverride MergeShadow;
        public readonly bool UseGroupOutlineParameters;
        public readonly Color OutlineColor;
        public readonly float OutlineThickness;
        public readonly Color ShadowColor;
        public readonly Vector2 ShadowOffset;
        public readonly float ShadowOpacity;
        public readonly float ShadowSoftness;

        public MemberSnapshot(int memberId, int rendererId, int spriteId, bool visible, bool correctOwner,
            int sortingLayerId, int sortingOrder, Matrix4x4 localToWorld, Color rendererColor,
            OutlineMergeOverride mergeOutline, OutlineMergeOverride mergeShadow,
            bool useGroupOutlineParameters, Color outlineColor,
            float outlineThickness, Color shadowColor, Vector2 shadowOffset, float shadowOpacity,
            float shadowSoftness)
        {
            MemberId = memberId;
            RendererId = rendererId;
            SpriteId = spriteId;
            Visible = visible;
            CorrectOwner = correctOwner;
            SortingLayerId = sortingLayerId;
            SortingOrder = sortingOrder;
            LocalToWorld = localToWorld;
            RendererColor = rendererColor;
            MergeOutline = mergeOutline;
            MergeShadow = mergeShadow;
            UseGroupOutlineParameters = useGroupOutlineParameters;
            OutlineColor = outlineColor;
            OutlineThickness = outlineThickness;
            ShadowColor = shadowColor;
            ShadowOffset = shadowOffset;
            ShadowOpacity = shadowOpacity;
            ShadowSoftness = shadowSoftness;
        }

        public bool Equals(MemberSnapshot other)
        {
            return MemberId == other.MemberId && RendererId == other.RendererId && SpriteId == other.SpriteId &&
                Visible == other.Visible && CorrectOwner == other.CorrectOwner &&
                SortingLayerId == other.SortingLayerId && SortingOrder == other.SortingOrder &&
                LocalToWorld == other.LocalToWorld && RendererColor == other.RendererColor &&
                MergeOutline == other.MergeOutline && MergeShadow == other.MergeShadow &&
                UseGroupOutlineParameters == other.UseGroupOutlineParameters &&
                OutlineColor == other.OutlineColor && Mathf.Approximately(OutlineThickness, other.OutlineThickness) &&
                ShadowColor == other.ShadowColor && ShadowOffset == other.ShadowOffset &&
                Mathf.Approximately(ShadowOpacity, other.ShadowOpacity) &&
                Mathf.Approximately(ShadowSoftness, other.ShadowSoftness);
        }

        public override bool Equals(object value)
        {
            return value is MemberSnapshot && Equals((MemberSnapshot)value);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = MemberId;
                hash = hash * 31 + RendererId;
                hash = hash * 31 + SpriteId;
                hash = hash * 31 + Visible.GetHashCode();
                hash = hash * 31 + CorrectOwner.GetHashCode();
                hash = hash * 31 + SortingLayerId;
                hash = hash * 31 + SortingOrder;
                hash = hash * 31 + LocalToWorld.GetHashCode();
                hash = hash * 31 + RendererColor.GetHashCode();
                hash = hash * 31 + (int)MergeOutline;
                hash = hash * 31 + (int)MergeShadow;
                hash = hash * 31 + UseGroupOutlineParameters.GetHashCode();
                hash = hash * 31 + OutlineColor.GetHashCode();
                hash = hash * 31 + OutlineThickness.GetHashCode();
                hash = hash * 31 + ShadowColor.GetHashCode();
                hash = hash * 31 + ShadowOffset.GetHashCode();
                hash = hash * 31 + ShadowOpacity.GetHashCode();
                hash = hash * 31 + ShadowSoftness.GetHashCode();
                return hash;
            }
        }
    }
}
