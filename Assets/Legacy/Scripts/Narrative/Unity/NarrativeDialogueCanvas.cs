using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EatWhat.Narrative.Unity
{
    // Reserved capability only. This version has no blur implementation or blackout transition.
    public interface INarrativeSceneBlur
    {
        void RequestBlur(object owner, float amount, double seconds);
        void WithdrawBlur(object owner, double seconds);
    }

    /// <summary>Portable, runtime-owned passive HUD layers; no scene camera or input routing.</summary>
    [DisallowMultipleComponent]
    public sealed class NarrativeDialogueCanvas : MonoBehaviour
    {
        [SerializeField, Tooltip("Bind the same existing font asset used by the bubble renderer; never edited here.")]
        private TMP_FontAsset fontAsset = null;
        [SerializeField, Tooltip("Optional existing shared material; empty uses the font asset material.")]
        private Material fontMaterial = null;
        [SerializeField, Tooltip("Overlay order. Keep above the bubble canvas (current Street order 100).")]
        private int sortingOrder = 200;
        [SerializeField] private Canvas authoredCanvas = null;
        [SerializeField] private RectTransform authoredDimLayer = null;
        [SerializeField] private Image authoredDimImage = null;
        [SerializeField] private RectTransform authoredPortraitLayer = null;
        [SerializeField] private RectTransform authoredDialogueLayer = null;
        private Canvas runtimeCanvas;
        private bool ownsCanvas;
        private Image dimImage;
        private float appliedAmount; // Last rendered value only; interpolation/request ownership stays in the dimmer.

        public Canvas RootCanvas { get { return runtimeCanvas; } }
        public RectTransform DimLayer { get; private set; }
        public RectTransform PortraitLayer { get; private set; }
        public RectTransform DialogueLayer { get; private set; }
        public TMP_FontAsset FontAsset { get { return fontAsset; } }
        public Material SharedFontMaterial { get { return fontMaterial != null ? fontMaterial : fontAsset == null ? null : fontAsset.material; } }

        public void ConfigureFont(TMP_FontAsset sharedFont, Material sharedMaterial = null)
        {
            if (sharedFont == null) throw new ArgumentNullException(nameof(sharedFont));
            fontAsset = sharedFont; fontMaterial = sharedMaterial;
        }

        public void EnsureCreated()
        {
            if (runtimeCanvas != null) return;
            if (authoredCanvas != null || authoredDimLayer != null || authoredDimImage != null || authoredPortraitLayer != null || authoredDialogueLayer != null)
            {
                if (authoredCanvas == null || authoredDimLayer == null || authoredDimImage == null || authoredPortraitLayer == null || authoredDialogueLayer == null ||
                    authoredCanvas.renderMode != RenderMode.ScreenSpaceOverlay || !authoredCanvas.transform.IsChildOf(transform) || authoredDimLayer.parent != authoredCanvas.transform ||
                    authoredPortraitLayer.parent != authoredCanvas.transform || authoredDialogueLayer.parent != authoredCanvas.transform ||
                    !authoredDimImage.transform.IsChildOf(authoredDimLayer) ||
                    authoredDimLayer.GetSiblingIndex() >= authoredPortraitLayer.GetSiblingIndex() || authoredPortraitLayer.GetSiblingIndex() >= authoredDialogueLayer.GetSiblingIndex())
                    throw new InvalidOperationException("Assign the complete local authored Canvas / Dim Image / Dim, Portraits, Dialogue layers in that sibling order.");
                runtimeCanvas = authoredCanvas; DimLayer = authoredDimLayer; dimImage = authoredDimImage;
                PortraitLayer = authoredPortraitLayer; DialogueLayer = authoredDialogueLayer; ownsCanvas = false;
                runtimeCanvas.enabled = true; dimImage.raycastTarget = false; ApplyDimming(appliedAmount); return;
            }
            if (sortingOrder < short.MinValue || sortingOrder > short.MaxValue)
                throw new InvalidOperationException("Canvas sorting order must fit Unity's signed 16-bit range.");
            var root = new GameObject("NarrativeDialogueCanvas_Runtime", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false);
            runtimeCanvas = root.GetComponent<Canvas>();
            ownsCanvas = true;
            runtimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            runtimeCanvas.sortingOrder = sortingOrder;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2560f, 1440f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            DimLayer = Layer("Dimming", root.transform);
            dimImage = DimLayer.gameObject.AddComponent<Image>();
            dimImage.color = new Color(0, 0, 0, appliedAmount); dimImage.raycastTarget = false;
            var passive = DimLayer.gameObject.AddComponent<CanvasGroup>();
            passive.interactable = false; passive.blocksRaycasts = false;
            PortraitLayer = Layer("Portraits", root.transform); // Intentionally empty in 0.1.4.
            DialogueLayer = Layer("Dialogue", root.transform);
            // No GraphicRaycaster, EventSystem or UGUI input controls are created.
        }

        static RectTransform Layer(string name, Transform parent)
        {
            var child = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)child.transform; rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        internal void ApplyDimming(float amount)
        { appliedAmount = amount; if (dimImage != null) dimImage.color = new Color(0, 0, 0, amount); }

        private void OnEnable() { EnsureCreated(); }
        private void OnDisable() { ReleaseCanvas(); }
        private void OnDestroy() { ReleaseCanvas(); }
        private void ReleaseCanvas()
        {
            if (runtimeCanvas != null && ownsCanvas)
            { runtimeCanvas.gameObject.SetActive(false); Destroy(runtimeCanvas.gameObject); }
            else if (runtimeCanvas != null) runtimeCanvas.enabled = false;
            runtimeCanvas = null; dimImage = null; DimLayer = PortraitLayer = DialogueLayer = null;
            ownsCanvas = false;
        }
    }
}
