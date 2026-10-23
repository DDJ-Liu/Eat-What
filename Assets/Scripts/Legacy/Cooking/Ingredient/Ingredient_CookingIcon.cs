using UnityEngine;

public class Ingredient_CookingIcon : SpawnableIcon
{
    public IngredientData data;
    public string IngredientName;
    public SpriteRenderer ItemSprite;

    public void onRefresh()
    {
        //动画接口

        //动画接口
        ItemSprite.sprite = data.Inv_Sprite;
        IngredientName = data.name;
    }

    public void setData(IngredientData data)
    {
        this.data = data;
        IngredientName = data.name;
        onRefresh();
    }

    public void ClearData()
    {
        data = null;
        IngredientName = string.Empty;
        ItemSprite.sprite = null;
    }
    
    public void onSpawnIngredient()
    {
        if (data == null) return;
        onSpawn("Ingredient", IngredientName, CookingManager.Instance.SpawnedIngredientParent);
    }
}
