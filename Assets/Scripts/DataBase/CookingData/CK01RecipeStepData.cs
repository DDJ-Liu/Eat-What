using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CK01RecipeStepData : ScriptableObject
{
    public string recipe_id;
    public string next_step_id;
    public string display_text_key;
    public List<string> standard_ingredients = new List<string>();
    public List<string> standard_tools = new List<string>();
    public List<string> trigger_items = new List<string>();
    public List<string> permanent_trigger_items = new List<string>();
}
