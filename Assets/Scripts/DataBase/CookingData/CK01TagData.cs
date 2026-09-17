using System;
using UnityEngine;

[Serializable]
public class CK01TagData : ScriptableObject
{
    public string tag_class;
    public string name_key;
    public Sprite icon_sprite;
    public Sprite sticker_sprite;
    public int filter_order;
}
