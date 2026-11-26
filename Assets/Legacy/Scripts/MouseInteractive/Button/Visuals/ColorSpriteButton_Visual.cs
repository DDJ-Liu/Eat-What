using System.Collections;
using UnityEngine;

public class ColorSpriteButton_Visual : Button_Visual
{
    [SerializeField] private SpriteRenderer targetSprite;
    [Header("Colors")]
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color highLightColor = Color.yellow;
    [SerializeField] private Color notAllowColor = Color.gray;
    [SerializeField] private Color pressedColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    private void Awake()
    {
        if (targetSprite == null)
        {
            targetSprite = GetComponent<SpriteRenderer>();
        }
    }

    protected override void Start()
    {
        base.Start();

        if (targetSprite == null)
        {
            targetSprite = GetComponent<SpriteRenderer>();
        }
        setIdle();
    }

    private void Update()
    {
        if (IsAllowToUse())
        {
            if (targetSprite.color == notAllowColor)
            {
                targetSprite.color = idleColor;
            }
        }
        else
        {
            if (targetSprite.color == idleColor)
            {
                targetSprite.color = notAllowColor;
            }
        }
    }

    private void OnDisable()
    {
        if (IsAllowToUse()) targetSprite.color = idleColor;
        else targetSprite.color = notAllowColor;
    }

    public override void setHighlight()
    {
        if (isPressing) return;
        if (IsAllowToUse()) targetSprite.color = highLightColor;
        else targetSprite.color = notAllowColor;
    }

    public override void setIdle()
    {
        if (isPressing) return;
        if (IsAllowToUse()) targetSprite.color = idleColor;
        else targetSprite.color = notAllowColor;
    }

    public override void OnPress()
    {
        isPressing = true;
        //delayedReady = false;
        StartCoroutine(onPressed(0.3f));
    }

    IEnumerator onPressed(float waitTime)
    {
        targetSprite.color = highLightColor;
        yield return new WaitForSecondsRealtime(0.1f);
        targetSprite.color = pressedColor;
        yield return new WaitForSecondsRealtime(0.1f);
        if (IsAllowToUse()) targetSprite.color = idleColor;
        else targetSprite.color = notAllowColor;
        yield return new WaitForSecondsRealtime(waitTime);
        //delayedReady = true;
        isPressing = false;
    }
}
