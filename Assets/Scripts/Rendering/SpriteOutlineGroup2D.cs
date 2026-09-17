using System.Collections.Generic;
using EatWhat.Tools.Rendering;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class SpriteOutlineGroup2D : MonoBehaviour
{
    [SerializeField] private Material outlineMaterial;
    [SerializeField, ColorUsage(true, true)] private Color outlineColor = new Color32(48, 35, 31, 255);
    [SerializeField, Range(0f, SpriteOutline2D.MaxOutlineThickness)]
    [Tooltip("统一描边粗细，单位为源图片像素。设为 0 可关闭整组描边。")]
    private float thickness = 6f;
    [SerializeField] private bool shadowEnabled = false;
    [SerializeField, ColorUsage(false, true)] private Color shadowColor = new Color(0f, 0f, 0f, 1f);
    [SerializeField, Tooltip("统一阴影偏移，单位为源图片像素。")]
    private Vector2 shadowOffset = Vector2.zero;
    [SerializeField, Range(0f, 1f)] private float shadowOpacity = 0f;
    [SerializeField, Range(0f, SpriteOutline2D.MaxShadowBlur)] private float shadowBlur = 0f;
    [SerializeField] private bool includeInactive = true;
    [SerializeField] private bool directChildrenOnly = false;
    [SerializeField] private bool mergeOutline = true;
    [SerializeField] private bool mergeShadow = true;
    [SerializeField] private string defaultProfileId = SpriteOutlineDefaults.GenericProfileId;

    public Material OutlineMaterial { get { return outlineMaterial; } }
    public bool IncludeInactive { get { return includeInactive; } }
    public bool DirectChildrenOnly { get { return directChildrenOnly; } }
    public bool MergeOutlineEnabled { get { return mergeOutline; } set { mergeOutline = value; SynchronizeMergeBackendConfiguration(); } }
    public bool MergeShadowEnabled { get { return mergeShadow; } set { mergeShadow = value; SynchronizeMergeBackendConfiguration(); } }
    public string DefaultProfileId { get { return defaultProfileId; } }
    public SpriteOutlineMergeRenderer2D MergeBackend { get { return GetComponent<SpriteOutlineMergeRenderer2D>(); } }

    public Color OutlineColor
    {
        get { return outlineColor; }
        set { outlineColor = value; SynchronizeFollowingMembers(); }
    }

    public float Thickness
    {
        get { return thickness; }
        set { thickness = SpriteOutline2D.ClampFinite(value, 0f, float.MaxValue, 0f); SynchronizeFollowingMembers(); }
    }

    public bool ShadowEnabled
    {
        get { return shadowEnabled; }
        set { shadowEnabled = value; SynchronizeFollowingMembers(); }
    }

    public Color ShadowColor
    {
        get { return shadowColor; }
        set { shadowColor = SpriteOutline2D.SanitizeShadowColor(value); SynchronizeFollowingMembers(); }
    }

    public Vector2 ShadowOffset
    {
        get { return shadowOffset; }
        set { shadowOffset = SpriteOutline2D.SanitizeShadowOffset(value); SynchronizeFollowingMembers(); }
    }

    public float ShadowOpacity
    {
        get { return shadowOpacity; }
        set { shadowOpacity = SpriteOutline2D.ClampFinite(value, 0f, 1f, 0f); SynchronizeFollowingMembers(); }
    }

    public float ShadowBlur
    {
        get { return shadowBlur; }
        set { shadowBlur = SpriteOutline2D.ClampFinite(value, 0f, SpriteOutline2D.MaxShadowBlur, 0f); SynchronizeFollowingMembers(); }
    }

    public void SetDefaultProfileId(string roleId)
    {
        defaultProfileId = string.IsNullOrEmpty(roleId) ? SpriteOutlineDefaults.GenericProfileId : roleId;
    }

    public void Configure(Material material, Color color, float widthInSourcePixels)
    {
        outlineMaterial = material;
        outlineColor = color;
        thickness = SpriteOutline2D.ClampFinite(widthInSourcePixels, 0f, float.MaxValue, 0f);
        SynchronizeFollowingMembers();
    }

    public void ConfigureShadow(bool enabled, Color color, Vector2 offsetInSourcePixels,
        float opacity, float blurInSourcePixels)
    {
        shadowEnabled = enabled;
        shadowColor = SpriteOutline2D.SanitizeShadowColor(color);
        shadowOffset = SpriteOutline2D.SanitizeShadowOffset(offsetInSourcePixels);
        shadowOpacity = SpriteOutline2D.ClampFinite(opacity, 0f, 1f, 0f);
        shadowBlur = SpriteOutline2D.ClampFinite(blurInSourcePixels, 0f, SpriteOutline2D.MaxShadowBlur, 0f);
        SynchronizeFollowingMembers();
    }

    [ContextMenu("显式创建缺失成员")]
    public int CreateMissingMembers()
    {
        var created = 0;
        SpriteRenderer[] renderers = GetOwnedRenderers();
        for (var index = 0; index < renderers.Length; index++)
        {
            SpriteRenderer renderer = renderers[index];
            if (renderer == null || IsGeneratedMergeHost(renderer.gameObject.name))
                continue;

            SpriteOutline2D outline = renderer.GetComponent<SpriteOutline2D>();
            if (outline != null)
                continue;

            outline = renderer.gameObject.AddComponent<SpriteOutline2D>();
            if (outline == null)
                continue;

            outline.SetOverrideGroup(true);
            outline.SetDefaultProfileId(defaultProfileId);
            SynchronizeMember(outline);
            created++;
        }
        return created;
    }

    [ContextMenu("显式同步跟随成员")]
    public int SynchronizeFollowingMembers()
    {
        var synchronized = 0;
        SpriteOutline2D[] members = GetOwnedMembers();
        for (var index = 0; index < members.Length; index++)
            if (SynchronizeMember(members[index])) synchronized++;
        SynchronizeMergeBackendConfiguration();
        return synchronized;
    }

    public bool SynchronizeMember(SpriteOutline2D outline)
    {
        return outline != null && outline.ApplyFromGroup(this, outlineMaterial, outlineColor, thickness,
            shadowEnabled, shadowColor, shadowOffset, shadowOpacity, shadowBlur);
    }

    /// <summary>
    /// Explicitly adds only the lightweight member adapters to a preconfigured backend.
    /// Camera, shader, hosts and the backend itself remain Builder/ENGINE-owned.
    /// </summary>
    [ContextMenu("显式接线现有合并后端")]
    public int CreateOrSynchronizeMergeAdapters()
    {
        SpriteOutlineMergeRenderer2D backend = MergeBackend;
        if (backend == null)
            return 0;

        var created = 0;
        SpriteOutline2D[] outlines = GetOwnedMembers();
        var adapters = new List<SpriteOutlineMergeMember2D>(outlines.Length);
        for (var index = 0; index < outlines.Length; index++)
        {
            SpriteOutline2D outline = outlines[index];
            SpriteOutlineMergeMember2D adapter = outline.GetComponent<SpriteOutlineMergeMember2D>();
            if (adapter == null)
            {
                adapter = outline.gameObject.AddComponent<SpriteOutlineMergeMember2D>();
                created++;
            }
            outline.SynchronizeMergeAdapter();
            adapters.Add(adapter);
        }
        backend.ReplaceMembersIfChanged(adapters);
        SynchronizeMergeBackendConfiguration();
        return created;
    }

    public SpriteOutlineMergeMember2D[] GetOwnedMergeAdapters()
    {
        SpriteOutline2D[] outlines = GetOwnedMembers();
        var adapters = new List<SpriteOutlineMergeMember2D>(outlines.Length);
        for (var index = 0; index < outlines.Length; index++)
        {
            SpriteOutlineMergeMember2D adapter = outlines[index].GetComponent<SpriteOutlineMergeMember2D>();
            if (adapter != null) adapters.Add(adapter);
        }
        return adapters.ToArray();
    }

    public void SynchronizeMergeBackendConfiguration()
    {
        SpriteOutlineMergeRenderer2D backend = MergeBackend;
        if (backend == null)
            return;
        backend.ConfigureFormalGroup(mergeOutline, outlineColor, thickness, mergeShadow,
            shadowEnabled, shadowColor, shadowOffset, shadowOpacity, shadowBlur);
    }

    [ContextMenu("重新应用到全部子素材")]
    public void ApplyToChildren()
    {
        SynchronizeFollowingMembers();
    }

    public SpriteOutline2D[] GetOwnedMembers()
    {
        SpriteOutline2D[] candidates = GetComponentsInChildren<SpriteOutline2D>(includeInactive);
        var owned = new List<SpriteOutline2D>(candidates.Length);
        for (var index = 0; index < candidates.Length; index++)
        {
            SpriteOutline2D member = candidates[index];
            if (member == null || member.NearestGroup != this || IsGeneratedMergeHost(member.gameObject.name))
                continue;
            if (directChildrenOnly && member.transform.parent != transform)
                continue;
            owned.Add(member);
        }
        return owned.ToArray();
    }

    private SpriteRenderer[] GetOwnedRenderers()
    {
        SpriteRenderer[] candidates = GetComponentsInChildren<SpriteRenderer>(includeInactive);
        var owned = new List<SpriteRenderer>(candidates.Length);
        for (var index = 0; index < candidates.Length; index++)
        {
            SpriteRenderer renderer = candidates[index];
            if (renderer == null || IsGeneratedMergeHost(renderer.gameObject.name))
                continue;
            if (directChildrenOnly && renderer.transform.parent != transform)
                continue;

            SpriteOutlineGroup2D nearest = ResolveNearestGroup(renderer.transform);
            if (nearest == this)
                owned.Add(renderer);
        }
        return owned.ToArray();
    }

    internal void ApplyDefaultStyle(SpriteOutlineStyle style)
    {
        if (style == null)
            return;
        outlineMaterial = style.OutlineMaterial;
        outlineColor = style.OutlineColor;
        thickness = SpriteOutline2D.ClampFinite(style.Thickness, 0f, SpriteOutline2D.MaxOutlineThickness, 0f);
        shadowEnabled = style.ShadowEnabled;
        shadowColor = SpriteOutline2D.SanitizeShadowColor(style.ShadowColor);
        shadowOffset = SpriteOutline2D.SanitizeShadowOffset(style.ShadowOffset);
        shadowOpacity = SpriteOutline2D.ClampFinite(style.ShadowOpacity, 0f, 1f, 0f);
        shadowBlur = SpriteOutline2D.ClampFinite(style.ShadowBlur, 0f, SpriteOutline2D.MaxShadowBlur, 0f);
    }

    private void Reset()
    {
        SpriteOutlineDefaults defaults = SpriteOutlineDefaults.LoadDefault();
        if (defaults != null)
            defaults.ApplyTo(this, defaultProfileId);
    }

    private void OnDidApplyAnimationProperties()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        thickness = SpriteOutline2D.ClampFinite(thickness, 0f, float.MaxValue, 0f);
        shadowColor = SpriteOutline2D.SanitizeShadowColor(shadowColor);
        shadowOffset = SpriteOutline2D.SanitizeShadowOffset(shadowOffset);
        shadowOpacity = SpriteOutline2D.ClampFinite(shadowOpacity, 0f, 1f, 0f);
        shadowBlur = SpriteOutline2D.ClampFinite(shadowBlur, 0f, SpriteOutline2D.MaxShadowBlur, 0f);
        SynchronizeFollowingMembers();
    }

    private static SpriteOutlineGroup2D ResolveNearestGroup(Transform start)
    {
        Transform current = start;
        while (current != null)
        {
            SpriteOutlineGroup2D group = current.GetComponent<SpriteOutlineGroup2D>();
            if (group != null)
                return group;
            current = current.parent;
        }
        return null;
    }

    private static bool IsGeneratedMergeHost(string objectName)
    {
        return !string.IsNullOrEmpty(objectName) &&
            (objectName.EndsWith("_Merged", System.StringComparison.Ordinal) ||
             objectName.EndsWith("_MergedShadow", System.StringComparison.Ordinal) ||
             objectName.EndsWith("_MergedOutline", System.StringComparison.Ordinal));
    }
}
