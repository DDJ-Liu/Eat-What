using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransitionBehaviour_Scale : TransitionBehaviour
{
    [System.Serializable]
    public class ScaleTransitionEntry
    {
        public string name;
        public float duration = 0.3f;
        public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 1.2f);
        public bool useUnscaledTime;
        public bool useExplicitRange;
        public Vector3 fromScale = Vector3.one;
        public Vector3 toScale = Vector3.one;
    }

    public Transform targetTransform;
    public List<ScaleTransitionEntry> transitions = new List<ScaleTransitionEntry>();

    protected override bool TryCreateTransition(string transitionName, out IEnumerator transition)
    {
        var config = transitions.Find(t => t.name == transitionName);

        if (config == null)
        {
            Debug.LogWarning("Scale transition not found: " + transitionName);
            transition = null;
            return false;
        }
        transition = AnimateScale(config);
        return true;
    }

    private IEnumerator AnimateScale(ScaleTransitionEntry config)
    {
        Vector3 initialScale = targetTransform.localScale;
        float elapsed = 0f;

        while (elapsed < config.duration)
        {
            elapsed += GetDeltaTime(config.useUnscaledTime);
            float t = elapsed / config.duration;
            float scaleMultiplier = config.scaleCurve.Evaluate(t);
            targetTransform.localScale = config.useExplicitRange
                ? Vector3.LerpUnclamped(config.fromScale, config.toScale, scaleMultiplier)
                : initialScale * scaleMultiplier;
            yield return null;
        }

        var finalValue = config.scaleCurve.Evaluate(1f);
        targetTransform.localScale = config.useExplicitRange
            ? Vector3.LerpUnclamped(config.fromScale, config.toScale, finalValue)
            : initialScale * finalValue;
    }
}
