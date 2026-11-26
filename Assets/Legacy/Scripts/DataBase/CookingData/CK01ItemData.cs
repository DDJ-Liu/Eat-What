using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CK01ItemData : ScriptableObject
{
    public string item_kind;
    public string name_key;
    public List<string> ingredient_categories = new List<string>();
    public string stack_type;
    public int display_order_in_fridge;
    public Sprite icon_sprite;
    public string flavor_key;
    public float container_capacity;
    public float container_use_step;
    public int pack_size;
    public int shelf_life_days;
}
