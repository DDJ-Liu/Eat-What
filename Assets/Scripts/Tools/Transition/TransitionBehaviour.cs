using System;
using System.Collections;
using UnityEngine;

public abstract class TransitionBehaviour : MonoBehaviour
{
    protected TransitionController controller;
    protected Coroutine activeAnimationCoroutine;
    private Action activeComplete;
    private Action activeCancel;
    private bool callbackPending;

    public bool IsAnimating { get { return callbackPending || activeAnimationCoroutine != null; } }

    protected virtual void Awake()
    {
        controller = GetComponent<TransitionController>();
        if (controller == null)
        {
            Debug.LogError("TransitionBehaviour on " + gameObject.name + " requires TransitionController component");
            return;
        }
        controller.RegisterBehaviour(this);
    }

    public Coroutine PlayTransition(string transitionName)
    {
        return PlayTransition(transitionName, null, null);
    }

    public Coroutine PlayTransition(string transitionName, Action onComplete, Action onCancel = null)
    {
        Cancel();
        IEnumerator transition;
        if (!TryCreateTransition(transitionName, out transition) || transition == null)
        {
            if (onCancel != null) onCancel();
            return null;
        }
        return PlayRoutine(transition, onComplete, onCancel);
    }

    protected Coroutine PlayRoutine(IEnumerator transition, Action onComplete, Action onCancel)
    {
        Cancel();
        activeComplete = onComplete;
        activeCancel = onCancel;
        callbackPending = true;
        activeAnimationCoroutine = StartCoroutine(RunTransition(transition));
        return activeAnimationCoroutine;
    }

    public void StopActiveAnimation() { Cancel(); }

    public bool Cancel()
    {
        if (!callbackPending && activeAnimationCoroutine == null) return false;
        if (activeAnimationCoroutine != null) StopCoroutine(activeAnimationCoroutine);
        activeAnimationCoroutine = null;
        var callback = callbackPending ? activeCancel : null;
        ClearCallbacks();
        if (callback != null) callback();
        return true;
    }

    protected abstract bool TryCreateTransition(string transitionName, out IEnumerator transition);

    protected float GetDeltaTime(bool useUnscaledTime)
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    protected IEnumerator WaitForDuration(float duration, bool useUnscaledTime)
    {
        var elapsed = 0f;
        var safeDuration = duration <= 0f ? 0.0001f : duration;
        while (elapsed < safeDuration)
        {
            elapsed += GetDeltaTime(useUnscaledTime);
            yield return null;
        }
    }

    protected virtual void OnDisable() { Cancel(); }
    protected virtual void OnDestroy() { Cancel(); }

    private IEnumerator RunTransition(IEnumerator transition)
    {
        yield return transition;
        activeAnimationCoroutine = null;
        var callback = callbackPending ? activeComplete : null;
        ClearCallbacks();
        if (callback != null) callback();
    }

    private void ClearCallbacks()
    {
        callbackPending = false;
        activeComplete = null;
        activeCancel = null;
    }
}
