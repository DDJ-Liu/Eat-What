using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteSwitch_Visual : Switch_Visual
{
    [SerializeField] private SpriteRenderer targetSprite;
    private bool _mouseOver = false;

    [Header("每个状态的精灵组")]
    public List<SwitchStateSprites> stateSprites = new List<SwitchStateSprites>();

    [Serializable]
    public class SwitchStateSprites
    {
        public Sprite idle;
        public Sprite highlight;
        public Sprite notAllow;
        public Sprite pressed;
    }

    protected override void Start()
    {
        base.Start();

        if (targetSprite == null)
            targetSprite = GetComponent<SpriteRenderer>();

        // 确保每个状态都有后备精灵
        foreach (var s in stateSprites)
        {
            if (s.notAllow == null) s.notAllow = s.idle;
            if (s.pressed == null) s.pressed = s.idle;
        }

        setIdle();
    }

    private void Update()
    {
        if (button == null || targetSprite == null || isPressing) return;
        int state = switchButton.CurrentStateIndex;
        if (!HasValidState(state)) return;

        var sprites = stateSprites[state];
        if (button.allowToUse)
        {
            if (targetSprite.sprite == sprites.notAllow)
                targetSprite.sprite = sprites.idle;
        }
        else
        {
            if (targetSprite.sprite == sprites.idle)
                targetSprite.sprite = sprites.notAllow;
        }
    }

    private void OnDisable()
    {
        if (button == null || targetSprite == null) return;
        int state = switchButton != null ? switchButton.CurrentStateIndex : 0;
        if (!HasValidState(state)) return;

        var sprites = stateSprites[state];
        targetSprite.sprite = button.allowToUse ? sprites.idle : sprites.notAllow;
    }

    public override void setHighlight(int stateIndex)
    {
        _mouseOver = true;
        if (isPressing || targetSprite == null || !HasValidState(stateIndex)) return;
        var sprites = stateSprites[stateIndex];
        targetSprite.sprite = button.allowToUse ? sprites.highlight : sprites.notAllow;
    }

    public override void setIdle(int stateIndex)
    {
        _mouseOver = false;
        if (isPressing || targetSprite == null || !HasValidState(stateIndex)) return;
        var sprites = stateSprites[stateIndex];
        targetSprite.sprite = button.allowToUse ? sprites.idle : sprites.notAllow;
    }

    public override void OnPress(int currentStateIndex, int targetStateIndex)
    {
        isPressing = true;
        //delayedReady = false;
        StartCoroutine(OnPressed(currentStateIndex, targetStateIndex));
    }

    private IEnumerator OnPressed(int currentStateIndex, int targetStateIndex)
    {
        if (!HasValidState(targetStateIndex) || !HasValidState(currentStateIndex)) yield break;

        var prevSprites = stateSprites[currentStateIndex];
        var newSprites = stateSprites[targetStateIndex];

        // 旧状态 highlight → pressed
        targetSprite.sprite = button.allowToUse ? prevSprites.highlight : prevSprites.notAllow;
        yield return new WaitForSecondsRealtime(0.1f);
        targetSprite.sprite = prevSprites.pressed;
        yield return new WaitForSecondsRealtime(0.1f);
        // 新状态 highlight → idle/notAllow
        targetSprite.sprite = button.allowToUse ? newSprites.highlight : newSprites.notAllow;
        yield return new WaitForSecondsRealtime(0.1f);
        if (!_mouseOver)
            targetSprite.sprite = button.allowToUse ? newSprites.idle : newSprites.notAllow;
        

        //delayedReady = true;
        isPressing = false;
    }

    private bool HasValidState(int stateIndex)
    {
        if (stateSprites == null || stateIndex < 0 || stateIndex >= stateSprites.Count)
        {
            Debug.LogWarning($"[SpriteSwitch_Visual] {gameObject.name}: 状态 {stateIndex} 没有对应的精灵配置！");
            return false;
        }
        return true;
    }
}
