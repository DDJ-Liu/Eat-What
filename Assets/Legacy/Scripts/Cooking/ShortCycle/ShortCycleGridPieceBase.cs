using System;
using EatWhat.Tools.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace EatWhat.Cooking.ShortCycle
{
    [Serializable]
    public sealed class ShortCycleGridPieceDisplay
    {
        public Sprite icon_sprite;
        public string name_key;
        public string fallback_name;
        public string badge_text;
        public bool is_placeholder;
    }

    /// <summary>
    /// Common world-space prefab contract from A5-2: structure + data-driven
    /// Sprite/TMP display + MouseInteract hooks, grouped by a root SortingGroup.
    /// </summary>
    public abstract class ShortCycleGridPieceBase : MonoBehaviour, IShortCycleStateGatedInteraction
    {
        [Header("A5-2 world-space structure")]
        [SerializeField] private SortingGroup sortingGroup = null;
        [SerializeField] private SpriteRenderer iconRenderer = null;
        [SerializeField] private TextMeshPro nameLabel = null;
        [SerializeField] private TextMeshPro badgeLabel = null;
        [SerializeField] private SpriteRenderer placeholderRenderer = null;

        [Header("Framework interaction hooks")]
        [SerializeField] private ShortCycleSessionManager sessionManager = null;
        [SerializeField] private ShortCycleTrayStateController trayStateController = null;
        [SerializeField] private Behaviour[] mouseInteractBehaviours = new Behaviour[0];
        [SerializeField] private ShortCyclePhase requiredPhase = ShortCyclePhase.Phase1;
        [SerializeField] private bool requirePhase0View = false;
        [SerializeField] private ShortCyclePhase0View requiredPhase0View = ShortCyclePhase0View.RecipeBrowse;
        [SerializeField] private bool requireTrayView = false;
        [SerializeField] private ShortCycleTrayView requiredTrayView = ShortCycleTrayView.Collapsed;

        public SortingGroup SortingGroup { get { return sortingGroup; } }
        protected bool HasPlaceholderRenderer { get { return placeholderRenderer != null; } }
        protected SpriteRenderer PrimaryIconRenderer { get { return iconRenderer; } }
        protected SpriteRenderer PlaceholderRenderer { get { return placeholderRenderer; } }
        protected bool ContentVisible { get { return contentVisible; } }
        public bool InteractionAllowed { get; private set; }
        public ShortCycleGridPieceDisplay Display { get; private set; }

        protected virtual void OnEnable()
        {
            if (sessionManager != null)
            {
                sessionManager.StateChanged += HandleSessionStateChanged;
            }
            if (trayStateController != null)
            {
                trayStateController.StateChanged += HandleTrayStateChanged;
            }
            RefreshInteractionGate();
            OnDisplayStateChanged();
        }

        protected virtual void OnDisable()
        {
            if (sessionManager != null)
            {
                sessionManager.StateChanged -= HandleSessionStateChanged;
            }
            if (trayStateController != null)
            {
                trayStateController.StateChanged -= HandleTrayStateChanged;
            }
            OnDisplayStateChanged();
        }

        public void Configure(ShortCycleGridPieceDisplay display)
        {
            Display = display ?? new ShortCycleGridPieceDisplay();
            if (iconRenderer != null)
            {
                iconRenderer.sprite = Display.icon_sprite;
            }
            if (nameLabel != null)
            {
                LocalizedText.ConfigureContent(nameLabel, Display.name_key, ResolveName(Display.name_key, Display.fallback_name));
            }
            if (badgeLabel != null)
            {
                LocalizedText.ConfigureContent(badgeLabel, string.Empty, Display.badge_text ?? string.Empty);
            }
            if (placeholderRenderer != null)
            {
                placeholderRenderer.enabled = Display.is_placeholder;
            }
            OnDisplayStateChanged();
        }

        public void SetPlaceholder(bool isPlaceholder)
        {
            if (Display == null)
            {
                Display = new ShortCycleGridPieceDisplay();
            }
            Display.is_placeholder = isPlaceholder;
            if (placeholderRenderer != null)
            {
                placeholderRenderer.enabled = isPlaceholder;
            }
            OnDisplayStateChanged();
        }

        protected virtual bool ContentAllowsInteraction { get { return true; } }
        private bool contentVisible = true;
        public void SetContentVisible(bool visible)
        {
            contentVisible = visible;
            // Existing grid prefabs have a root paper/frame renderer in addition to their icon.
            var surface = GetComponent<SpriteRenderer>();
            if (surface != null) surface.enabled = visible;
            if (iconRenderer != null) iconRenderer.enabled = visible;
            if (nameLabel != null) nameLabel.enabled = visible;
            if (badgeLabel != null) badgeLabel.enabled = visible;
            if (placeholderRenderer != null) placeholderRenderer.enabled = visible && Display != null && Display.is_placeholder;
            OnDisplayStateChanged();
            RefreshInteractionGate();
        }

        protected virtual void OnDisplayStateChanged() { }

        public void SetInteractionAllowed(bool allowed)
        {
            allowed = allowed && contentVisible && ContentAllowsInteraction;
            InteractionAllowed = allowed;
            foreach (var behaviour in mouseInteractBehaviours)
            {
                if (behaviour != null)
                {
                    behaviour.enabled = allowed;
                }
            }
        }

        protected string CurrentBadgeText { get { return badgeLabel == null ? null : badgeLabel.text; } }

        protected void SetBadge(string value)
        {
            if (badgeLabel != null)
            {
                LocalizedText.ConfigureContent(badgeLabel, string.Empty, value ?? string.Empty);
            }
        }

        protected void SetSecondaryIcon(SpriteRenderer renderer, Sprite value)
        {
            if (renderer != null)
            {
                renderer.sprite = value;
            }
        }

        private void HandleSessionStateChanged(ShortCyclePhase phase, ShortCyclePhase0View phase0View)
        {
            RefreshInteractionGate();
        }

        private void HandleTrayStateChanged(ShortCycleTrayView view)
        {
            RefreshInteractionGate();
        }

        protected void RefreshInteractionGate()
        {
            var sessionAllowed = sessionManager != null && sessionManager.CurrentPhase == requiredPhase;
            if (sessionAllowed && requirePhase0View)
            {
                sessionAllowed = sessionManager.CurrentPhase0View == requiredPhase0View;
            }

            var trayAllowed = !requireTrayView ||
                (trayStateController != null && trayStateController.CurrentView == requiredTrayView);
            SetInteractionAllowed(sessionAllowed && trayAllowed);
        }

        private static string ResolveName(string nameKey, string fallback)
        {
            if (!string.IsNullOrEmpty(nameKey))
            {
                return LocService.Get(nameKey);
            }
            return fallback ?? string.Empty;
        }
    }
}
