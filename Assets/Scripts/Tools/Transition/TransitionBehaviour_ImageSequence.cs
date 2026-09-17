using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TransitionBehaviour_ImageSequence : TransitionBehaviour
{
    [System.Serializable]
    public class ImageSheetEntry
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

    [Header("Target Images")]
    [Tooltip("目标 Image 列表（支持多个 Image 同步播放）")]
    public List<Image> targetImages = new List<Image>();

    [Header("Auto Collection")]
    [Tooltip("从此父对象自动收集子 Image 组件")]
    public Transform targetParent;

    [Header("Image Sheets")]
    public List<ImageSheetEntry> imageSheets = new List<ImageSheetEntry>();

    protected override void Awake()
    {
        base.Awake();
        AutoCollectImages();
    }

    private void AutoCollectImages()
    {
        if (targetParent != null)
        {
            targetImages.Clear();
            foreach (var img in targetParent.GetComponentsInChildren<Image>(true))
            {
                targetImages.Add(img);
            }
            Debug.Log("[TransitionBehaviour_ImageSequence] Auto-collected " + targetImages.Count + " Images from " + targetParent.name);
        }

        if (targetImages.Count == 0)
        {
            var img = GetComponent<Image>();
            if (img != null)
            {
                targetImages.Add(img);
                Debug.Log("[TransitionBehaviour_ImageSequence] Auto-added self Image component");
            }
        }
    }

    protected override bool TryCreateTransition(string transitionName, out IEnumerator transition)
    {
        var sheet = imageSheets.Find(s => s.name == transitionName);
        if (sheet == null)
        {
            Debug.LogWarning("[TransitionBehaviour_ImageSequence] ImageSheet not found: " + transitionName);
            transition = null;
            return false;
        }

        transition = PlaySheet(sheet, transitionName);
        return true;
    }

    private IEnumerator PlaySheet(ImageSheetEntry sheet, string transitionName)
    {

        Sprite[] spritesToPlay = sheet.GetSprites();

        if (spritesToPlay == null || spritesToPlay.Length == 0)
        {
            Debug.LogWarning("[TransitionBehaviour_ImageSequence] ImageSheet '" + transitionName + "' has no sprites");
            yield break;
        }

        var activeImages = GetActiveImages();
        if (activeImages.Count == 0)
        {
            Debug.LogWarning("[TransitionBehaviour_ImageSequence] No target Images found on " + gameObject.name);
            yield break;
        }

        float frameDuration = 1f / sheet.frameRate;

        foreach (var sprite in spritesToPlay)
        {
            foreach (var img in activeImages)
            {
                if (img != null)
                {
                    img.sprite = sprite;
                }
            }

            yield return WaitForDuration(frameDuration, sheet.useUnscaledTime);
        }
    }

    private List<Image> GetActiveImages()
    {
        if (targetImages != null && targetImages.Count > 0)
            return targetImages;

        return new List<Image>();
    }
}
