using Cinemachine;
using UnityEngine;

public abstract class CookingPhaseStateBase
{
    public MouseInteractionLayer myLayer;
    public CinemachineVirtualCamera myCam;
    public virtual void EnterState(CookingManager cm)
    {
        if (myLayer != null)
        {
            myLayer.OnPushLayer();
        }

        if (myCam != null)
        {
            myCam.gameObject.SetActive(true);
        }
    }
    public virtual void UpdateState(CookingManager cm) { }
    public virtual void ExitState(CookingManager cm)
    {
        if (myLayer != null)
        {
            myLayer.OnRemoveLayer();
        }
        if (myCam != null)
        {
            myCam.gameObject.SetActive(false);
        }
    }
}

public class NotCookingState : CookingPhaseStateBase
{
}

[System.Serializable]
public class KitchenChoiceState : CookingPhaseStateBase
{
    public GameObject KitchenRoomParent;
    public override void EnterState(CookingManager cm)
    {
        base.EnterState(cm);
        if (KitchenRoomParent != null) KitchenRoomParent.SetActive(true);
        Debug.Log("[CookingFSM] Enter KitchenChoiceState");
    }
    public override void ExitState(CookingManager cm)
    {
        base.ExitState(cm);
        if (KitchenRoomParent != null) KitchenRoomParent.SetActive(false);
    }
}

[System.Serializable]
public class IngredientChooseState : CookingPhaseStateBase
{
    public GameObject followCamUI;
    public override void EnterState(CookingManager cm)
    {
        myCam.transform.position = cm.currentRoomObject.GetComponent<CookingRoomManager>().CamPos.position;
        base.EnterState(cm);
        if (cm.tableManager != null) cm.tableManager.onEnterCooking();
        if (cm.currentRoomObject != null) cm.currentRoomObject.SetActive(true);
        if (followCamUI != null) followCamUI.SetActive(true);

        Debug.Log("[CookingFSM] Enter CookStepState");
    }

    public override void ExitState(CookingManager cm)
    {
        base.ExitState(cm);
        if (cm.currentRoomObject != null) cm.currentRoomObject.SetActive(false);
        if (followCamUI != null) followCamUI.SetActive(false);

    }
}

[System.Serializable]
public class InteractionState : CookingPhaseStateBase
{
    [System.NonSerialized] public CookingRule contextRule;
    [System.NonSerialized] public CookingDropZone contextZone;
    [System.NonSerialized] public Tool_Cooking contextTool;
    [System.NonSerialized] public InteractionController contextController;

    /// <summary>
    /// 由 CookingManager 在 InterruptState 之前注入加工上下文。
    /// </summary>
    public void Setup(CookingRule rule, CookingDropZone zone, Tool_Cooking tool)
    {
        contextRule = rule;
        contextZone = zone;
        contextTool = tool;
    }

    public override void EnterState(CookingManager cm)
    {
        base.EnterState(cm);
        Debug.Log($"[CookingFSM] Enter ProcessInteractionState | rule={contextRule?.name} zone={contextZone?.name} tool={contextTool?.name}");
    }

    public override void ExitState(CookingManager cm)
    {
        base.ExitState(cm);

        // 销毁 InteractionController
        if (contextController != null)
        {
            if (contextController.gameObject != null)
            {
                GameObject.Destroy(contextController.gameObject);
            }
            contextController = null;
        }

        // 清空上下文
        contextRule = null;
        contextZone = null;
        contextTool = null;
    }
}

[System.Serializable]
public class ProcessSeasoningState : CookingPhaseStateBase
{
    public override void EnterState(CookingManager cm)
    {
        base.EnterState(cm);
        Debug.Log("[CookingFSM] Enter ProcessSeasoningState");
    }
}

[System.Serializable]
public class CustomerReviewState : CookingPhaseStateBase
{
    public override void EnterState(CookingManager cm)
    {
        base.EnterState(cm);
        Debug.Log("[CookingFSM] Enter CustomerReviewState");
    }
}
