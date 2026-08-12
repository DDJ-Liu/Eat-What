using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ScaleLerp : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform targetTransform;

    [Header("Scale Settings")]
    [SerializeField] private Vector3 startScale = Vector3.one;
    [SerializeField] private Vector3 endScale = Vector3.one * 1.5f;

    [Header("Animation")]
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Execution Mode")]
    [SerializeField] private bool autoStart = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onComplete;

    private Coroutine animationCoroutine;

    private void Start()
    {
        if (targetTransform == null)
            targetTransform = transform;

        if (autoStart)
        {
            StartLerp();
        }
    }

    private void OnDisable()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
    }

    public void StartLerp()
    {
        // 停止已有的动画协程
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        animationCoroutine = StartCoroutine(AnimateScale());
    }

    private IEnumerator AnimateScale()
    {
        // 处理 duration <= 0 的边界情况
        if (duration <= 0f)
        {
            targetTransform.localScale = endScale;
            onComplete?.Invoke();
            animationCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration; // 归一化时间 [0, 1]
            float factor = curve.Evaluate(t); // 从曲线获取插值因子
            Vector3 currentScale = Vector3.LerpUnclamped(startScale, endScale, factor);
            targetTransform.localScale = currentScale;
            yield return null;
        }

        // 确保最终精确设置到 endScale
        targetTransform.localScale = endScale;
        onComplete?.Invoke();
        animationCoroutine = null;
    }
}
