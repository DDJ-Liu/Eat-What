using UnityEngine;
using Spine;
using Spine.Unity;
using Spine.Unity.AttachmentTools;
using System.Collections.Generic;

/// <summary>
/// 基于 Spine-Unity 的角色分部位换装系统
/// 支持多个装备槽位的动态换装，并可选择性地进行皮肤重打包优化
/// </summary>
public class CharacterEquipment : MonoBehaviour
{
    [System.Serializable]
    public class EquipmentSlot
    {
        public string slotName;           // 槽位名称（用于显示）
        [SpineSkin] public string skinName;  // 当前装备的皮肤名

        public EquipmentSlot(string name, string skin = "")
        {
            slotName = name;
            skinName = skin;
        }
    }

    [Header("骨骼组件")]
    [Tooltip("可以是 SkeletonAnimation 或 SkeletonMecanim 组件")]
    [SerializeField] private SkeletonRenderer skeletonRenderer;

    [Header("基础皮肤配置")]
    [SpineSkin]
    [Tooltip("基础身体皮肤，通常包含裸体模型")]
    public string baseSkin = "base";

    [Tooltip("初始化时是否应用装备槽位中配置的默认装备（如果为 false，则仅显示基础皮肤）")]
    public bool applyDefaultEquipmentOnStart = true;

    [Header("装备槽位配置")]
    [Tooltip("定义角色的各个装备部位")]
    public List<EquipmentSlot> equipmentSlots = new List<EquipmentSlot>
    {
        new EquipmentSlot("头发", "hair/brown"),
        new EquipmentSlot("眼睛", "eyes/green"),
        new EquipmentSlot("鼻子", "nose/short"),
        new EquipmentSlot("上衣", "clothes/hoodie"),
        new EquipmentSlot("裤子", "legs/pants"),
        new EquipmentSlot("配饰", "accessories/hat")
    };

    [Header("性能优化")]
    [Tooltip("是否在换装后自动重打包皮肤以减少 DrawCall（性能消耗较大，建议角色创建完成后执行一次）")]
    public bool autoOptimize = false;

    [Header("运行时数据（勿手动赋值）")]
    [SerializeField] private Material runtimeMaterial;
    [SerializeField] private Texture2D runtimeAtlas;

    private Skeleton skeleton;
    private SkeletonData skeletonData;
    private Skin combinedSkin;

    void Awake()
    {
        if (skeletonRenderer == null)
        {
            // 尝试获取 SkeletonMecanim（优先）
            skeletonRenderer = GetComponent<SkeletonMecanim>();

            // 如果没有，尝试获取 SkeletonAnimation
            if (skeletonRenderer == null)
                skeletonRenderer = GetComponent<SkeletonAnimation>();
        }
    }

    void Start()
    {
        if (skeletonRenderer == null)
        {
            Debug.LogError("CharacterEquipment: 未找到 SkeletonRenderer 组件（SkeletonAnimation 或 SkeletonMecanim）！", this);
            return;
        }

        skeleton = skeletonRenderer.Skeleton;
        skeletonData = skeleton.Data;

        // 初始化时根据配置决定是否应用装备
        if (applyDefaultEquipmentOnStart)
        {
            UpdateCharacterSkin();
        }
        else
        {
            // 仅应用基础皮肤，清空所有装备槽位
            foreach (var slot in equipmentSlots)
                slot.skinName = "";
            UpdateCharacterSkin();
        }
    }

    /// <summary>
    /// 更换指定槽位的装备
    /// </summary>
    /// <param name="slotIndex">槽位索引</param>
    /// <param name="skinName">新皮肤名称，传入空字符串表示卸下该部位装备</param>
    public void EquipSlot(int slotIndex, string skinName)
    {
        if (slotIndex < 0 || slotIndex >= equipmentSlots.Count)
        {
            Debug.LogError($"CharacterEquipment: 槽位索引 {slotIndex} 越界！");
            return;
        }

        equipmentSlots[slotIndex].skinName = skinName;
        UpdateCharacterSkin();
    }

    /// <summary>
    /// 通过槽位名称更换装备
    /// </summary>
    public void EquipSlotByName(string slotName, string skinName)
    {
        int index = equipmentSlots.FindIndex(slot => slot.slotName == slotName);
        if (index == -1)
        {
            Debug.LogError($"CharacterEquipment: 未找到名为 '{slotName}' 的槽位！");
            return;
        }

        EquipSlot(index, skinName);
    }

    /// <summary>
    /// 批量装备多个部位（用于角色初始化或快速换装）
    /// </summary>
    public void EquipMultiple(Dictionary<string, string> slotSkinPairs)
    {
        foreach (var pair in slotSkinPairs)
        {
            int index = equipmentSlots.FindIndex(slot => slot.slotName == pair.Key);
            if (index != -1)
                equipmentSlots[index].skinName = pair.Value;
        }
        UpdateCharacterSkin();
    }

    /// <summary>
    /// 卸下指定槽位的装备
    /// </summary>
    public void UnequipSlot(int slotIndex)
    {
        EquipSlot(slotIndex, "");
    }

    /// <summary>
    /// 卸下所有装备，只保留基础皮肤
    /// </summary>
    public void UnequipAll()
    {
        foreach (var slot in equipmentSlots)
            slot.skinName = "";
        UpdateCharacterSkin();
    }

    /// <summary>
    /// 更新角色皮肤组合
    /// </summary>
    private void UpdateCharacterSkin()
    {
        if (skeleton == null || skeletonData == null)
            return;

        // 创建新的组合皮肤
        combinedSkin = new Skin("character-combined");

        Debug.Log("=== 开始构建皮肤组合 ===");

        // 首先添加默认皮肤（如果存在），这通常包含所有基础部位
        if (skeletonData.DefaultSkin != null)
        {
            combinedSkin.AddSkin(skeletonData.DefaultSkin);
            Debug.Log($"添加 DefaultSkin");
        }

        // 然后添加基础皮肤（会覆盖默认皮肤中的部分）
        if (!string.IsNullOrEmpty(baseSkin))
        {
            Skin foundBaseSkin = skeletonData.FindSkin(baseSkin);
            if (foundBaseSkin != null)
            {
                combinedSkin.AddSkin(foundBaseSkin);
                Debug.Log($"添加 BaseSkin: '{baseSkin}'");
            }
            else
            {
                Debug.LogWarning($"CharacterEquipment: 未找到基础皮肤 '{baseSkin}'");
            }
        }

        // 最后按顺序添加各部位装备皮肤（会覆盖前面的皮肤）
        foreach (var slot in equipmentSlots)
        {
            if (string.IsNullOrEmpty(slot.skinName))
            {
                Debug.Log($"跳过空槽位: {slot.slotName}");
                continue;
            }

            Skin partSkin = skeletonData.FindSkin(slot.skinName);
            if (partSkin != null)
            {
                combinedSkin.AddSkin(partSkin);
                Debug.Log($"添加装备皮肤: '{slot.skinName}' (槽位: {slot.slotName})");
            }
            else
            {
                Debug.LogWarning($"CharacterEquipment: 未找到皮肤 '{slot.skinName}'（槽位: {slot.slotName}）");
            }
        }

        Debug.Log("=== 皮肤组合构建完成 ===");

        // 应用组合皮肤到骨骼
        skeleton.SetSkin(combinedSkin);
        skeleton.SetSlotsToSetupPose();

        // 诊断信息
        Debug.Log($"组合皮肤包含的 Attachment 数量: {combinedSkin.Attachments.Count}");
        Debug.Log($"Skeleton 当前皮肤: {skeleton.Skin?.Name ?? "null"}");
        Debug.Log($"SkeletonRenderer 类型: {skeletonRenderer.GetType().Name}");

        // 强制刷新渲染器
        if (skeletonRenderer != null)
        {
            skeletonRenderer.LateUpdate();
            Debug.Log("CharacterEquipment: 已强制刷新 SkeletonRenderer");
        }

        // 如果启用自动优化，则重打包皮肤
        if (autoOptimize)
            OptimizeSkin();
    }

    /// <summary>
    /// 优化皮肤：将所有使用的贴图重新打包到单个图集，减少 DrawCall
    /// 注意：此操作性能消耗较大，建议在角色创建完成或换装完成后调用一次
    /// 需要确保所有源纹理在 Inspector 中设置为 Read/Write Enabled
    /// </summary>
    public void OptimizeSkin()
    {
        if (skeleton == null || combinedSkin == null)
            return;

        // 清理旧的运行时资源
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
            runtimeMaterial = null;
        }
        if (runtimeAtlas != null)
        {
            Destroy(runtimeAtlas);
            runtimeAtlas = null;
        }

        // 获取源材质
        Material sourceMaterial = skeletonRenderer.SkeletonDataAsset.atlasAssets[0].PrimaryMaterial;

        // 重打包皮肤
        Skin repackedSkin = combinedSkin.GetRepackedSkin(
            "optimized-skin",
            sourceMaterial,
            out runtimeMaterial,
            out runtimeAtlas
        );

        // 应用优化后的皮肤
        skeleton.SetSkin(repackedSkin);
        skeleton.SetSlotsToSetupPose();
        skeletonRenderer.LateUpdate();

        // 清理缓存
        Spine.Unity.AttachmentTools.AtlasUtilities.ClearCache();
        Resources.UnloadUnusedAssets();

        Debug.Log("CharacterEquipment: 皮肤优化完成");
    }

    /// <summary>
    /// 获取当前装备的皮肤名称
    /// </summary>
    public string GetEquippedSkin(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= equipmentSlots.Count)
            return null;
        return equipmentSlots[slotIndex].skinName;
    }

    /// <summary>
    /// 获取所有槽位名称
    /// </summary>
    public List<string> GetAllSlotNames()
    {
        List<string> names = new List<string>();
        foreach (var slot in equipmentSlots)
            names.Add(slot.slotName);
        return names;
    }

    void OnDestroy()
    {
        // 清理运行时创建的资源
        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
        if (runtimeAtlas != null)
            Destroy(runtimeAtlas);
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器下的测试方法
    /// </summary>
    [ContextMenu("调试：列出所有可用皮肤")]
    private void DebugListAllSkins()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("CharacterEquipment: 此功能仅在运行时可用");
            return;
        }

        if (skeletonData == null)
        {
            Debug.LogError("SkeletonData 未初始化");
            return;
        }

        Debug.Log($"=== 所有可用皮肤 ({skeletonData.Skins.Count}) ===");
        foreach (var skin in skeletonData.Skins)
        {
            Debug.Log($"皮肤: {skin.Name}");

            // 列出这个皮肤包含的所有附件
            var attachments = new System.Collections.Generic.List<string>();
            foreach (var entry in skin.Attachments)
            {
                int slotIndex = entry.Key.SlotIndex;
                string slotName = skeletonData.Slots.Items[slotIndex].Name;
                string attachmentName = entry.Key.Name;
                attachments.Add($"  - Slot[{slotName}] -> Attachment[{attachmentName}]");
            }

            if (attachments.Count > 0)
            {
                Debug.Log($"  包含 {attachments.Count} 个附件：");
                foreach (var att in attachments)
                    Debug.Log(att);
            }
            else
            {
                Debug.Log("  (空皮肤)");
            }
        }
    }

    [ContextMenu("测试：随机换装")]
    private void TestRandomEquip()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("CharacterEquipment: 此功能仅在运行时可用");
            return;
        }

        // 这里可以添加随机换装逻辑，需要根据实际的皮肤名称来实现
        Debug.Log("CharacterEquipment: 执行随机换装测试");
        UpdateCharacterSkin();
    }

    [ContextMenu("测试：卸下所有装备")]
    private void TestUnequipAll()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("CharacterEquipment: 此功能仅在运行时可用");
            return;
        }

        UnequipAll();
    }

    [ContextMenu("测试：优化皮肤")]
    private void TestOptimize()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("CharacterEquipment: 此功能仅在运行时可用");
            return;
        }

        OptimizeSkin();
    }
#endif

    public void Test_EquipHair(string equipmentName)
    {
        EquipSlotByName("Hair", equipmentName);
    }

    public void Test_EquipBottom(string equipmentName)
    {
        EquipSlotByName("Bottom", equipmentName);
    }
}
