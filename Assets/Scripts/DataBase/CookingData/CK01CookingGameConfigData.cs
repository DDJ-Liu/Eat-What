using System;
using UnityEngine;

[Serializable]
public class CK01CookingGameConfigData : ScriptableObject
{
    public const string UnknownProductItemKey = "unknown_product_item";
    public const string DemoFridgeCapacityLevelKey = "demo_fridge_capacity_level";

    public string value;
}
