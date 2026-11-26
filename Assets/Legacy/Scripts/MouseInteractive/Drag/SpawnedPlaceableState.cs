using UnityEngine;

public abstract class SpawnedPlaceableState<T> where T : SpawnedPlaceable<T>
{
    public virtual void EnterState(T owner) { }
    public virtual void UpdateState(T owner) { }
    public virtual void ExitState(T owner) { }
}

public abstract class IdleStateBase<T> : SpawnedPlaceableState<T>
    where T : SpawnedPlaceable<T> { }

public abstract class PlacingStateBase<T> : SpawnedPlaceableState<T>
    where T : SpawnedPlaceable<T>
{
    public override void UpdateState(T owner)
    {
        owner.transform.position = Tools.getMousePos();
    }
}
