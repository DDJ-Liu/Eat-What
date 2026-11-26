using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MouseDraggableObject : MonoBehaviour
{
    public enum InteractionBoundsMode { None, RectTransform, Collider }

    public bool dragging = false;
    public UnityEvent startDraggingEvent;
    public UnityEvent draggingUpdateEvent;
    public UnityEvent endDraggingEvent;
    public UnityEvent cancelDraggingEvent;
    [Tooltip("释放时命中了 DropZone（无论是否被 zone 接受）")]
    public UnityEvent<DropZone> onDropHitEvent;
    [Tooltip("释放时未命中任何 DropZone")]
    public UnityEvent onDropMissedEvent;

    [Header("允许投放的区域")]
    [Tooltip("与 DropZone.zoneTags 做按位与判定；默认 Everything 表示不限制")]
    public DropTag allowedDropTags = DropTag.Everything;

    [Header("交互范围限制")]
    public InteractionBoundsMode interactionBoundsMode = InteractionBoundsMode.None;
    public RectTransform interactionBoundsRect;
    public Collider2D interactionBoundsCollider;

    private Vector3[] _boundsCorners = new Vector3[4];

    /// <summary>
    /// 检查指定点是否在交互范围内。mode 为 None 时始终返回 true。
    /// </summary>
    public bool IsPointInInteractionBounds(Vector2 point)
    {
        switch (interactionBoundsMode)
        {
            case InteractionBoundsMode.RectTransform:
                if (interactionBoundsRect == null) return true;
                interactionBoundsRect.GetWorldCorners(_boundsCorners);
                return point.x >= _boundsCorners[0].x && point.x <= _boundsCorners[2].x
                    && point.y >= _boundsCorners[0].y && point.y <= _boundsCorners[2].y;
            case InteractionBoundsMode.Collider:
                if (interactionBoundsCollider == null) return true;
                return interactionBoundsCollider.OverlapPoint(point);
            default:
                return true;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(dragging)
        {
            draggingUpdateEvent.Invoke();
        }
    }

    public void enterDrag()
    {
        dragging = true;
        startDraggingEvent.Invoke();
    }

    public void exitDrag()
    {
        dragging = false;
        endDraggingEvent.Invoke();
    }

    public void cancelDrag()
    {
        dragging = false;
        cancelDraggingEvent.Invoke();
    }

    public void debugEnter()
    {
        Debug.Log($"Enter Hold {gameObject.name}");
    }

    public void debugUpdate()
    {
        Debug.Log($"Update Hold {gameObject.name}");
    }

    public void debugExit()
    {
        Debug.Log($"Exit Hold {gameObject.name}");
    }
}
