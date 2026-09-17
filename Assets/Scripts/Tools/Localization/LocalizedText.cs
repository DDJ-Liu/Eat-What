using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EatWhat.Tools.Localization
{
    /// <summary>Reusable TMP localization binding with audit-only fallback reporting.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string localizationKey = string.Empty;
        [SerializeField] private string auditedFallback = string.Empty;

        public string LocalizationKey { get { return localizationKey; } }
        public string AuditedFallback { get { return auditedFallback; } }

        public void Configure(string key, string fallback)
        {
            localizationKey = key ?? string.Empty;
            auditedFallback = fallback ?? string.Empty;
            RefreshText();
        }

        public void RefreshText()
        {
            var label = GetComponent<TMP_Text>();
            if (label == null) return;
            if (string.IsNullOrEmpty(localizationKey))
            {
                label.text = auditedFallback;
                return;
            }
            var resolved = LocService.Get(localizationKey);
            label.text = IsFallbackToken(resolved) ? auditedFallback : resolved;
        }

        public static int RefreshAll(Scene scene)
        {
            var count = 0;
            foreach (var binding in Resources.FindObjectsOfTypeAll<LocalizedText>())
            {
                if (binding == null || binding.gameObject.scene != scene) continue;
                binding.RefreshText();
                count++;
            }
            return count;
        }

        /// <summary>
        /// Historical API name retained. The method reports unresolved tokens
        /// and never clears or rewrites scene labels.
        /// </summary>
        public static IList<string> RemoveFallbackTokens(Scene scene)
        {
            var tokens = new List<string>();
            foreach (var label in Resources.FindObjectsOfTypeAll<TMP_Text>())
            {
                if (label == null || label.gameObject.scene != scene || !IsFallbackToken(label.text)) continue;
                if (!tokens.Contains(label.text)) tokens.Add(label.text);
            }
            tokens.Sort(System.StringComparer.Ordinal);
            if (tokens.Count > 0)
                Debug.LogWarning("CK01-C unresolved localization fallback tokens: " + string.Join(",", tokens));
            return tokens.AsReadOnly();
        }

        private static bool IsFallbackToken(string value)
        {
            return !string.IsNullOrEmpty(value) && value.Length > 2 && value[0] == '#' && value[value.Length - 1] == '#';
        }
    }
}
