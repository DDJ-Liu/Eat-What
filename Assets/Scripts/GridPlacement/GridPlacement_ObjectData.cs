using UnityEngine;

/// <summary>
/// 可放置物体的数据定义。
/// 用于配置物体类型，通过 Spawn() 创建实例。
/// 占位偏移由 prefab 子物体的 localPosition 自动读取，无需在此配置。
/// </summary>
[CreateAssetMenu(fileName = "NewPlaceableObject", menuName = "GridPlacement/ObjectData")]
public class GridPlacement_ObjectData : ScriptableObject
{
    public string objectName;
    [Tooltip("需挂载 GridPlacement_Object 组件")]
    public GameObject prefab;
    public bool allowRotate = true;
    [Tooltip("UI/预览用图标")]
    public Sprite icon;

    /// <summary>
    /// 实例化物体并配置参数。
    /// </summary>
    public GridPlacement_Object Spawn(Transform parent = null)
    {
        if (prefab == null)
        {
            Debug.LogError($"GridPlacement_ObjectData '{objectName}': prefab is null");
            return null;
        }

        GameObject instance = Instantiate(prefab, parent);
        instance.name = prefab.name;

        var obj = instance.GetComponent<GridPlacement_Object>();
        if (obj == null)
        {
            Debug.LogError($"GridPlacement_ObjectData '{objectName}': prefab missing GridPlacement_Object component");
            Destroy(instance);
            return null;
        }

        obj.allowRotate = allowRotate;
        return obj;
    }
}
