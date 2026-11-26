using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorSpriteSwitch_Visual : Switch_Visual
{
    [SerializeField] private SpriteRenderer targetSprite;
    private bool _mouseOver = false;

    [Header("每个状态的颜色组")]
    public List<SwitchStateColors> stateColors = new List<SwitchStateColors>();

    [Serializable]
    public class SwitchStateColors
    {
        public Color idle = Color.white;
        public Color highlight = Color.yellow;
        public Color notAllow = Color.gray;
        public Color pressed = new Color(0.4f, 0.4f, 0.4f, 1f);
    }

    protected override void Start()
    {
        base.Start();

        if (targetSprite == null)
            targetSprite = GetComponent<SpriteRenderer>();

        setIdle();
    }

    private void Update()
    {
        if (button == null || targetSprite == null || isPressing) return;
        int state = switchButton.CurrentStateIndex;
        if (!HasValidState(state)) return;

        var colors = stateColors[state];
        if (button.allowToUse)
        {
            if (targetSprite.color == colors.notAllow)
                targetSprite.color = colors.idle;
        }
        else
        {
            if (targetSprite.color == colors.idle)
                targetSprite.color = colors.notAllow;
        }
    }

    private void OnDisable()
    {
        if (button == null || targetSprite == null) return;
        int state = switchButton != null ? switchButton.CurrentStateIndex : 0;
        if (!HasValidState(state)) return;

        var colors = stateColors[state];
        targetSprite.color = button.allowToUse ? colors.idle : colors.notAllow;
    }

    public override void setHighlight(int stateIndex)
    {
        _mouseOver = true;
        if (isPressing || targetSprite == null || !HasValidState(stateIndex)) return;
        var colors = stateColors[stateIndex];
        targetSprite.color = button.allowToUse ? colors.highlight : colors.notAllow;
    }

    public override void setIdle(int stateIndex)
    {
        _mouseOver = false;
        if (isPressing || targetSprite == null || !HasValidState(stateIndex)) return;
        var colors = stateColors[stateIndex];
        targetSprite.color = button.allowToUse ? colors.idle : colors.notAllow;
    }

    public override void OnPress(int currentStateIndex, int targetStateIndex)
    {
        isPressing = true;
        StartCoroutine(OnPressed(currentStateIndex, targetStateIndex));
    }

    private IEnumerator OnPressed(int currentStateIndex, int targetStateIndex)
    {
        if (!HasValidState(targetStateIndex) || !HasValidState(currentStateIndex)) yield break;

        var prevColors = stateColors[currentStateIndex];
        var newColors = stateColors[targetStateIndex];

        // 旧状态 highlight → pressed
        targetSprite.color = button.allowToUse ? prevColors.highlight : prevColors.notAllow;
        yield return new WaitForSecondsRealtime(0.1f);
        targetSprite.color = prevColors.pressed;
        yield return new WaitForSecondsRealtime(0.1f);
        // 新状态 highlight → idle/notAllow
        targetSprite.color = button.allowToUse ? newColors.highlight : newColors.notAllow;
        yield return new WaitForSecondsRealtime(0.1f);
        if (!_mouseOver)
            targetSprite.color = button.allowToUse ? newColors.idle : newColors.notAllow;

        isPressing = false;
    }

    private bool HasValidState(int stateIndex)
    {
        if (stateColors == null || stateIndex < 0 || stateIndex >= stateColors.Count)
        {
            Debug.LogWarning($"[ColorSpriteSwitch_Visual] {gameObject.name}: 状态 {stateIndex} 没有对应的颜色配置！");
            return false;
        }
        return true;
    }
}
