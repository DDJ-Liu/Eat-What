using UnityEngine;

/// <summary>
/// 防止自身跟随父物体旋转，始终保持脚本启用瞬间的世界 Z 朝向（2D）。
/// 通过直接设置 localEulerAngles 绕开四元数回算，避免父级非均匀/翻转缩放导致 localEulerAngles 被压成 0。
/// </summary>
[ExecuteAlways]
public class IgnoreParentRotation : MonoBehaviour
{
    private float _targetWorldZ;

    void OnEnable()
    {
        _targetWorldZ = transform.eulerAngles.z;
    }

    void LateUpdate()
    {
        float parentZ = transform.parent != null ? transform.parent.eulerAngles.z : 0f;
        transform.localEulerAngles = new Vector3(0f, 0f, _targetWorldZ - parentZ);
    }
}
