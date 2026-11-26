using UnityEngine;

/// <summary>
/// HoverZone 逻辑执行器基类。
/// 通过 Editor 一键绑定将方法持久化接线到 HoverZone 的 UnityEvent。
/// </summary>
public abstract class HoverZone_Logic : MonoBehaviour
{
    [Tooltip("若未手动设置，将会自动查找同一GameObject上的HoverZone_MouseInteract")]
    public HoverZone_MouseInteract hoverZone;

    protected virtual void Start()
    {
        if (hoverZone == null)
            hoverZone = GetComponent<HoverZone_MouseInteract>();
    }

    /// <summary>
    /// 鼠标进入悬停区域时调用（绑定到 overEvent）
    /// </summary>
    public abstract void OnHoverEnter();

    /// <summary>
    /// 鼠标离开悬停区域时调用（绑定到 outEvent）
    /// </summary>
    public abstract void OnHoverExit();

    /// <summary>
    /// 悬停计时完成触发时调用（绑定到 hoverTriggerEvent）
    /// </summary>
    public abstract void OnTrigger();
}
