using UnityEngine;


public class Ingredient_Fridge : SpawnedPlaceable<Ingredient_Fridge>
{
    public IngredientData data;
    public FridgeManager fridge;
    public Ingredient_FridgeIdleState idleState = new Ingredient_FridgeIdleState();
    public Ingredient_FridgePlacingState placingState = new Ingredient_FridgePlacingState();

    public override IdleStateBase<Ingredient_Fridge> IdleState => idleState;
    public override PlacingStateBase<Ingredient_Fridge> PlacingState => placingState;




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

    public override void onSpawned(SpawnableIcon icon)
    {
        var fridgeIcon = (IngredientFridgeIcon)icon;
        this.data = fridgeIcon.data;
        this.fridge = fridgeIcon.fridge;
        base.onSpawned(icon);
    }

    public void onMissedDropZone()
    {
        Debug.Log($"{name} Released NO DropZone");
        onTossed();
    }

    public override void onPickUp()
    {
        base.onPickUp();
        fridge.ingredientTray.RemoveItem(this);
    }

    /// <summary>
    /// PickUp when Fridge Tray is openend, add close tray code than onPickUp();
    /// </summary>
    /// <param name="draggableObject"></param>
    public void onPickUp_FromOpened(MouseDraggableObject draggableObject)
    {
        Debug.Log(123);
        prevPos = transform.position;
        TakeOverDrag(draggableObject);
        fridge.ingredientTray.RemoveItem(this);
        fridge.ingredientTray.onClose_IngredientOnHand();
        ChangeState(PlacingState);
    }

    public void onPlaceCancel_Opened(MouseDraggableObject draggableObject)
    {
        transform.position = prevPos;
    }
}
