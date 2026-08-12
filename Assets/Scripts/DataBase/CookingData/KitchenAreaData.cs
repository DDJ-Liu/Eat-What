using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New KitchenArea", menuName = "CookingData/KitchenArea")]
public class KitchenAreaData : ScriptableObject
{
    public string areaName;
    public List<ContainerTag> containerTags;   // 该区域关联的容器类型集合
}
