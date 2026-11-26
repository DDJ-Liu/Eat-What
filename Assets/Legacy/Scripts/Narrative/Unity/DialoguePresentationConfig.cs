using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;
using UnityEngine;

namespace EatWhat.Narrative.Unity
{
    /// <summary>One asset type with ten groups. This task does not create an asset instance.</summary>
    [CreateAssetMenu(menuName = "Eat What/Dialogue/Presentation Config")]
    public sealed class DialoguePresentationConfig : ScriptableObject
    {
        [SerializeField] private DialoguePresentationData data = new DialoguePresentationData();
        private string lastSerializedState;
        public DialoguePresentationData Data { get { return data; } }
        public event Action Changed;

        public IReadOnlyList<PresentationConfigDiagnostic> ValidateData()
        { return DialoguePresentationValidation.Validate(data); }

        // Call after changing authoring data through code. A read or identical value does not notify.
        public bool NotifyChanged()
        {
            string state = SerializedState();
            if (lastSerializedState == null) { lastSerializedState = state; return false; }
            if (string.Equals(lastSerializedState, state, StringComparison.Ordinal)) return false;
            lastSerializedState = state;
            Changed?.Invoke();
            return true;
        }

        private string SerializedState() { return data == null ? "<null>" : JsonUtility.ToJson(data); }
        private void OnEnable() { lastSerializedState = SerializedState(); }
        private void OnValidate()
        {
            NotifyChanged();
            foreach (var issue in ValidateData()) Debug.LogWarning(issue.ToString(), this);
        }
    }
}
