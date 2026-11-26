using UnityEngine;

/// <summary>
/// 防止自身跟随父物体缩放，始终保持 Start 时的世界缩放。
/// </summary>
public class IgnoreParentScale : MonoBehaviour
{
    private Vector3 _worldScale;

    void Start()
    {
        _worldScale = transform.lossyScale;
    }

    void LateUpdate()
    {
        Transform p = transform.parent;
        if (p == null) return;

        Vector3 parentScale = p.lossyScale;
        transform.localScale = new Vector3(
            parentScale.x != 0 ? _worldScale.x / parentScale.x : transform.localScale.x,
            parentScale.y != 0 ? _worldScale.y / parentScale.y : transform.localScale.y,
            parentScale.z != 0 ? _worldScale.z / parentScale.z : transform.localScale.z
        );
    }
}
