using System.Collections;
using UnityEngine;
using TMPro;

public class ColorTextButton_Visual : Button_Visual
{
    [SerializeField] private TMP_Text targetText;
    [Header("Colors")]
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color highLightColor = Color.yellow;
    [SerializeField] private Color notAllowColor = Color.gray;
    [SerializeField] private Color pressedColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    protected override void Start()
    {
        base.Start();

        if (targetText == null)
        {
            targetText = GetComponent<TMP_Text>();
        }
        setIdle();
    }

    private void Update()
    {
        if (IsAllowToUse())
        {
            if (targetText.color == notAllowColor)
            {
                targetText.color = idleColor;
            }
        }
        else
        {
            if (targetText.color == idleColor)
            {
                targetText.color = notAllowColor;
            }
        }
    }

    private void OnDisable()
    {
        if (IsAllowToUse()) targetText.color = idleColor;
        else targetText.color = notAllowColor;
    }

    public override void setHighlight()
    {
        if (isPressing) return;
        if (IsAllowToUse()) targetText.color = highLightColor;
        else targetText.color = notAllowColor;
    }

    public override void setIdle()
    {
        if (isPressing) return;
        if (IsAllowToUse()) targetText.color = idleColor;
        else targetText.color = notAllowColor;
    }

    public override void OnPress()
    {
        isPressing = true;
        //delayedReady = false;
        StartCoroutine(onPressed(0.3f));
    }

    IEnumerator onPressed(float waitTime)
    {
        targetText.color = highLightColor;
        yield return new WaitForSecondsRealtime(0.1f);
        targetText.color = pressedColor;
        yield return new WaitForSecondsRealtime(0.1f);
        if (IsAllowToUse()) targetText.color = idleColor;
        else targetText.color = notAllowColor;
        yield return new WaitForSecondsRealtime(waitTime);
        //delayedReady = true;
        isPressing = false;
    }
}
