using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TransitionCoordinateSpace { Local, World }
public enum TransitionTargetMode { Value, Anchor }

public sealed class TransitionBehaviour_Position : TransitionBehaviour
{
    [Serializable]
    public sealed class PositionTransitionEntry
    {
        public string name;
        public float duration = 0.3f;
        public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public bool useUnscaledTime;
        public TransitionCoordinateSpace coordinateSpace = TransitionCoordinateSpace.Local;
        public TransitionTargetMode targetMode = TransitionTargetMode.Value;
        public Vector3 targetValue;
        public Transform targetAnchor;
        public bool useExplicitFrom;
        public Vector3 fromValue;
    }

    public Transform targetTransform;
    public List<PositionTransitionEntry> transitions = new List<PositionTransitionEntry>();

    protected override bool TryCreateTransition(string transitionName, out IEnumerator transition)
    {
        PositionTransitionEntry entry = null;
        foreach (var candidate in transitions)
            if (candidate != null && candidate.name == transitionName) { entry = candidate; break; }
        if (entry == null || targetTransform == null)
        {
            transition = null;
            return false;
        }
        var targetSnapshot = entry.targetMode == TransitionTargetMode.Anchor && entry.targetAnchor != null
            ? (entry.coordinateSpace == TransitionCoordinateSpace.Local ? entry.targetAnchor.localPosition : entry.targetAnchor.position)
            : entry.targetValue;
        var fromSnapshot = entry.useExplicitFrom
            ? entry.fromValue
            : ReadPosition(entry.coordinateSpace);
        transition = Animate(fromSnapshot, targetSnapshot, entry.duration, entry.curve, entry.useUnscaledTime, entry.coordinateSpace);
        return true;
    }

    public Coroutine PlayTo(Vector3 worldTarget, float duration, bool useUnscaledTime, Action onComplete, Action onCancel)
    {
        if (targetTransform == null) return null;
        var startSnapshot = targetTransform.position;
        return PlayRoutine(Animate(startSnapshot, worldTarget, duration, null, useUnscaledTime, TransitionCoordinateSpace.World), onComplete, onCancel);
    }

    private IEnumerator Animate(Vector3 from, Vector3 to, float duration, AnimationCurve curve, bool unscaled, TransitionCoordinateSpace space)
    {
        var elapsed = 0f;
        var safeDuration = duration <= 0f ? 0.0001f : duration;
        while (elapsed < safeDuration)
        {
            elapsed += GetDeltaTime(unscaled);
            var progress = Mathf.Clamp01(elapsed / safeDuration);
            var value = curve == null ? progress : curve.Evaluate(progress);
            WritePosition(space, Vector3.LerpUnclamped(from, to, value));
            yield return null;
        }
        WritePosition(space, to);
    }

    private Vector3 ReadPosition(TransitionCoordinateSpace space)
    {
        return space == TransitionCoordinateSpace.Local ? targetTransform.localPosition : targetTransform.position;
    }

    private void WritePosition(TransitionCoordinateSpace space, Vector3 value)
    {
        if (space == TransitionCoordinateSpace.Local) targetTransform.localPosition = value;
        else targetTransform.position = value;
    }
}
