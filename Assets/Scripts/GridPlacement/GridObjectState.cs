using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// GridPlacement_Object 的状态基类。
/// </summary>
public abstract class GridObjectState
{
    public virtual void EnterState(GridPlacement_Object owner) { }
    public virtual void UpdateState(GridPlacement_Object owner) { }
    public virtual void ExitState(GridPlacement_Object owner) { }
}

/// <summary>
/// 已放置/空闲状态，物体静止在网格上。
/// </summary>
public class GridObjectState_Normal : GridObjectState
{

}

/// <summary>
/// 放置中状态，物体跟随鼠标、吸附网格、显示预览颜色。
/// </summary>
public class GridObjectState_Placing : GridObjectState
{
    private LayerMask cellLayerMask;
    private Color validColor;
    private Color invalidColor;

    private SpriteRenderer[] previewRenderers;
    private Color[] originalColors;

    public void init(LayerMask cellLayerMask, Color validColor, Color invalidColor)
    {
        this.cellLayerMask = cellLayerMask;
        this.validColor = validColor;
        this.invalidColor = invalidColor;
    }

    public override void EnterState(GridPlacement_Object owner)
    {
        owner.cellLayerMask = cellLayerMask;
        owner.CollectOccupiedMarkers();

        // 缓存渲染器和原始颜色
        previewRenderers = owner.GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[previewRenderers.Length];
        for (int i = 0; i < previewRenderers.Length; i++)
        {
            originalColors[i] = previewRenderers[i].color;
        }
    }

    public override void UpdateState(GridPlacement_Object owner)
    {
        // 鼠标位置 → 物体跟随鼠标
        Vector3 mousePos = Tools.getMousePos();
        owner.transform.position = mousePos;

        // 以第一个 Occupied marker 为锚点，raycast 找到 Cell → 获取所属 Grid → 对齐
        if (owner.occupiedMarkers.Count > 0)
        {
            Transform anchor = owner.occupiedMarkers[0];
            Collider2D hit = Physics2D.OverlapPoint(anchor.position, cellLayerMask);

            if (hit != null)
            {
                var cell = hit.GetComponent<GridPlacement_Cell>();
                if (cell != null && cell.grid != null)
                {
                    GridPlacement_Grid grid = cell.grid;
                    Vector2Int anchorGridPos = grid.WorldToGrid(anchor.position);

                    if (grid.IsInBounds(anchorGridPos))
                    {
                        Vector3 cellCenter = grid.GridToWorld(anchorGridPos);
                        Vector3 snapOffset = cellCenter - (Vector3)((Vector2)anchor.position);
                        owner.transform.position += snapOffset;
                    }
                }
            }
        }

        // 更新预览颜色
        UpdatePreviewColor(owner);
    }

    public override void ExitState(GridPlacement_Object owner)
    {
        RestoreOriginalColors(owner);
    }

    private void UpdatePreviewColor(GridPlacement_Object owner)
    {
        if (previewRenderers == null) return;

        bool canPlace = owner.CanPlace();
        Color tint = canPlace ? validColor : invalidColor;

        for (int i = 0; i < previewRenderers.Length; i++)
        {
            if (previewRenderers[i] != null)
            {
                previewRenderers[i].color = tint;
            }
        }
    }

    private void RestoreOriginalColors(GridPlacement_Object owenr)
    {
        if (previewRenderers == null || originalColors == null) return;

        for (int i = 0; i < previewRenderers.Length; i++)
        {
            if (previewRenderers[i] != null && i < originalColors.Length)
            {
                previewRenderers[i].color = originalColors[i];
            }
        }
    }
}
