using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CookingRoomManager : MonoBehaviour
{
    public KitchenAreaData roomData;
    public List<Container> containers = new List<Container>();
    //public List<CookingDropZone> dropZones = new List<CookingDropZone>();
    public Transform CamPos;
    public Transform InteractionParent;

    private void Start()
    {
        // 默认所有 DropZone 关闭（防止初始场景中 DropZone 残留为可交互）
        HandleItem(ContainerTag.None, CookingManager.InHandSource.FromInventory);
    }

    /// <summary>
    /// CookingRoomManager 的统一操作接口。
    /// 根据物品信息和房间当前状态自动决定 Container 转换。
    /// </summary>
    /// <param name="itemTag">物品对应的 ContainerTag（None 表示手中无物）</param>
    /// <param name="itemSource">物品来源（FromInventory / FromContainer）</param>
    public void HandleItem(ContainerTag itemTag, CookingManager.InHandSource itemSource)
    {
        // 内部决策 1：是否手中有物
        bool hasItemInHand = (itemTag != ContainerTag.None);

        if (!hasItemInHand)
        {
            // 手中无物 → 根据 Container 内容决定状态
            foreach (var c in containers)
            {
                if (c != null && c.ContainerState == Container.state.Highlight)
                {
                    // 检查是否有内容
                    bool hasContent = c.dropZone != null && c.dropZone.contents != null && c.dropZone.contents.Count > 0;

                    if (hasContent)
                    {
                        // 有内容：保持 Highlight，关闭 DropZone
                        c.TransitionTo(Container.state.Highlight, dropZoneShouldBeOpen: false);
                    }
                    else
                    {
                        // 无内容：回到 Idle
                        c.TransitionTo(Container.state.Idle, dropZoneShouldBeOpen: false);
                    }
                }
            }
            return;
        }

        // 手中有物
        // 内部决策 2：查找当前激活的 Container 和目标 Container
        var activeContainer = getActiveSingleContainer();
        var targetContainer = searchForContainerByTag(itemTag);

        if (targetContainer == null) return;  // 找不到对应 Container，无操作

        // 内部决策 3：判断是否需要激活新 Container
        if (activeContainer == null)
        {
            // 3-1：无激活 Container → 激活目标 Container 并打开 DropZone
            targetContainer.TransitionTo(Container.state.Highlight, dropZoneShouldBeOpen: true);
        }
        else if (activeContainer.ContainerTag == itemTag)
        {
            // 3-2 match：已有匹配 Container → 打开 DropZone（保持 Highlight）
            activeContainer.TransitionTo(Container.state.Highlight, dropZoneShouldBeOpen: true);
        }
        // 3-2 mismatch：不做操作

        // TODO: 未来可根据 itemSource 区分行为（垃圾区/特效）
    }

    public Container searchForContainerByTag(ContainerTag tagKey)
    {
        foreach(Container container in containers)
        {
            if(container.ContainerTag == tagKey)
            {
                return container;
            }
        }

        return null;
    }

    public void resetAllContainers()
    {
        Debug.Log("Reset ALL====");
        foreach(Container c in containers)
        {
            c.TransitionTo(Container.state.Idle, dropZoneShouldBeOpen: false);
        }
        Debug.Log("Reset ALL====");
    }

    public void resetSingleContainer(Container container)
    {
        container.TransitionTo(Container.state.Highlight, dropZoneShouldBeOpen: false);
    }

    public void setContainerToHighlight(Container container)
    {
        container.TransitionTo(Container.state.Highlight, dropZoneShouldBeOpen: false);
    }

    public Container getActiveSingleContainer()
    {
        foreach(Container c in containers)
        {
            if(c.ContainerState == Container.state.Highlight)
            {
                return c;
            }
        }

        return null;
    }

    public List<Container> getActiveContainers()
    {
        List<Container> activeContaienrs = new List<Container>();
        foreach(Container c in containers)
        {
            if (c.ContainerState == Container.state.Highlight)
            {
                activeContaienrs.Add(c);
            }
        }

        return activeContaienrs;
    }

    /// <summary>
    /// 检查指定 Container 是否应该解除激活。
    /// 当 Container 中没有普通食材（只有 CommonMaterial 或为空）时，解除激活。
    /// 由 Ingredient_Cooking.onTossed 调用。
    /// </summary>
    /// <param name="container">要检查的 Container</param>
    public void CheckAndDeactivateContainerIfEmpty(Container container)
    {
        if (container == null || container.dropZone == null) return;
        if (container.ContainerState != Container.state.Highlight) return;

        // 检查 DropZone 中是否有普通食材（非 CommonMaterial）
        bool hasNormalIngredient = false;
        foreach (var ingredient in container.dropZone.contents)
        {
            if (ingredient != null && ingredient.data != null && !ingredient.data.isCommonMaterial)
            {
                hasNormalIngredient = true;
                break;
            }
        }

        // 如果没有普通食材，解除激活
        if (!hasNormalIngredient)
        {
            container.TransitionTo(Container.state.Idle, dropZoneShouldBeOpen: false);
        }
    }
}
