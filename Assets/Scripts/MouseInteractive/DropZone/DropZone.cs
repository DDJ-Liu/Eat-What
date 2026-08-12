using UnityEngine;
using UnityEngine.Events;

public class DropZone : MonoBehaviour
{
    [Header("类别标签")]
    [Tooltip("该 DropZone 所属类别，Draggable 的 allowedDropTags 与此做按位与匹配")]
    public DropTag zoneTags = DropTag.Everything;

    [Header("事件")]
    public UnityEvent<MouseDraggableObject> onDropEvent;
    [Tooltip("Draggable 的 allowedDropTags 与本 zoneTags 无交集时触发")]
    public UnityEvent<MouseDraggableObject> onDropRejectedEvent;

    public void OnDrop(MouseDraggableObject draggable)
    {
        if (CanAccept(draggable))
            onDropEvent?.Invoke(draggable);
        else
            onDropRejectedEvent?.Invoke(draggable);
    }

    public bool CanAccept(MouseDraggableObject draggable)
    {
        if (draggable == null) return false;
        return (draggable.allowedDropTags & zoneTags) != 0;
    }

    public void onDropDebug(MouseDraggableObject draggable)
    {
        Debug.Log(draggable.gameObject.name + "Droped");
    }

    public void AlignDroppedToZoneCenter(MouseDraggableObject draggable)
    {
        draggable.transform.position = this.transform.position;
    }
}
