using System;
using System.Collections.Generic;
using UnityEngine;

// Generated.DataTables owns these immutable snapshots; there is no ShortCycle dependency.
public sealed class RecipeIngredientView
{
    public string item_id { get; }
    public string name_key { get; }
    public Sprite icon_sprite { get; }
    public int required_count { get; }
    public RecipeIngredientView(string id, string key, Sprite icon, int count)
    { item_id = id; name_key = key; icon_sprite = icon; required_count = count; }
}
public sealed class RecipeSubstituteView
{
    public int anchor_index { get; }
    public string item_id { get; }
    public int required_count { get; }
    public RecipeSubstituteView(int anchor, string id, int count)
    { anchor_index = anchor; item_id = id; required_count = count; }
}
public sealed class RecipeSlotView
{
    public string slot_id { get; }
    public RecipeIngredientView standard_ingredient { get; }
    public string standard_action { get; }
    public IReadOnlyList<RecipeSubstituteView> substitutes { get; }
    public RecipeSlotView(string id, RecipeIngredientView ingredient, string action, IEnumerable<RecipeSubstituteView> subs)
    { slot_id = id; standard_ingredient = ingredient; standard_action = action;
      substitutes = new List<RecipeSubstituteView>(subs).AsReadOnly(); }
}
public sealed class RecipeTagView
{
    public string tag_id { get; }
    public string name_key { get; }
    public Sprite icon_sprite { get; }
    public Sprite sticker_sprite { get; }
    public RecipeTagView(string id, string key, Sprite icon, Sprite sticker)
    { tag_id = id; name_key = key; icon_sprite = icon; sticker_sprite = sticker; }
}
public sealed class RecipeIconView
{
    public string id { get; }
    public string name_key { get; }
    public Sprite icon_sprite { get; }
    public RecipeIconView(string stableId, string key, Sprite icon)
    { id = stableId; name_key = key; icon_sprite = icon; }
}
public sealed class RecipeStepView
{
    public string step_id { get; }
    public string display_text_key { get; }
    public IReadOnlyList<RecipeIconView> standard_ingredients { get; }
    public IReadOnlyList<RecipeIconView> standard_tools { get; }
    public RecipeStepView(string id, string key, IEnumerable<RecipeIconView> ingredients, IEnumerable<RecipeIconView> tools)
    { step_id = id; display_text_key = key;
      standard_ingredients = new List<RecipeIconView>(ingredients).AsReadOnly();
      standard_tools = new List<RecipeIconView>(tools).AsReadOnly(); }
}
public sealed class RecipeView
{
    public string recipe_id { get; }
    public string name_key { get; }
    public Sprite display_sprite { get; }
    public int display_order_in_menu { get; }
    public bool is_unlocked { get; }
    public IReadOnlyList<RecipeTagView> note_tags { get; }
    public IReadOnlyList<RecipeSlotView> slots { get; }
    public IReadOnlyList<RecipeStepView> steps { get; }
    public RecipeView(string id, string key, Sprite sprite, int order, bool unlocked,
        IEnumerable<RecipeTagView> tags, IEnumerable<RecipeSlotView> requirements, IEnumerable<RecipeStepView> orderedSteps = null)
    { recipe_id = id; name_key = key; display_sprite = sprite; display_order_in_menu = order;
      is_unlocked = unlocked; note_tags = new List<RecipeTagView>(tags).AsReadOnly();
      slots = new List<RecipeSlotView>(requirements).AsReadOnly();
      steps = new List<RecipeStepView>(orderedSteps ?? new RecipeStepView[0]).AsReadOnly(); }
    public RecipeView WithUnlocked(bool unlocked)
    { return new RecipeView(recipe_id, name_key, display_sprite, display_order_in_menu, unlocked, note_tags, slots, steps); }
}
