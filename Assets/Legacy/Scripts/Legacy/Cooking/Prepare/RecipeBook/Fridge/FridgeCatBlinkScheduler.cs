using System;
using System.Collections;
using EatWhat.Cooking.ShortCycle;
using UnityEngine;

/// <summary>
/// Requests a synchronized fridge-cat blink at randomized intervals.
/// One scheduler invokes the two independent FridgeCatEye Transition rigs together.
/// </summary>
[DisallowMultipleComponent]
public class FridgeCatBlinkScheduler : MonoBehaviour
{
    private const float DefaultMinBlinkInterval = 3f;
    private const float DefaultMaxBlinkInterval = 5f;

    [Header("Synchronized blink output")]
    [Tooltip("Independent left-eye rig. Both eye references are required before either eye is triggered.")]
    [SerializeField] private FridgeCatEye leftEye = null;
    [Tooltip("Independent right-eye rig. Both eye references are required before either eye is triggered.")]
    [SerializeField] private FridgeCatEye rightEye = null;

    [Header("Timing")]
    [Min(0f)]
    [Tooltip("Random wait lower bound in seconds. Defaults to 3 seconds.")]
    [SerializeField] private float minBlinkInterval = DefaultMinBlinkInterval;
    [Min(0f)]
    [Tooltip("Random wait upper bound in seconds. Defaults to 5 seconds.")]
    [SerializeField] private float maxBlinkInterval = DefaultMaxBlinkInterval;
    [Tooltip("First blink delay in seconds. A negative value uses the normal randomized interval.")]
    [SerializeField] private float firstBlinkDelay = -1f;

    private Coroutine blinkCoroutine;
    private Func<float, float, float> randomRangeProvider;
    private bool hasWarnedAboutMissingBlinkOutput;

    private void OnEnable()
    {
        StartBlinkScheduling();
    }

    private void OnDisable()
    {
        StopBlinkScheduling();
    }

    private void OnDestroy()
    {
        StopBlinkScheduling();
    }

    /// <summary>
    /// Starts the scheduler if this component is active and does not already own a coroutine.
    /// </summary>
    public void StartBlinkScheduling()
    {
        if (!isActiveAndEnabled || blinkCoroutine != null)
        {
            return;
        }

        blinkCoroutine = StartCoroutine(BlinkCoroutine());
    }

    /// <summary>
    /// Stops the active scheduler without starting another blink request.
    /// </summary>
    public void StopBlinkScheduling()
    {
        if (blinkCoroutine == null)
        {
            return;
        }

        StopCoroutine(blinkCoroutine);
        blinkCoroutine = null;
    }

    /// <summary>
    /// Immediately requests one complete blink. This is also safe to call from a UnityEvent.
    /// </summary>
    public void RequestBlink()
    {
        if (leftEye != null && rightEye != null && leftEye.CanBlink && rightEye.CanBlink)
        {
            leftEye.Blink();
            rightEye.Blink();
            return;
        }

        if (!hasWarnedAboutMissingBlinkOutput)
        {
            Debug.LogWarning("FridgeCatBlinkScheduler: Assign both independent FridgeCatEye references before scheduling synchronized blinks.", this);
            hasWarnedAboutMissingBlinkOutput = true;
        }
    }

    /// <summary>
    /// Allows external tests to supply deterministic samples. Pass null to restore UnityEngine.Random.Range.
    /// </summary>
    public void SetRandomRangeProvider(Func<float, float, float> provider)
    {
        randomRangeProvider = provider;
    }

    /// <summary>
    /// Returns the normalized random interval range. Reversed bounds are swapped and invalid values use defaults.
    /// </summary>
    public void GetBlinkIntervalRange(out float minimum, out float maximum)
    {
        minimum = NormalizeDelay(minBlinkInterval, DefaultMinBlinkInterval);
        maximum = NormalizeDelay(maxBlinkInterval, DefaultMaxBlinkInterval);

        if (minimum > maximum)
        {
            float temporary = minimum;
            minimum = maximum;
            maximum = temporary;
        }
    }

    private IEnumerator BlinkCoroutine()
    {
        yield return new WaitForSeconds(GetFirstBlinkDelay());

        while (isActiveAndEnabled)
        {
            RequestBlink();
            yield return new WaitForSeconds(GetRandomBlinkInterval());
        }

        blinkCoroutine = null;
    }

    private float GetFirstBlinkDelay()
    {
        if (IsValidNonNegativeDelay(firstBlinkDelay))
        {
            return firstBlinkDelay;
        }

        return GetRandomBlinkInterval();
    }

    private float GetRandomBlinkInterval()
    {
        float minimum;
        float maximum;
        GetBlinkIntervalRange(out minimum, out maximum);
        float sampledDelay = randomRangeProvider != null
            ? randomRangeProvider(minimum, maximum)
            : UnityEngine.Random.Range(minimum, maximum);

        return IsValidNonNegativeDelay(sampledDelay)
            ? Mathf.Clamp(sampledDelay, minimum, maximum)
            : minimum;
    }

    private static float NormalizeDelay(float value, float fallback)
    {
        return IsValidNonNegativeDelay(value) ? value : fallback;
    }

    private static bool IsValidNonNegativeDelay(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
    }
}
