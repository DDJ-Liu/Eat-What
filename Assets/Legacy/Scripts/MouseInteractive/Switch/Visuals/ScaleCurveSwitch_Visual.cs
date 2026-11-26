using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleCurveSwitch_Visual : Switch_Visual
{
    [Header("Target Transform")]
    public Transform targetTransform;

    [Header("Axis Control")]
    [SerializeField] private bool controlX = true;
    [SerializeField] private bool controlY = true;
    [SerializeField] private bool controlZ = true;

    [Header("Scale Settings")]
    [Tooltip("为 true 时所有状态共用 stateScales[0] 的缩放值，无需为每个状态单独配置")]
    public bool useSharedScaleForAllStates = false;
    public List<SwitchScaleSettings> stateScales = new List<SwitchScaleSettings>();

    [Header("Animation Curves")]
    [SerializeField] private float blendTimeMax = 0.3f;
    [SerializeField] private AnimationCurve idleToHighlightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve highlightToIdleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float pressTimeMax = 0.6f;
    [SerializeField] private AnimationCurve pressCurve = AnimationCurve.EaseInOut(0, 1, 1f, 0.4f);
    [Tooltip("press 动画进度超过此值后，stateScale 切换为 targetState 的缩放（0=始终用targetState，1=始终用currentState）")]
    [SerializeField] [Range(0f, 1f)] private float pressBlendPoint = 0.3f;

    [Serializable]
    public class SwitchScaleSettings
    {
        public float idleScale = 1f;
        public float highlightScale = 1.2f;
    }

    private enum AnimState { Idle, Highlighting, IdleToHighlight, HighlightToIdle, Pressing }
    private AnimState currentAnimState = AnimState.Idle;
    private Coroutine currentAnimationCoroutine;
    private bool _mouseOver = false;
    private Vector3 baseScale = Vector3.one; // Transform 在 Inspector 中设置的基础 Scale

    protected override void Start()
    {
        base.Start();
        if (targetTransform == null)
            targetTransform = transform;
        baseScale = targetTransform.localScale;
        SetScale(GetIdleScale(switchButton != null ? switchButton.CurrentStateIndex : 0));
        currentAnimState = AnimState.Idle;
    }

    protected void OnDisable()
    {
        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
            currentAnimationCoroutine = null;
        }
        int state = switchButton != null ? switchButton.CurrentStateIndex : 0;
        SetScale(GetIdleScale(state));
        currentAnimState = AnimState.Idle;
        isPressing = false;
    }

    public override void setHighlight(int stateIndex)
    {
        _mouseOver = true;
        if (isPressing) return;
        if (currentAnimState == AnimState.Highlighting || currentAnimState == AnimState.IdleToHighlight)
            return;

        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
            currentAnimationCoroutine = null;
        }

        float fromScale = GetCurrentScale();
        float toScale = GetHighlightScale(stateIndex);
        currentAnimationCoroutine = StartCoroutine(AnimateScale(fromScale, toScale, idleToHighlightCurve,
            AnimState.IdleToHighlight, AnimState.Highlighting));
    }

    public override void setIdle(int stateIndex)
    {
        _mouseOver = false;
        if (isPressing) return;
        if (currentAnimState == AnimState.Idle || currentAnimState == AnimState.HighlightToIdle)
            return;

        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
            currentAnimationCoroutine = null;
        }

        float fromScale = GetCurrentScale();
        float toScale = GetIdleScale(stateIndex);
        currentAnimationCoroutine = StartCoroutine(AnimateScale(fromScale, toScale, highlightToIdleCurve,
            AnimState.HighlightToIdle, AnimState.Idle));
    }

    public override void OnPress(int currentStateIndex, int targetStateIndex)
    {
        if (isPressing) return;

        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
            currentAnimationCoroutine = null;
        }

        isPressing = true;
        currentAnimState = AnimState.Pressing;
        currentAnimationCoroutine = StartCoroutine(PlayPressAnimation(currentStateIndex, targetStateIndex));
    }

    private IEnumerator AnimateScale(float fromScale, float toScale, AnimationCurve curve,
        AnimState runningState, AnimState endState)
    {
        currentAnimState = runningState;

        float duration = blendTimeMax > 0f ? blendTimeMax :
            (curve.keys.Length > 0 ? curve.keys[curve.length - 1].time : 0f);

        if (duration <= 0f)
        {
            SetScale(toScale);
            currentAnimState = endState;
            currentAnimationCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float factor = curve.Evaluate(t);
            SetScale(Mathf.LerpUnclamped(fromScale, toScale, factor));
            yield return null;
        }

        SetScale(toScale);
        currentAnimState = endState;
        currentAnimationCoroutine = null;
    }

    private IEnumerator PlayPressAnimation(int currentStateIndex, int newStateIndex)
    {
        float duration = pressTimeMax > 0f ? pressTimeMax :
            (pressCurve.keys.Length > 0 ? pressCurve.keys[pressCurve.length - 1].time : 0f);

        if (duration <= 0f)
        {
            //delayedReady = true;
            FinishPress(newStateIndex);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float stateScale = t < pressBlendPoint
                ? GetHighlightScale(currentStateIndex)
                : (_mouseOver ? GetHighlightScale(newStateIndex) : GetIdleScale(newStateIndex));
            float multiplier = pressCurve.Evaluate(t);
            SetScale(stateScale * multiplier);
            yield return null;
        }

        float finalStateScale = _mouseOver ? GetHighlightScale(newStateIndex) : GetIdleScale(newStateIndex);
        SetScale(finalStateScale * pressCurve.Evaluate(1f));
        //delayedReady = true;
        FinishPress(newStateIndex);
    }

    private void FinishPress(int stateIndex)
    {
        isPressing = false;
        currentAnimationCoroutine = null;

        if (_mouseOver)
        {
            SetScale(GetHighlightScale(stateIndex));
            currentAnimState = AnimState.Highlighting;
        }
        else
        {
            SetScale(GetIdleScale(stateIndex));
            currentAnimState = AnimState.Idle;
        }
    }

    // ── 辅助方法 ──

    private float GetIdleScale(int state)
    {
        return GetSettings(state).idleScale;
    }

    private float GetHighlightScale(int state)
    {
        return GetSettings(state).highlightScale;
    }

    private SwitchScaleSettings GetSettings(int state)
    {
        if (useSharedScaleForAllStates || stateScales == null || stateScales.Count == 0)
        {
            return stateScales != null && stateScales.Count > 0
                ? stateScales[0]
                : new SwitchScaleSettings();
        }
        int idx = Mathf.Clamp(state, 0, stateScales.Count - 1);
        return stateScales[idx];
    }

    private float GetCurrentScale()
    {
        if (targetTransform == null) return GetIdleScale(0);
        Vector3 scale = targetTransform.localScale;
        if (controlX && baseScale.x != 0f) return scale.x / baseScale.x;
        if (controlY && baseScale.y != 0f) return scale.y / baseScale.y;
        if (controlZ && baseScale.z != 0f) return scale.z / baseScale.z;
        return GetIdleScale(0);
    }

    private void SetScale(float scale)
    {
        if (targetTransform == null) return;
        Vector3 newScale = targetTransform.localScale;
        if (controlX) newScale.x = scale * baseScale.x;
        if (controlY) newScale.y = scale * baseScale.y;
        if (controlZ) newScale.z = scale * baseScale.z;
        targetTransform.localScale = newScale;
    }
}
