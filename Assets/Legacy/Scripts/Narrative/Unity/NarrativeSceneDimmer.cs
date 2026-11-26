using System;
using System.Collections.Generic;
using EatWhat.Narrative.Core;
using UnityEngine;

namespace EatWhat.Narrative.Unity
{
    /// <summary>Owns current dim amount and owner-scoped requests, independent of dialogue/FSM progress.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(NarrativeDialogueCanvas))]
    public sealed class NarrativeSceneDimmer : MonoBehaviour, INarrativeDialogueDimming
    {
        [SerializeField, Tooltip("The dialogue HUD on this portable root. Empty resolves the same-object component.")]
        private NarrativeDialogueCanvas dialogueCanvas = null;
        private readonly Dictionary<object, float> requests = new Dictionary<object, float>();
        private double elapsed, duration;
        private float from;
        private bool externalTime;
        public float CurrentAmount { get; private set; }
        public float TargetAmount { get; private set; }
        public int RequestCount { get { return requests.Count; } }
        public bool IsTransitioning { get { return CurrentAmount != TargetAmount; } }

        public void RequestNormal(object owner, DialoguePresentationSnapshot snapshot, double seconds)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            Request(owner, snapshot.GetGroup("tier").Get<float>("normalDim"), seconds);
        }

        public void Request(object owner, float amount, double seconds)
        {
            CheckOwner(owner); CheckSeconds(seconds);
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0 || amount > 1)
                throw new ArgumentOutOfRangeException(nameof(amount));
            requests[owner] = amount;
            Retarget(seconds);
        }

        public bool Withdraw(object owner, double seconds)
        {
            CheckOwner(owner); CheckSeconds(seconds);
            if (!requests.Remove(owner)) return false;
            Retarget(seconds); return true;
        }

        void Retarget(double seconds)
        {
            float maximum = 0;
            foreach (float amount in requests.Values) maximum = Math.Max(maximum, amount);
            from = CurrentAmount; TargetAmount = maximum; elapsed = 0; duration = seconds;
            if (seconds == 0) CurrentAmount = TargetAmount;
            Apply(); // Positive durations retain the exact current amount on every request/withdrawal.
        }

        public void Advance(double seconds)
        {
            CheckSeconds(seconds);
            if (!IsTransitioning || seconds == 0) return;
            elapsed = seconds >= duration - elapsed ? duration : elapsed + seconds;
            CurrentAmount = elapsed == duration ? TargetAmount : (float)(from + (TargetAmount - from) * (elapsed / duration));
            Apply();
        }

        void Apply() { if (dialogueCanvas != null) dialogueCanvas.ApplyDimming(CurrentAmount); }
        private void OnEnable()
        {
            if (dialogueCanvas == null) dialogueCanvas = GetComponent<NarrativeDialogueCanvas>();
            if (dialogueCanvas != null) { dialogueCanvas.EnsureCreated(); Apply(); }
        }
        public void SetExternalTime(bool external) { externalTime = external; }
        private void Update() { if (!externalTime) Advance(Time.unscaledDeltaTime); }
        private void OnDisable()
        { requests.Clear(); elapsed = duration = 0; CurrentAmount = TargetAmount = from = 0; Apply(); }

        static void CheckOwner(object owner)
        { if (owner == null) throw new ArgumentNullException(nameof(owner)); }
        static void CheckSeconds(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
                throw new ArgumentOutOfRangeException("seconds", "Expected finite nonnegative seconds.");
        }
    }
}
