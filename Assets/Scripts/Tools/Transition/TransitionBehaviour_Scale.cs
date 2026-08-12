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
    }

    public Transform targetTransform;
    public List<ScaleTransitionEntry> transitions = new List<ScaleTransitionEntry>();

    protected override IEnumerator TransitionCoroutine(string transitionName)
    {
        var config = transitions.Find(t => t.name == transitionName);

        if (config == null)
        {
            Debug.LogWarning($"Scale transition not found: {transitionName}");
            yield break;
        }

        yield return AnimateScale(config);
    }

    private IEnumerator AnimateScale(ScaleTransitionEntry config)
    {
        Vector3 initialScale = targetTransform.localScale;
        float elapsed = 0f;

        while (elapsed < config.duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / config.duration;
            float scaleMultiplier = config.scaleCurve.Evaluate(t);
            targetTransform.localScale = initialScale * scaleMultiplier;
            yield return null;
        }

        targetTransform.localScale = initialScale * config.scaleCurve.Evaluate(1f);
    }
}
