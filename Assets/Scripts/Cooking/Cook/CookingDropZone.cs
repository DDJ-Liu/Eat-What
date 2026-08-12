using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 烹饪阶段的食材汇集点。搭配 <see cref="DropZone"/> 使用：
/// DropZone 负责丢入信号路由与 Tag 过滤，本组件只做烹饪逻辑（准入校验、内容记录、变化广播）。
/// 一个 dropzone 可绑定单个 ContainerTag（由策划手动配置）。
/// contents 变化时直接回调 <see cref="CookingManager.onDropZoneContentsChanged"/>。
/// </summary>
[RequireComponent(typeof(DropZone))]
public class CookingDropZone : MonoBehaviour
{
    [Header("配置：本 zone 关联的 ContainerTags")]
    public ContainerTag containerTag;

    [Header("运行时")]
    [Tooltip("已接受的食材实例；通过 ingredient.data 取 IngredientData")]
    public List<Ingredient_Cooking> contents = new List<Ingredient_Cooking>();

    [Header("事件（业务扩展用；规则决策走 CookingManager）")]
    [Tooltip("contents 发生变化时触发；CookingManager 已在内部直接处理，本事件供其他业务订阅")]
    public UnityEvent onContentsChanged;
    [Tooltip("普通食材准入失败；调用方播拒绝反馈")]
    public UnityEvent<IngredientData> onIngredientRejected;
    [Tooltip("工具准入失败；调用方播拒绝反馈")]
    public UnityEvent<ToolData> onToolRejected;

    private DropZone _dropZone;

    /*[Header("食材位点")]
    public ArcLayout arc;*/

    private void Awake()
    {
        _dropZone = GetComponent<DropZone>();
    }

    /// <summary>
    /// 手动挂载到 <see cref="DropZone.onDropEvent"/>。
    /// 通过 GetComponent 区分 Tool / Ingredient_Cooking 并分发到下一步。
    /// </summary>
    public void onDrop(MouseDraggableObject draggable)
    {
        Debug.Log("CookingDropZone onDrop()");
        if (draggable == null) return;

        var tool = draggable.GetComponent<Tool_Cooking>();
        if (tool != null)
        {
            Debug.Log("Tool in DropZone");
            bool accepted = TryAcceptTool(tool);
            if (accepted)
            {
                Debug.Log("Tool Accepted");
                if (CookingManager.Instance != null)
                    CookingManager.Instance.onToolPlacedIn(tool, this);
                tool.onAcceptedByZone(_dropZone);
            }
            else
            {
                Debug.Log("Tool Rejected");
                tool.onDropMissed();
            }
            return;
        }

        var ingredient = draggable.GetComponent<Ingredient_Cooking>();
        if (ingredient != null && ingredient.data != null)
        {
            Debug.Log("Ingredient in DropZone");
            bool accepted = TryAcceptIngredient(ingredient);
            if (accepted)
            {
                Debug.Log("Ingredient Accepted");
                if (CookingManager.Instance != null)
                {
                    bool isFirstPlacement = !ingredient.hasBeenPlaced;
                    CookingManager.Instance.onIngredientPlacedIn(ingredient, this, isFirstPlacement);
                }
                ingredient.onPlaceRight();
                ingredient.dropZone = this;
            }
            else
            {
                Debug.Log("Ingredient Rejected");
                ingredient.onPlaceCancel();
            }
        }
    }

    /// <summary>
    /// 食材准入入口：
    ///   - 通用素材（isCommonMaterial == true）：旁路普通校验，转发 <see cref="AcceptCommonMaterial"/>。
    ///     popup 已确保只在 needed 列表中显示通用素材，玩家拖入即视为合法。
    ///   - 普通食材：通过 Recipe 白名单 + 当前 ContainerTag 校验。
    /// 接受 <see cref="Ingredient_Cooking"/> 实例本身（contents 记录实例，未来销毁/UI 用），
    /// 通过 ingredient.data 取 <see cref="IngredientData"/>。
    /// </summary>
    public bool TryAcceptIngredient(Ingredient_Cooking ingredient)
    {
        if (ingredient == null || ingredient.data == null) return false;
        var ing = ingredient.data;

        if (ing.isCommonMaterial)
        {
            AcceptCommonMaterial(ingredient);
            return true;
        }

        var recipe = CookingManager.Instance != null ? CookingManager.Instance.currentRecipe : null;
        if (recipe == null || recipe.whitelistRules == null) return false;

        bool allowed = false;
        foreach (var rule in recipe.whitelistRules)
        {
            if (rule == null) continue;
            if (containerTag != rule.containerTag) continue;
            if (rule.ingredients != null && rule.ingredients.Contains(ing))
            {
                allowed = true;
                break;
            }
        }

        if (!allowed)
        {
            onIngredientRejected?.Invoke(ing);
            return false;
        }

        // 统计当前 zone 中该食材的数量
        int currentCount = contents.Count(i => i != null && i.data != null && i.data.uid == ing.uid);

        // 计算规则允许的最大数量（复用 CookingManager 的方法）
        int maxAllowed = 0;
        if (CookingManager.Instance != null)
        {
            maxAllowed = CookingManager.Instance.GetMaxAllowedCount(ing, containerTag);
        }

        // 如果已达上限，拒绝
        if (currentCount >= maxAllowed)
        {
            onIngredientRejected?.Invoke(ing);
            return false;
        }

        contents.Add(ingredient);
        NotifyContentsChanged();
        return true;
    }

    /// <summary>
    /// 工具准入入口：
    /// 通过 CookingManager.CanAcceptTool 检查当前 Recipe 和 Zone 是否允许该工具。
    /// 检查逻辑：Tool 的 toolTags 是否匹配 Recipe 白名单规则中的 toolTag，
    /// 且 Zone 的 containerTags 包含规则的 containerTag。
    /// </summary>
    public bool TryAcceptTool(Tool_Cooking tool)
    {
        if (tool == null || tool.data == null) return false;

        if (CookingManager.Instance == null || !CookingManager.Instance.CanAcceptTool(tool, this))
        {
            onToolRejected?.Invoke(tool.data);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 通用素材直接注入（绕过普通准入校验，由 popup 路径调用）。
    /// 接受 <see cref="Ingredient_Cooking"/> 实例（popup 拖入路径应已 spawn 出 ingredient 实例）。
    /// </summary>
    public void AcceptCommonMaterial(Ingredient_Cooking ingredient)
    {
        if (ingredient == null || ingredient.data == null || !ingredient.data.isCommonMaterial) return;
        contents.Add(ingredient);
        NotifyContentsChanged();
    }

    /// <summary>
    /// 给 RuleBook.Query 用：从 contents 实例提取 IngredientData 并按 uid 排序（防外部 mutate）。
    /// </summary>
    public IReadOnlyList<IngredientData> GetSortedContents()
    {
        return contents
            .Where(i => i != null && i.data != null)
            .Select(i => i.data)
            .OrderBy(d => d.uid)
            .ToList();
    }

    /// <summary>
    /// 加工完成后由 CookingManager 调用：清空 contents 并通知。
    /// </summary>
    public void ClearContents()
    {
        if (contents.Count == 0) return;
        contents.Clear();
        NotifyContentsChanged();
    }

    /// <summary>
    /// 食材被玩家从 zone 中拾起时调用：从 contents 中移除并广播变化。
    /// 由 <see cref="Ingredient_Cooking.onPickUp"/> 调用。
    /// </summary>
    public bool RemoveContent(Ingredient_Cooking ingredient)
    {
        if (ingredient == null) return false;
        if (!contents.Remove(ingredient)) return false;
        NotifyContentsChanged();
        return true;
    }

    /// <summary>
    /// 取消放置后将食材还原回此 zone 的 contents：用于 pickup 后 cancelPlace 自动落回原位。
    /// 由 <see cref="Ingredient_Cooking.onPlaceCancel"/> 调用，绕过准入校验。
    /// </summary>
    public void RestoreContent(Ingredient_Cooking ingredient)
    {
        if (ingredient == null || contents.Contains(ingredient)) return;
        contents.Add(ingredient);
        NotifyContentsChanged();
    }

    private void NotifyContentsChanged()
    {
        if (CookingManager.Instance != null)
            CookingManager.Instance.onDropZoneContentsChanged(this);
        onContentsChanged?.Invoke();
    }


}
