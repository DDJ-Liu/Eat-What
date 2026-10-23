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

    // Reviewed ASM-01 project assemblies. Never discover commands in arbitrary packages.
    private static readonly HashSet<string> ProjectAssemblies = new HashSet<string>(StringComparer.Ordinal)
    {
        "Assembly-CSharp", "Assembly-CSharp-Editor",
        "Toolkit", "Generated.DataTables", "ShortCycle", "Legacy", "MiniGames.Core", "LongLoop",
        "Editor.Project", "Editor.ShortCycle", "Editor.DataTables", "Editor.ProjectValidation"
    };

    /// <summary>
    /// Resolve current names, or a unique project type saved under a legacy default assembly.
    /// This does not rewrite serialized command IDs or load unapproved assemblies.
    /// </summary>
    public static Type ResolveDeclaringType(string qualifiedName)
    {
        if (string.IsNullOrWhiteSpace(qualifiedName)) return null;
        var parts = qualifiedName.Split(',');
        var typeName = parts[0].Trim();
        if (typeName.Length == 0 || typeName.IndexOf('[') >= 0) return null;
        var assemblyName = parts.Length > 1 ? parts[1].Trim() : null;
        if (assemblyName != null && !ProjectAssemblies.Contains(assemblyName)) return null;
        var legacy = assemblyName == null || assemblyName == "Assembly-CSharp" ||
            assemblyName == "Assembly-CSharp-Editor";
        Type resolved = null;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var name = assembly.GetName().Name;
            if (!ProjectAssemblies.Contains(name) || (!legacy && name != assemblyName)) continue;
            Type candidate;
            try { candidate = assembly.GetType(typeName, false); }
            catch (ArgumentException) { return null; }
            if (candidate == null) continue;
            if (resolved != null && resolved != candidate) return null; // Ambiguous alias: fail closed.
            resolved = candidate;
        }
        return resolved;
    }

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
        if (string.IsNullOrEmpty(id)) return null;
        var entries = GetAll();
        var exact = entries.Find(e => e.Id == id);
        if (exact != null) return exact;
        var separator = id.LastIndexOf("::", StringComparison.Ordinal);
        if (separator <= 0 || separator + 2 >= id.Length) return null;
        var type = ResolveDeclaringType(id.Substring(0, separator));
        if (type == null) return null;
        var methodName = id.Substring(separator + 2);
        return entries.Find(e => e.MethodName == methodName &&
            ResolveDeclaringType(e.DeclaringType) == type);
    }

    private static List<CommandEntry> Scan()
    {
        var results = new List<CommandEntry>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            // 只扫描项目脚本程序集
            string asmName = assembly.GetName().Name;
            if (!ProjectAssemblies.Contains(asmName))
                continue;

            Type[] types;
            try { types = assembly.GetTypes(); }
            catch (ReflectionTypeLoadException error)
            {
                types = error.Types;
                Debug.LogWarning("[CommandRegistry] Partial type load: " + asmName);
            }
            foreach (var type in types)
            {
                if (type == null) continue;
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
