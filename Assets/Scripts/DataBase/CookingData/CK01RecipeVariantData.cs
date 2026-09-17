using System;
using UnityEngine;

[Serializable]
public class CK01RecipeVariantData : ScriptableObject
{
    public string recipe_id;
    public string final_item;
    public string name_key;
    public string actual_dish_category;
    public Sprite display_sprite;
}
