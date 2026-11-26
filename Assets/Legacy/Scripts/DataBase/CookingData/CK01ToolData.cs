using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CK01ToolData : ScriptableObject
{
    public string name_key;
    public string tool_kind;
    public List<string> supported_actions = new List<string>();
    public string mapped_dish_category;
    public Sprite icon_sprite;
}
