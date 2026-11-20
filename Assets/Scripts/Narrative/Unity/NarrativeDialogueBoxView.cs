using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;
using EatWhat.Toolkit.Input;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EatWhat.Narrative.Unity
{
    /// <summary>Runtime-only ordinary dialogue rectangles. V014-07/08/09 own orchestration and service wiring.</summary>
    [DisallowMultipleComponent]
    public sealed class NarrativeDialogueBoxView : MonoBehaviour, INarrativeDialogueBoxSurface, INarrativeDialogueBoxPresentation
    {
        [SerializeField] NarrativeDialogueCanvas dialogueCanvas = null;
        [SerializeField, Tooltip("Toolkit IGameInput provider. Interact is read once per Update.")]
        MonoBehaviour inputProvider = null;
        [SerializeField] RectTransform authoredRoot = null, authoredBox = null, authoredNameBox = null;
        [SerializeField] TextMeshProUGUI authoredBodyText = null, authoredNameText = null;
        [SerializeField] Image authoredAdvanceSurface = null, authoredAutoButton = null, authoredFastForwardButton = null;
        [SerializeField] CanvasGroup authoredGroup = null;
        [SerializeField] Vector2 boxSize = new Vector2(2107, 333);
        [SerializeField] float boxBottom = 54;
        [SerializeField] float textFontSize = 59;
        [SerializeField, Tooltip("Baseline-to-baseline distance in reference pixels, fixed before reveal.")]
        float textLineHeight = 83;
        [SerializeField] float textLeft = 93, textRight = 107, textTop = 70;
        [SerializeField] float nameHeight = 83, nameMinWidth = 267, nameFontSize = 43;
        [SerializeField] float nameLeft = 67, nameTop = 45;
        [SerializeField] Vector2 buttonSize = new Vector2(133, 53);
        [SerializeField] float buttonBottom = 30, autoRight = 440, fastForwardRight = 267;
        NarrativeDialogueBoxPresenter presenter;
        IGameInput gameInput;
        RectTransform runtimeRoot, box, nameBox;
        TextMeshProUGUI bodyText, nameText;
        Image autoImage, fastImage;
        CanvasGroup group;
        GraphicRaycaster ownedRaycaster;
        GameObject ownedEventSystem;
        bool externalTime;
        bool ownsVisuals;
        Vector2 authoredBoxPosition, authoredNameSize;
        Vector3 authoredBoxScale;
        sealed class PointerBinding { public EventTrigger Trigger; public EventTrigger.Entry Entry; }
        readonly List<PointerBinding> pointerBindings = new List<PointerBinding>();
        public event Action AdvanceRequested;
        public event Action<NarrativeLinePresentation> TextCompleted;
        // Real TMP state for the assigned engine lane's ordinary readback; no synthetic layout data.
        public TMP_Text BodyTextComponent { get { return bodyText; } }
        public int VisibleTextLength { get { return presenter == null ? 0 : presenter.VisibleTextLength; } }
        public int VisibleCharacterCount { get { return presenter == null ? 0 : presenter.VisibleCharacterCount; } }

        public void Configure(NarrativeAdvanceInputModel input, IGameInput toolkitInput)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (presenter != null) presenter.Hide();
            gameInput = toolkitInput;
            presenter = new NarrativeDialogueBoxPresenter(this, input);
            presenter.AdvanceRequested += () => AdvanceRequested?.Invoke();
            presenter.TextCompleted += value => TextCompleted?.Invoke(value);
        }

        public void Show(DialoguePresentationSnapshot snapshot)
        {
            if (presenter == null) throw new InvalidOperationException("Configure the existing advance input model first.");
            EnsureCreated(); runtimeRoot.gameObject.SetActive(true); presenter.PrepareEntrance(snapshot);
            SetBoxProgress(snapshot, 0);
        }
        public void BeginLine(NarrativeLinePresentation line, string finalLocalizedText)
        {
            if (presenter == null) throw new InvalidOperationException("Configure the existing advance input model first.");
            EnsureCreated(); runtimeRoot.gameObject.SetActive(true); presenter.BeginLine(line, finalLocalizedText);
        }
        public void CompleteLine() { presenter?.CompleteLine(); }
        public void Hide()
        { presenter?.Hide(); if (runtimeRoot != null) runtimeRoot.gameObject.SetActive(false); }
        public void SetBoxProgress(DialoguePresentationSnapshot snapshot, float progress)
        {
            ApplyPose(NarrativeDialogueBoxPresenter.Pose(snapshot, progress));
        }
        public void SetEntranceProgress(DialoguePresentationSnapshot snapshot, float progress)
        { ApplyPose(NarrativeDialogueBoxPresenter.EntrancePose(snapshot, progress)); }
        void ApplyPose(NarrativeDialogueBoxPose pose)
        {
            EnsureCreated();
            box.anchoredPosition = ownsVisuals ? new Vector2(0, boxBottom + pose.OffsetY) : new Vector2(authoredBoxPosition.x, authoredBoxPosition.y + pose.OffsetY);
            box.localScale = ownsVisuals ? new Vector3(pose.Scale, pose.Scale, 1) : authoredBoxScale * pose.Scale; group.alpha = pose.Alpha;
        }

        public void SetExternalTime(bool external) { externalTime = external; }
        public void Advance(double seconds) { presenter?.Tick(seconds); }
        void Update()
        {
            if (presenter == null || !presenter.Active) return;
            if (gameInput == null) gameInput = inputProvider as IGameInput;
            // External time suppresses only automatic Tick; Toolkit Interact is still read exactly once here.
            presenter.ProcessFrame(externalTime ? 0 : Time.unscaledDeltaTime, !externalTime,
                gameInput != null && gameInput.Interact);
        }

        void EnsureCreated()
        {
            if (runtimeRoot != null) return;
            if (dialogueCanvas == null) dialogueCanvas = GetComponent<NarrativeDialogueCanvas>();
            if (dialogueCanvas == null || dialogueCanvas.FontAsset == null)
                throw new InvalidOperationException("Bind NarrativeDialogueCanvas and its existing font/material before showing dialogue.");
            dialogueCanvas.EnsureCreated();
            if (authoredRoot != null || authoredBox != null || authoredNameBox != null || authoredBodyText != null || authoredNameText != null ||
                authoredAdvanceSurface != null || authoredAutoButton != null || authoredFastForwardButton != null || authoredGroup != null)
            {
                if (authoredRoot == null || authoredBox == null || authoredNameBox == null || authoredBodyText == null || authoredNameText == null ||
                    authoredAdvanceSurface == null || authoredAutoButton == null || authoredFastForwardButton == null || authoredGroup == null ||
                    authoredRoot == transform || authoredRoot == authoredBox || authoredRoot == authoredNameBox || authoredBox == authoredNameBox ||
                    !authoredRoot.IsChildOf(dialogueCanvas.DialogueLayer) ||
                    !authoredBox.IsChildOf(authoredRoot) || !authoredNameBox.IsChildOf(authoredBox) ||
                    !authoredBodyText.transform.IsChildOf(authoredBox) || !authoredNameText.transform.IsChildOf(authoredNameBox) ||
                    !authoredAdvanceSurface.transform.IsChildOf(authoredRoot) || !authoredAutoButton.transform.IsChildOf(authoredRoot) ||
                    !authoredFastForwardButton.transform.IsChildOf(authoredRoot) || authoredGroup.gameObject != authoredRoot.gameObject ||
                    authoredAutoButton == authoredFastForwardButton || authoredAutoButton == authoredAdvanceSurface || authoredFastForwardButton == authoredAdvanceSurface ||
                    authoredBodyText == authoredNameText)
                    throw new InvalidOperationException("Assign the complete local authored dialogue root, box/name/text, advance surface, AUTO/fast images and root group.");
                runtimeRoot = authoredRoot; box = authoredBox; nameBox = authoredNameBox;
                bodyText = authoredBodyText; nameText = authoredNameText; group = authoredGroup;
                autoImage = authoredAutoButton; fastImage = authoredFastForwardButton; ownsVisuals = false;
                authoredBoxPosition = box.anchoredPosition; authoredBoxScale = box.localScale; authoredNameSize = nameBox.sizeDelta;
                bodyText.fontSharedMaterial = dialogueCanvas.SharedFontMaterial; bodyText.font = dialogueCanvas.FontAsset;
                nameText.fontSharedMaterial = dialogueCanvas.SharedFontMaterial; nameText.font = dialogueCanvas.FontAsset;
                bodyText.raycastTarget = nameText.raycastTarget = false;
                var authoredBoxPlate = box.GetComponent<Image>(); var authoredNamePlate = nameBox.GetComponent<Image>();
                if (authoredBoxPlate != null) authoredBoxPlate.raycastTarget = false;
                if (authoredNamePlate != null) authoredNamePlate.raycastTarget = false;
                group.interactable = group.blocksRaycasts = true;
                authoredAdvanceSurface.raycastTarget = autoImage.raycastTarget = fastImage.raycastTarget = true;
                Pointer((RectTransform)authoredAdvanceSurface.transform, () => presenter?.ScreenClicked());
                Pointer((RectTransform)autoImage.transform, () => presenter?.AutoClicked());
                Pointer((RectTransform)fastImage.transform, () => presenter?.FastForwardClicked());
                EnsureInput(); nameBox.gameObject.SetActive(false); runtimeRoot.gameObject.SetActive(false); return;
            }
            runtimeRoot = Rect("DialogueBox_Runtime", dialogueCanvas.DialogueLayer);
            ownsVisuals = true;
            Stretch(runtimeRoot); group = runtimeRoot.gameObject.AddComponent<CanvasGroup>();
            // Existing clickable world objects use Physics2D. Original V014-06 explicitly allows this UI-event fallback.
            var backdrop = Rect("AdvanceSurface", runtimeRoot); Stretch(backdrop);
            var clickImage = backdrop.gameObject.AddComponent<Image>(); clickImage.color = Color.clear;
            Pointer(backdrop, () => presenter?.ScreenClicked());
            box = Rect("Box", runtimeRoot); Bottom(box, boxSize, new Vector2(0, boxBottom));
            var plate = box.gameObject.AddComponent<Image>(); plate.color = new Color(.08f, .09f, .13f, .96f); plate.raycastTarget = false;
            var textArea = Rect("Text", box); Stretch(textArea);
            textArea.offsetMin = new Vector2(textLeft, 0); textArea.offsetMax = new Vector2(-textRight, -textTop);
            bodyText = Text(textArea, textFontSize); bodyText.useMaxVisibleDescender = false;
            nameBox = Rect("Name", box); nameBox.anchorMin = nameBox.anchorMax = new Vector2(0, 1);
            nameBox.pivot = new Vector2(0, 1); nameBox.anchoredPosition = new Vector2(nameLeft, nameTop);
            var namePlate = nameBox.gameObject.AddComponent<Image>(); namePlate.color = new Color(.2f, .23f, .32f, 1); namePlate.raycastTarget = false;
            var nameArea = Rect("Text", nameBox); Stretch(nameArea); nameText = Text(nameArea, nameFontSize);
            nameText.alignment = TextAlignmentOptions.Center; nameText.textWrappingMode = TextWrappingModes.NoWrap;
            autoImage = Toggle("AUTO", autoRight, () => presenter?.AutoClicked());
            fastImage = Toggle(">>>", fastForwardRight, () => presenter?.FastForwardClicked());
            EnsureInput();
            nameBox.gameObject.SetActive(false); runtimeRoot.gameObject.SetActive(false);
        }
        void EnsureInput()
        {
            if (dialogueCanvas.RootCanvas.GetComponent<GraphicRaycaster>() == null)
                ownedRaycaster = dialogueCanvas.RootCanvas.gameObject.AddComponent<GraphicRaycaster>();
            if (EventSystem.current == null)
            {
                ownedEventSystem = new GameObject("DialogueEventSystem_Runtime", typeof(EventSystem), typeof(StandaloneInputModule));
                ownedEventSystem.transform.SetParent(transform, false);
                ownedEventSystem.GetComponent<EventSystem>().sendNavigationEvents = false;
            }
        }

        Image Toggle(string caption, float right, Action click)
        {
            var rect = Rect(caption, box); rect.anchorMin = rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(.5f, 0); rect.sizeDelta = buttonSize; rect.anchoredPosition = new Vector2(-right, buttonBottom);
            var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.25f, .27f, .33f, 1);
            var label = Rect("Caption", rect); Stretch(label); var text = Text(label, 26); text.text = caption; text.alignment = TextAlignmentOptions.Center;
            // PointerClick only: Submit/navigation must not activate a selected toggle and also advance via Toolkit.
            Pointer(rect, click); return image;
        }
        void Pointer(RectTransform rect, Action callback)
        {
            var trigger = rect.GetComponent<EventTrigger>();
            if (trigger == null) trigger = rect.gameObject.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener(value => { var pointer = value as PointerEventData;
                if (pointer != null && pointer.button == PointerEventData.InputButton.Left) callback(); });
            trigger.triggers.Add(entry);
            pointerBindings.Add(new PointerBinding { Trigger = trigger, Entry = entry });
        }
        TextMeshProUGUI Text(RectTransform rect, float size)
        {
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSharedMaterial = dialogueCanvas.SharedFontMaterial; text.font = dialogueCanvas.FontAsset;
            text.fontSize = size; text.enableAutoSizing = false; text.richText = false;
            text.isOrthographic = true; text.alignment = TextAlignmentOptions.TopLeft;
            text.color = Color.white; text.raycastTarget = false; return text;
        }
        static RectTransform Rect(string name, Transform parent)
        { var result = (RectTransform)new GameObject(name, typeof(RectTransform)).transform; result.SetParent(parent, false); return result; }
        static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        static void Bottom(RectTransform rect, Vector2 size, Vector2 position)
        { rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0); rect.pivot = new Vector2(.5f, 0); rect.sizeDelta = size; rect.anchoredPosition = position; }

        public void LayoutFullLine(string finalText, string finalName, bool showName)
        {
            EnsureCreated(); bodyText.text = finalText; bodyText.maxVisibleCharacters = int.MaxValue;
            bodyText.lineSpacing = 0; bodyText.ForceMeshUpdate(true, true);
            // TMP expresses additional spacing in em units. Calibrate using actual full-line baselines before typing.
            if (bodyText.textInfo.lineCount > 1)
            {
                float baselineGap = bodyText.textInfo.lineInfo[0].baseline - bodyText.textInfo.lineInfo[1].baseline;
                bodyText.lineSpacing = (textLineHeight - baselineGap) / (bodyText.fontSize * .01f);
                bodyText.ForceMeshUpdate(true, true);
            }
            nameText.text = finalName; nameBox.sizeDelta = new Vector2(Mathf.Max(ownsVisuals ? nameMinWidth : authoredNameSize.x, nameText.preferredWidth + 32), ownsVisuals ? nameHeight : authoredNameSize.y);
            nameBox.gameObject.SetActive(showName);
        }
        public void SetVisiblePrefix(int utf16End)
        {
            if (bodyText == null) return;
            int visible = 0; var info = bodyText.textInfo;
            while (visible < info.characterCount && info.characterInfo[visible].index < utf16End) visible++;
            bodyText.maxVisibleCharacters = visible;
        }
        public void SetToggleStates(bool auto, bool fastForward)
        {
            if (autoImage != null) autoImage.color = ToggleColor(auto);
            if (fastImage != null) fastImage.color = ToggleColor(fastForward);
        }
        static Color ToggleColor(bool enabled) { return enabled ? new Color(.2f, .6f, .45f, 1) : new Color(.25f, .27f, .33f, 1); }
        void OnDisable() { Release(false); }
        void OnDestroy() { Release(true); }
        void Release(bool destroying)
        {
            presenter?.Hide();
            foreach (var binding in pointerBindings) if (binding.Trigger != null) binding.Trigger.triggers.Remove(binding.Entry);
            pointerBindings.Clear();
            if (runtimeRoot != null)
            {
                runtimeRoot.gameObject.SetActive(false);
                if (ownsVisuals) Destroy(runtimeRoot.gameObject);
                else { box.anchoredPosition = authoredBoxPosition; box.localScale = authoredBoxScale; nameBox.sizeDelta = authoredNameSize; }
            }
            // Destroy is deferred. Retain this canvas component through disable/re-enable in the same frame.
            if (destroying && ownedRaycaster != null) { Destroy(ownedRaycaster); ownedRaycaster = null; }
            if (ownedEventSystem != null) { ownedEventSystem.SetActive(false); Destroy(ownedEventSystem); }
            runtimeRoot = box = nameBox = null; bodyText = nameText = null; autoImage = fastImage = null;
            group = null; ownedEventSystem = null;
        }
    }
}
