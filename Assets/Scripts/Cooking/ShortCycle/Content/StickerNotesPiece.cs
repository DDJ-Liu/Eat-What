using UnityEngine;
using EatWhat.Toolkit;
namespace EatWhat.Cooking.ShortCycle
{
    public sealed class StickerNotesPiece : MonoBehaviour
    {
        public enum ArtState { Ready, Missing, Aigc }
        [SerializeField] private Sticker[] notes = new Sticker[5];
        [SerializeField] private Sprite genericNoteTemplate = null;
        public static ArtState GetArtState(Sprite sprite)
        {
            var hasSprite = sprite != null;
            switch (PlaceholderRule.Classify(hasSprite, hasSprite ? sprite.name : null))
            {
                case ToolkitPlaceholderState.Missing: return ArtState.Missing;
                case ToolkitPlaceholderState.Aigc: return ArtState.Aigc;
                default: return ArtState.Ready;
            }
        }
        public bool CanBind(int count)
        {
            if (count > 5 || count > notes.Length) return false;
            for (var index = 0; index < count; index++) if (notes[index] == null) return false;
            return true;
        }
        public void Apply(RecipeView view)
        {
            for (var index = 0; index < notes.Length; index++)
            {
                if (notes[index] == null) continue;
                var tag = view != null && index < view.note_tags.Count ? view.note_tags[index] : null;
                var state = GetArtState(tag == null ? null : tag.sticker_sprite);
                notes[index].Configure(new ShortCycleGridPieceDisplay {
                    icon_sprite = tag == null ? null : tag.sticker_sprite ?? genericNoteTemplate,
                    name_key = tag == null ? null : tag.name_key, is_placeholder = tag != null && state == ArtState.Aigc });
                notes[index].SetContentVisible(tag != null);
            }
        }
    }
}
