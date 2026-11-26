using UnityEngine;

public class IngredientIcon : SpawnableIcon
{
    public IngredientData data;
    public string IngredientName;

    public void onSpawnIngredient()
    {
        onSpawn("Ingredient", IngredientName, CookingManager.Instance.SpawnedIngredientParent);
    }
}
