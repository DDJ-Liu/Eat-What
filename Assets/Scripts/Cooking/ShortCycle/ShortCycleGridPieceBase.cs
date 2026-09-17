using System;
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
                nameLabel.text = ResolveName(Display.name_key, Display.fallback_name);
            }
            if (badgeLabel != null)
            {
                badgeLabel.text = Display.badge_text ?? string.Empty;
            }
            if (placeholderRenderer != null)
            {
                placeholderRenderer.enabled = Display.is_placeholder;
            }
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
        }

        public void SetInteractionAllowed(bool allowed)
        {
            InteractionAllowed = allowed;
            foreach (var behaviour in mouseInteractBehaviours)
            {
                if (behaviour != null)
                {
                    behaviour.enabled = allowed;
                }
            }
        }

        protected void SetBadge(string value)
        {
            if (badgeLabel != null)
            {
                badgeLabel.text = value ?? string.Empty;
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

        private void RefreshInteractionGate()
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
