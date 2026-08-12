using System.Collections.Generic;
using UnityEngine;

public class Tool_Cooking : MonoBehaviour
{
    public ToolData data;
    public string ToolName;
    public List<ToolTag> tags = new List<ToolTag>();

    public Vector3 prevPos;

    [Header("位置管理")]
    [Tooltip("挂点位置：Tool 初始生成的位置，由存档决定")]
    public Vector3 spawnPosition;

    [Header("放置状态")]
    [Tooltip("标识此 Tool 是否曾被 DropZone 成功接受过（用于区分初次放置 vs 捡起后重新放置）\n注：策划目前不需要 Tool 被接受后再次拖拽，但保留此实现以防需求变更")]
    public bool hasBeenAccepted = false;

    [Header("DropZone 追踪（预留功能）")]
    [Tooltip("当前所属的 DropZone（如果有）")]
    public DropZone currentDropZone;

    [Tooltip("拖拽前所属的 DropZone（用于取消时回退）")]
    public DropZone prevDropZone;

    private void Start()
    {
        Initialize(transform.position);
    }

    /// <summary>
    /// 初始化 Tool：设置挂点位置。
    /// 由生成逻辑在 Instantiate 后调用。
    /// </summary>
    /// <param name="position">挂点位置（由存档决定）</param>
    public void Initialize(Vector3 position)
    {
        spawnPosition = position;
        transform.position = position;
        hasBeenAccepted = false;
        currentDropZone = null;
        prevDropZone = null;

        Debug.Log($"[Tool] {ToolName} initialized at {spawnPosition}");
    }

    public void onStartDragging()
    {
        prevPos = transform.position;
        ToolInHandBehaviors(CookingManager.InHandSource.FromInventory);
    }

    /// <summary>
    /// Tool 拿在手上时的统一入口：转发给 CookingManager 决定是否打开当前激活 Container 的 DropZone。
    /// </summary>
    public void ToolInHandBehaviors(CookingManager.InHandSource source)
    {
        if (data == null || CookingManager.Instance == null) return;
        CookingManager.Instance.onToolInHand(data, source);
    }

    public void onDraggingUpdate()
    {
        transform.position = Tools.getMousePos();
    }

    /// <summary>
    /// 拖拽释放时未命中任何 DropZone：调用统一的取消逻辑。
    /// 由 MouseDraggableObject.onDropMissedEvent 触发。
    /// </summary>
    public void onDropMissed()
    {
        OnPlaceCancel();
    }

    /// <summary>
    /// Tool 放置取消时的核心逻辑：
    /// - 如果从未被接受过 → 回到挂点位置（spawnPosition）
    /// - 如果已被接受过且有 prevDropZone → 回到拖拽开始位置（prevPos）
    /// 注：策划目前不需要第二种情况，但保留实现以防需求变更。
    /// </summary>
    public void OnPlaceCancel()
    {
        if (hasBeenAccepted && prevDropZone != null)
        {
            // 情况 1：已被接受过，且有前一个 zone → 回到拖拽开始位置
            // 注：策划目前不需要此情况，但保留实现
            transform.position = prevPos;
            currentDropZone = prevDropZone;
            prevDropZone = null;
            Debug.Log($"[Tool] {ToolName} cancel: return to prevPos {prevPos} (hasBeenAccepted)");
        }
        else
        {
            // 情况 2：从未被接受过 → 回到挂点位置
            transform.position = spawnPosition;
            Debug.Log($"[Tool] {ToolName} cancel: return to spawnPosition {spawnPosition}");
        }

        if (CookingManager.Instance != null) CookingManager.Instance.onItemReleased();
    }

    // TODO: 命中 DropZone 后的视觉/位置行为（动画、停留、回原位等）由后续补完。
    /// <summary>
    /// Tool 被 DropZone 接受时调用：记录当前 zone，标记为已接受。
    /// 由 CookingDropZone.onDrop() 调用。
    /// TODO: 命中 DropZone 后的视觉/位置行为（动画、停留、回原位等）由后续补完。
    /// </summary>
    public void onAcceptedByZone(DropZone zone)
    {
        currentDropZone = zone;
        hasBeenAccepted = true;
        prevDropZone = null;

        // TODO: 视觉/位置行为（动画、停留等）
        Debug.Log($"[Tool] {ToolName} accepted by zone: {zone.name}");

        if (CookingManager.Instance != null) CookingManager.Instance.onItemReleased();
    }

    /// <summary>
    /// Tool 被拾起时调用（预留功能）。
    /// 策划目前不需要"Tool 被接受后再次拖拽"，但保留实现。
    /// 如果未来需要从 DropZone 中拾起 Tool，可以调用此方法。
    /// </summary>
    public void onPickUp()
    {
        prevDropZone = currentDropZone;
        currentDropZone = null;
        prevPos = transform.position;
        Debug.Log($"[Tool] {ToolName} picked up from {prevPos}");
    }

    public void onInteractionStart()
    {
        Debug.Log($"Tool: {name} enter Interaction");
    }

    // TODO: 与 onAcceptedByZone 配套，加工完成后的复位/视觉收尾。
    public void onInteractionDone()
    {
    }
}
