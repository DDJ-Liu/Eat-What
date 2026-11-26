using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorSwitch_Visual : Switch_Visual
{
    [SerializeField] private Animator anim;
    [Tooltip("MouseSelect trigger 对应的 Animator 状态名")]
    [SerializeField] private string pressStateName = "MouseSelect";

    protected override void Start()
    {
        base.Start();

        if (anim == null)
            anim = GetComponent<Animator>();

        setIdle();
    }

    private void Update()
    {
        if (button == null || anim == null) return;

        if (Tools.AnimatorHasParameter(anim, "AllowUse"))
            anim.SetBool("AllowUse", button.allowToUse);

        if (switchButton != null && Tools.AnimatorHasParameter(anim, "SwitchState"))
            anim.SetInteger("SwitchState", switchButton.CurrentStateIndex);
    }

    private void OnDisable()
    {
        isPressing = false;
    }

    public override void setHighlight(int stateIndex)
    {
        if (isPressing || anim == null) return;
        if (Tools.AnimatorHasParameter(anim, "MouseOver"))
            anim.SetBool("MouseOver", true);
    }

    public override void setIdle(int stateIndex)
    {
        if (isPressing || anim == null) return;
        if (Tools.AnimatorHasParameter(anim, "MouseOver"))
            anim.SetBool("MouseOver", false);
    }

    public override void OnPress(int currentStateIndex, int targetStateIndex)
    {
        isPressing = true;
        //delayedReady = false;
        if (anim == null)
        {
            //delayedReady = true;
            isPressing = false;
            return;
        }
        if (Tools.AnimatorHasParameter(anim, "MouseSelect"))
            anim.SetTrigger("MouseSelect");
        StartCoroutine(WaitForPressAnimation());
    }

    private IEnumerator WaitForPressAnimation()
    {
        // 等待 Animator 进入 press 状态
        yield return null;
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        while (!stateInfo.IsName(pressStateName))
        {
            yield return null;
            stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        }
        // 等待 press 状态播放完毕
        while (stateInfo.IsName(pressStateName) && stateInfo.normalizedTime < 1f)
        {
            yield return null;
            stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        }
        //delayedReady = true;
        isPressing = false;
    }
}
