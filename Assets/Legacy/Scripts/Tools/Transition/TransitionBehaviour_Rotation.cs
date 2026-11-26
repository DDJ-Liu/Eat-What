using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class TransitionBehaviour_Rotation : TransitionBehaviour
{
    [Serializable]
    public sealed class RotationTransitionEntry
    {
        public string name;
        public float duration = 0.3f;
        public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public bool useUnscaledTime;
        public TransitionCoordinateSpace coordinateSpace = TransitionCoordinateSpace.Local;
        public TransitionTargetMode targetMode = TransitionTargetMode.Value;
        public float targetAngle;
        public Transform targetAnchor;
        public bool useExplicitFrom;
        public float fromAngle;
    }

    public Transform targetTransform;
    public List<RotationTransitionEntry> transitions = new List<RotationTransitionEntry>();

    protected override bool TryCreateTransition(string transitionName, out IEnumerator transition)
    {
        RotationTransitionEntry entry = null;
        foreach (var candidate in transitions)
            if (candidate != null && candidate.name == transitionName) { entry = candidate; break; }
        if (entry == null || targetTransform == null)
        {
            transition = null;
            return false;
        }
        var targetSnapshot = entry.targetMode == TransitionTargetMode.Anchor && entry.targetAnchor != null
            ? ReadAngle(entry.targetAnchor, entry.coordinateSpace)
            : entry.targetAngle;
        var fromSnapshot = entry.useExplicitFrom ? entry.fromAngle : ReadAngle(targetTransform, entry.coordinateSpace);
        transition = Animate(fromSnapshot, targetSnapshot, entry.duration, entry.curve, entry.useUnscaledTime, entry.coordinateSpace);
        return true;
    }

    private IEnumerator Animate(float from, float to, float duration, AnimationCurve curve, bool unscaled, TransitionCoordinateSpace space)
    {
        var elapsed = 0f;
        var safeDuration = duration <= 0f ? 0.0001f : duration;
        var delta = Mathf.DeltaAngle(from, to);
        while (elapsed < safeDuration)
        {
            elapsed += GetDeltaTime(unscaled);
            var progress = Mathf.Clamp01(elapsed / safeDuration);
            var value = curve == null ? progress : curve.Evaluate(progress);
            WriteAngle(space, from + (delta * value));
            yield return null;
        }
        WriteAngle(space, from + delta);
    }

    private static float ReadAngle(Transform value, TransitionCoordinateSpace space)
    {
        return space == TransitionCoordinateSpace.Local ? value.localEulerAngles.z : value.eulerAngles.z;
    }

    private void WriteAngle(TransitionCoordinateSpace space, float angle)
    {
        if (space == TransitionCoordinateSpace.Local)
        {
            var value = targetTransform.localEulerAngles;
            value.z = angle;
            targetTransform.localEulerAngles = value;
        }
        else
        {
            var value = targetTransform.eulerAngles;
            value.z = angle;
            targetTransform.eulerAngles = value;
        }
    }
}
