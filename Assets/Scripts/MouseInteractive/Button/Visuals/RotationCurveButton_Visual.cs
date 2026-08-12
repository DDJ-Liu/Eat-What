using System.Collections;
using UnityEngine;

public class RotationCurveButton_Visual : Button_Visual
{
    [Header("Target Transform")]
    public Transform targetTransform;

    [Header("Rotation Settings")]
    [SerializeField] private float idleRotation = 0f;          // 空闲时的角度
    [SerializeField] private float highlightRotation = 15f;    // 高亮时的角度（例如 15° 或 -15°）

    [Header("Animation Curves")]
    [SerializeField] private float blendTimeMax = 0.3f;        // 过渡时长（若为 0 则取曲线时长）
    [SerializeField] private AnimationCurve idleToHighlightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve highlightToIdleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // 按压动画在此脚本中无意义，保留变量仅为与原始结构保持一致，实际不使用
    [Header("Press (Unused)")]
    [SerializeField] private float pressTimeMax = 0f;
    [SerializeField] private AnimationCurve pressCurve;

    private enum AnimState { Idle, Highlighting, IdleToHighlight, HighlightToIdle }
    private AnimState currentState = AnimState.Idle;
    private Coroutine currentAnimationCoroutine;
    private bool _mouseOver = false; // 记录鼠标是否悬停
    private float _baseRotationZ;    // 初始基准旋转角度

    protected override void Start()
    {
        base.Start();
        if (targetTransform == null)
            targetTransform = transform;
        _baseRotationZ = targetTransform.localEulerAngles.z;
        SetRotationZ(idleRotation);
        currentState = AnimState.Idle;
    }

    protected void OnDisable()
    {
        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
            currentAnimationCoroutine = null;
        }
        SetRotationZ(idleRotation);
        currentState = AnimState.Idle;
    }

    public override void setHighlight()
    {
        _mouseOver = true;
        // 按压逻辑已移除，直接判断状态
        if (currentState == AnimState.Highlighting || currentState == AnimState.IdleToHighlight)
            return;

        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
            currentAnimationCoroutine = null;
        }

        float fromAngle = GetCurrentRotationZ();
        currentAnimationCoroutine = StartCoroutine(AnimateRotation(fromAngle, highlightRotation, idleToHighlightCurve,
            AnimState.IdleToHighlight, AnimState.Highlighting));
    }

    public override void setIdle()
    {
        _mouseOver = false;
        if (currentState == AnimState.Idle || currentState == AnimState.HighlightToIdle)
            return;

        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
            currentAnimationCoroutine = null;
        }

        float fromAngle = GetCurrentRotationZ();
        currentAnimationCoroutine = StartCoroutine(AnimateRotation(fromAngle, idleRotation, highlightToIdleCurve,
            AnimState.HighlightToIdle, AnimState.Idle));
    }

    // 按压无意义，直接返回
    public override void OnPress()
    {
        // 不需要任何按压行为
        //delayedReady = true;
    }

    private IEnumerator AnimateRotation(float fromAngle, float toAngle, AnimationCurve curve, AnimState runningState, AnimState endState)
    {
        currentState = runningState;

        // 确定动画时长
        float duration;
        if (blendTimeMax > 0f)
            duration = blendTimeMax;
        else
            duration = curve.keys.Length > 0 ? curve.keys[curve.length - 1].time : 0f;

        if (duration <= 0f)
        {
            SetRotationZ(toAngle);
            currentState = endState;
            currentAnimationCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float factor = curve.Evaluate(t);
            // 使用 LerpAngle 处理角度环绕问题
            float currentAngle = Mathf.LerpAngle(fromAngle, toAngle, factor);
            SetRotationZ(currentAngle);
            yield return null;
        }

        SetRotationZ(toAngle);
        currentState = endState;
        currentAnimationCoroutine = null;
    }

    private float GetCurrentRotationZ()
    {
        if (targetTransform == null) return idleRotation;
        return targetTransform.localEulerAngles.z - _baseRotationZ;
    }

    private void SetRotationZ(float angle)
    {
        if (targetTransform == null) return;
        Vector3 angles = targetTransform.localEulerAngles;
        angles.z = _baseRotationZ + angle;
        targetTransform.localEulerAngles = angles;
    }
}