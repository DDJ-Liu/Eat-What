using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New IngredientData", menuName = "IngredientData/RawIngredient")]
public class IngredientData : SlotItem
{
    public int uid;
    /*public List<IngredientTag> tags = new List<IngredientTag>();
    public Dictionary<IngredientTag, float> tag_scores = new Dictionary<IngredientTag, float>();*/
    public int shelf_life = -1;
    public float tasteScore_Base;
    public float appearanceScore_base;

    [Header("通用素材标记")]
    [Tooltip("水/油/盐 等通用素材；走 popup 注入路径，绕过普通食材准入校验")]
    public bool isCommonMaterial;

    [Header("成品标记")]
    [Tooltip("菜品成品标记这个")]
    public bool isFinishedDish;

    [Header("临时处理数据")]
    public Sprite Inv_Sprite;
    public Sprite Obj_Sprite;
}
