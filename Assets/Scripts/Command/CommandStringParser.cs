using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// 已解析的单条命令：命令名 + 位置参数
/// </summary>
[System.Serializable]
public struct ParsedCommand
{
    public string commandKey;
    public string[] args;
}

/// <summary>
/// 命令字符串解析器
/// 将函数调用风格的字符串（如 "SetFlag(quest1,true);AddItem(sword,1)"）
/// 解析为 ParsedCommand 列表，并构建 CommandContext
/// </summary>
public static class CommandStringParser
{
    /// <summary>
    /// 将命令字符串解析为 ParsedCommand 列表
    /// 语法：命令名(参数1,参数2,...);命令名(参数1,...)
    /// </summary>
    public static List<ParsedCommand> Parse(string commandString)
    {
        var results = new List<ParsedCommand>();

        if (string.IsNullOrWhiteSpace(commandString))
            return results;

        var segments = commandString.Split(';');

        foreach (var raw in segments)
        {
            var segment = raw.Trim();
            if (string.IsNullOrEmpty(segment))
                continue;

            int parenOpen = segment.IndexOf('(');

            // 无括号 → 无参命令
            if (parenOpen < 0)
            {
                results.Add(new ParsedCommand
                {
                    commandKey = segment,
                    args = System.Array.Empty<string>()
                });
                continue;
            }

            int parenClose = segment.LastIndexOf(')');
            if (parenClose < parenOpen)
            {
                Debug.LogWarning($"[CommandStringParser] 括号未闭合，跳过: {segment}");
                continue;
            }

            string key = segment.Substring(0, parenOpen).Trim();
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning($"[CommandStringParser] 命令名为空，跳过: {segment}");
                continue;
            }

            string inner = segment.Substring(parenOpen + 1, parenClose - parenOpen - 1).Trim();

            string[] args;
            if (string.IsNullOrEmpty(inner))
            {
                args = System.Array.Empty<string>();
            }
            else
            {
                var parts = inner.Split(',');
                args = new string[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                    args[i] = parts[i].Trim();
            }

            results.Add(new ParsedCommand
            {
                commandKey = key,
                args = args
            });
        }

        return results;
    }

    /// <summary>
    /// 根据 ParsedCommand 构建 CommandContext
    /// 位置参数以 arg0, arg1, arg2... 为 key，自动推断 bool/int/float/string 类型
    /// 同时写入 argCount 表示参数总数
    /// </summary>
    public static CommandContext BuildContext(ParsedCommand cmd)
    {
        var ctx = new CommandContext();
        ctx.SetArg("argCount", cmd.args.Length);

        for (int i = 0; i < cmd.args.Length; i++)
        {
            string argKey = "arg" + i;
            string value = cmd.args[i];

            // 按优先级推断类型：bool → int → float → string
            if (bool.TryParse(value, out bool boolVal))
            {
                ctx.SetArg(argKey, boolVal);
            }
            else if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intVal))
            {
                ctx.SetArg(argKey, intVal);
            }
            else if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatVal))
            {
                ctx.SetArg(argKey, floatVal);
            }
            else
            {
                ctx.SetArg(argKey, value);
            }
        }

        return ctx;
    }
}
