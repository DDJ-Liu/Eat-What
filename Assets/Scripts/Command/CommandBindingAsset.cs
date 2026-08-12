using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 命令绑定配置资产
/// 存储 string key → 命令 Id 的映射关系
/// </summary>
[CreateAssetMenu(fileName = "NewCommandBinding", menuName = "Command/Binding Asset")]
public class CommandBindingAsset : ScriptableObject
{
    [System.Serializable]
    public class Binding
    {
        public string key;           // 策划定义的字符串 key
        public string commandId;     // CommandEntry.Id（类型::方法名）
        public string displayName;   // 编辑器显示用
    }

    [SerializeField]
    public List<Binding> bindings = new List<Binding>();

    /// <summary>
    /// 运行时查找：key → commandId
    /// </summary>
    public string GetCommandId(string key)
    {
        var binding = bindings.Find(b => b.key == key);
        return binding?.commandId;
    }

    /// <summary>
    /// 检查 key 是否已存在
    /// </summary>
    public bool HasKey(string key)
    {
        return bindings.Exists(b => b.key == key);
    }
}
