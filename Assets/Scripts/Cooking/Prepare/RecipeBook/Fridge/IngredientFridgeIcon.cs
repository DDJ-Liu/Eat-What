using UnityEngine;

public class IngredientFridgeIcon : SpawnableIcon
{
    public IngredientData data;
    public string IngredientName;
    public FridgeManager fridge;
    public int amount;
    public void onSpawnIngredient()
    {
        onSpawn("FridgeIngredient", IngredientName);
        fridge.onIngredientSelected();
    }

    public override void onSpawnedTossed()
    {
        base.onSpawnedTossed();
        fridge.onIngredientTossed();
    }
}
