using System;
using EatWhat.Toolkit.Input;
using EatWhat.Tools.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EatWhat.Tools
{
    /// <summary>Passive screen-space prompt. Caller selects the target; this never reads Interact.</summary>
    [DisallowMultipleComponent]
    public sealed class InteractionPrompt : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera = null;
        [SerializeField] private MonoBehaviour inputProvider = null;
        [SerializeField] private TMP_FontAsset fontAsset = null;
        [SerializeField] private Material fontMaterial = null;
        [SerializeField, Min(0)] private float fadeInTime = .08f;
        [SerializeField, Min(0)] private float fadeOutTime = .08f;
        [SerializeField] private Vector2 anchorPivot = new Vector2(0, .5f);
        [SerializeField] private string keyboardKeyCaption = "E";
        [SerializeField] private string gamepadConfirmCaption = "A";
        [SerializeField] private int sortingOrder = 150;
        private IGameInput input;
        private InteractionPromptState state;
        private Canvas canvas;
        private RectTransform panel;
        private CanvasGroup group;
        private TextMeshProUGUI keyLabel, actionLabel;
        private Texture2D roundedTexture;
        private Sprite roundedSprite;
        public string LastDiagnostic { get; private set; }
        public Transform Target { get { return state == null ? null : state.Target as Transform; } }
        public void ConfigureInput(IGameInput value) { input = value; }

        public bool Show(Transform anchor, string localizationKey)
        {
            if (!isActiveAndEnabled || anchor == null || worldCamera == null || fontAsset == null)
            { LastDiagnostic = "Prompt/anchor/world camera/font unavailable."; return false; }
            if (!Finite(fadeInTime) || fadeInTime < 0 || !Finite(fadeOutTime) || fadeOutTime < 0 ||
                !Finite(anchorPivot.x) || !Finite(anchorPivot.y) || anchorPivot.x < 0 || anchorPivot.x > 1 || anchorPivot.y < 0 || anchorPivot.y > 1 ||
                sortingOrder < short.MinValue || sortingOrder > short.MaxValue)
            { LastDiagnostic = "Fade seconds must be nonnegative, pivot in [0,1], sorting order signed 16-bit."; return false; }
            EnsureCreated(); state.Show(anchor, localizationKey); LastDiagnostic = null;
            Advance(0); return true;
        }
        public void Hide() { if (state != null) state.Hide(); }
        private void OnEnable() { if (input == null) input = inputProvider as IGameInput; }
        private void Update() { Advance(Time.unscaledDeltaTime); }

        public void Advance(double seconds)
        {
            if (state == null) return;
            if (state.Target != null && Target == null) state.Reset(); // Destroyed native anchor cannot be projected.
            state.Advance(seconds, input == null ? GameInputDevice.Keyboard : input.CurrentDevice);
            if (canvas == null) return;
            if (keyLabel.text != state.KeyCaption) keyLabel.text = state.KeyCaption;
            if (actionLabel.text != state.Text)
                LocalizedText.ConfigureContent(actionLabel, state.LocalizationKey, state.Text);
            var anchor = Target;
            if (anchor == null || worldCamera == null || !worldCamera.isActiveAndEnabled || !anchor.gameObject.activeInHierarchy)
            { group.alpha = 0; return; }
            Vector3 screen = worldCamera.WorldToScreenPoint(anchor.position);
            Vector2 local = Vector2.zero;
            bool visible = screen.z > 0 && RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, screen, null, out local);
            if (visible) panel.anchoredPosition = local;
            group.alpha = visible ? state.Alpha : 0;
        }

        private void EnsureCreated()
        {
            if (canvas != null) return;
            state = new InteractionPromptState(fadeInTime, fadeOutTime, keyboardKeyCaption, gamepadConfirmCaption);
            var root = new GameObject("InteractionPromptCanvas_Runtime", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = sortingOrder;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(2560, 1440);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight = 1;
            roundedSprite = MakeRoundedSprite();
            var holder = new GameObject("Prompt", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            panel = (RectTransform)holder.transform; panel.SetParent(root.transform, false);
            panel.anchorMin = panel.anchorMax = new Vector2(.5f, .5f); panel.pivot = anchorPivot; panel.sizeDelta = new Vector2(420, 64);
            group = holder.GetComponent<CanvasGroup>(); group.alpha = 0; group.interactable = false; group.blocksRaycasts = false;
            var background = holder.GetComponent<Image>(); background.sprite = roundedSprite; background.type = Image.Type.Sliced;
            background.color = new Color(.06f, .06f, .06f, .88f); background.raycastTarget = false;
            var key = new GameObject("Key", typeof(RectTransform), typeof(Image));
            var keyRect = (RectTransform)key.transform; keyRect.SetParent(panel, false);
            keyRect.anchorMin = keyRect.anchorMax = keyRect.pivot = new Vector2(0, .5f);
            keyRect.anchoredPosition = new Vector2(8, 0); keyRect.sizeDelta = new Vector2(64, 48);
            var cap = key.GetComponent<Image>(); cap.sprite = roundedSprite; cap.type = Image.Type.Sliced; cap.color = new Color(1, 1, 1, .18f); cap.raycastTarget = false;
            keyLabel = Label("KeyCaption", keyRect); keyLabel.alignment = TextAlignmentOptions.Center;
            actionLabel = Label("Action", panel); actionLabel.alignment = TextAlignmentOptions.MidlineLeft;
            var textRect = (RectTransform)actionLabel.transform; textRect.offsetMin = new Vector2(84, 0); textRect.offsetMax = new Vector2(-12, 0);
            actionLabel.gameObject.AddComponent<LocalizedText>();
            // A single fixed layout for both devices; switching changes keyLabel.text only. No input controls/raycaster.
        }

        private TextMeshProUGUI Label(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSharedMaterial = fontMaterial != null ? fontMaterial : fontAsset.material; text.font = fontAsset;
            text.isOrthographic = true; text.fontSize = 28; text.enableAutoSizing = false;
            text.enableWordWrapping = false; text.overflowMode = TextOverflowModes.Ellipsis; text.raycastTarget = false; text.color = Color.white;
            return text;
        }

        private Sprite MakeRoundedSprite()
        {
            const int size = 64, radius = 16;
            roundedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            roundedTexture.name = "InteractionPromptRounded_Runtime"; roundedTexture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = Math.Max(0, Math.Max(radius - .5f - x, x - (size - radius - .5f)));
                float dy = Math.Max(0, Math.Max(radius - .5f - y, y - (size - radius - .5f)));
                pixels[y * size + x] = new Color(1, 1, 1, dx * dx + dy * dy <= radius * radius ? 1 : 0);
            }
            roundedTexture.SetPixels(pixels); roundedTexture.Apply(false, true);
            return Sprite.Create(roundedTexture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }
        static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        private void OnDisable() { Release(); }
        private void OnDestroy() { Release(); }
        private void Release()
        {
            if (canvas != null) { canvas.gameObject.SetActive(false); Destroy(canvas.gameObject); }
            if (roundedSprite != null) Destroy(roundedSprite);
            if (roundedTexture != null) Destroy(roundedTexture);
            canvas = null; panel = null; group = null; keyLabel = actionLabel = null; roundedSprite = null; roundedTexture = null;
            if (state != null) state.Reset(); state = null;
        }
    }
}
