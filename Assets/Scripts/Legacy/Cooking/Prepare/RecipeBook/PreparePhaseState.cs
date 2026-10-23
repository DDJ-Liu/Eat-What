using UnityEngine;

public abstract class PreparePhaseStateBase
{
    public MouseInteractionLayer myLayer;
    public virtual void EnterState(PrepareIngredientManager pm)
    {
        if (myLayer != null)
        {
            myLayer.OnPushLayer();
        }
    }
    public virtual void UpdateState(PrepareIngredientManager pm) { }
    public virtual void ExitState(PrepareIngredientManager pm)
    {
        if (myLayer != null)
        {
            myLayer.OnRemoveLayer();
        }
    }
}

public class PrepareIdleState : PreparePhaseStateBase
{
}

[System.Serializable]
public class RecipeMenuState : PreparePhaseStateBase
{
    //public GameObject myReturnButton;
    public GameObject myCam;
    public GameObject menuParent;
    public GameObject recipeParent;
    public override void EnterState(PrepareIngredientManager pm)
    {
        base.EnterState(pm);
        Debug.Log("[PrepareFSM] Enter RecipeBookState");
        myCam.SetActive(true);
        menuParent.SetActive(true);
        recipeParent.SetActive(false);
        pm.RecipeInfoParent.SetActive(false);
    }

    public override void ExitState(PrepareIngredientManager pm)
    {
        base.ExitState(pm);
        myCam.SetActive(false);
        menuParent.SetActive(false);
        recipeParent.SetActive(true);
    }
}

[System.Serializable]
public class RecipeBookState : PreparePhaseStateBase
{
    public GameObject FridgeEdge;
    public GameObject myReturnButton;
    public GameObject myCam;
    public override void EnterState(PrepareIngredientManager pm)
    {
        base.EnterState(pm);
        Debug.Log("[PrepareFSM] Enter RecipeBookState");
        FridgeEdge.SetActive(true);
        myReturnButton.SetActive(true);
        myCam.SetActive(true);
        pm.RecipeInfoParent.SetActive(false);
    }

    public override void ExitState(PrepareIngredientManager pm)
    {
        base.ExitState(pm);
        FridgeEdge.SetActive(false);
        myReturnButton.SetActive(false);
        myCam.SetActive(false);

    }
}

[System.Serializable]
public class RecipeDetailState : PreparePhaseStateBase
{
    public override void EnterState(PrepareIngredientManager pm)
    {
        base.EnterState(pm);
        Debug.Log("[PrepareFSM] Enter RecipeDetailState");
    }


}

[System.Serializable]
public class RecipeBookFridgeState : PreparePhaseStateBase
{
    public GameObject FridgeEdge;
    public GameObject myReturnButton;
    public GameObject myCam;
    public override void EnterState(PrepareIngredientManager pm)
    {
        base.EnterState(pm);
        Debug.Log("[PrepareFSM] Enter RecipeBookFridgeState");
        FridgeEdge.SetActive(true);
        myReturnButton.SetActive(true);
        myCam.SetActive(true);
        pm.fridgeManager.gameObject.SetActive(true);
        pm.RecipeInfoParent.SetActive(true);
    }

    public override void ExitState(PrepareIngredientManager pm)
    {
        base.ExitState(pm);
        FridgeEdge.SetActive(false);
        myReturnButton.SetActive(false);
        myCam.SetActive(false);
    }
}

[System.Serializable]
public class FridgeState : PreparePhaseStateBase
{
    public GameObject myCam;
    public GameObject RecipeBookParent;
    public override void EnterState(PrepareIngredientManager pm)
    {
        base.EnterState(pm);
        Debug.Log("[PrepareFSM] Enter FridgeState");
        myCam.SetActive(true);
        RecipeBookParent.SetActive(false);
        pm.RecipeInfoParent.SetActive(true);
    }

    public override void ExitState(PrepareIngredientManager pm)
    {
        base.ExitState(pm);
        myCam.SetActive(false);
        pm.fridgeManager.gameObject.SetActive(false);
        RecipeBookParent.SetActive(true);

    }
}
