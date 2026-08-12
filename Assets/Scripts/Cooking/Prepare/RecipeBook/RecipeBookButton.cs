using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecipeBookButton : MonoBehaviour
{
    public RecipeBookManager myManager;
    public Recipe myRecipe;

    public SpriteRenderer mainIconRenderer;
    public SpriteRenderer nameRenderer;
    public SpriteRenderer tag1Renderer;
    public SpriteRenderer tag2Renderer;
    public SpriteRenderer tag3Renderer;

    public void onSetInfo(RecipeBookManager manager, Recipe recipe)
    {
        myManager = manager;
        myRecipe = recipe;

        //TODO: Set Sprite
        mainIconRenderer.sprite = recipe.recipeIcon;
    }

    public void onSelected()
    {
        myManager.onRecipeSelected(myRecipe);
    }
}
