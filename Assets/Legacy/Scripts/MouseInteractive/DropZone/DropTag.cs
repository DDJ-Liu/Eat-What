[System.Flags]
public enum DropTag
{
    None = 0,
    Ingredient = 1 << 0,
    Tool       = 1 << 1,
    Recipe     = 1 << 2,
    Trash      = 1 << 3,
    FridgeIngredient = 1 << 4,
    Everything = ~0
}
