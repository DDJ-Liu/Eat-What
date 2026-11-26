using UnityEngine;
using UnityEngine.UI;

public static class ImageButtonEditorHelper
{
    /// <summary>
    /// 在子物体的 Canvas 下查找 Image 组件。
    /// 优先返回名称包含 "MainImage"（大小写无关）的 Image，否则返回第一个找到的。
    /// </summary>
    public static Image FindImageInChildCanvas(GameObject root)
    {
        return Tools.FindImageInChildCanvas(root);
    }
}
