using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 反射扫描器：发现所有标记了 [Command] 的方法
/// Runtime 和 Editor 共用
/// </summary>
public static class CommandRegistry
{
    private static List<CommandEntry> cachedEntries;

    /// <summary>
    /// 获取所有已发现的命令（带缓存）
    /// </summary>
    public static List<CommandEntry> GetAll(bool forceRescan = false)
    {
        if (cachedEntries == null || forceRescan)
            cachedEntries = Scan();
        return cachedEntries;
    }

    /// <summary>
    /// 按分组返回命令
    /// </summary>
    public static Dictionary<string, List<CommandEntry>> GetGrouped(bool forceRescan = false)
    {
        var all = GetAll(forceRescan);
        var grouped = new Dictionary<string, List<CommandEntry>>();
        foreach (var entry in all)
        {
            if (!grouped.ContainsKey(entry.Group))
                grouped[entry.Group] = new List<CommandEntry>();
            grouped[entry.Group].Add(entry);
        }
        return grouped;
    }

    /// <summary>
    /// 通过 Id 查找命令
    /// </summary>
    public static CommandEntry FindById(string id)
    {
        return GetAll().Find(e => e.Id == id);
    }

    private static List<CommandEntry> Scan()
    {
        var results = new List<CommandEntry>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            // 只扫描项目脚本程序集
            string asmName = assembly.GetName().Name;
            if (asmName != "Assembly-CSharp" && asmName != "Assembly-CSharp-Editor")
                continue;

            foreach (var type in assembly.GetTypes())
            {
                var methods = type.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Static | BindingFlags.Instance |
                    BindingFlags.DeclaredOnly);

                foreach (var method in methods)
                {
                    var attr = method.GetCustomAttribute<CommandAttribute>();
                    if (attr == null) continue;

                    // 验证签名：void MethodName(CommandContext)
                    var parameters = method.GetParameters();
                    if (method.ReturnType != typeof(void) ||
                        parameters.Length != 1 ||
                        parameters[0].ParameterType != typeof(CommandContext))
                    {
                        Debug.LogWarning(
                            $"[CommandRegistry] 跳过 {type.Name}.{method.Name}：" +
                            $"签名必须为 void(CommandContext)");
                        continue;
                    }

                    results.Add(new CommandEntry
                    {
                        Name = attr.Name,
                        Group = attr.Group,
                        MethodName = method.Name,
                        DeclaringType = type.AssemblyQualifiedName,
                        IsStatic = method.IsStatic
                    });
                }
            }
        }

        return results;
    }
}
