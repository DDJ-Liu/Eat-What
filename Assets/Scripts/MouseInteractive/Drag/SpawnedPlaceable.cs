using UnityEngine;

public abstract class SpawnedPlaceable<T> : MonoBehaviour, ISpawnedPlaceable
    where T : SpawnedPlaceable<T>
{
    #region FSM
    protected SpawnedPlaceableState<T> currentState;
    [TextArea, SerializeField] protected string currentStateIdentifier;

    public abstract IdleStateBase<T> IdleState { get; }
    public abstract PlacingStateBase<T> PlacingState { get; }

    public void ChangeState(SpawnedPlaceableState<T> newState)
    {
        currentState?.ExitState((T)this);
        currentState = newState;
        currentState?.EnterState((T)this);
    }

    public SpawnedPlaceableState<T> GetCurrentState() => currentState;
    #endregion

    public SpawnableIcon myIcon;

    public Vector3 prevPos;

    protected virtual void Update()
    {
        if (currentState != null)
        {
            currentStateIdentifier = currentState.GetType().Name;
            currentState.UpdateState((T)this);
        }
    }

    #region 工具方法
    protected void TakeOverDrag()
    {
        var draggable = GetComponent<MouseDraggableObject>();
        if (MouseManager.Instance.currentDraggableObject != null)
            MouseManager.Instance.currentDraggableObject.exitDrag();
        MouseManager.Instance.currentDraggableObject = draggable;
        MouseManager.Instance.dragStarted = false;
        MouseManager.Instance.dragPerforming = true;
        draggable.enterDrag();
    }

    protected void TakeOverDrag(MouseDraggableObject draggable)
    {
        if (MouseManager.Instance.currentDraggableObject != null)
            MouseManager.Instance.currentDraggableObject.exitDrag();
        MouseManager.Instance.currentDraggableObject = draggable;
        MouseManager.Instance.dragStarted = false;
        MouseManager.Instance.dragPerforming = true;
        draggable.enterDrag();
    }
    #endregion

    #region ISpawnedPlaceable 默认实现
    public virtual void onSpawned(SpawnableIcon icon) 
    { 
        myIcon = icon;
        TakeOverDrag(); 
        ChangeState(PlacingState); 
    }
    public virtual void onPickUp() 
    { 
        prevPos = transform.position;
        TakeOverDrag(); 
        ChangeState(PlacingState); 
    }

    public virtual void onPlaceRight()
    {
        Debug.Log("PlacedRight");
        ChangeState(IdleState);
        if (myIcon != null)
        {
            myIcon.onSpawnedUsed();
        }
    }

    public virtual void onPlaceWrong() { onPlaceCancel(); }

    public virtual void onPlaceCancel() 
    {
        if (myIcon != null)
        {
            myIcon.onSpawnedTossed();
        }
        Destroy(gameObject);
    }

    public virtual void onTossed()
    {
        if (myIcon != null)
        {
            myIcon.onSpawnedTossed();
        }
        Destroy(gameObject);
    }
    #endregion
}
