using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransitionBehaviour_SpriteSequence : TransitionBehaviour
{
    [System.Serializable]
    public class SpriteSheetEntry
    {
        public string name;

        [Header("Manual Sprites")]
        [Tooltip("手动指定的 Sprite 数组（优先使用）")]
        public Sprite[] sprites;

        [Header("Auto Load from Folder")]
        [Tooltip("从 Resources 文件夹自动加载（如 'Transitions/FadeOut'），仅当 sprites 为空时使用")]
        public string folderPath;

        public float frameRate = 12f;
        public bool useUnscaledTime;

        /// <summary>
        /// 获取最终使用的 Sprite 数组（优先使用手动指定的 sprites，否则从 folderPath 加载）
        /// </summary>
        public Sprite[] GetSprites()
        {
            if (sprites != null && sprites.Length > 0)
            {
                return sprites;
            }

            if (!string.IsNullOrEmpty(folderPath))
            {
                return Tools.LoadSpritesFromFolder(folderPath);
            }

            return new Sprite[0];
        }
    }

    public SpriteRenderer targetRenderer;
    public List<SpriteSheetEntry> spriteSheets = new List<SpriteSheetEntry>();

    protected override bool TryCreateTransition(string transitionName, out IEnumerator transition)
    {
        var sheet = spriteSheets.Find(s => s.name == transitionName);
        if (sheet == null)
        {
            Debug.LogWarning("SpriteSheet not found for transition: " + transitionName);
            transition = null;
            return false;
        }

        transition = PlaySheet(sheet, transitionName);
        return true;
    }

    private IEnumerator PlaySheet(SpriteSheetEntry sheet, string transitionName)
    {

        Sprite[] spritesToPlay = sheet.GetSprites();

        if (spritesToPlay == null || spritesToPlay.Length == 0)
        {
            Debug.LogWarning("SpriteSheet '" + transitionName + "' has no sprites");
            yield break;
        }

        float frameDuration = 1f / sheet.frameRate;
        foreach (var sprite in spritesToPlay)
        {
            targetRenderer.sprite = sprite;
            yield return WaitForDuration(frameDuration, sheet.useUnscaledTime);
        }
    }
}
