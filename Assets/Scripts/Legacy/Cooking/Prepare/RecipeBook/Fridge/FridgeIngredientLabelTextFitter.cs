using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Keeps an ingredient label within its safe area by applying a stable one- or two-line TMP representation
/// and choosing the largest permitted font size that fits the available RectTransform.
/// </summary>
[AddComponentMenu("Cooking/Prepare/Fridge Ingredient Label Text Fitter")]
[DisallowMultipleComponent]
public sealed class FridgeIngredientLabelTextFitter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text targetText;
    [Tooltip("Optional yellow label interior. When empty, the target TMP RectTransform is used.")]
    [SerializeField] private RectTransform yellowSafeArea;

    [Header("Text Rules")]
    [Min(1)]
    [SerializeField] private int singleLineVisibleCharacterLimit = FridgeIngredientLabelTextFormatter.DefaultSingleLineCharacterLimit;
    [Min(1f)]
    [SerializeField] private float minFontSize = 18f;
    [Min(1f)]
    [SerializeField] private float maxFontSize = 36f;

    private const float SizeEpsilon = 0.01f;
    private const int FontSizeSearchIterations = 8;

    private bool subscribedToTextChanges;
    private bool isApplying;
    private bool missingTargetWarningLogged;
    private string lastSourceText;
    private string lastAppliedText;
    private Vector2 lastSafeAreaSize;
    private float lastAppliedMinFontSize;
    private float lastAppliedMaxFontSize;
    private int lastAppliedSingleLineLimit;

    /// <summary>
    /// Lets an ENGINE_MCP wiring task or another label owner request a refresh after changing serialized settings.
    /// Text changes themselves are already observed through TMPro_EventManager.TEXT_CHANGED_EVENT.
    /// </summary>
    public void RefreshTextLayout()
    {
        ApplyIfRequired(true);
    }

    private void OnEnable()
    {
        NormalizeSerializedValues();
        SubscribeToTextChanges();
        ApplyIfRequired(false);
    }

    private void Start()
    {
        // Layout groups can calculate their RectTransform after OnEnable. This is a one-time retry, not polling.
        ApplyIfRequired(false);
    }

    private void OnDisable()
    {
        UnsubscribeFromTextChanges();
    }

    private void OnDestroy()
    {
        UnsubscribeFromTextChanges();
    }

    private void OnRectTransformDimensionsChange()
    {
        ApplyIfRequired(false);
    }

    private void OnValidate()
    {
        NormalizeSerializedValues();

        if (Application.isPlaying)
        {
            ApplyIfRequired(true);
        }
    }

    private void SubscribeToTextChanges()
    {
        if (subscribedToTextChanges || !TryResolveTargetText())
        {
            return;
        }

        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        subscribedToTextChanges = true;
    }

    private void UnsubscribeFromTextChanges()
    {
        if (!subscribedToTextChanges)
        {
            return;
        }

        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        subscribedToTextChanges = false;
    }

    private void OnTextChanged(UnityEngine.Object changedObject)
    {
        if (isApplying || changedObject != targetText)
        {
            return;
        }

        ApplyIfRequired(false);
    }

    private void ApplyIfRequired(bool force)
    {
        if (!TryResolveTargetText())
        {
            return;
        }

        RectTransform safeArea = yellowSafeArea != null ? yellowSafeArea : targetText.rectTransform;
        if (safeArea == null)
        {
            return;
        }

        Vector2 safeAreaSize = safeArea.rect.size;
        if (safeAreaSize.x <= SizeEpsilon || safeAreaSize.y <= SizeEpsilon)
        {
            return;
        }

        string currentText = targetText.text ?? string.Empty;
        if (!string.Equals(currentText, lastAppliedText, StringComparison.Ordinal))
        {
            lastSourceText = currentText;
        }

        string sourceText = lastSourceText ?? currentText;
        string formattedText = FridgeIngredientLabelTextFormatter.Format(sourceText, singleLineVisibleCharacterLimit);

        if (!force
            && string.Equals(formattedText, lastAppliedText, StringComparison.Ordinal)
            && ApproximatelyEqual(safeAreaSize, lastSafeAreaSize)
            && Mathf.Approximately(minFontSize, lastAppliedMinFontSize)
            && Mathf.Approximately(maxFontSize, lastAppliedMaxFontSize)
            && singleLineVisibleCharacterLimit == lastAppliedSingleLineLimit)
        {
            return;
        }

        isApplying = true;
        try
        {
            targetText.enableAutoSizing = false;
            targetText.enableWordWrapping = false;
            targetText.overflowMode = TextOverflowModes.Ellipsis;

            float fontSize = FindLargestFittingFontSize(formattedText, safeAreaSize);
            if (!Mathf.Approximately(targetText.fontSize, fontSize))
            {
                targetText.fontSize = fontSize;
            }

            if (!string.Equals(targetText.text, formattedText, StringComparison.Ordinal))
            {
                targetText.text = formattedText;
            }
        }
        finally
        {
            isApplying = false;
        }

        lastAppliedText = formattedText;
        lastSafeAreaSize = safeAreaSize;
        lastAppliedMinFontSize = minFontSize;
        lastAppliedMaxFontSize = maxFontSize;
        lastAppliedSingleLineLimit = singleLineVisibleCharacterLimit;
    }

    private float FindLargestFittingFontSize(string formattedText, Vector2 safeAreaSize)
    {
        if (!DoesTextFit(formattedText, minFontSize, safeAreaSize))
        {
            // Ellipsis mode remains as the no-overflow fallback when even the configured minimum cannot fit.
            return minFontSize;
        }

        float lowerBound = minFontSize;
        float upperBound = maxFontSize;

        for (int iteration = 0; iteration < FontSizeSearchIterations; iteration++)
        {
            float candidate = (lowerBound + upperBound) * 0.5f;
            if (DoesTextFit(formattedText, candidate, safeAreaSize))
            {
                lowerBound = candidate;
            }
            else
            {
                upperBound = candidate;
            }
        }

        return lowerBound;
    }

    private bool DoesTextFit(string formattedText, float fontSize, Vector2 safeAreaSize)
    {
        targetText.fontSize = fontSize;
        Vector2 preferredSize = targetText.GetPreferredValues(formattedText);
        // TMP's Ellipsis overflow mode enforces the RectTransform boundary exactly. Allowing even the
        // general layout epsilon here can select a font that is slightly wider than the safe rect and
        // replace otherwise valid characters with an ellipsis at render time.
        return preferredSize.x <= safeAreaSize.x
               && preferredSize.y <= safeAreaSize.y;
    }

    private bool TryResolveTargetText()
    {
        if (targetText != null)
        {
            return true;
        }

        targetText = GetComponent<TMP_Text>();
        if (targetText != null)
        {
            return true;
        }

        if (!missingTargetWarningLogged)
        {
            Debug.LogWarning($"{nameof(FridgeIngredientLabelTextFitter)} on '{name}' needs a TMP_Text reference.", this);
            missingTargetWarningLogged = true;
        }

        return false;
    }

    private void NormalizeSerializedValues()
    {
        singleLineVisibleCharacterLimit = Mathf.Max(1, singleLineVisibleCharacterLimit);
        minFontSize = Mathf.Max(1f, minFontSize);
        maxFontSize = Mathf.Max(minFontSize, maxFontSize);
    }

    private static bool ApproximatelyEqual(Vector2 first, Vector2 second)
    {
        return Mathf.Abs(first.x - second.x) <= SizeEpsilon
               && Mathf.Abs(first.y - second.y) <= SizeEpsilon;
    }
}
