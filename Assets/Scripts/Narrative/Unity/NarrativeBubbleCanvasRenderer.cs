using EatWhat.Tools.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EatWhat.Narrative.Unity
{
    /// <summary>Runtime-owned overlay UI; all distances here are reference pixels, never street units.</summary>
    [DisallowMultipleComponent]
    public sealed class NarrativeBubbleCanvasRenderer : MonoBehaviour, INarrativeBubbleSurfaceFactory
    {
        [SerializeField, Tooltip("Explicit camera that renders the street. It is read only for projection.")]
        private Camera worldCamera = null;
        [SerializeField, Tooltip("Existing TMP font asset. This component never edits the asset.")]
        private TMP_FontAsset fontAsset = null;
        [SerializeField, Tooltip("Optional existing material; empty uses Font Asset's shared material.")]
        private Material fontMaterial = null;
        [SerializeField, Tooltip("Optional existing UI sprite; empty creates a plain rectangular placeholder.")]
        private Sprite background = null;
        [SerializeField, Tooltip("Maximum text width in 2560 x 1440 reference pixels; must be positive.")]
        private float maxTextWidth = 480f;
        [SerializeField, Tooltip("TMP font size in reference pixels; must be positive.")]
        private float fontSize = 28f;
        [SerializeField, Tooltip("Horizontal/vertical padding on each side, in reference pixels.")]
        private Vector2 padding = new Vector2(24f, 14f);
        [SerializeField, Tooltip("Screen-up offset above Head Anchor, in reference pixels.")]
        private float headOffset = 20f;
        [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.88f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField, Tooltip("Overlay Canvas sorting order; above street renderers, relative to other UI only.")]
        private int sortingOrder = 100;
        private Canvas runtimeCanvas;

        public INarrativeBubbleSurface Create(Transform head, string textKey, string text, out string diagnostic)
        {
            diagnostic = null;
            if (!isActiveAndEnabled || worldCamera == null || !worldCamera.isActiveAndEnabled || head == null || fontAsset == null)
            { diagnostic = "Renderer/camera/head/font unavailable."; return null; }
            if (!Positive(maxTextWidth) || !Positive(fontSize) || !Nonnegative(padding.x) ||
                !Nonnegative(padding.y) || !Finite(headOffset))
            { diagnostic = "Width/font size must be positive; padding nonnegative; layout fields finite."; return null; }
            if (runtimeCanvas == null)
            {
                var root = new GameObject("NarrativeBubbleCanvas_Runtime", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                runtimeCanvas = root.GetComponent<Canvas>();
                runtimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                runtimeCanvas.sortingOrder = sortingOrder;
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(2560f, 1440f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 1f;
                // No GraphicRaycaster: this overlay never consumes pointer input.
            }
            var go = new GameObject("NarrativeBubble_Runtime", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            go.SetActive(false);
            var rect = (RectTransform)go.transform;
            rect.SetParent(runtimeCanvas.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0f);
            var image = go.GetComponent<Image>();
            image.sprite = background; image.type = Image.Type.Simple; image.color = backgroundColor; image.raycastTarget = false;
            var group = go.GetComponent<CanvasGroup>();
            group.alpha = 0; group.interactable = false; group.blocksRaycasts = false;
            var labelObject = new GameObject("Text", typeof(RectTransform));
            labelObject.transform.SetParent(rect, false);
            var label = labelObject.AddComponent<TextMeshProUGUI>();
            // Bind shared material first, then font. Do not mutate either shared asset.
            label.fontSharedMaterial = fontMaterial != null ? fontMaterial : fontAsset.material;
            label.font = fontAsset;
            // The inactive parent defers TMP's UI Awake. Font assignment loads glyphs/material,
            // but preferred-size calculation would still use the pre-Awake 0.1 world-text scale.
            label.isOrthographic = true;
            label.fontSize = fontSize; label.color = textColor;
            label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            label.richText = false; label.textWrappingMode = TextWrappingModes.Normal;
            labelObject.AddComponent<LocalizedText>();
            LocalizedText.ConfigureContent(label, textKey, text);
            Vector2 preferred = label.GetPreferredValues(label.text, maxTextWidth, float.PositiveInfinity);
            rect.sizeDelta = new Vector2(Mathf.Min(maxTextWidth, preferred.x) + 2 * padding.x, preferred.y + 2 * padding.y);
            var textRect = label.rectTransform;
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = padding; textRect.offsetMax = -padding;
            go.SetActive(true);
            return new Surface(this, head, rect, group);
        }

        private sealed class Surface : INarrativeBubbleSurface
        {
            private readonly NarrativeBubbleCanvasRenderer owner;
            private readonly Transform head;
            private RectTransform rect;
            private readonly CanvasGroup group;
            public Surface(NarrativeBubbleCanvasRenderer owner, Transform head, RectTransform rect, CanvasGroup group)
            { this.owner = owner; this.head = head; this.rect = rect; this.group = group; }
            public bool Apply(float alpha, float risePixels)
            {
                if (owner == null || !owner.isActiveAndEnabled || owner.runtimeCanvas == null ||
                    owner.worldCamera == null || !owner.worldCamera.isActiveAndEnabled || head == null || rect == null) return false;
                Vector3 screen = owner.worldCamera.WorldToScreenPoint(head.position);
                Vector2 local = Vector2.zero;
                bool visible = head.gameObject.activeInHierarchy && screen.z > 0 &&
                    RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)owner.runtimeCanvas.transform, screen, null, out local);
                // Screen positions are converted into CanvasScaler reference units. No parent/lossy/depth scale is copied.
                if (visible)
                {
                    rect.anchoredPosition = local + Vector2.up * (owner.headOffset + risePixels);
                }
                group.alpha = visible ? alpha : 0;
                return true;
            }
            public void Dispose()
            { if (rect != null) { rect.gameObject.SetActive(false); Object.Destroy(rect.gameObject); rect = null; } }
        }
        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        private static bool Positive(float value) { return Finite(value) && value > 0; }
        private static bool Nonnegative(float value) { return Finite(value) && value >= 0; }
        private void OnDisable() { ReleaseCanvas(); }
        private void OnDestroy() { ReleaseCanvas(); }
        private void ReleaseCanvas()
        {
            if (runtimeCanvas == null) return;
            runtimeCanvas.gameObject.SetActive(false); Destroy(runtimeCanvas.gameObject); runtimeCanvas = null;
        }
    }
}
