using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ImageButton_Visual : Button_Visual
{
    [SerializeField] private Image targetImage;
    [Header("Sprites")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite highLightSprite;
    [SerializeField] private Sprite NotAllowSprite;
    [SerializeField] private Sprite pressedSprite;

    protected override void Start()
    {
        base.Start();

        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }
        if (NotAllowSprite == null) NotAllowSprite = idleSprite;
        if (pressedSprite == null) pressedSprite = idleSprite;
        setIdle();
    }
    private void Update()
    {
        if (IsAllowToUse())
        {
            if (targetImage.sprite == NotAllowSprite)
            {
                targetImage.sprite = idleSprite;
            }
        }
        else
        {
            if (targetImage.sprite == idleSprite)
            {
                targetImage.sprite = NotAllowSprite;
            }
        }
    }
    private void OnDisable()
    {
        if (IsAllowToUse()) targetImage.sprite = idleSprite;
        else targetImage.sprite = NotAllowSprite;
    }

    public override void setHighlight()
    {
        if (isPressing)
        {
            return;
        }
        if (IsAllowToUse()) targetImage.sprite = highLightSprite;
        else targetImage.sprite = NotAllowSprite;
    }

    public override void setIdle()
    {
        if (isPressing)
        {
            return;
        }
        if (IsAllowToUse()) targetImage.sprite = idleSprite;
        else targetImage.sprite = NotAllowSprite;
    }

    public override void OnPress()
    {
        isPressing = true;
        StartCoroutine(onPressed(0.3f));
    }


    IEnumerator onPressed(float waitTime)
    {
        targetImage.sprite = highLightSprite;
        yield return new WaitForSecondsRealtime(0.1f);
        targetImage.sprite = pressedSprite;
        yield return new WaitForSecondsRealtime(0.1f);
        if (IsAllowToUse()) targetImage.sprite = idleSprite;
        else targetImage.sprite = NotAllowSprite;
        yield return new WaitForSecondsRealtime(waitTime);
        isPressing = false;
    }
}
