using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New KitchenArea", menuName = "CookingData/KitchenArea")]
public class KitchenAreaData : ScriptableObject
{
    public string areaName;
    public List<ContainerTag> containerTags;   // 该区域关联的容器类型集合

    // CK01-B v2 additions. Existing KitchenArea assets retain their original
    // areaName/containerTags values; the new IDs are resolved by the runtime
    // data registry after the generic table importer has generated them.
    public string core_facility;
    public List<string> preset_tools = new List<string>();
    public List<string> allowed_actions = new List<string>();
    public List<string> allowed_carriers = new List<string>();
    public bool is_unlocked_default = true;
    public string unlock_condition;
}
