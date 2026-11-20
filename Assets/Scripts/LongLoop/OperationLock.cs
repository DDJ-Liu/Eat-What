using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;
using UnityEngine;

namespace EatWhat.LongLoop
{
    /// <summary>Shared named-owner operation lock. Owners explicitly release only their own hold.</summary>
    [DisallowMultipleComponent]
    public sealed class OperationLock : MonoBehaviour, INarrativeDialogueLock
    {
        private readonly Dictionary<string, bool> holders = new Dictionary<string, bool>(StringComparer.Ordinal);
        public bool IsLocked { get { return holders.Count > 0; } }
        public int HolderCount { get { return holders.Count; } }
        public string LastDiagnostic { get; private set; }
        public event Action Locked;
        public event Action Unlocked;

        public bool IsHeldBy(string holder)
        { return holder != null && holders.ContainsKey(holder); }

        public bool Lock(string holder)
        {
            CheckName(holder);
            if (holders.ContainsKey(holder)) return false;
            bool first = !IsLocked;
            holders.Add(holder, true);
            if (first) Locked?.Invoke();
            return true;
        }

        public bool Unlock(string holder)
        {
            CheckName(holder);
            if (!holders.Remove(holder))
            {
                LastDiagnostic = "OperationLock: no hold owned by '" + holder + "'.";
                Debug.LogWarning(LastDiagnostic, this);
                return false;
            }
            if (!IsLocked) Unlocked?.Invoke();
            return true;
        }

        // The FSM owns time. This capability needs only its stable dialogue ID as owner name.
        public void Acquire(string dialogueId, double nowSeconds) { Lock(dialogueId); }
        public void Release(string dialogueId, double nowSeconds) { Unlock(dialogueId); }

        static void CheckName(string holder)
        { if (string.IsNullOrWhiteSpace(holder)) throw new ArgumentException("A lock holder needs a name.", nameof(holder)); }
    }
}
