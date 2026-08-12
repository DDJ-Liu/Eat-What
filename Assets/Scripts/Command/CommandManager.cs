using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 命令系统全局单例
/// 通过 string key 调用已绑定的 [Command] 方法
/// </summary>
public class CommandManager : MonoBehaviour
{
    public static CommandManager Instance;

    [Header("绑定配置")]
    [SerializeField] private CommandBindingAsset bindingAsset;

    // 运行时缓存：key → CommandEntry
    private Dictionary<string, CommandEntry> runtimeBindings = new Dictionary<string, CommandEntry>();

    // 实例方法的对象缓存：Type → instance
    private Dictionary<Type, object> instanceCache = new Dictionary<Type, object>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        Initialize();
    }

    /// <summary>
    /// 从 SO 构建运行时查找表
    /// </summary>
    private void Initialize()
    {
        runtimeBindings.Clear();
        if (bindingAsset == null)
        {
            Debug.LogWarning("[CommandManager] 未指定 CommandBindingAsset");
            return;
        }

        // 触发一次全量扫描
        CommandRegistry.GetAll();

        foreach (var binding in bindingAsset.bindings)
        {
            if (string.IsNullOrEmpty(binding.key) || string.IsNullOrEmpty(binding.commandId))
                continue;

            var entry = CommandRegistry.FindById(binding.commandId);
            if (entry == null)
            {
                Debug.LogWarning(
                    $"[CommandManager] 绑定 '{binding.key}' 指向的命令 '{binding.commandId}' 未找到");
                continue;
            }

            if (runtimeBindings.ContainsKey(binding.key))
            {
                Debug.LogWarning($"[CommandManager] 重复 key '{binding.key}'，后者覆盖前者");
            }

            runtimeBindings[binding.key] = entry;
        }

        Debug.Log($"[CommandManager] 初始化完成，已加载 {runtimeBindings.Count} 条绑定");
    }

    // ── 公开 API ──

    /// <summary>
    /// 通过 key 执行命令
    /// </summary>
    public void Execute(string key, CommandContext ctx = null)
    {
        if (ctx == null) ctx = new CommandContext();

        if (!runtimeBindings.TryGetValue(key, out var entry))
        {
            Debug.LogWarning($"[CommandManager] 未找到 key: {key}");
            return;
        }

        object instance = null;
        if (!entry.IsStatic)
        {
            instance = ResolveInstance(entry);
            if (instance == null)
            {
                Debug.LogError(
                    $"[CommandManager] 无法获取 {entry.DeclaringType} 的实例来执行 '{key}'");
                return;
            }
        }

        entry.Invoke(ctx, instance);
    }

    /// <summary>
    /// 执行并获取结果的便捷方法
    /// </summary>
    public T Execute<T>(string key, CommandContext ctx = null)
    {
        if (ctx == null) ctx = new CommandContext();
        Execute(key, ctx);
        return ctx.GetResult<T>();
    }

    /// <summary>
    /// 检查某个 key 是否有效绑定
    /// </summary>
    public bool HasCommand(string key)
    {
        return runtimeBindings.ContainsKey(key);
    }

    /// <summary>
    /// 解析命令字符串并顺序执行所有命令
    /// 语法：SetFlag(quest1,true);AddItem(sword,1)
    /// </summary>
    public void ExecuteString(string commandString)
    {
        var commands = CommandStringParser.Parse(commandString);
        foreach (var cmd in commands)
        {
            var ctx = CommandStringParser.BuildContext(cmd);
            Execute(cmd.commandKey, ctx);
        }
    }

    /// <summary>
    /// 解析命令字符串并执行，返回所有命令结果的 AND 合并值
    /// 用于条件判断：空字符串返回 true（无条件 = 始终通过）
    /// 任一命令返回 false 或未设置 Result，整体返回 false
    /// </summary>
    public bool EvaluateString(string commandString)
    {
        var commands = CommandStringParser.Parse(commandString);
        if (commands.Count == 0) return true;

        foreach (var cmd in commands)
        {
            var ctx = CommandStringParser.BuildContext(cmd);
            Execute(cmd.commandKey, ctx);

            if (!ctx.HasResult || !ctx.GetResult<bool>())
                return false;
        }
        return true;
    }

    // ── 实例解析 ──

    private object ResolveInstance(CommandEntry entry)
    {
        var type = Type.GetType(entry.DeclaringType);
        if (type == null) return null;

        // 检查缓存
        if (instanceCache.TryGetValue(type, out object cached))
        {
            // MonoBehaviour 可能已被 Destroy
            if (cached is MonoBehaviour mb && mb != null)
                return cached;
            if (cached != null && !(cached is UnityEngine.Object))
                return cached;
            instanceCache.Remove(type);
        }

        // MonoBehaviour：从场景中查找
        if (typeof(MonoBehaviour).IsAssignableFrom(type))
        {
            var found = FindObjectOfType(type);
            if (found != null)
            {
                instanceCache[type] = found;
                return found;
            }
            return null;
        }

        // 普通 C# 类：尝试无参构造
        try
        {
            var instance = Activator.CreateInstance(type);
            instanceCache[type] = instance;
            return instance;
        }
        catch (Exception e)
        {
            Debug.LogError($"[CommandManager] 创建 {type.Name} 实例失败: {e.Message}");
            return null;
        }
    }
}
