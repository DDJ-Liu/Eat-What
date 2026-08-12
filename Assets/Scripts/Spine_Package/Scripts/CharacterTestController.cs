using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CharacterEquipment;

/// <summary>
/// 用于测试复杂换装角色的测试控制器
/// 支持动画触发器管理和装备同步
/// </summary>
public class CharacterTestController : MonoBehaviour
{
    public Animator anim;
    public CharacterEquipment equipmentCore;
    public List<EquipmentSlot> equipments = new List<EquipmentSlot>();

    void Start()
    {

    }

    void Update()
    {

    }

    /// <summary>
    /// 设置动画触发器，并在1帧后自动重置
    /// </summary>
    public void SetAction(string action)
    {
        if (anim == null)
        {
            Debug.LogError("CharacterTestController: Animator 未设置！", this);
            return;
        }

        anim.SetTrigger(action);
        StartCoroutine(ResetTriggerNextFrame(action));
    }

    private IEnumerator ResetTriggerNextFrame(string triggerName)
    {
        yield return null; // 等待1帧
        anim.ResetTrigger(triggerName);
    }

    /// <summary>
    /// 将 Character 上设置的 equipments 同步到 equipmentCore 实现换装
    /// </summary>
    public void SyncEquipment()
    {
        if (equipmentCore == null)
        {
            Debug.LogError("CharacterTestController: equipmentCore 未设置！", this);
            return;
        }

        // 获取 equipmentCore 中所有可用的槽位名称
        List<string> availableSlots = equipmentCore.GetAllSlotNames();

        int syncedCount = 0;

        // 遍历当前角色的装备配置
        foreach (var equipment in equipments)
        {
            // 跳过空的或 'Non' 的装备
            if (string.IsNullOrEmpty(equipment.skinName) || equipment.skinName == "Non")
            {
                Debug.Log($"CharacterTestController: 跳过空装备槽位 '{equipment.slotName}'");
                continue;
            }

            // 检查 equipmentCore 是否有对应的槽位
            if (!availableSlots.Contains(equipment.slotName))
            {
                Debug.LogError($"CharacterTestController: equipmentCore 中不存在名为 '{equipment.slotName}' 的槽位！可用槽位：{string.Join(", ", availableSlots)}", this);
                continue;
            }

            // 同步装备到 equipmentCore
            equipmentCore.EquipSlotByName(equipment.slotName, equipment.skinName);
            syncedCount++;
        }

        Debug.Log($"CharacterTestController: 已同步 {syncedCount}/{equipments.Count} 个装备槽位到 equipmentCore");
    }
}
