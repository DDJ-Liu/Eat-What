using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ShowGameObjectSwitch_Visual : Switch_Visual
{
    [Header("每个状态的对象组")]
    public List<SwitchObjectGroup> stateGroups = new List<SwitchObjectGroup>();

    [Header("淡入淡出")]
    public bool enableFade = true;
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float fadeOutDuration = 0.15f;
    [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Serializable]
    public class SwitchObjectGroup
    {
        [Tooltip("此状态下显示的对象")]
        public List<GameObject> showObjects = new List<GameObject>();
        [Tooltip("此状态下隐藏的对象（通常是其他状态的对象，可留空让系统自动管理）")]
        public List<GameObject> hideObjects = new List<GameObject>();
    }

    private Coroutine _currentCoroutine;
    private int _displayedState = -1; // 当前正在显示的状态，-1 表示未初始化

    protected override void Start()
    {
        base.Start();

        // 初始隐藏所有对象
        foreach (var group in stateGroups)
        {
            if (group == null) continue;
            foreach (var obj in group.showObjects)
            {
                if (obj != null) { SetAlpha(obj, 0f); obj.SetActive(false); }
            }
        }

        // 显示当前状态
        int initState = switchButton != null ? switchButton.CurrentStateIndex : 0;
        ShowStateImmediate(initState);
    }

    private void OnDisable()
    {
        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
            _currentCoroutine = null;
        }
        // 隐藏所有对象
        foreach (var group in stateGroups)
        {
            if (group == null) continue;
            foreach (var obj in group.showObjects)
            {
                if (obj != null) { SetAlpha(obj, 0f); obj.SetActive(false); }
            }
        }
        _displayedState = -1;
    }

    // ── Switch_Visual 抽象方法实现 ──

    public override void setHighlight(int stateIndex)
    {
        // 此组件不区分高亮和空闲的对象显示，统一由状态决定
        EnsureStateDisplayed(stateIndex);
    }

    public override void setIdle(int stateIndex)
    {
        EnsureStateDisplayed(stateIndex);
    }

    public override void OnPress(int currentStateIndex, int targetStateIndex) { /*delayedReady = true;*/ }

    public override void OnStateChanged(int newState)
    {
        // 状态切换：淡出旧状态，淡入新状态
        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
            _currentCoroutine = null;
        }
        _currentCoroutine = StartCoroutine(TransitionToState(newState));
    }

    // ── 核心显示逻辑 ──

    private void EnsureStateDisplayed(int stateIndex)
    {
        if (_displayedState == stateIndex) return;
        ShowStateImmediate(stateIndex);
    }

    private void ShowStateImmediate(int stateIndex)
    {
        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
            _currentCoroutine = null;
        }

        // 隐藏所有状态的对象
        foreach (var group in stateGroups)
        {
            if (group == null) continue;
            foreach (var obj in group.showObjects)
            {
                if (obj != null) { SetAlpha(obj, 0f); obj.SetActive(false); }
            }
        }

        // 显示目标状态对象
        if (!HasValidState(stateIndex)) return;
        foreach (var obj in stateGroups[stateIndex].showObjects)
        {
            if (obj != null) { obj.SetActive(true); SetAlpha(obj, 1f); }
        }
        _displayedState = stateIndex;
    }

    private IEnumerator TransitionToState(int newState)
    {
        int oldState = _displayedState;

        // 淡出旧状态
        if (HasValidState(oldState))
        {
            var oldGroup = stateGroups[oldState];
            if (enableFade)
                yield return StartCoroutine(FadeObjects(oldGroup.showObjects, false));
            else
                foreach (var obj in oldGroup.showObjects)
                    if (obj != null) { SetAlpha(obj, 0f); obj.SetActive(false); }
        }

        // 淡入新状态
        if (HasValidState(newState))
        {
            var newGroup = stateGroups[newState];
            foreach (var obj in newGroup.showObjects)
                if (obj != null) { obj.SetActive(true); SetAlpha(obj, 0f); }

            if (enableFade)
                yield return StartCoroutine(FadeObjects(newGroup.showObjects, true));
            else
                foreach (var obj in newGroup.showObjects)
                    if (obj != null) SetAlpha(obj, 1f);
        }

        _displayedState = newState;
        _currentCoroutine = null;
    }

    private IEnumerator FadeObjects(List<GameObject> objects, bool fadeIn)
    {
        float duration = fadeIn ? fadeInDuration : fadeOutDuration;
        AnimationCurve curve = fadeIn ? fadeInCurve : fadeOutCurve;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = curve.Evaluate(Mathf.Clamp01(elapsed / duration));
            foreach (var obj in objects)
                if (obj != null) SetAlpha(obj, alpha);
            yield return null;
        }

        float finalAlpha = fadeIn ? 1f : 0f;
        foreach (var obj in objects)
        {
            if (obj == null) continue;
            SetAlpha(obj, finalAlpha);
            if (!fadeIn) obj.SetActive(false);
        }
    }

    // ── 辅助方法 ──

    private bool HasValidState(int stateIndex)
    {
        return stateGroups != null && stateIndex >= 0 && stateIndex < stateGroups.Count;
    }

    private void SetAlpha(GameObject obj, float alpha)
    {
        foreach (var sr in obj.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Color c = sr.color; c.a = alpha; sr.color = c;
        }
        foreach (var tmp in obj.GetComponentsInChildren<TMP_Text>(true))
        {
            Color c = tmp.color; c.a = alpha; tmp.color = c;
        }
    }
}
