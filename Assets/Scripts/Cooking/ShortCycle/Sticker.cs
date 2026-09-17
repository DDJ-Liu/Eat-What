namespace EatWhat.Cooking.ShortCycle
{
    public sealed class Sticker : ShortCycleGridPieceBase
    {
        public void ConfigureTag(CK01TagData tag)
        {
            Configure(new ShortCycleGridPieceDisplay
            {
                icon_sprite = tag == null ? null : tag.icon_sprite,
                name_key = tag == null ? null : tag.name_key,
                fallback_name = tag == null ? string.Empty : tag.name,
                badge_text = string.Empty,
                is_placeholder = false
            });
        }
    }
}
