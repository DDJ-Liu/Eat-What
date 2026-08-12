using System;

/// <summary>
/// 标记一个方法为可被命令系统调用的命令
/// 方法签名必须为 void MethodName(CommandContext ctx)
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class CommandAttribute : Attribute
{
    public string Name { get; }
    public string Group { get; set; }

    public CommandAttribute(string name)
    {
        Name = name;
        Group = "Default";
    }
}
