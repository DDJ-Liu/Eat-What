namespace EatWhat.Cooking.ShortCycle
{
    public sealed class TraySlot : ShortCycleGridPieceBase
    {
        public void ConfigureItem(CK01ItemData item, int quantity)
        {
            Configure(new ShortCycleGridPieceDisplay
            {
                icon_sprite = item == null ? null : item.icon_sprite,
                name_key = item == null ? null : item.name_key,
                fallback_name = item == null ? string.Empty : item.name,
                badge_text = quantity > 1 ? "x" + quantity : string.Empty,
                is_placeholder = false
            });
        }
    }
}
