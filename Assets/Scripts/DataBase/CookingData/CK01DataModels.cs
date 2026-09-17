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

/// <summary>Read-only recipe-level view over the global ProcessingRecords dictionary.</summary>
public sealed class RecipeWhitelist
{
    private readonly List<string> processingRecordIds;

    public RecipeWhitelist(string recipeId, IEnumerable<string> processingRecordIds)
    {
        RecipeId = recipeId ?? string.Empty;
        this.processingRecordIds = processingRecordIds == null
            ? new List<string>()
            : new List<string>(processingRecordIds);
    }

    public string RecipeId { get; private set; }
    public IList<string> ProcessingRecordIds { get { return processingRecordIds.AsReadOnly(); } }
}

/// <summary>Read-only area action/carrier constraint surface for later P2 consumers.</summary>
public sealed class AreaConstraint
{
    private readonly List<string> allowedActions;
    private readonly List<string> allowedCarriers;

    public AreaConstraint(string areaId, IEnumerable<string> allowedActions, IEnumerable<string> allowedCarriers)
    {
        AreaId = areaId ?? string.Empty;
        this.allowedActions = allowedActions == null ? new List<string>() : new List<string>(allowedActions);
        this.allowedCarriers = allowedCarriers == null ? new List<string>() : new List<string>(allowedCarriers);
    }

    public string AreaId { get; private set; }
    public IList<string> AllowedActions { get { return allowedActions.AsReadOnly(); } }
    public IList<string> AllowedCarriers { get { return allowedCarriers.AsReadOnly(); } }
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
