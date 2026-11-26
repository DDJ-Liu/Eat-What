using EatWhat.Tools.Rendering;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class SpriteOutline2D : MonoBehaviour
{
    internal const float MaxOutlineThickness = 32f;
    internal const float MaxShadowOffset = 128f;
    internal const float MaxShadowBlur = 32f;
    internal const float MaxShadowColorComponent = 8f;

    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineThicknessId = Shader.PropertyToID("_OutlineThickness");
    private static readonly int ShadowEnabledId = Shader.PropertyToID("_ShadowEnabled");
    private static readonly int ShadowColorId = Shader.PropertyToID("_ShadowColor");
    private static readonly int ShadowOffsetId = Shader.PropertyToID("_ShadowOffset");
    private static readonly int ShadowOpacityId = Shader.PropertyToID("_ShadowOpacity");
    private static readonly int ShadowBlurId = Shader.PropertyToID("_ShadowBlur");

    [SerializeField]
    private Material outlineMaterial;

    [SerializeField]
    [ColorUsage(true, true)]
    private Color outlineColor = new Color32(48, 35, 31, 255);

    [SerializeField]
    [Range(0f, MaxOutlineThickness)]
    [Tooltip("描边粗细，单位为源图片像素。设为 0 可关闭描边。")]
    private float thickness = 6f;

    [SerializeField]
    [Tooltip("启用由原 Sprite alpha 生成的单色投影阴影。")]
    private bool shadowEnabled = false;

    [SerializeField]
    [ColorUsage(false, true)]
    private Color shadowColor = new Color(0f, 0f, 0f, 1f);

    [SerializeField]
    [Tooltip("阴影偏移，单位为源图片像素。正 X/Y 将阴影移向纹理右/上方。")]
    private Vector2 shadowOffset = Vector2.zero;

    [SerializeField]
    [Range(0f, 1f)]
    private float shadowOpacity = 0f;

    [SerializeField]
    [Range(0f, MaxShadowBlur)]
    [Tooltip("3x3 高斯核半径，单位为源图片像素。0 为硬边阴影。")]
    private float shadowBlur = 0f;

    [SerializeField]
    [HideInInspector]
    private Material originalMaterial;

    [SerializeField]
    [Tooltip("启用后，描边/阴影参数跟随最近的 SpriteOutlineGroup2D；合并开关仍由下方两个独立三态决定。")]
    private bool overrideGroup = false;

    [SerializeField]
    private OutlineMergeOverride mergeOutline = OutlineMergeOverride.FollowGroup;

    [SerializeField]
    private OutlineMergeOverride mergeShadow = OutlineMergeOverride.FollowGroup;

    [SerializeField]
    private string defaultProfileId = SpriteOutlineDefaults.GenericProfileId;

    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock propertyBlock;
    private Sprite lastSynchronizedSprite;
    private bool propertyBlockRefreshRequired = true;
    private SpriteOutlineMergeMember2D mergeAdapter;
    private bool lastMergedOutline;
    private bool lastMergedShadow;

    public Material OutlineMaterial { get { return outlineMaterial; } }
    public Material OriginalMaterial { get { return originalMaterial; } }
    public bool OverrideGroup { get { return overrideGroup; } }
    public OutlineMergeOverride MergeOutline { get { return mergeOutline; } }
    public OutlineMergeOverride MergeShadow { get { return mergeShadow; } }
    public string DefaultProfileId { get { return defaultProfileId; } }
    public SpriteOutlineGroup2D NearestGroup { get { return ResolveNearestGroup(); } }
    public SpriteOutlineMergeMember2D MergeAdapter { get { CacheMergeAdapter(); return mergeAdapter; } }

    public Color OutlineColor
    {
        get { return outlineColor; }
        set
        {
            outlineColor = value;
            ApplyProperties();
        }
    }

    public float Thickness
    {
        get { return thickness; }
        set
        {
            thickness = ClampFinite(value, 0f, float.MaxValue, 0f);
            ApplyProperties();
        }
    }

    public bool ShadowEnabled
    {
        get { return shadowEnabled; }
        set
        {
            shadowEnabled = value;
            ApplyProperties();
        }
    }

    public Color ShadowColor
    {
        get { return shadowColor; }
        set
        {
            shadowColor = SanitizeShadowColor(value);
            ApplyProperties();
        }
    }

    public Vector2 ShadowOffset
    {
        get { return shadowOffset; }
        set
        {
            shadowOffset = SanitizeShadowOffset(value);
            ApplyProperties();
        }
    }

    public float ShadowOpacity
    {
        get { return shadowOpacity; }
        set
        {
            shadowOpacity = ClampFinite(value, 0f, 1f, 0f);
            ApplyProperties();
        }
    }

    public float ShadowBlur
    {
        get { return shadowBlur; }
        set
        {
            shadowBlur = ClampFinite(value, 0f, MaxShadowBlur, 0f);
            ApplyProperties();
        }
    }

    public void SetOverrideGroup(bool shouldFollowGroup)
    {
        overrideGroup = shouldFollowGroup;
        ApplyProperties();
    }

    public void SetMergeModes(OutlineMergeOverride outlineMode, OutlineMergeOverride shadowMode)
    {
        mergeOutline = outlineMode;
        mergeShadow = shadowMode;
        ApplyProperties();
    }

    public void SetDefaultProfileId(string roleId)
    {
        defaultProfileId = string.IsNullOrEmpty(roleId) ? SpriteOutlineDefaults.GenericProfileId : roleId;
    }

    public bool ResolveOutlineMergeEnabled()
    {
        SpriteOutlineGroup2D group = ResolveNearestGroup();
        return ResolveMergeMode(mergeOutline, group != null && group.MergeOutlineEnabled);
    }

    public bool ResolveShadowMergeEnabled()
    {
        SpriteOutlineGroup2D group = ResolveNearestGroup();
        return ResolveMergeMode(mergeShadow, group != null && group.MergeShadowEnabled);
    }

    public static bool ResolveMergeMode(OutlineMergeOverride mode, bool groupEnabled)
    {
        if (mode == OutlineMergeOverride.ForceOn)
            return true;
        if (mode == OutlineMergeOverride.ForceOff)
            return false;
        return groupEnabled;
    }

    public void Configure(Material material, Color color, float widthInSourcePixels)
    {
        outlineMaterial = material;
        outlineColor = color;
        thickness = ClampFinite(widthInSourcePixels, 0f, float.MaxValue, 0f);
        ApplyProperties();
    }

    public void ConfigureShadow(
        bool enabled,
        Color color,
        Vector2 offsetInSourcePixels,
        float opacity,
        float blurInSourcePixels)
    {
        shadowEnabled = enabled;
        shadowColor = SanitizeShadowColor(color);
        shadowOffset = SanitizeShadowOffset(offsetInSourcePixels);
        shadowOpacity = ClampFinite(opacity, 0f, 1f, 0f);
        shadowBlur = ClampFinite(blurInSourcePixels, 0f, MaxShadowBlur, 0f);
        ApplyProperties();
    }

    public void Configure(
        Material material,
        Color color,
        float widthInSourcePixels,
        bool enableShadow,
        Color configuredShadowColor,
        Vector2 offsetInSourcePixels,
        float opacity,
        float blurInSourcePixels)
    {
        outlineMaterial = material;
        outlineColor = color;
        thickness = ClampFinite(widthInSourcePixels, 0f, float.MaxValue, 0f);
        shadowEnabled = enableShadow;
        shadowColor = SanitizeShadowColor(configuredShadowColor);
        shadowOffset = SanitizeShadowOffset(offsetInSourcePixels);
        shadowOpacity = ClampFinite(opacity, 0f, 1f, 0f);
        shadowBlur = ClampFinite(blurInSourcePixels, 0f, MaxShadowBlur, 0f);
        ApplyProperties();
    }

    private void Reset()
    {
        SpriteOutlineDefaults defaults = SpriteOutlineDefaults.LoadDefault();
        if (defaults != null)
            defaults.ApplyTo(this, defaultProfileId);

        CacheRenderer();
        ApplyProperties();
    }

    private void OnEnable()
    {
        CacheRenderer();
        CacheMergeAdapter();
        if (mergeAdapter != null) mergeAdapter.SetFormalSourceActive(true);
        ApplyProperties();
    }

    private void OnValidate()
    {
        thickness = ClampFinite(thickness, 0f, float.MaxValue, 0f);
        shadowColor = SanitizeShadowColor(shadowColor);
        shadowOffset = SanitizeShadowOffset(shadowOffset);
        shadowOpacity = ClampFinite(shadowOpacity, 0f, 1f, 0f);
        shadowBlur = ClampFinite(shadowBlur, 0f, MaxShadowBlur, 0f);
        CacheRenderer();
        ApplyProperties();
    }

    private void OnDisable()
    {
        CacheRenderer();
        CacheMergeAdapter();
        if (mergeAdapter != null) mergeAdapter.SetFormalSourceActive(false);
        ApplyPropertyBlock(0f, false);
        lastSynchronizedSprite = null;
        propertyBlockRefreshRequired = true;
    }

    private void OnDidApplyAnimationProperties()
    {
        ApplyProperties();
    }

    private void LateUpdate()
    {
        // Animator sprite curves are evaluated before LateUpdate. Refreshing here keeps
        // the outline property block aligned with the frame selected for this render.
        SynchronizeSpriteDependentProperties();
    }

    private void CacheRenderer()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();
    }

    private void CacheMergeAdapter()
    {
        if (mergeAdapter == null)
            mergeAdapter = GetComponent<SpriteOutlineMergeMember2D>();
    }

    private void ApplyProperties()
    {
        CacheRenderer();
        if (spriteRenderer == null)
            return;

        if (outlineMaterial != null && spriteRenderer.sharedMaterial != outlineMaterial)
        {
            originalMaterial = spriteRenderer.sharedMaterial;
            spriteRenderer.sharedMaterial = outlineMaterial;
        }

        propertyBlockRefreshRequired = true;
        SynchronizeSpriteDependentProperties();
    }

    internal void ApplyDefaultStyle(SpriteOutlineStyle style)
    {
        if (style == null)
            return;

        outlineMaterial = style.OutlineMaterial;
        outlineColor = style.OutlineColor;
        thickness = ClampFinite(style.Thickness, 0f, MaxOutlineThickness, 0f);
        shadowEnabled = style.ShadowEnabled;
        shadowColor = SanitizeShadowColor(style.ShadowColor);
        shadowOffset = SanitizeShadowOffset(style.ShadowOffset);
        shadowOpacity = ClampFinite(style.ShadowOpacity, 0f, 1f, 0f);
        shadowBlur = ClampFinite(style.ShadowBlur, 0f, MaxShadowBlur, 0f);
        ApplyProperties();
    }

    internal bool ApplyFromGroup(SpriteOutlineGroup2D owner, Material material, Color color,
        float widthInSourcePixels, bool enableShadow, Color configuredShadowColor,
        Vector2 offsetInSourcePixels, float opacity, float blurInSourcePixels)
    {
        if (!overrideGroup || owner == null || ResolveNearestGroup() != owner)
            return false;

        float sanitizedThickness = ClampFinite(widthInSourcePixels, 0f, float.MaxValue, 0f);
        Color sanitizedShadowColor = SanitizeShadowColor(configuredShadowColor);
        Vector2 sanitizedShadowOffset = SanitizeShadowOffset(offsetInSourcePixels);
        float sanitizedOpacity = ClampFinite(opacity, 0f, 1f, 0f);
        float sanitizedBlur = ClampFinite(blurInSourcePixels, 0f, MaxShadowBlur, 0f);
        if (outlineMaterial == material && ColorsEqual(outlineColor, color) &&
            Mathf.Approximately(thickness, sanitizedThickness) && shadowEnabled == enableShadow &&
            ColorsEqual(shadowColor, sanitizedShadowColor) && VectorsEqual(shadowOffset, sanitizedShadowOffset) &&
            Mathf.Approximately(shadowOpacity, sanitizedOpacity) && Mathf.Approximately(shadowBlur, sanitizedBlur))
            return true;

        Configure(material, color, sanitizedThickness, enableShadow, sanitizedShadowColor,
            sanitizedShadowOffset, sanitizedOpacity, sanitizedBlur);
        return true;
    }

    private static bool ColorsEqual(Color left, Color right)
    {
        return Mathf.Approximately(left.r, right.r) && Mathf.Approximately(left.g, right.g) &&
            Mathf.Approximately(left.b, right.b) && Mathf.Approximately(left.a, right.a);
    }

    private static bool VectorsEqual(Vector2 left, Vector2 right)
    {
        return Mathf.Approximately(left.x, right.x) && Mathf.Approximately(left.y, right.y);
    }

    private SpriteOutlineGroup2D ResolveNearestGroup()
    {
        Transform current = transform;
        while (current != null)
        {
            SpriteOutlineGroup2D group = current.GetComponent<SpriteOutlineGroup2D>();
            if (group != null)
                return group;
            current = current.parent;
        }
        return null;
    }

    private void SynchronizeSpriteDependentProperties()
    {
        CacheRenderer();
        if (spriteRenderer == null)
        {
            return;
        }

        SynchronizeMergeAdapter();
        bool mergedOutline;
        bool mergedShadow;
        ResolveActualMergeOwnership(out mergedOutline, out mergedShadow);
        Sprite currentSprite = spriteRenderer.sprite;
        if (!propertyBlockRefreshRequired && currentSprite == lastSynchronizedSprite &&
            mergedOutline == lastMergedOutline && mergedShadow == lastMergedShadow)
        {
            return;
        }

        ApplyPropertyBlock(isActiveAndEnabled && !mergedOutline ? thickness : 0f,
            isActiveAndEnabled && shadowEnabled && !mergedShadow);
        lastSynchronizedSprite = currentSprite;
        lastMergedOutline = mergedOutline;
        lastMergedShadow = mergedShadow;
        propertyBlockRefreshRequired = false;
    }

    internal bool SynchronizeMergeAdapter()
    {
        CacheRenderer();
        CacheMergeAdapter();
        SpriteOutlineGroup2D group = ResolveNearestGroup();
        if (mergeAdapter == null || group == null || spriteRenderer == null)
            return false;
        group.SynchronizeMergeBackendConfiguration();
        mergeAdapter.SetFormalSourceActive(isActiveAndEnabled);
        return mergeAdapter.ConfigureFormal(spriteRenderer, mergeOutline, mergeShadow, overrideGroup,
            outlineColor, thickness, shadowColor, shadowOffset, shadowOpacity, shadowBlur);
    }

    public void ResolveActualMergeOwnership(out bool outlineOwnedByMerge, out bool shadowOwnedByMerge)
    {
        outlineOwnedByMerge = false;
        shadowOwnedByMerge = false;
        SpriteOutlineGroup2D group = ResolveNearestGroup();
        CacheMergeAdapter();
        if (group == null || mergeAdapter == null)
            return;
        SpriteOutlineMergeRenderer2D backend = group.MergeBackend;
        if (backend == null || !backend.IsReadyFor(mergeAdapter))
            return;
        outlineOwnedByMerge = ResolveMergeMode(mergeOutline, group.MergeOutlineEnabled);
        shadowOwnedByMerge = ResolveMergeMode(mergeShadow, group.MergeShadowEnabled);
    }

    private void ApplyPropertyBlock(float appliedThickness, bool appliedShadowEnabled)
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(OutlineColorId, outlineColor);
        propertyBlock.SetFloat(OutlineThicknessId, appliedThickness);
        propertyBlock.SetFloat(ShadowEnabledId, appliedShadowEnabled ? 1f : 0f);
        propertyBlock.SetColor(ShadowColorId, shadowColor);
        propertyBlock.SetVector(ShadowOffsetId, new Vector4(shadowOffset.x, shadowOffset.y, 0f, 0f));
        propertyBlock.SetFloat(ShadowOpacityId, appliedShadowEnabled ? shadowOpacity : 0f);
        propertyBlock.SetFloat(ShadowBlurId, shadowBlur);
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }

    internal static float ClampFinite(float value, float minimum, float maximum, float fallback)
    {
        return float.IsNaN(value) || float.IsInfinity(value)
            ? fallback
            : Mathf.Clamp(value, minimum, maximum);
    }

    internal static Vector2 SanitizeShadowOffset(Vector2 value)
    {
        return new Vector2(
            ClampFinite(value.x, -MaxShadowOffset, MaxShadowOffset, 0f),
            ClampFinite(value.y, -MaxShadowOffset, MaxShadowOffset, 0f));
    }

    internal static Color SanitizeShadowColor(Color value)
    {
        return new Color(
            ClampFinite(value.r, 0f, MaxShadowColorComponent, 0f),
            ClampFinite(value.g, 0f, MaxShadowColorComponent, 0f),
            ClampFinite(value.b, 0f, MaxShadowColorComponent, 0f),
            ClampFinite(value.a, 0f, 1f, 0f));
    }
}
