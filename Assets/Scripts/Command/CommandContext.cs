using System.Collections.Generic;

/// <summary>
/// 命令的统一输入/输出容器
/// 通过 SetArg/GetArg 传递任意类型参数，通过 SetResult/GetResult 获取返回值
/// </summary>
public class CommandContext
{
    private Dictionary<string, object> args = new Dictionary<string, object>();
    private object result;

    public bool HasResult { get; private set; }

    // ── 参数操作 ──

    public void SetArg<T>(string key, T value)
    {
        args[key] = value;
    }

    public T GetArg<T>(string key, T defaultValue = default)
    {
        if (args.TryGetValue(key, out object val) && val is T typed)
            return typed;
        return defaultValue;
    }

    public bool HasArg(string key)
    {
        return args.ContainsKey(key);
    }

    // ── 结果操作 ──

    public void SetResult<T>(T value)
    {
        result = value;
        HasResult = true;
    }

    public T GetResult<T>(T defaultValue = default)
    {
        if (HasResult && result is T typed)
            return typed;
        return defaultValue;
    }

    // ── 工厂方法 ──

    public static CommandContext Create()
    {
        return new CommandContext();
    }

    public static CommandContext WithArg<T>(string key, T value)
    {
        var ctx = new CommandContext();
        ctx.SetArg(key, value);
        return ctx;
    }
}
