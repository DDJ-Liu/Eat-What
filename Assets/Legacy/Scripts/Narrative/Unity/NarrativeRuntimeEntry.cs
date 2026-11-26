using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;
using UnityEngine;

namespace EatWhat.Narrative.Unity
{
    /// <summary>Scene configuration/data access only. Derived snapshots never own dialogue or NPC progress.</summary>
    [DisallowMultipleComponent]
    public sealed class NarrativeRuntimeEntry : MonoBehaviour
    {
        [SerializeField, Tooltip("The explicitly assigned presentation asset; no asset search or default asset creation.")]
        private DialoguePresentationConfig presentationConfig = null;
        [SerializeField, Tooltip("The existing generated catalog. Its own source policy chooses SO or encrypted data.")]
        private CK01GeneratedDataCatalog dataCatalog = null;

        private DialoguePresentationConfig initializedConfig;
        private CK01GeneratedDataCatalog initializedCatalog;
        private DialoguePresentationConfig subscribedConfig;
        private INarrativeDataDirectory directory;
        private IReadOnlyList<string> diagnostics = new List<string>().AsReadOnly();
        private bool initialized;
        private bool available;
        private string sourceKind;

        public bool IsAvailable => EnsureInitialized();
        public IReadOnlyList<string> Diagnostics { get { EnsureInitialized(); return diagnostics; } }
        public string SourceKind { get { EnsureInitialized(); return sourceKind; } }

        public void Configure(DialoguePresentationConfig config, CK01GeneratedDataCatalog catalog)
        {
            Unsubscribe();
            presentationConfig = config;
            dataCatalog = catalog;
            Invalidate();
        }

        public bool Initialize()
        {
            Unsubscribe();
            initialized = true;
            available = false;
            directory = null;
            sourceKind = null;
            initializedConfig = presentationConfig;
            initializedCatalog = dataCatalog;
            var issues = new List<string>();
            if (!isActiveAndEnabled) issues.Add("DISABLED: Runtime entry is not active.");
            if (presentationConfig == null) issues.Add("MISSING_CONFIG: Presentation Config is not assigned.");
            else if (isActiveAndEnabled)
            {
                subscribedConfig = presentationConfig;
                subscribedConfig.Changed += Invalidate;
            }
            if (dataCatalog == null) issues.Add("MISSING_CATALOG: Data Catalog is not assigned.");
            if (issues.Count != 0) return Unavailable(issues);
            try
            {
                bool invalid = false;
                foreach (var issue in presentationConfig.ValidateData())
                {
                    issues.Add("CONFIG: " + issue);
                    if (issue.IsError) invalid = true;
                }
                if (invalid) return Unavailable(issues);
                sourceKind = dataCatalog.SourceKind;
                INarrativeDataDirectory candidate;
                IReadOnlyList<NarrativeDataDiagnostic> dataIssues;
                bool success = NarrativeDataAdapter.TryCreate(dataCatalog, out candidate, out dataIssues);
                foreach (var issue in dataIssues)
                    issues.Add("DATA: " + issue.Table + "/" + issue.Key + ": " + issue.Reason);
                if (!success || candidate == null) return Unavailable(issues);
                directory = candidate;
                diagnostics = issues.AsReadOnly();
                available = true;
                return true;
            }
            catch (Exception error)
            {
                issues.Add("READ_FAILED: " + error.GetType().Name + ": " + error.Message);
                return Unavailable(issues);
            }
        }

        public bool TryGetDirectory(out INarrativeDataDirectory value)
        {
            bool ready = EnsureInitialized();
            value = ready ? directory : null;
            return ready;
        }

        public Sprite GetCharacterPortraitSprite(string characterId)
        {
            if (!EnsureInitialized()) return null;
            Sprite sprite; string diagnostic;
            return NarrativeDataAdapter.TryGetCharacterPortraitSprite(dataCatalog, characterId, out sprite, out diagnostic) ? sprite : null;
        }
        public bool TryGetCharacterVisualPrefab(string characterId, out GameObject prefab, out string diagnostic)
        {
            prefab = null; diagnostic = null;
            if (!EnsureInitialized()) { diagnostic = FailureText(); return false; }
            try { return NarrativeDataAdapter.TryGetCharacterVisualPrefab(dataCatalog, characterId, out prefab, out diagnostic); }
            catch (Exception error) { diagnostic = ReadFailure(error); return false; }
        }

        public bool TryResolveDialogue(string dialogueId, out DialoguePresentationResolution resolution,
            out string diagnostic, string triggerTemplateName = null)
        {
            resolution = null;
            diagnostic = null;
            if (!EnsureInitialized()) { diagnostic = FailureText(); return false; }
            try
            {
                NarrativeDialogue dialogue;
                if (!directory.TryGetDialogue(dialogueId, out dialogue, out diagnostic)) return false;
                resolution = DialoguePresentationResolver.Resolve(presentationConfig.Data, dialogue, triggerTemplateName);
                if (resolution.Success) return true;
                var issues = new List<string>();
                foreach (var issue in resolution.ConfigurationDiagnostics) issues.Add(issue.ToString());
                diagnostic = string.Join("; ", issues);
                return false;
            }
            catch (Exception error) { diagnostic = ReadFailure(error); return false; }
        }

        public DialoguePresentationResolution ResolveForService(NarrativeDialogue dialogue, NarrativeDialogueContinuationPolicy policy)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            return policy.Resolve(EnsureInitialized() ? presentationConfig.Data : null, dialogue);
        }

        public bool TryResolveTrigger(string templateName, out DialogueTriggerResolution resolution, out string diagnostic)
        {
            resolution = null;
            diagnostic = null;
            if (!EnsureInitialized()) { diagnostic = FailureText(); return false; }
            try
            {
                resolution = DialoguePresentationResolver.ResolveTrigger(presentationConfig.Data, templateName);
                if (resolution.Success) return true;
                var issues = new List<string>();
                foreach (var issue in resolution.Diagnostics) issues.Add(issue.ToString());
                diagnostic = string.Join("; ", issues);
                return false;
            }
            catch (Exception error) { diagnostic = ReadFailure(error); return false; }
        }

        private bool EnsureInitialized()
        {
            if (!initialized || !ReferenceEquals(initializedConfig, presentationConfig) ||
                !ReferenceEquals(initializedCatalog, dataCatalog) ||
                (available && (!isActiveAndEnabled || presentationConfig == null || dataCatalog == null)))
                return Initialize();
            return available;
        }
        private bool Unavailable(List<string> issues)
        {
            available = false;
            directory = null;
            if (issues.Count == 0) issues.Add("DATA_UNAVAILABLE: Adapter did not provide a directory.");
            diagnostics = issues.AsReadOnly();
            Debug.LogWarning("NarrativeRuntimeEntry unavailable: " + FailureText());
            return false;
        }
        private string ReadFailure(Exception error)
        {
            Unavailable(new List<string> { "READ_FAILED: " + error.GetType().Name + ": " + error.Message });
            return FailureText();
        }
        private string FailureText() { return string.Join("; ", diagnostics); }
        private void Invalidate() { initialized = false; available = false; directory = null; sourceKind = null; }
        private void Unsubscribe()
        {
            if (!ReferenceEquals(subscribedConfig, null)) subscribedConfig.Changed -= Invalidate;
            subscribedConfig = null;
        }
        private void OnEnable() { Invalidate(); }
        private void OnDisable() { Unsubscribe(); Invalidate(); }
        private void OnDestroy() { Unsubscribe(); Invalidate(); }
        private void OnValidate() { Unsubscribe(); Invalidate(); }
    }
}
