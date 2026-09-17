using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SpriteOutlineStyle
{
    [SerializeField] private Material outlineMaterial = null;
    [SerializeField, ColorUsage(true, true)] private Color outlineColor = new Color32(48, 35, 31, 255);
    [SerializeField, Range(0f, SpriteOutline2D.MaxOutlineThickness)] private float thickness = 6f;
    [SerializeField] private bool shadowEnabled = false;
    [SerializeField, ColorUsage(false, true)] private Color shadowColor = new Color(0f, 0f, 0f, 1f);
    [SerializeField] private Vector2 shadowOffset = Vector2.zero;
    [SerializeField, Range(0f, 1f)] private float shadowOpacity = 0f;
    [SerializeField, Range(0f, SpriteOutline2D.MaxShadowBlur)] private float shadowBlur = 0f;

    public Material OutlineMaterial { get { return outlineMaterial; } }
    public Color OutlineColor { get { return outlineColor; } }
    public float Thickness { get { return thickness; } }
    public bool ShadowEnabled { get { return shadowEnabled; } }
    public Color ShadowColor { get { return shadowColor; } }
    public Vector2 ShadowOffset { get { return shadowOffset; } }
    public float ShadowOpacity { get { return shadowOpacity; } }
    public float ShadowBlur { get { return shadowBlur; } }

    public void Configure(Material material, Color color, float widthInSourcePixels,
        bool enableShadow, Color configuredShadowColor, Vector2 offsetInSourcePixels,
        float opacity, float blurInSourcePixels)
    {
        outlineMaterial = material;
        outlineColor = color;
        thickness = SpriteOutline2D.ClampFinite(widthInSourcePixels, 0f, SpriteOutline2D.MaxOutlineThickness, 0f);
        shadowEnabled = enableShadow;
        shadowColor = SpriteOutline2D.SanitizeShadowColor(configuredShadowColor);
        shadowOffset = SpriteOutline2D.SanitizeShadowOffset(offsetInSourcePixels);
        shadowOpacity = SpriteOutline2D.ClampFinite(opacity, 0f, 1f, 0f);
        shadowBlur = SpriteOutline2D.ClampFinite(blurInSourcePixels, 0f, SpriteOutline2D.MaxShadowBlur, 0f);
    }
}

[Serializable]
public sealed class SpriteOutlineDefaultEntry
{
    [SerializeField] private string roleId = string.Empty;
    [SerializeField] private SpriteOutlineStyle style = new SpriteOutlineStyle();

    public string RoleId { get { return roleId; } }
    public SpriteOutlineStyle Style { get { return style; } }

    public void Configure(string configuredRoleId, Material material, Color color, float thickness)
    {
        roleId = configuredRoleId ?? string.Empty;
        if (style == null)
            style = new SpriteOutlineStyle();
        style.Configure(material, color, thickness, false, Color.black, Vector2.zero, 0f, 0f);
    }
}

[CreateAssetMenu(fileName = "SpriteOutlineDefaults", menuName = "Eat What/Rendering/Sprite Outline Defaults")]
public sealed class SpriteOutlineDefaults : ScriptableObject
{
    public const string GenericProfileId = "Generic";
    public const string ResourcePath = "SpriteOutlineDefaults";

    [SerializeField] private SpriteOutlineStyle generic = new SpriteOutlineStyle();
    [SerializeField] private List<SpriteOutlineDefaultEntry> roleDefaults = new List<SpriteOutlineDefaultEntry>();

    public SpriteOutlineStyle Generic { get { return generic; } }
    public IReadOnlyList<SpriteOutlineDefaultEntry> RoleDefaults { get { return roleDefaults; } }

    public static SpriteOutlineDefaults LoadDefault()
    {
        return Resources.Load<SpriteOutlineDefaults>(ResourcePath);
    }

    public bool TryGetStyle(string roleId, out SpriteOutlineStyle style)
    {
        if (!string.IsNullOrEmpty(roleId) && roleId != GenericProfileId)
        {
            for (var index = 0; index < roleDefaults.Count; index++)
            {
                SpriteOutlineDefaultEntry entry = roleDefaults[index];
                if (entry != null && string.Equals(entry.RoleId, roleId, StringComparison.Ordinal))
                {
                    style = entry.Style;
                    return style != null;
                }
            }
        }

        style = generic;
        return style != null;
    }

    public void ConfigureGeneric(Material material, Color color, float thickness)
    {
        if (generic == null)
            generic = new SpriteOutlineStyle();

        generic.Configure(material, color, thickness, false, Color.black, Vector2.zero, 0f, 0f);
    }

    public void SetRole(string roleId, Material material, Color color, float thickness)
    {
        if (string.IsNullOrEmpty(roleId) || roleId == GenericProfileId)
        {
            ConfigureGeneric(material, color, thickness);
            return;
        }

        for (var index = 0; index < roleDefaults.Count; index++)
        {
            SpriteOutlineDefaultEntry existing = roleDefaults[index];
            if (existing != null && string.Equals(existing.RoleId, roleId, StringComparison.Ordinal))
            {
                existing.Configure(roleId, material, color, thickness);
                return;
            }
        }

        var entry = new SpriteOutlineDefaultEntry();
        entry.Configure(roleId, material, color, thickness);
        roleDefaults.Add(entry);
    }

    public bool ApplyTo(SpriteOutline2D outline, string roleId)
    {
        SpriteOutlineStyle style;
        if (outline == null || !TryGetStyle(roleId, out style))
            return false;

        outline.ApplyDefaultStyle(style);
        return true;
    }

    public bool ApplyTo(SpriteOutlineGroup2D group, string roleId)
    {
        SpriteOutlineStyle style;
        if (group == null || !TryGetStyle(roleId, out style))
            return false;

        group.ApplyDefaultStyle(style);
        return true;
    }
}
