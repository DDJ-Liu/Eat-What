using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ShowGameObjectButton_Visual : Button_Visual
{
    [Header("显示对象")]
    [Tooltip("悬停时显示、离开时隐藏的 GameObject 列表，每个对象上的 SpriteRenderer / TMP_Text 都会参与淡入淡出")]
    public List<GameObject> targetObjects = new List<GameObject>();

    [Header("淡入淡出")]
    public bool enableFade = true;
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float fadeOutDuration = 0.15f;
    [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    private enum FadeState { Hidden, FadingIn, Visible, FadingOut }
    private FadeState _fadeState = FadeState.Hidden;
    private Coroutine _currentCoroutine;

    protected override void Start()
    {
        base.Start();
        foreach (var obj in targetObjects)
        {
            if (obj != null)
            {
                SetAlpha(obj, 0f);
                obj.SetActive(false);
            }
        }
        _fadeState = FadeState.Hidden;
    }

    private void OnDisable()
    {
        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
            _currentCoroutine = null;
        }
        SetObjectsActive(false, 0f);
        _fadeState = FadeState.Hidden;
    }

    public override void setHighlight()
    {
        if (_fadeState == FadeState.FadingIn || _fadeState == FadeState.Visible) return;

        if (_currentCoroutine != null)
            StopCoroutine(_currentCoroutine);

        if (enableFade)
        {
            _currentCoroutine = StartCoroutine(FadeRoutine(true));
        }
        else
        {
            SetObjectsActive(true, 1f);
            _fadeState = FadeState.Visible;
        }
    }

    public override void setIdle()
    {
        if (_fadeState == FadeState.FadingOut || _fadeState == FadeState.Hidden) return;

        if (_currentCoroutine != null)
            StopCoroutine(_currentCoroutine);

        if (enableFade)
        {
            _currentCoroutine = StartCoroutine(FadeRoutine(false));
        }
        else
        {
            SetObjectsActive(false, 0f);
            _fadeState = FadeState.Hidden;
        }
    }

    public override void OnPress() { /*delayedReady = true;*/ }

    // ──────────────────────────────────────────────
    // 淡入淡出协程
    // ──────────────────────────────────────────────

    private IEnumerator FadeRoutine(bool fadeIn)
    {
        _fadeState = fadeIn ? FadeState.FadingIn : FadeState.FadingOut;

        float duration = fadeIn ? fadeInDuration : fadeOutDuration;
        AnimationCurve curve = fadeIn ? fadeInCurve : fadeOutCurve;

        if (fadeIn)
        {
            foreach (var obj in targetObjects)
            {
                if (obj == null) continue;
                obj.SetActive(true);
                SetAlpha(obj, 0f);
            }
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = curve.Evaluate(Mathf.Clamp01(elapsed / duration));
            foreach (var obj in targetObjects)
            {
                if (obj != null) SetAlpha(obj, alpha);
            }
            yield return null;
        }

        float finalAlpha = fadeIn ? 1f : 0f;
        foreach (var obj in targetObjects)
        {
            if (obj == null) continue;
            SetAlpha(obj, finalAlpha);
            if (!fadeIn) obj.SetActive(false);
        }

        _fadeState = fadeIn ? FadeState.Visible : FadeState.Hidden;
        _currentCoroutine = null;
    }

    // ──────────────────────────────────────────────
    // 辅助方法
    // ──────────────────────────────────────────────

    private void SetObjectsActive(bool active, float alpha)
    {
        foreach (var obj in targetObjects)
        {
            if (obj == null) continue;
            obj.SetActive(active);
            SetAlpha(obj, alpha);
        }
    }

    private void SetAlpha(GameObject obj, float alpha)
    {
        foreach (var sr in obj.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }

        foreach (var tmp in obj.GetComponentsInChildren<TMP_Text>(true))
        {
            Color c = tmp.color;
            c.a = alpha;
            tmp.color = c;
        }
    }
}
