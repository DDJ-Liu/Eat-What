using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EatWhat.Tools.Rendering
{
    /// <summary>
    /// Lab-first, explicit two-RT group compositor. Passes are scheduled at the
    /// selected camera's beginCameraRendering callback; hosts remain ordinary 2D renderers.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Eat What/Rendering/Sprite Outline Merge Renderer 2D")]
    public sealed class SpriteOutlineMergeRenderer2D : MonoBehaviour
    {
        public const int MaskPassIndex = 0;
        public const int CandidatePassIndex = 1;
        public const int ShadowDisplayPassIndex = 2;
        public const int OutlineDisplayPassIndex = 3;
        private const int BytesPerPixel = 4;
        private const int ResolutionQuantum = 8;
        // Material.SetShaderPassEnabled keys the ShaderLab LightMode tag, not Pass Name.
        private const string OffscreenLightMode = "SpriteOutlineMergeOffscreen";
        private const string ShadowDisplayLightMode = "Universal2D";
        private const string OutlineDisplayLightMode = "SRPDefaultUnlit";

        [Header("Explicit references")]
        [SerializeField] private Camera targetCamera = null;
        [SerializeField] private Shader mergeShader = null;
        [SerializeField] private MeshRenderer shadowHost = null;
        [SerializeField] private MeshFilter shadowHostMesh = null;
        [SerializeField] private MeshRenderer outlineHost = null;
        [SerializeField] private MeshFilter outlineHostMesh = null;
        [SerializeField] private List<SpriteOutlineMergeMember2D> members = new List<SpriteOutlineMergeMember2D>();

        [Header("Resolved group defaults")]
        [SerializeField] private bool groupOutlineEnabled = true;
        [SerializeField] private Color groupOutlineColor = Color.white;
        [SerializeField, Min(0f)] private float groupOutlineThicknessInSourcePixels = 1f;
        [SerializeField] private bool groupShadowEnabled = true;
        [SerializeField] private bool groupShadowEffectEnabled = true;
        [SerializeField] private Color groupShadowColor = Color.black;
        [SerializeField] private Vector2 groupShadowOffsetInSourcePixels = new Vector2(1f, -1f);
        [SerializeField, Range(0f, 1f)] private float groupShadowOpacity = 0.5f;
        [SerializeField, Min(0f)] private float groupShadowSoftnessInSourcePixels = 1f;
        [SerializeField, Min(64)] private int maximumTextureSize = 4096;

        private readonly List<MemberRuntime> activeMembers = new List<MemberRuntime>(16);
        private readonly List<MemberSnapshot> snapshots = new List<MemberSnapshot>(16);
        private readonly List<Material> memberMaterials = new List<Material>(16);
        private RenderTexture maskTexture;
        private RenderTexture candidateTexture;
        private Material shadowDisplayMaterial;
        private Material outlineDisplayMaterial;
        private Mesh unitQuad;
        private Mesh shadowDisplayQuad;
        private Mesh outlineDisplayQuad;
        private Material[] originalShadowMaterials;
        private Material[] originalOutlineMaterials;
        private MaterialPropertyBlock originalShadowBlock;
        private MaterialPropertyBlock originalOutlineBlock;
        private Mesh originalShadowMesh;
        private Mesh originalOutlineMesh;
        private bool hostStateCaptured;
        private bool subscribed;
        private bool hasSnapshot;
        private int lastStateHash;
        private Rect worldRect;
        private int redrawCount;
        private int allocationCountThisFrame;
        private int releaseCountThisFrame;
        private int peakLiveTextureCount;
        private string lastDirtyReason = "NotRendered";
        private string lastError = string.Empty;
        private int lastRenderedFrame = -1;

        public Camera TargetCamera { get { return targetCamera; } }
        public IReadOnlyList<SpriteOutlineMergeMember2D> Members { get { return members; } }
        public MeshRenderer ShadowHost { get { return shadowHost; } }
        public MeshRenderer OutlineHost { get { return outlineHost; } }
        public int OwnedRenderTextureCount { get { return (maskTexture == null ? 0 : 1) + (candidateTexture == null ? 0 : 1); } }
        public int PeakLiveRenderTextureCount { get { return peakLiveTextureCount; } }
        public int RenderTextureWidth { get { return maskTexture == null ? 0 : maskTexture.width; } }
        public int RenderTextureHeight { get { return maskTexture == null ? 0 : maskTexture.height; } }
        public long OwnedRenderTextureBytes { get { return (long)RenderTextureWidth * RenderTextureHeight * BytesPerPixel * OwnedRenderTextureCount; } }
        public int RedrawCount { get { return redrawCount; } }
        public int AllocationCountThisFrame { get { return allocationCountThisFrame; } }
        public int ReleaseCountThisFrame { get { return releaseCountThisFrame; } }
        public int MaterialInstanceCount { get { return memberMaterials.Count + (shadowDisplayMaterial == null ? 0 : 1) + (outlineDisplayMaterial == null ? 0 : 1); } }
        public string LastDirtyReason { get { return lastDirtyReason; } }
        public string LastError { get { return lastError; } }
        public int LastRenderedFrame { get { return lastRenderedFrame; } }
        public bool IsConfigured
        {
            get
            {
                return targetCamera != null && mergeShader != null && shadowHost != null && shadowHostMesh != null &&
                    outlineHost != null && outlineHostMesh != null;
            }
        }

        public void Configure(Camera camera, Shader shader, MeshRenderer configuredShadowHost,
            MeshFilter configuredShadowMesh, MeshRenderer configuredOutlineHost,
            MeshFilter configuredOutlineMesh, IList<SpriteOutlineMergeMember2D> configuredMembers)
        {
            targetCamera = camera;
            mergeShader = shader;
            shadowHost = configuredShadowHost;
            shadowHostMesh = configuredShadowMesh;
            outlineHost = configuredOutlineHost;
            outlineHostMesh = configuredOutlineMesh;
            members.Clear();
            if (configuredMembers != null)
            {
                for (var index = 0; index < configuredMembers.Count; index++)
                    if (configuredMembers[index] != null) members.Add(configuredMembers[index]);
            }
            Invalidate("Configuration");
        }

        public void ConfigureGroup(bool outlineEnabled, Color outlineColor, float outlineThickness,
            bool shadowEnabled, Color shadowColor, Vector2 shadowOffset, float shadowOpacity, float shadowSoftness)
        {
            ConfigureFormalGroup(outlineEnabled, outlineColor, outlineThickness, shadowEnabled,
                shadowEnabled, shadowColor, shadowOffset, shadowOpacity, shadowSoftness);
        }

        public bool ConfigureFormalGroup(bool outlineMergeEnabled, Color outlineColor, float outlineThickness,
            bool shadowMergeEnabled, bool shadowEffectEnabled, Color shadowColor, Vector2 shadowOffset,
            float shadowOpacity, float shadowSoftness)
        {
            Color sanitizedOutlineColor = SpriteOutlineMergeMember2D.SanitizeColor(outlineColor, Color.white);
            float sanitizedOutlineThickness = SpriteOutlineMergeMember2D.SanitizeNonNegative(outlineThickness, 1f);
            Color sanitizedShadowColor = SpriteOutlineMergeMember2D.SanitizeColor(shadowColor, Color.black);
            Vector2 sanitizedShadowOffset = SpriteOutlineMergeMember2D.SanitizeVector(shadowOffset);
            float sanitizedShadowOpacity = SpriteOutlineMergeMember2D.SanitizeRange(shadowOpacity, 0f, 1f, 0.5f);
            float sanitizedShadowSoftness = SpriteOutlineMergeMember2D.SanitizeNonNegative(shadowSoftness, 1f);
            if (groupOutlineEnabled == outlineMergeEnabled && groupOutlineColor == sanitizedOutlineColor &&
                Mathf.Approximately(groupOutlineThicknessInSourcePixels, sanitizedOutlineThickness) &&
                groupShadowEnabled == shadowMergeEnabled && groupShadowEffectEnabled == shadowEffectEnabled &&
                groupShadowColor == sanitizedShadowColor && groupShadowOffsetInSourcePixels == sanitizedShadowOffset &&
                Mathf.Approximately(groupShadowOpacity, sanitizedShadowOpacity) &&
                Mathf.Approximately(groupShadowSoftnessInSourcePixels, sanitizedShadowSoftness))
                return false;

            groupOutlineEnabled = outlineMergeEnabled;
            groupOutlineColor = sanitizedOutlineColor;
            groupOutlineThicknessInSourcePixels = sanitizedOutlineThickness;
            groupShadowEnabled = shadowMergeEnabled;
            groupShadowEffectEnabled = shadowEffectEnabled;
            groupShadowColor = sanitizedShadowColor;
            groupShadowOffsetInSourcePixels = sanitizedShadowOffset;
            groupShadowOpacity = sanitizedShadowOpacity;
            groupShadowSoftnessInSourcePixels = sanitizedShadowSoftness;
            Invalidate("GroupParameters");
            return true;
        }

        public bool ReplaceMembersIfChanged(IList<SpriteOutlineMergeMember2D> configuredMembers)
        {
            int configuredCount = configuredMembers == null ? 0 : configuredMembers.Count;
            var filtered = new List<SpriteOutlineMergeMember2D>(configuredCount);
            for (var index = 0; index < configuredCount; index++)
                if (configuredMembers[index] != null) filtered.Add(configuredMembers[index]);
            if (members.Count == filtered.Count)
            {
                var equal = true;
                for (var index = 0; index < members.Count; index++)
                    if (members[index] != filtered[index]) { equal = false; break; }
                if (equal) return false;
            }
            members.Clear();
            members.AddRange(filtered);
            Invalidate("MemberList");
            return true;
        }

        public bool IsReadyFor(SpriteOutlineMergeMember2D member)
        {
            return member != null && IsFormalOwnerActive() && isActiveAndEnabled && IsConfigured && string.IsNullOrEmpty(lastError) &&
                member.NearestOwner == this && members.Contains(member);
        }

        public void Invalidate(string reason)
        {
            hasSnapshot = false;
            lastDirtyReason = string.IsNullOrEmpty(reason) ? "Explicit" : reason;
        }

        public string ExportBudgetState()
        {
            return "rt=" + OwnedRenderTextureCount + "/2" +
                " size=" + RenderTextureWidth + "x" + RenderTextureHeight +
                " format=ARGB32 aa=1 bytes=" + OwnedRenderTextureBytes +
                " peak=" + peakLiveTextureCount + " allocFrame=" + allocationCountThisFrame +
                " releaseFrame=" + releaseCountThisFrame + " materials=" + MaterialInstanceCount +
                " redraws=" + redrawCount + " dirty=" + lastDirtyReason +
                " hosts=" + HostSummary() + " error=" + lastError;
        }

        internal static int RoundUpToEight(int value)
        {
            return Mathf.Max(ResolutionQuantum, ((Mathf.Max(1, value) + ResolutionQuantum - 1) / ResolutionQuantum) * ResolutionQuantum);
        }

        internal static long CalculateBudgetBytes(int width, int height)
        {
            return (long)width * height * BytesPerPixel * 2L;
        }

        internal static bool ResolveMode(OutlineMergeOverride mode, bool groupEnabled)
        {
            if (mode == OutlineMergeOverride.ForceOn) return true;
            if (mode == OutlineMergeOverride.ForceOff) return false;
            return groupEnabled;
        }

        private void OnEnable()
        {
            CaptureHostState();
            Subscribe();
            Invalidate("Enable");
        }

        private void OnDisable()
        {
            Unsubscribe();
            ReleaseResources();
            RestoreHostState();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            ReleaseResources();
            RestoreHostState();
        }

        private void OnValidate()
        {
            groupOutlineColor = SpriteOutlineMergeMember2D.SanitizeColor(groupOutlineColor, Color.white);
            groupOutlineThicknessInSourcePixels = SpriteOutlineMergeMember2D.SanitizeNonNegative(groupOutlineThicknessInSourcePixels, 1f);
            groupShadowColor = SpriteOutlineMergeMember2D.SanitizeColor(groupShadowColor, Color.black);
            groupShadowOffsetInSourcePixels = SpriteOutlineMergeMember2D.SanitizeVector(groupShadowOffsetInSourcePixels);
            groupShadowOpacity = SpriteOutlineMergeMember2D.SanitizeRange(groupShadowOpacity, 0f, 1f, 0.5f);
            groupShadowSoftnessInSourcePixels = SpriteOutlineMergeMember2D.SanitizeNonNegative(groupShadowSoftnessInSourcePixels, 1f);
            maximumTextureSize = Mathf.Max(64, maximumTextureSize);
            Invalidate("Validate");
        }

        private void Subscribe()
        {
            if (subscribed) return;
            RenderPipelineManager.beginCameraRendering += HandleBeginCameraRendering;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed) return;
            RenderPipelineManager.beginCameraRendering -= HandleBeginCameraRendering;
            subscribed = false;
        }

        private void HandleBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (!Application.isPlaying || camera != targetCamera || lastRenderedFrame == Time.frameCount) return;
            allocationCountThisFrame = 0;
            releaseCountThisFrame = 0;
            lastRenderedFrame = Time.frameCount;

            string error;
            if (!TryBuildRuntime(out error))
            {
                DisableHosts(error);
                return;
            }

            var currentHash = CalculateStateHash(camera);
            if (hasSnapshot && currentHash == lastStateHash)
            {
                lastDirtyReason = "CachedUnchanged";
                return;
            }

            if (!TryEnsureResources(camera, out error))
            {
                DisableHosts(error);
                return;
            }

            DrawOffscreen(context);
            ConfigureHosts();
            lastStateHash = currentHash;
            hasSnapshot = true;
            redrawCount++;
            lastDirtyReason = "StateOrResolutionChanged";
            lastError = string.Empty;
        }

        private bool TryBuildRuntime(out string error)
        {
            activeMembers.Clear();
            snapshots.Clear();
            error = string.Empty;
            if (!IsFormalOwnerActive())
            {
                error = "Formal group inactive.";
                return false;
            }
            if (targetCamera == null || mergeShader == null || shadowHost == null || shadowHostMesh == null ||
                outlineHost == null || outlineHostMesh == null)
            {
                error = "Explicit camera/shader/host references are incomplete.";
                return false;
            }
            if (!targetCamera.orthographic)
            {
                error = "Only orthographic cameras are supported by the Lab backend.";
                return false;
            }

            for (var index = 0; index < members.Count; index++)
            {
                var member = members[index];
                if (member == null) continue;
                var snapshot = member.CaptureSnapshot(this);
                snapshots.Add(snapshot);
                var renderer = member.SourceRenderer;
                if (!snapshot.Visible || !snapshot.CorrectOwner || renderer == null || renderer.sprite == null) continue;
                if (renderer == shadowHost || renderer == outlineHost) continue;
                if (renderer.sprite.packed && renderer.sprite.packingRotation != SpritePackingRotation.None)
                {
                    error = "Packed-rotated sprites are unsupported: " + renderer.name;
                    return false;
                }
                activeMembers.Add(new MemberRuntime(member, renderer));
            }
            if (activeMembers.Count == 0)
            {
                error = "No active members owned by this nearest group.";
                return false;
            }
            activeMembers.Sort(MemberRuntime.CompareStable);
            if (activeMembers[0].Renderer.sortingOrder < short.MinValue + 2)
            {
                error = "Sorting order has no room for two hosts below the minimum member.";
                return false;
            }
            for (var index = 1; index < activeMembers.Count; index++)
            {
                if (activeMembers[index].Renderer.sortingLayerID != activeMembers[0].Renderer.sortingLayerID)
                {
                    error = "Mixed sorting layers require separate merge groups.";
                    return false;
                }
            }
            var hostDomain = SortingDomainId(shadowHost);
            if (hostDomain != SortingDomainId(outlineHost))
            {
                error = "Display hosts belong to different SortingGroup domains.";
                return false;
            }
            for (var index = 0; index < activeMembers.Count; index++)
            {
                if (SortingDomainId(activeMembers[index].Renderer) != hostDomain)
                {
                    error = "Members and display hosts must share one SortingGroup domain.";
                    return false;
                }
            }
            return true;
        }

        private int CalculateStateHash(Camera camera)
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + camera.pixelWidth;
                hash = hash * 31 + camera.pixelHeight;
                hash = hash * 31 + camera.orthographicSize.GetHashCode();
                hash = hash * 31 + camera.aspect.GetHashCode();
                hash = hash * 31 + maximumTextureSize;
                hash = hash * 31 + groupOutlineEnabled.GetHashCode();
                hash = hash * 31 + groupOutlineColor.GetHashCode();
                hash = hash * 31 + groupOutlineThicknessInSourcePixels.GetHashCode();
                hash = hash * 31 + groupShadowEnabled.GetHashCode();
                hash = hash * 31 + groupShadowEffectEnabled.GetHashCode();
                hash = hash * 31 + groupShadowColor.GetHashCode();
                hash = hash * 31 + groupShadowOffsetInSourcePixels.GetHashCode();
                hash = hash * 31 + groupShadowOpacity.GetHashCode();
                hash = hash * 31 + groupShadowSoftnessInSourcePixels.GetHashCode();
                hash = hash * 31 + shadowHost.transform.localToWorldMatrix.GetHashCode();
                hash = hash * 31 + outlineHost.transform.localToWorldMatrix.GetHashCode();
                for (var index = 0; index < snapshots.Count; index++) hash = hash * 31 + snapshots[index].GetHashCode();
                for (var index = 0; index < activeMembers.Count; index++)
                {
                    hash = hash * 31 + activeMembers[index].Renderer.flipX.GetHashCode();
                    hash = hash * 31 + activeMembers[index].Renderer.flipY.GetHashCode();
                }
                return hash;
            }
        }

        private bool TryEnsureResources(Camera camera, out string error)
        {
            error = string.Empty;
            worldRect = CalculateWorldRect();
            var pixelsPerWorldY = camera.pixelHeight / Mathf.Max(0.0001f, camera.orthographicSize * 2f);
            var pixelsPerWorldX = camera.pixelWidth / Mathf.Max(0.0001f, camera.orthographicSize * 2f * camera.aspect);
            var width = RoundUpToEight(Mathf.CeilToInt(worldRect.width * pixelsPerWorldX));
            var height = RoundUpToEight(Mathf.CeilToInt(worldRect.height * pixelsPerWorldY));
            var deviceLimit = Mathf.Min(maximumTextureSize, SystemInfo.maxTextureSize);
            if (width > deviceLimit || height > deviceLimit)
            {
                error = "Required RT " + width + "x" + height + " exceeds limit " + deviceLimit + ".";
                return false;
            }

            EnsureTexture(ref maskTexture, width, height, "_Mask");
            EnsureTexture(ref candidateTexture, width, height, "_Candidates");
            EnsureMaterials();
            if (maskTexture == null || candidateTexture == null || !maskTexture.IsCreated() || !candidateTexture.IsCreated())
            {
                error = "Two-RT allocation failed.";
                return false;
            }
            peakLiveTextureCount = Mathf.Max(peakLiveTextureCount, OwnedRenderTextureCount);
            if (OwnedRenderTextureCount > 2)
            {
                error = "Two-RT invariant violated.";
                return false;
            }
            return true;
        }

        private Rect CalculateWorldRect()
        {
            var bounds = activeMembers[0].Renderer.bounds;
            for (var index = 1; index < activeMembers.Count; index++) bounds.Encapsulate(activeMembers[index].Renderer.bounds);

            var outlinePixels = 0f;
            for (var index = 0; index < activeMembers.Count; index++)
            {
                var runtime = activeMembers[index];
                if (runtime.Member.ResolveOutlineEnabled(groupOutlineEnabled))
                    outlinePixels = Mathf.Max(outlinePixels, runtime.ResolveOutlineThickness(groupOutlineThicknessInSourcePixels));
            }
            var shadowPixels = HasMergedShadowContributor()
                ? Mathf.Max(Mathf.Abs(groupShadowOffsetInSourcePixels.x), Mathf.Abs(groupShadowOffsetInSourcePixels.y)) + groupShadowSoftnessInSourcePixels
                : 0f;
            var marginPixels = Mathf.Max(outlinePixels, shadowPixels);
            var maxWorldPixel = 0f;
            for (var index = 0; index < activeMembers.Count; index++)
            {
                var runtime = activeMembers[index];
                var ppu = Mathf.Max(1f, runtime.Renderer.sprite.pixelsPerUnit);
                maxWorldPixel = Mathf.Max(maxWorldPixel, runtime.Renderer.transform.TransformVector(Vector3.right / ppu).magnitude);
                maxWorldPixel = Mathf.Max(maxWorldPixel, runtime.Renderer.transform.TransformVector(Vector3.up / ppu).magnitude);
            }
            var margin = Mathf.Max(0.0001f, marginPixels * maxWorldPixel);
            return Rect.MinMaxRect(bounds.min.x - margin, bounds.min.y - margin, bounds.max.x + margin, bounds.max.y + margin);
        }

        private void DrawOffscreen(ScriptableRenderContext context)
        {
            var command = CommandBufferPool.Get("Sprite Outline Merge 2D");
            try
            {
                var projection = Matrix4x4.Ortho(worldRect.xMin, worldRect.xMax, worldRect.yMin, worldRect.yMax, -1000f, 1000f);
                command.SetViewProjectionMatrices(Matrix4x4.identity, projection);
                command.SetRenderTarget(maskTexture);
                command.ClearRenderTarget(false, true, Color.clear);
                for (var index = 0; index < activeMembers.Count; index++)
                {
                    var runtime = activeMembers[index];
                    var material = GetMemberMaterial(index);
                    ConfigureMemberMaterial(material, runtime, true);
                    command.DrawMesh(unitQuad, runtime.ExpandedMatrix(0f), material, 0, MaskPassIndex);
                }

                command.SetRenderTarget(candidateTexture);
                command.ClearRenderTarget(false, true, Color.clear);
                for (var index = 0; index < activeMembers.Count; index++)
                {
                    var runtime = activeMembers[index];
                    if (!runtime.Member.ResolveOutlineEnabled(groupOutlineEnabled) || runtime.Member.MergeOutline == OutlineMergeOverride.ForceOff) continue;
                    var material = GetMemberMaterial(index);
                    ConfigureMemberMaterial(material, runtime, false);
                    material.SetTexture("_EntityMaskTex", maskTexture);
                    material.SetVector("_GroupWorldRect", new Vector4(worldRect.xMin, worldRect.yMin, worldRect.width, worldRect.height));
                    var margin = runtime.SourcePixelWorldSize * runtime.ResolveOutlineThickness(groupOutlineThicknessInSourcePixels);
                    command.DrawMesh(unitQuad, runtime.ExpandedMatrix(margin), material, 0, CandidatePassIndex);
                }
                command.SetViewProjectionMatrices(targetCamera.worldToCameraMatrix, targetCamera.projectionMatrix);
                context.ExecuteCommandBuffer(command);
            }
            finally
            {
                command.Clear();
                CommandBufferPool.Release(command);
            }
        }

        private void ConfigureMemberMaterial(Material material, MemberRuntime runtime, bool maskPass)
        {
            var sprite = runtime.Renderer.sprite;
            var texture = sprite.texture;
            var textureRect = sprite.textureRect;
            var localTextureRect = CalculateTextureLocalRect(sprite, runtime.Renderer.flipX, runtime.Renderer.flipY);
            material.SetTexture("_MainTex", texture);
            material.SetMatrix("_WorldToSprite", runtime.Renderer.transform.worldToLocalMatrix);
            material.SetVector("_SpriteLocalRect", new Vector4(
                localTextureRect.xMin, localTextureRect.yMin, localTextureRect.width, localTextureRect.height));
            material.SetVector("_SpriteUvRect", new Vector4(
                textureRect.xMin / texture.width, textureRect.yMin / texture.height,
                textureRect.width / texture.width, textureRect.height / texture.height));
            material.SetVector("_SpriteFlip", new Vector4(runtime.Renderer.flipX ? 1f : 0f, runtime.Renderer.flipY ? 1f : 0f, 0f, 0f));
            material.SetColor("_RendererColor", runtime.Renderer.color);
            material.SetFloat("_ShadowContributor", runtime.Member.ResolveShadowEnabled(groupShadowEnabled) &&
                runtime.Member.MergeShadow != OutlineMergeOverride.ForceOff ? 1f : 0f);
            material.SetColor("_OutlineColor", runtime.ResolveOutlineColor(groupOutlineColor));
            material.SetFloat("_OutlineThickness", runtime.ResolveOutlineThickness(groupOutlineThicknessInSourcePixels));
            if (!maskPass) material.SetFloat("_SourcePixelWorld", runtime.SourcePixelWorldSize);
        }

        private static Rect CalculateTextureLocalRect(Sprite sprite, bool flipX, bool flipY)
        {
            // textureRectOffset is measured from the source Sprite rect before packing.
            // Mapping that cropped rectangle, rather than the full sprite.bounds canvas,
            // preserves Tight alpha placement and keeps one texture texel at 1 / PPU.
            var pixelsPerUnit = Mathf.Max(1f, sprite.pixelsPerUnit);
            var size = sprite.textureRect.size / pixelsPerUnit;
            var minimum = (sprite.textureRectOffset - sprite.pivot) / pixelsPerUnit;
            if (flipX) minimum.x = -minimum.x - size.x;
            if (flipY) minimum.y = -minimum.y - size.y;
            return new Rect(minimum, size);
        }

        private void ConfigureHosts()
        {
            var worldZ = transform.position.z;
            ConfigureHost(shadowHost, shadowHostMesh, shadowDisplayMaterial, shadowDisplayQuad, worldRect, worldZ,
                activeMembers[0].Renderer.sortingLayerID, activeMembers[0].Renderer.sortingOrder - 2);
            ConfigureHost(outlineHost, outlineHostMesh, outlineDisplayMaterial, outlineDisplayQuad, worldRect, worldZ,
                activeMembers[0].Renderer.sortingLayerID, activeMembers[0].Renderer.sortingOrder - 1);

            shadowDisplayMaterial.SetTexture("_EntityMaskTex", maskTexture);
            shadowDisplayMaterial.SetColor("_GroupShadowColor", groupShadowColor);
            var reference = activeMembers[0];
            var ppu = Mathf.Max(1f, reference.Renderer.sprite.pixelsPerUnit);
            var worldOffset = reference.Renderer.transform.TransformVector(
                new Vector3(groupShadowOffsetInSourcePixels.x / ppu, groupShadowOffsetInSourcePixels.y / ppu, 0f));
            shadowDisplayMaterial.SetVector("_GroupShadowOffset", new Vector4(
                worldOffset.x / worldRect.width, worldOffset.y / worldRect.height, 0f, 0f));
            var softnessWorld = reference.SourcePixelWorldSize * groupShadowSoftnessInSourcePixels;
            var softnessRtPixels = softnessWorld * Mathf.Max(
                maskTexture.width / worldRect.width, maskTexture.height / worldRect.height);
            shadowDisplayMaterial.SetFloat("_GroupShadowOpacity",
                groupShadowEffectEnabled && HasMergedShadowContributor() ? groupShadowOpacity : 0f);
            shadowDisplayMaterial.SetFloat("_GroupShadowSoftness", softnessRtPixels);
            outlineDisplayMaterial.SetTexture("_EntityMaskTex", maskTexture);
            outlineDisplayMaterial.SetTexture("_CandidateTex", candidateTexture);
            shadowHost.enabled = true;
            outlineHost.enabled = true;
        }

        private static void ConfigureHost(MeshRenderer renderer, MeshFilter filter, Material material,
            Mesh displayQuad, Rect rectangle, float worldZ, int sortingLayerId, int sortingOrder)
        {
            UpdateDisplayQuad(displayQuad, renderer.transform.worldToLocalMatrix, rectangle, worldZ);
            filter.sharedMesh = displayQuad;
            renderer.sharedMaterial = material;
            renderer.sortingLayerID = sortingLayerId;
            renderer.sortingOrder = sortingOrder;
        }

        private void EnsureTexture(ref RenderTexture texture, int width, int height, string suffix)
        {
            if (texture != null && texture.width == width && texture.height == height && texture.IsCreated()) return;
            ReleaseTexture(ref texture);
            texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                name = name + suffix,
                antiAliasing = 1,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false,
                hideFlags = HideFlags.HideAndDontSave
            };
            if (!texture.Create())
            {
                DestroyRuntimeObject(texture);
                texture = null;
                return;
            }
            allocationCountThisFrame++;
        }

        private void EnsureMaterials()
        {
            if (unitQuad == null) unitQuad = CreateUnitQuad();
            if (shadowDisplayQuad == null) shadowDisplayQuad = CreateDisplayQuad("ShadowDisplayQuad");
            if (outlineDisplayQuad == null) outlineDisplayQuad = CreateDisplayQuad("OutlineDisplayQuad");
            if (shadowDisplayMaterial == null)
            {
                shadowDisplayMaterial = CreateMaterial("ShadowDisplay");
                ConfigureDisplayLightModes(shadowDisplayMaterial, true);
            }
            if (outlineDisplayMaterial == null)
            {
                outlineDisplayMaterial = CreateMaterial("OutlineDisplay");
                ConfigureDisplayLightModes(outlineDisplayMaterial, false);
            }
            while (memberMaterials.Count < activeMembers.Count) memberMaterials.Add(CreateMaterial("Member" + memberMaterials.Count));
        }

        private Material GetMemberMaterial(int index)
        {
            return memberMaterials[index];
        }

        private Material CreateMaterial(string suffix)
        {
            return new Material(mergeShader) { name = name + "_" + suffix, hideFlags = HideFlags.HideAndDontSave };
        }

        private static void ConfigureDisplayLightModes(Material material, bool shadowRole)
        {
            material.SetShaderPassEnabled(OffscreenLightMode, false);
            material.SetShaderPassEnabled(ShadowDisplayLightMode, shadowRole);
            material.SetShaderPassEnabled(OutlineDisplayLightMode, !shadowRole);
        }

        private static Mesh CreateUnitQuad()
        {
            var mesh = new Mesh { name = "SpriteOutlineMerge_UnitQuad", hideFlags = HideFlags.HideAndDontSave };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f)
            };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.UploadMeshData(true);
            return mesh;
        }

        private Mesh CreateDisplayQuad(string suffix)
        {
            var mesh = new Mesh { name = name + "_" + suffix, hideFlags = HideFlags.HideAndDontSave };
            mesh.vertices = new[] { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            return mesh;
        }

        private static void UpdateDisplayQuad(Mesh mesh, Matrix4x4 worldToLocal, Rect rectangle, float worldZ)
        {
            // Keep the hidden host Transform untouched. Converting the desired world-space
            // rectangle into that Transform's local mesh space remains exact under scaled,
            // rotated, non-uniform, or sheared parent hierarchies.
            mesh.vertices = new[]
            {
                worldToLocal.MultiplyPoint3x4(new Vector3(rectangle.xMin, rectangle.yMin, worldZ)),
                worldToLocal.MultiplyPoint3x4(new Vector3(rectangle.xMax, rectangle.yMin, worldZ)),
                worldToLocal.MultiplyPoint3x4(new Vector3(rectangle.xMax, rectangle.yMax, worldZ)),
                worldToLocal.MultiplyPoint3x4(new Vector3(rectangle.xMin, rectangle.yMax, worldZ))
            };
            mesh.RecalculateBounds();
        }

        private void CaptureHostState()
        {
            if (hostStateCaptured || shadowHost == null || outlineHost == null) return;
            originalShadowMaterials = shadowHost.sharedMaterials;
            originalOutlineMaterials = outlineHost.sharedMaterials;
            originalShadowBlock = new MaterialPropertyBlock();
            originalOutlineBlock = new MaterialPropertyBlock();
            shadowHost.GetPropertyBlock(originalShadowBlock);
            outlineHost.GetPropertyBlock(originalOutlineBlock);
            originalShadowMesh = shadowHostMesh == null ? null : shadowHostMesh.sharedMesh;
            originalOutlineMesh = outlineHostMesh == null ? null : outlineHostMesh.sharedMesh;
            hostStateCaptured = true;
        }

        private void RestoreHostState()
        {
            if (!hostStateCaptured) return;
            if (shadowHost != null)
            {
                shadowHost.sharedMaterials = originalShadowMaterials;
                shadowHost.SetPropertyBlock(originalShadowBlock);
            }
            if (outlineHost != null)
            {
                outlineHost.sharedMaterials = originalOutlineMaterials;
                outlineHost.SetPropertyBlock(originalOutlineBlock);
            }
            if (shadowHostMesh != null) shadowHostMesh.sharedMesh = originalShadowMesh;
            if (outlineHostMesh != null) outlineHostMesh.sharedMesh = originalOutlineMesh;
            hostStateCaptured = false;
        }

        private void DisableHosts(string error)
        {
            lastError = error;
            if (shadowHost != null) shadowHost.enabled = false;
            if (outlineHost != null) outlineHost.enabled = false;
        }

        private string HostSummary()
        {
            return shadowHost == null || outlineHost == null ? "missing" :
                shadowHost.sortingLayerID + ":" + shadowHost.sortingOrder + "," +
                outlineHost.sortingLayerID + ":" + outlineHost.sortingOrder;
        }

        private bool HasMergedShadowContributor()
        {
            for (var index = 0; index < activeMembers.Count; index++)
            {
                var member = activeMembers[index].Member;
                if (member.MergeShadow != OutlineMergeOverride.ForceOff && member.ResolveShadowEnabled(groupShadowEnabled))
                    return true;
            }
            return false;
        }

        private static int SortingDomainId(Renderer renderer)
        {
            if (renderer == null) return 0;
            var group = renderer.GetComponentInParent<SortingGroup>();
            return group == null ? 0 : group.GetInstanceID();
        }

        private bool IsFormalOwnerActive()
        {
            SpriteOutlineGroup2D formalOwner = GetComponent<SpriteOutlineGroup2D>();
            return formalOwner == null || formalOwner.isActiveAndEnabled;
        }

        private void ReleaseResources()
        {
            ReleaseTexture(ref maskTexture);
            ReleaseTexture(ref candidateTexture);
            for (var index = 0; index < memberMaterials.Count; index++) DestroyRuntimeObject(memberMaterials[index]);
            memberMaterials.Clear();
            DestroyRuntimeObject(shadowDisplayMaterial);
            DestroyRuntimeObject(outlineDisplayMaterial);
            DestroyRuntimeObject(unitQuad);
            DestroyRuntimeObject(shadowDisplayQuad);
            DestroyRuntimeObject(outlineDisplayQuad);
            shadowDisplayMaterial = null;
            outlineDisplayMaterial = null;
            unitQuad = null;
            shadowDisplayQuad = null;
            outlineDisplayQuad = null;
            hasSnapshot = false;
        }

        private void ReleaseTexture(ref RenderTexture texture)
        {
            if (texture == null) return;
            texture.Release();
            DestroyRuntimeObject(texture);
            texture = null;
            releaseCountThisFrame++;
        }

        private static void DestroyRuntimeObject(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }

        private struct MemberRuntime
        {
            public readonly SpriteOutlineMergeMember2D Member;
            public readonly SpriteRenderer Renderer;

            public MemberRuntime(SpriteOutlineMergeMember2D member, SpriteRenderer renderer)
            {
                Member = member;
                Renderer = renderer;
            }

            public float SourcePixelWorldSize
            {
                get
                {
                    var ppu = Mathf.Max(1f, Renderer.sprite.pixelsPerUnit);
                    return Mathf.Max(Renderer.transform.TransformVector(Vector3.right / ppu).magnitude,
                        Renderer.transform.TransformVector(Vector3.up / ppu).magnitude);
                }
            }

            public Color ResolveOutlineColor(Color groupColor)
            {
                return Member.ResolveOutlineColor(groupColor);
            }

            public float ResolveOutlineThickness(float groupThickness)
            {
                return Member.ResolveOutlineThickness(groupThickness);
            }

            public Matrix4x4 ExpandedMatrix(float margin)
            {
                var bounds = Renderer.bounds;
                return Matrix4x4.TRS(bounds.center, Quaternion.identity,
                    new Vector3(bounds.size.x + margin * 2f, bounds.size.y + margin * 2f, 1f));
            }

            public static int CompareStable(MemberRuntime left, MemberRuntime right)
            {
                var layer = left.Renderer.sortingLayerID.CompareTo(right.Renderer.sortingLayerID);
                if (layer != 0) return layer;
                var order = left.Renderer.sortingOrder.CompareTo(right.Renderer.sortingOrder);
                if (order != 0) return order;
                return left.Renderer.GetInstanceID().CompareTo(right.Renderer.GetInstanceID());
            }
        }
    }
}
