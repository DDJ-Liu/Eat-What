using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ColorImageButton_Visual : Button_Visual
{
    [SerializeField] private Image targetImage;
    [Header("Colors")]
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color highLightColor = Color.yellow;
    [SerializeField] private Color notAllowColor = Color.gray;
    [SerializeField] private Color pressedColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    private void Awake()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }
    }

    protected override void Start()
    {
        base.Start();

        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }
        setIdle();
    }

    private void Update()
    {
        if (IsAllowToUse())
        {
            if (targetImage.color == notAllowColor)
            {
                targetImage.color = idleColor;
            }
        }
        else
        {
            if (targetImage.color == idleColor)
            {
                targetImage.color = notAllowColor;
            }
        }
    }

    private void OnDisable()
    {
        if (IsAllowToUse()) targetImage.color = idleColor;
        else targetImage.color = notAllowColor;
    }

    public override void setHighlight()
    {
        if (isPressing) return;
        if (IsAllowToUse()) targetImage.color = highLightColor;
        else targetImage.color = notAllowColor;
    }

    public override void setIdle()
    {
        if (isPressing) return;
        if (IsAllowToUse()) targetImage.color = idleColor;
        else targetImage.color = notAllowColor;
    }

    public override void OnPress()
    {
        isPressing = true;
        StartCoroutine(onPressed(0.3f));
    }

    IEnumerator onPressed(float waitTime)
    {
        targetImage.color = highLightColor;
        yield return new WaitForSecondsRealtime(0.1f);
        targetImage.color = pressedColor;
        yield return new WaitForSecondsRealtime(0.1f);
        if (IsAllowToUse()) targetImage.color = idleColor;
        else targetImage.color = notAllowColor;
        yield return new WaitForSecondsRealtime(waitTime);
        isPressing = false;
    }
}
