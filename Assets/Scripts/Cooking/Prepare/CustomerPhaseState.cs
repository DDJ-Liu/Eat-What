using UnityEngine;

public abstract class CustomerPhaseStateBase
{
    public MouseInteractionLayer myLayer;
    public virtual void EnterState(CustomerManager cm)
    {
        if (myLayer != null)
        {
            myLayer.OnPushLayer();
        }
    }
    public virtual void UpdateState(CustomerManager cm) { }
    public virtual void ExitState(CustomerManager cm)
    {
        if (myLayer != null)
        {
            myLayer.OnRemoveLayer();
        }
    }
}

public class CustomerIdleState : CustomerPhaseStateBase
{
}

[System.Serializable]
public class ChooseCustomerState : CustomerPhaseStateBase
{
    public override void EnterState(CustomerManager cm)
    {
        base.EnterState(cm);
        Debug.Log("[CustomerFSM] Enter ChooseCustomerState");
        cm.gameObject.SetActive(true);
        cm.onInitialize();
    }
}

[System.Serializable]
public class CustomerDialogState : CustomerPhaseStateBase
{
    public override void EnterState(CustomerManager cm)
    {
        base.EnterState(cm);
        Debug.Log("[CustomerFSM] Enter CustomerDialogState");
    }
}
