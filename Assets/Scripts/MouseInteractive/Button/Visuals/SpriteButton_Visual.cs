using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SpriteButton_Visual : Button_Visual
{
    [SerializeField] private SpriteRenderer targetSprite;
    [Header("Sprites")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite highLightSprite;
    [SerializeField] private Sprite NotAllowSprite;
    [SerializeField] private Sprite pressedSprite;
    [Header("Startup")]
    [SerializeField] private float startupDelay = 0.1f;

    protected async override void Start()
    {
        base.Start();

        if (targetSprite == null)
        {
            targetSprite = GetComponent<SpriteRenderer>();
        }
        if (NotAllowSprite == null) NotAllowSprite = idleSprite;
        if (pressedSprite == null) pressedSprite = idleSprite;
        targetSprite.sprite = NotAllowSprite;

        await Task.Delay(Mathf.RoundToInt(startupDelay * 1000));
        if (this == null) return;

        setIdle();
    }
    private void Update()
    {
        if (IsAllowToUse())
        {
            if (targetSprite.sprite == NotAllowSprite)
            {
                targetSprite.sprite = idleSprite;
            }
        }
        else
        {
            if (targetSprite.sprite == idleSprite)
            {
                targetSprite.sprite = NotAllowSprite;
            }
        }
    }

    private void OnEnable()
    {
        if (targetSprite == null)
        {
            targetSprite = GetComponent<SpriteRenderer>();
        }
        targetSprite.sprite = NotAllowSprite;
    }

    private void OnDisable()
    {
        if (IsAllowToUse()) targetSprite.sprite = idleSprite;
        else targetSprite.sprite = NotAllowSprite;
    }

    public override void setHighlight()
    {
        if (isPressing)
        {
            return;
        }
        if (IsAllowToUse()) targetSprite.sprite = highLightSprite;
        else targetSprite.sprite = NotAllowSprite;
    }

    public override void setIdle()
    {
        if (isPressing)
        {
            return;
        }
        if (IsAllowToUse()) targetSprite.sprite = idleSprite;
        else targetSprite.sprite = NotAllowSprite;
    }

    public override void OnPress()
    {
        isPressing = true;
        //delayedReady = false;
        StartCoroutine(onPressed(0.3f));
    }


    IEnumerator onPressed(float waitTime)
    {
        targetSprite.sprite = highLightSprite;
        yield return new WaitForSecondsRealtime(0.1f);
        targetSprite.sprite = pressedSprite;
        yield return new WaitForSecondsRealtime(0.1f);
        if (IsAllowToUse()) targetSprite.sprite = idleSprite;
        else targetSprite.sprite = NotAllowSprite;
        yield return new WaitForSecondsRealtime(waitTime);
        //delayedReady = true;
        isPressing = false;
    }
}
