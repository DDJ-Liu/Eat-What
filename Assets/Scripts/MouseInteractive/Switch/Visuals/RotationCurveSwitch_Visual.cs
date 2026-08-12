using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationCurveSwitch_Visual : Switch_Visual
{
    [Header("Target Transform")]
    public Transform targetTransform;

    [Header("Rotation Settings")]
    [Tooltip("为 true 时所有状态共用 stateRotations[0] 的旋转值，无需为每个状态单独配置")]
    public bool useSharedRotationForAllStates = false;
    public List<SwitchRotationSettings> stateRotations = new List<SwitchRotationSettings>();

    [Header("Animation Curves")]
    [SerializeField] private float blendTimeMax = 0.3f;
    [SerializeField] private AnimationCurve idleToHighlightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve highlightToIdleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Press (Unused)")]
    [SerializeField] private float pressTimeMax = 0f;
    [SerializeField] private AnimationCurve pressCurve;

    [Serializable]
    public class SwitchRotationSettings
    {
        public float idleRotation = 0f;
        public float highlightRotation = 15f;
    }

    private enum AnimState { Idle, Highlighting, IdleToHighlight, HighlightToIdle }
    private AnimState currentAnimState = AnimState.Idle;
    private Coroutine currentAnimationCoroutine;
    private bool _mouseOver = false;
    private float _baseRotationZ;

    protected override void Start()
    {
        base.Start();
        if (targetTransform == null)
            targetTransform = transform;
        _baseRotationZ = targetTransform.localEulerAngles.z;
        SetRotationZ(GetIdleRotation(switchButton != null ? switchButton.CurrentStateIndex : 0));
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
        SetRotationZ(GetIdleRotation(state));
        currentAnimState = AnimState.Idle;
    }

    public override void setHighlight(int stateIndex)
    {
        _mouseOver = true;
        if (currentAnimState == AnimState.Highlighting || currentAnimState == AnimState.IdleToHighlight)
            return;

        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
            currentAnimationCoroutine = null;
        }

        float fromAngle = GetCurrentRotationZ();
        float toAngle = GetHighlightRotation(stateIndex);
        currentAnimationCoroutine = StartCoroutine(AnimateRotation(fromAngle, toAngle, idleToHighlightCurve,
            AnimState.IdleToHighlight, AnimState.Highlighting));
    }

    public override void setIdle(int stateIndex)
    {
        _mouseOver = false;
        if (currentAnimState == AnimState.Idle || currentAnimState == AnimState.HighlightToIdle)
            return;

        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
            currentAnimationCoroutine = null;
        }

        float fromAngle = GetCurrentRotationZ();
        float toAngle = GetIdleRotation(stateIndex);
        currentAnimationCoroutine = StartCoroutine(AnimateRotation(fromAngle, toAngle, highlightToIdleCurve,
            AnimState.HighlightToIdle, AnimState.Idle));
    }

    // 旋转无按压行为
    public override void OnPress(int currentStateIndex, int targetStateIndex) { /*delayedReady = true;*/ }

    public override void OnStateChanged(int newState)
    {
        // 状态切换时直接跳转到新状态的空闲旋转（无动画）
        if (!isPressing)
        {
            if (currentAnimationCoroutine != null)
            {
                StopCoroutine(currentAnimationCoroutine);
                currentAnimationCoroutine = null;
            }
            SetRotationZ(GetIdleRotation(newState));
            currentAnimState = AnimState.Idle;
        }
    }

    private IEnumerator AnimateRotation(float fromAngle, float toAngle, AnimationCurve curve,
        AnimState runningState, AnimState endState)
    {
        currentAnimState = runningState;

        float duration = blendTimeMax > 0f ? blendTimeMax :
            (curve.keys.Length > 0 ? curve.keys[curve.length - 1].time : 0f);

        if (duration <= 0f)
        {
            SetRotationZ(toAngle);
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
            SetRotationZ(Mathf.LerpAngle(fromAngle, toAngle, factor));
            yield return null;
        }

        SetRotationZ(toAngle);
        currentAnimState = endState;
        currentAnimationCoroutine = null;
    }

    // ── 辅助方法 ──

    private float GetIdleRotation(int state)
    {
        return GetSettings(state).idleRotation;
    }

    private float GetHighlightRotation(int state)
    {
        return GetSettings(state).highlightRotation;
    }

    private SwitchRotationSettings GetSettings(int state)
    {
        if (useSharedRotationForAllStates || stateRotations == null || stateRotations.Count == 0)
        {
            return stateRotations != null && stateRotations.Count > 0
                ? stateRotations[0]
                : new SwitchRotationSettings();
        }
        int idx = Mathf.Clamp(state, 0, stateRotations.Count - 1);
        return stateRotations[idx];
    }

    private float GetCurrentRotationZ()
    {
        if (targetTransform == null) return GetIdleRotation(0);
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
