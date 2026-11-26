using UnityEngine;
using UnityEngine.Events;

public class Ingredient_Cooking : SpawnedPlaceable<Ingredient_Cooking>
{
    public IngredientData data;

    public IngredientState_Idle idleState = new IngredientState_Idle();
    public IngredientState_Placing placingState = new IngredientState_Placing();

    public override IdleStateBase<Ingredient_Cooking> IdleState => idleState;
    public override PlacingStateBase<Ingredient_Cooking> PlacingState => placingState;

    public CookingDropZone dropZone;
    public CookingDropZone prevDropZone;

    [Header("Placement State")]
    [Tooltip("标识此食材是否曾被成功放置过（用于区分初次放置 vs 捡起后重新放置）")]
    public bool hasBeenPlaced = false;

    private void Awake()
    {
        MouseManager.OnMouseCancel += onPlaceCancel;
    }

    private void OnDestroy()
    {
        MouseManager.OnMouseCancel -= onPlaceCancel;
    }

    void Start()
    {
        //ChangeState(idleState);
    }

    public override void onPlaceRight()
    {
        base.onPlaceRight();
        prevDropZone = null;
        hasBeenPlaced = true;
        CookingManager.Instance.onItemReleased();
    }

    public override void onSpawned(SpawnableIcon icon)
    {
        base.onSpawned(icon);
        IngredientInHandBehaviors(CookingManager.InHandSource.FromInventory);
    }

    public void onMissedDropZone()
    {
        Debug.Log($"{name} Released NO DropZone");
        onPlaceCancel();
    }

    public override void onPickUp()
    {
        base.onPickUp();

        prevDropZone = dropZone;
        if (prevDropZone != null)
        {
            prevDropZone.RemoveContent(this);
        }
        dropZone = null;

        var source = prevDropZone != null
            ? CookingManager.InHandSource.FromContainer
            : CookingManager.InHandSource.FromInventory;
        IngredientInHandBehaviors(source);
    }

    public override void onPlaceCancel()
    {
        // 只有正在拖拽中（PlacingState）的食材才响应取消
        if (currentState != PlacingState)
        {
            return;
        }

        if (prevDropZone != null)
        {
            var zone = prevDropZone;
            prevDropZone = null;

            transform.position = prevPos;
            zone.RestoreContent(this);
            dropZone = zone;
            onPlaceRight();
            return;
        }

        base.onPlaceCancel();
        CookingManager.Instance.onItemReleased();
    }

    public override void onTossed()
    {
        // 记录当前所在的 Container（通过 dropZone）
        Container currentContainer = null;
        if (dropZone != null && CookingManager.Instance != null && CookingManager.Instance.currentRoomManager != null)
        {
            // 查找包含此 dropZone 的 Container
            foreach (var container in CookingManager.Instance.currentRoomManager.containers)
            {
                if (container != null && container.dropZone == dropZone)
                {
                    currentContainer = container;
                    break;
                }
            }

            // 从 dropZone 中移除自己
            dropZone.RemoveContent(this);
        }

        base.onTossed();

        // 检查 Container 是否应该解除激活
        if (currentContainer != null && CookingManager.Instance != null && CookingManager.Instance.currentRoomManager != null)
        {
            CookingManager.Instance.currentRoomManager.CheckAndDeactivateContainerIfEmpty(currentContainer);
        }
    }

    public void IngredientInHandBehaviors(CookingManager.InHandSource source)
    {
        CookingManager.Instance.onIngredientInHand(data, source);
    }
}
