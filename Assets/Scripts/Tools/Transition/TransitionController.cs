using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransitionController : MonoBehaviour
{
    [Header("Preset Target")]
    [SerializeField] private Transform presetTarget = null;
    [SerializeField] private List<SpriteRenderer> presetAlphaRenderers = new List<SpriteRenderer>();
    [SerializeField] private List<TransitionPreset> presets = new List<TransitionPreset>();
    [SerializeField] private float presetDuration = 0.3f;
    [SerializeField] private AnimationCurve presetCurve = null;
    [SerializeField] private bool presetUseUnscaledTime = true;

    private List<TransitionBehaviour> registeredBehaviours = new List<TransitionBehaviour>();
    private List<Coroutine> activeCoroutines = new List<Coroutine>();
    private Coroutine activePresetCoroutine;
    private Action activeCancel;
    private int generation;

    public void RegisterBehaviour(TransitionBehaviour behaviour)
    {
        if (behaviour != null && !registeredBehaviours.Contains(behaviour)) registeredBehaviours.Add(behaviour);
    }

    public void PlayTransition(string transitionName, Action onComplete = null)
    {
        CancelAll();
        var requestGeneration = ++generation;
        var remaining = registeredBehaviours.Count;
        if (remaining == 0)
        {
            if (onComplete != null) onComplete();
            return;
        }

        activeCoroutines.Clear();
        foreach (var behaviour in registeredBehaviours)
        {
            if (behaviour == null)
            {
                CompleteBehaviour(requestGeneration, ref remaining, onComplete);
                continue;
            }
            var coroutine = behaviour.PlayTransition(
                transitionName,
                () => CompleteBehaviour(requestGeneration, ref remaining, onComplete),
                () => CompleteBehaviour(requestGeneration, ref remaining, onComplete));
            if (coroutine != null) activeCoroutines.Add(coroutine);
        }
    }

    public void StopAllTransitions() { CancelAll(); }

    public bool GoTo(string presetName, Action onComplete = null, Action onCancel = null)
    {
        TransitionPreset preset = null;
        foreach (var candidate in presets)
            if (candidate != null && candidate.name == presetName) { preset = candidate; break; }
        if (preset == null || presetTarget == null) return false;

        CancelAll();
        var requestGeneration = ++generation;
        activeCancel = onCancel;
        activePresetCoroutine = StartCoroutine(ApplyPreset(requestGeneration, preset, onComplete));
        return true;
    }

    public bool CancelAll()
    {
        var cancelled = activePresetCoroutine != null || activeCoroutines.Count > 0 || activeCancel != null;
        generation++;
        if (activePresetCoroutine != null)
        {
            StopCoroutine(activePresetCoroutine);
            activePresetCoroutine = null;
        }
        foreach (var behaviour in registeredBehaviours)
            if (behaviour != null) cancelled |= behaviour.Cancel();
        activeCoroutines.Clear();
        var callback = activeCancel;
        activeCancel = null;
        if (callback != null) callback();
        return cancelled;
    }

    public bool CapturePreset(string presetName)
    {
        if (presetTarget == null) return false;
        TransitionPreset preset = null;
        foreach (var candidate in presets)
            if (candidate != null && candidate.name == presetName) { preset = candidate; break; }
        if (preset == null) return false;
        preset.position = preset.positionSpace == TransitionCoordinateSpace.Local
            ? presetTarget.localPosition
            : presetTarget.position;
        preset.localScale = presetTarget.localScale;
        if (presetAlphaRenderers.Count > 0 && presetAlphaRenderers[0] != null)
            preset.alpha = presetAlphaRenderers[0].color.a;
        return true;
    }

    private IEnumerator ApplyPreset(int requestGeneration, TransitionPreset preset, Action onComplete)
    {
        var fromPosition = preset.positionSpace == TransitionCoordinateSpace.Local ? presetTarget.localPosition : presetTarget.position;
        var fromScale = presetTarget.localScale;
        var fromAlpha = new float[presetAlphaRenderers.Count];
        for (var index = 0; index < fromAlpha.Length; index++)
            fromAlpha[index] = presetAlphaRenderers[index] == null ? 0f : presetAlphaRenderers[index].color.a;

        var elapsed = 0f;
        var duration = presetDuration <= 0f ? 0.0001f : presetDuration;
        while (elapsed < duration)
        {
            elapsed += presetUseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            var value = presetCurve == null ? progress : presetCurve.Evaluate(progress);
            if (preset.applyPosition) WritePosition(preset.positionSpace, Vector3.LerpUnclamped(fromPosition, preset.position, value));
            if (preset.applyScale) presetTarget.localScale = Vector3.LerpUnclamped(fromScale, preset.localScale, value);
            if (preset.applyAlpha) ApplyAlpha(fromAlpha, preset.alpha, value);
            yield return null;
        }

        if (requestGeneration != generation) yield break;
        if (preset.applyPosition) WritePosition(preset.positionSpace, preset.position);
        if (preset.applyScale) presetTarget.localScale = preset.localScale;
        if (preset.applyAlpha) ApplyAlpha(fromAlpha, preset.alpha, 1f);
        foreach (var row in preset.visibility ?? new List<TransitionVisibilityTarget>())
            if (row != null && row.target != null) row.target.SetActive(row.visible);
        activePresetCoroutine = null;
        activeCancel = null;
        if (onComplete != null) onComplete();
    }

    private void ApplyAlpha(float[] from, float target, float progress)
    {
        for (var index = 0; index < presetAlphaRenderers.Count; index++)
        {
            var renderer = presetAlphaRenderers[index];
            if (renderer == null) continue;
            var color = renderer.color;
            color.a = Mathf.LerpUnclamped(from[index], target, progress);
            renderer.color = color;
        }
    }

    private void WritePosition(TransitionCoordinateSpace space, Vector3 value)
    {
        if (space == TransitionCoordinateSpace.Local) presetTarget.localPosition = value;
        else presetTarget.position = value;
    }

    private void CompleteBehaviour(int requestGeneration, ref int remaining, Action onComplete)
    {
        if (requestGeneration != generation) return;
        remaining--;
        if (remaining > 0) return;
        activeCoroutines.Clear();
        if (onComplete != null) onComplete();
    }

    private void OnDisable() { CancelAll(); }
    private void OnDestroy() { CancelAll(); }
}
