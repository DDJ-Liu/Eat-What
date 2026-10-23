using System.Linq;

/// <summary>
/// 静态助手：根据 Tool 携带的多 ToolTag 与当前 Recipe 的 allowedToolTags 求交，
/// 在策划约束（同一 (Ingredients, ContainerTag) 下唯一命中）下取唯一 ToolTag。
///
/// 适用场景：UI/视觉层需要展示 Tool 当前会执行的 ToolTag。
/// 实际加工命中由 <see cref="CookingManager"/> 内基于候选 Rule 的精确匹配完成。
/// </summary>
public static class ToolTagResolver
{
    /// <summary>
    /// 取 tool.toolTags ∩ recipe.allowedToolTags 的第一个元素；
    /// 无交集返回 ToolTagType.None。
    /// </summary>
    public static ToolTag Resolve(Tool_Cooking tool, Recipe recipe)
    {
        if (tool == null || tool.data == null || tool.data.toolTags == null) return ToolTag.None;
        if (recipe == null || recipe.allowedToolTags == null || recipe.allowedToolTags.Count == 0) return ToolTag.None;

        return tool.data.toolTags.FirstOrDefault(t => recipe.allowedToolTags.Contains(t));
    }
}
