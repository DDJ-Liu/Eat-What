using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用素材 popup 渲染器（瘦身版）。
///
/// 不持有规则知识、不订阅事件 —— 仅暴露 <see cref="ShowFor"/> / <see cref="Hide"/> 两个 UI 入口。
/// 决策由 <see cref="CookingManager.onDropZoneContentsChanged"/> 统一驱动。
///
/// 实现要点（TODO 由用户完成）：
///   ShowFor: 清空 popupRoot 子节点，为 needed 中每个 IngredientData 实例化
///            commonMaterialIconPrefab，绑定 IngredientData + 目标 zone；
///            按钮拖入完成时 spawn 出 Ingredient_Cooking 实例，调 zone.AcceptCommonMaterial(ingredient)。
///   Hide:    销毁/隐藏 popupRoot 下所有按钮。
/// </summary>
public class CommonMaterialPopupManager : MonoBehaviour
{
    /*[Header("渲染挂载点")]
    public Transform popupRoot;*/

    [Header("数据对比")]
    public IngredientData waterData;
    public IngredientData oilData;
    public IngredientData flourData;

    [Header("Icon 预制体")]
    [Tooltip("CommonMaterialIcon : SpawnableIcon 预制体；TODO：用户实现")]
    public GameObject Water_IngredientIcon;
    public GameObject Oil_IngredientIcon;
    public GameObject Flour_IngredientIcon;



    /// <summary>
    /// 渲染 needed 通用素材按钮；按钮拖入目标 zone 时应调 <see cref="CookingDropZone.AcceptCommonMaterial"/>。
    /// </summary>
    public void ShowFor(IList<IngredientData> needed)
    {
        if(needed.Contains(waterData))
        {
            Water_IngredientIcon.SetActive(true);
        }

        if (needed.Contains(oilData))
        {
            Oil_IngredientIcon.SetActive(true);
        }

        if (needed.Contains(flourData))
        {
            Flour_IngredientIcon.SetActive(true);
        }
    }

    /// <summary>
    /// 隐藏并清空 popup。
    /// </summary>
    public void Hide()
    {
        Water_IngredientIcon.SetActive(false);
        Oil_IngredientIcon.SetActive(false);
        Flour_IngredientIcon.SetActive(false);

    }
}
