using UnityEngine;

public abstract class IngredientTrayStateBase
{
    public float targetFloatY;
    public virtual void EnterState(IngredientInventoryTray tray) 
    {
        tray.StartLerpToTargetY(targetFloatY);
    }
    public virtual void UpdateState(IngredientInventoryTray tray) { }
    public virtual void ExitState(IngredientInventoryTray tray) { }
}

[System.Serializable]
public class IngredientTrayIdleState : IngredientTrayStateBase
{
    public override void EnterState(IngredientInventoryTray tray)
    {
        base.EnterState(tray);
        tray.NextButton_Out.SetActive(true);
        tray.NextButton_Opened.SetActive(false);
    }

    public override void ExitState(IngredientInventoryTray tray)
    {
        base.ExitState(tray);
    }
}

[System.Serializable]
public class IngredientTrayHighlightState : IngredientTrayStateBase
{
    public override void EnterState(IngredientInventoryTray tray)
    {
        base.EnterState(tray);
        tray.NextButton_Out.SetActive(true);
        tray.NextButton_Opened.SetActive(false);
    }

    public override void ExitState(IngredientInventoryTray tray)
    {
        base.ExitState(tray);
    }
}

[System.Serializable]
public class IngredientTrayOpenedState : IngredientTrayStateBase
{
    public MouseInteractionLayer layer;
    public GameObject openedCam;
    public override void EnterState(IngredientInventoryTray tray)
    {
        base.EnterState(tray);
        layer.OnPushLayer();
        openedCam?.SetActive(true);
        tray.NextButton_Out.SetActive(false);
        tray.NextButton_Opened.SetActive(true);
        //tray.GetComponent<MouseParallax>().enableParallax = false;
    }

    public override void ExitState(IngredientInventoryTray tray)
    {
        base.ExitState(tray);
        layer.OnRemoveLayer();
        openedCam?.SetActive(false);
        //tray.GetComponent<MouseParallax>().enableParallax = true;
    }
}
