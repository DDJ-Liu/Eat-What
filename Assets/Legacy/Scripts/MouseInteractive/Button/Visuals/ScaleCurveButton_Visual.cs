using System.Collections;
using System.Security.Cryptography;
using UnityEngine;

public class ScaleCurveButton_Visual : Button_Visual
{
    [Header("Target Transform")]
    public Transform targetTransform;

    [Header("Scale Settings")]
    [SerializeField] private float idleScale = 1f;
    [SerializeField] private float highlightScale = 1.2f;

    [Header("Axis Control")]
    [SerializeField] private bool controlX = true;
    [SerializeField] private bool controlY = true;
    [SerializeField] private bool controlZ = true;

    [Header("Animation Curves")]
    [SerializeField] private float blendTimeMax = 0.3f;
    [SerializeField] private AnimationCurve idleToHighlightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve highlightToIdleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float pressTimeMax = 0.6f;
    [SerializeField] private AnimationCurve pressCurve = AnimationCurve.EaseInOut(0, 1, 1f, 0.4f);

    private enum AnimState { Idle, Highlighting, IdleToHighlight, HighlightToIdle, Pressing }
    private AnimState currentState = AnimState.Idle;
    private Coroutine currentAnimationCoroutine;
    private bool _mouseOver = false; // 记录鼠标是否悬停
    private Vector3 baseScale = Vector3.one; // Transform 在 Inspector 中设置的基础 Scale

    protected override void Start()
    {
        base.Start();
        if (targetTransform == null)
            targetTransform = transform;
        baseScale = targetTransform.localScale;
        SetScale(idleScale);
        currentState = AnimState.Idle;
    }

    protected void OnDisable()
    {
        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
            currentAnimationCoroutine = null;
        }
        SetScale(idleScale);
        currentState = AnimState.Idle;
        isPressing = false;
    }

    public override void setHighlight()
    {
        _mouseOver = true;
        if (isPressing) return;
        if (currentState == AnimState.Highlighting || currentState == AnimState.IdleToHighlight)
            return;

        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
            currentAnimationCoroutine = null;
        }

        float fromScale = GetCurrentScale();
        currentAnimationCoroutine = StartCoroutine(AnimateScale(fromScale, highlightScale, idleToHighlightCurve,
            AnimState.IdleToHighlight, AnimState.Highlighting));
    }

    public override void setIdle()
    {
        _mouseOver = false;
        if (isPressing) return;
        if (currentState == AnimState.Idle || currentState == AnimState.HighlightToIdle)
            return;

        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
            currentAnimationCoroutine = null;
        }

        float fromScale = GetCurrentScale();
        currentAnimationCoroutine = StartCoroutine(AnimateScale(fromScale, idleScale, highlightToIdleCurve,
            AnimState.HighlightToIdle, AnimState.Idle));
    }

    public override void OnPress()
    {
        if (isPressing) return;

        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
            currentAnimationCoroutine = null;
        }

        isPressing = true;
        currentState = AnimState.Pressing;
        currentAnimationCoroutine = StartCoroutine(PlayPressAnimation());
    }

    private IEnumerator AnimateScale(float fromScale, float toScale, AnimationCurve curve, AnimState runningState, AnimState endState)
    {
        currentState = runningState;

        // 确定动画时长：如果 blendTimeMax > 0 则使用 blendTimeMax，否则使用曲线自身时长
        float duration;
        if (blendTimeMax > 0f)
            duration = blendTimeMax;
        else
            duration = curve.keys.Length > 0 ? curve.keys[curve.length - 1].time : 0f;

        if (duration <= 0f)
        {
            SetScale(toScale);
            currentState = endState;
            currentAnimationCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;               // 归一化时间
            float factor = curve.Evaluate(t);            // 使用归一化时间评估曲线（曲线应设计为 t 在 0~1 范围）
            float currentScale = Mathf.LerpUnclamped(fromScale, toScale, factor);
            SetScale(currentScale);
            yield return null;
        }

        SetScale(toScale);
        currentState = endState;
        currentAnimationCoroutine = null;
    }

    private IEnumerator PlayPressAnimation()
    {
        float duration;
        if (pressTimeMax > 0f)
            duration = pressTimeMax;
        else
            duration = pressCurve.keys.Length > 0 ? pressCurve.keys[pressCurve.length - 1].time : 0f;

        if (duration <= 0f)
        {
            //delayedReady = true;
            FinishPress();
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float timeScale = elapsed / duration;
            float multiplier = pressCurve.Evaluate(timeScale);
            float currentScale = highlightScale * multiplier;
            SetScale(currentScale);
            yield return null;
        }

        SetScale(highlightScale);
        //delayedReady = true;
        FinishPress();
    }

    private void FinishPress()
    {
        isPressing = false;
        currentAnimationCoroutine = null;

        // 根据鼠标悬停状态恢复到高亮或空闲（无动画）
        if (_mouseOver)
        {
            SetScale(highlightScale);
            currentState = AnimState.Highlighting;
        }
        else
        {
            SetScale(idleScale);
            currentState = AnimState.Idle;
        }
    }

    private float GetCurrentScale()
    {
        if (targetTransform == null) return idleScale;
        Vector3 scale = targetTransform.localScale;
        // 从实际 scale 中除以 baseScale 还原出逻辑缩放值
        if (controlX && baseScale.x != 0f) return scale.x / baseScale.x;
        if (controlY && baseScale.y != 0f) return scale.y / baseScale.y;
        if (controlZ && baseScale.z != 0f) return scale.z / baseScale.z;
        return idleScale;
    }

    private void SetScale(float scale)
    {
        if (targetTransform == null) return;
        Vector3 newScale = targetTransform.localScale;
        // 将逻辑缩放值乘以 baseScale 得到实际 scale
        if (controlX) newScale.x = scale * baseScale.x;
        if (controlY) newScale.y = scale * baseScale.y;
        if (controlZ) newScale.z = scale * baseScale.z;
        targetTransform.localScale = newScale;
    }
}