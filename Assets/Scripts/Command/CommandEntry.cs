using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 一个已发现的 [Command] 方法的完整描述
/// 封装反射元数据，提供调用能力
/// </summary>
public class CommandEntry
{
    public string Name;           // [Command] 特性中的名称
    public string Group;          // 分组
    public string MethodName;     // C# 方法名
    public string DeclaringType;  // 声明类的 AssemblyQualifiedName
    public bool IsStatic;         // 是否为静态方法

    // 缓存的 MethodInfo（域重载后自动重建）
    [NonSerialized] private MethodInfo cachedMethod;

    /// <summary>
    /// 唯一标识符，用于 SO 中序列化引用
    /// </summary>
    public string Id => $"{DeclaringType}::{MethodName}";

    /// <summary>
    /// 编辑器显示用的友好名称
    /// </summary>
    public string DisplayName
    {
        get
        {
            // 从 AssemblyQualifiedName 中提取短类名
            string shortType = DeclaringType;
            int commaIndex = shortType.IndexOf(',');
            if (commaIndex > 0)
                shortType = shortType.Substring(0, commaIndex);
            int dotIndex = shortType.LastIndexOf('.');
            if (dotIndex >= 0)
                shortType = shortType.Substring(dotIndex + 1);

            return $"[{Group}] {Name} ({shortType}.{MethodName})";
        }
    }

    /// <summary>
    /// 执行命令
    /// </summary>
    public void Invoke(CommandContext ctx, object instance = null)
    {
        if (cachedMethod == null)
            RebuildCache();

        if (cachedMethod == null)
        {
            Debug.LogError($"[CommandSystem] 无法找到方法: {Id}");
            return;
        }

        cachedMethod.Invoke(IsStatic ? null : instance, new object[] { ctx });
    }

    private void RebuildCache()
    {
        var type = Type.GetType(DeclaringType);
        if (type == null)
        {
            Debug.LogWarning($"[CommandSystem] 无法找到类型: {DeclaringType}");
            return;
        }

        cachedMethod = type.GetMethod(MethodName,
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Static | BindingFlags.Instance |
            BindingFlags.DeclaredOnly);
    }
}
