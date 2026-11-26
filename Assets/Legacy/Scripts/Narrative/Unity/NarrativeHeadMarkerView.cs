using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EatWhat.Narrative.Unity
{
    /// <summary>World-space drawing only. The caller owns kind, tint, hiding, fade and bob time.</summary>
    [DisallowMultipleComponent]
    public sealed class NarrativeHeadMarkerView : MonoBehaviour
    {
        [SerializeField] TMP_FontAsset fontAsset = null;
        [SerializeField] Material fontMaterial = null;
        [SerializeField] Camera worldCamera = null;
        const float markerDiameter = .85f;
        [SerializeField] string sortingLayerName = "LL_MidgroundFront";
        [SerializeField] int sortingOrder = 0;
        GameObject root;
        CanvasGroup group;
        Image circle;
        TextMeshProUGUI label;
        Texture2D texture;
        Sprite sprite;
        bool warned;

        public bool Apply(Transform anchor, string glyph, Color tint, float alpha, float bobWorldY)
        {
            if (!isActiveAndEnabled || anchor == null || !anchor.gameObject.activeInHierarchy ||
                !Finite(alpha) || alpha < 0 || alpha > 1 || !Finite(bobWorldY))
            { HideImmediate(); return false; }
            if (!Ensure()) { HideImmediate(); return false; }
            // Detached world root: it follows the anchor position, never the NPC's depth scale.
            root.transform.position = anchor.position + new Vector3(0, bobWorldY, 0);
            root.transform.localScale = Vector3.one * (markerDiameter / 128f);
            if (worldCamera != null) root.transform.rotation = worldCamera.transform.rotation;
            circle.color = new Color(.08f * tint.r, .1f * tint.g, .14f * tint.b, .92f * tint.a);
            label.text = glyph; label.color = tint; group.alpha = alpha;
            root.SetActive(true); return true;
        }
        bool Ensure()
        {
            if (root != null) return true;
            int layer = 0; bool found = false;
            foreach (var value in SortingLayer.layers) if (value.name == sortingLayerName) { layer = value.id; found = true; break; }
            if (fontAsset == null || !found || sortingOrder < short.MinValue || sortingOrder > short.MaxValue)
            {
                if (!warned) { warned = true; Debug.LogWarning("NarrativeHeadMarkerView: assign a font and an existing sorting layer/order."); }
                return false;
            }
            texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            texture.name = "HeadMarkerCircle_Runtime";
            var pixels = new Color32[64 * 64];
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            { float dx = x - 31.5f, dy = y - 31.5f; pixels[y * 64 + x] = new Color32(255, 255, 255, (byte)(dx * dx + dy * dy <= 31 * 31 ? 255 : 0)); }
            texture.SetPixels32(pixels); texture.Apply(false, true);
            sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f), 64);
            root = new GameObject("NarrativeHeadMarker_Runtime", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            root.SetActive(false);
            ((RectTransform)root.transform).sizeDelta = new Vector2(128, 128);
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true; canvas.sortingLayerID = layer; canvas.sortingOrder = sortingOrder; canvas.worldCamera = worldCamera;
            group = root.GetComponent<CanvasGroup>(); group.interactable = false; group.blocksRaycasts = false;
            var disk = new GameObject("Circle", typeof(RectTransform), typeof(Image)); disk.transform.SetParent(root.transform, false);
            Stretch((RectTransform)disk.transform); circle = disk.GetComponent<Image>(); circle.sprite = sprite; circle.type = Image.Type.Simple; circle.raycastTarget = false;
            var text = new GameObject("Glyph", typeof(RectTransform)); text.transform.SetParent(root.transform, false);
            Stretch((RectTransform)text.transform); label = text.AddComponent<TextMeshProUGUI>();
            label.fontSharedMaterial = fontMaterial != null ? fontMaterial : fontAsset.material;
            label.font = fontAsset;
            label.isOrthographic = true; label.fontSize = 80; label.alignment = TextAlignmentOptions.Center;
            label.richText = false; label.textWrappingMode = TextWrappingModes.NoWrap; label.raycastTarget = false;
            return true;
        }
        static void Stretch(RectTransform value)
        { value.anchorMin = Vector2.zero; value.anchorMax = Vector2.one; value.offsetMin = value.offsetMax = Vector2.zero; }
        static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        public void HideImmediate() { if (root != null) { group.alpha = 0; root.SetActive(false); } }
        public void Clear()
        {
            if (root != null) Destroy(root); root = null; circle = null; label = null; group = null;
            if (sprite != null) Destroy(sprite); sprite = null;
            if (texture != null) Destroy(texture); texture = null;
        }
        void OnDisable() { Clear(); }
        void OnDestroy() { Clear(); }
    }
}
