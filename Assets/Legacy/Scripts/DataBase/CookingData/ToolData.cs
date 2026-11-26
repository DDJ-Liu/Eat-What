using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New ToolData", menuName = "ToolData")]
public class ToolData : SlotItem
{
    //[Header("工具标签（多 Tag，运行时通过 Recipe 白名单取唯一值）")]
    public List<ToolTag> toolTags;
}

