using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransitionBehaviour_Alpha : TransitionBehaviour
{
    [System.Serializable]
    public class AlphaTransitionEntry
    {
        public string name;
        public float duration = 0.3f;
        public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public bool useUnscaledTime;
        public bool useExplicitRange;
        public float fromAlpha;
        public float toAlpha = 1f;
    }

    [Tooltip("目标渲染器列表")]
    public List<SpriteRenderer> targetRenderers = new List<SpriteRenderer>();

    [Header("Auto Collection")]
    [Tooltip("从此父对象自动收集子 SpriteRenderer")]
    public Transform targetParent;

    public List<AlphaTransitionEntry> transitions = new List<AlphaTransitionEntry>();

    protected override void Awake()
    {
        base.Awake();
        AutoCollectRenderers();
    }

    private void AutoCollectRenderers()
    {
        if (targetParent != null)
        {
            targetRenderers.Clear();
            foreach (var sr in targetParent.GetComponentsInChildren<SpriteRenderer>())
            {
                targetRenderers.Add(sr);
            }
        }
    }

    private List<SpriteRenderer> GetActiveRenderers()
    {
        if (targetRenderers != null && targetRenderers.Count > 0)
            return targetRenderers;

        return new List<SpriteRenderer>();
    }

    protected override bool TryCreateTransition(string transitionName, out IEnumerator transition)
    {
        var config = transitions.Find(t => t.name == transitionName);
        if (config == null)
        {
            Debug.LogWarning("Alpha transition not found: " + transitionName);
            transition = null;
            return false;
        }
        transition = FadeAlpha(config);
        return true;
    }

    private IEnumerator FadeAlpha(AlphaTransitionEntry config)
    {
        float elapsed = 0f;
        var renderers = GetActiveRenderers();

        if (renderers.Count == 0)
        {
            Debug.LogWarning("No target renderers found on " + gameObject.name);
            yield break;
        }

        Color[] colors = new Color[renderers.Count];
        for (int i = 0; i < renderers.Count; i++)
        {
            colors[i] = renderers[i].color;
        }

        while (elapsed < config.duration)
        {
            elapsed += GetDeltaTime(config.useUnscaledTime);
            float t = elapsed / config.duration;
            float curveValue = config.curve.Evaluate(t);
            float alpha = config.useExplicitRange
                ? Mathf.LerpUnclamped(config.fromAlpha, config.toAlpha, curveValue)
                : curveValue;

            for (int i = 0; i < renderers.Count; i++)
            {
                Color color = colors[i];
                color.a = alpha;
                renderers[i].color = color;
            }

            yield return null;
        }

        float finalCurveValue = config.curve.Evaluate(1f);
        float finalAlpha = config.useExplicitRange
            ? Mathf.LerpUnclamped(config.fromAlpha, config.toAlpha, finalCurveValue)
            : finalCurveValue;
        for (int i = 0; i < renderers.Count; i++)
        {
            Color color = colors[i];
            color.a = finalAlpha;
            renderers[i].color = color;
        }
    }
}
