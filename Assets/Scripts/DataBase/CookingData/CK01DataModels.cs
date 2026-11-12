using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CK01RecipeData : ScriptableObject
{
    public string name_key;
    public string dish_category;
    public int display_order_in_menu;
    public bool is_unlocked_default;
    public string standard_variant_id;
    public List<string> note_tags = new List<string>();
    public List<string> slots = new List<string>();
    public List<string> whitelist_proc_ids = new List<string>();
    public string first_step_id;
    public string unlock_source;
    public string unlock_condition;
}

/// <summary>Resolved S-04 fallback item; it does not implement processing behavior.</summary>
public sealed class UnknownProductResolver
{
    public UnknownProductResolver(string itemId, CK01ItemData item)
    {
        ItemId = itemId ?? string.Empty;
        Item = item;
    }

    public string ItemId { get; private set; }
    public CK01ItemData Item { get; private set; }

    public bool TryResolve(out CK01ItemData item)
    {
        item = Item;
        return item != null;
    }
}
