# 命令系统使用手册

## 概述

命令系统提供一套通用的「字符串 Key → 方法」映射机制。开发者用 `[Command]` 特性标记方法，策划通过 Editor Window 配置绑定，运行时通过 `CommandManager.Execute("key")` 调用。

所有命令方法统一签名 `void MethodName(CommandContext ctx)`，通过 `CommandContext` 容器传递任意参数和获取返回值。

---

## 快速开始

### 1. 定义命令

在任意 MonoBehaviour 或普通 C# 类中，为方法添加 `[Command]` 特性：

```csharp
public class BattleSystem : MonoBehaviour
{
    [Command("攻击", Group = "战斗")]
    public void DoAttack(CommandContext ctx)
    {
        int targetId = ctx.GetArg<int>("targetId");
        int damage = CalculateDamage(targetId);
        ctx.SetResult(damage);
    }
}

public static class SaveSystem
{
    [Command("保存游戏", Group = "系统")]
    public static void SaveGame(CommandContext ctx)
    {
        string slot = ctx.GetArg<string>("slot", "auto");
        // ...保存逻辑
        ctx.SetResult(true);
    }
}
```

**要求**：
- 方法签名必须为 `void MethodName(CommandContext ctx)`
- 不符合签名的方法会被跳过并输出警告
- `Group` 参数可选，默认为 `"Default"`，用于编辑器分组显示

### 2. 配置绑定

1. 在 Project 窗口右键 → `Create > Command > Binding Asset` 创建配置资产
2. 打开菜单 `Tools > Command Binding Manager`
3. 在右侧「配置资产」字段选择刚创建的 SO
4. 左侧自动列出所有 `[Command]` 方法，按分组折叠
5. 点击方法旁的 `+` 按钮快速添加绑定，或在右侧底部手动输入 Key 并从下拉列表选择命令

### 3. 场景设置

1. 在场景中创建一个 GameObject，挂载 `CommandManager` 组件
2. 将 `CommandBindingAsset` 拖入 `bindingAsset` 字段

### 4. 运行时调用

```csharp
// 最简调用
CommandManager.Instance.Execute("attack");

// 带参数
var ctx = CommandContext.WithArg("targetId", 42);
CommandManager.Instance.Execute("attack", ctx);
int damage = ctx.GetResult<int>();

// 泛型便捷方法（自动取结果）
bool ok = CommandManager.Instance.Execute<bool>("saveGame",
    CommandContext.WithArg("slot", "slot1"));

// 检查命令是否存在
if (CommandManager.Instance.HasCommand("attack"))
{
    // ...
}
```

---

## 核心组件

| 文件 | 职责 |
|------|------|
| `CommandAttribute.cs` | `[Command("名称", Group = "分组")]` 特性定义 |
| `CommandContext.cs` | 参数/返回值容器 |
| `CommandEntry.cs` | 单条命令的反射元数据与调用封装 |
| `CommandRegistry.cs` | 反射扫描器，发现所有 `[Command]` 方法 |
| `CommandBindingAsset.cs` | ScriptableObject，存储 Key → 命令的绑定配置 |
| `CommandManager.cs` | 全局单例，运行时调度入口 |
| `CommandBindingWindow.cs`（Editor） | 绑定管理面板 |

---

## CommandContext API

### 参数操作

```csharp
ctx.SetArg<T>(string key, T value)    // 写入参数
ctx.GetArg<T>(string key)             // 读取参数（类型不匹配返回 default）
ctx.GetArg<T>(string key, T fallback) // 读取参数（类型不匹配返回 fallback）
ctx.HasArg(string key)                // 检查参数是否存在
```

### 结果操作

```csharp
ctx.SetResult<T>(T value)             // 命令内部写入结果
ctx.GetResult<T>()                    // 调用方读取结果
ctx.GetResult<T>(T fallback)          // 带默认值读取
ctx.HasResult                         // 是否已设置结果
```

### 工厂方法

```csharp
CommandContext.Create()                        // 空 context
CommandContext.WithArg<T>(string key, T value)  // 带单个参数的 context
```

如果需要传递多个参数，链式调用 `SetArg`：

```csharp
var ctx = CommandContext.Create();
ctx.SetArg("targetId", 42);
ctx.SetArg("weapon", "sword");
ctx.SetArg("isCrit", true);
```

---

## 编辑器窗口

通过 `Tools > Command Binding Manager` 打开。

### 左侧面板 — 已发现的命令

- 自动扫描所有带 `[Command]` 特性的方法
- 按 `Group` 分组折叠显示
- 顶部搜索框支持按名称、方法名、分组过滤
- 每条命令右侧 `+` 按钮：快速添加到当前 SO（使用命令名作为默认 Key）

### 右侧面板 — 绑定配置

- 顶部选择 `CommandBindingAsset`（记忆上次选择）
- 绑定列表：每行显示 Key（可编辑）、命令名、删除按钮
- 失效绑定（命令已删除/重命名）显示红色高亮
- 底部新增区域：输入 Key + 命令下拉列表 + 添加按钮
- 所有操作支持 Ctrl+Z 撤销

### 刷新

点击工具栏「刷新扫描」按钮重新扫描，适用于新增/修改了 `[Command]` 方法后。

---

## 实例方法 vs 静态方法

| 类型 | 适用场景 | 实例来源 |
|------|---------|---------|
| **静态方法** | 无状态工具命令（保存、加载、计算） | 无需实例 |
| **MonoBehaviour 实例方法** | 需要访问场景对象的命令 | `FindObjectOfType` 自动查找 |
| **普通类实例方法** | 有状态但非 MonoBehaviour | `Activator.CreateInstance` 自动创建（需无参构造） |

推荐：无状态命令优先使用 `static` 方法，避免实例解析开销。

---

## 注意事项

1. **方法重命名/删除后**，SO 中的 `commandId` 会失效，编辑器窗口会红色提示，需重新绑定
2. **反射扫描仅在初始化时执行一次**，运行时无扫描开销
3. **扫描范围限制为 `Assembly-CSharp`**，不会扫描 Unity 内部或第三方包
4. **Key 必须唯一**，重复 Key 后者覆盖前者（会输出警告）
5. **MonoBehaviour 命令**要求对应组件在场景中已存在，否则执行时报错

---

## 命令字符串解析

### 概述

`CommandStringParser` 支持从一个字符串中解析出多组「命令名 + 参数」，用于对话系统等需要在表格中配置命令序列的场景。

### 语法规则

```
命令名(参数1,参数2,...);命令名(参数1,...);命令名()
```

**示例：**

```
SetFlag(quest1,true);AddItem(sword,1);ShowDialog(npc_01,hello)
```

| 规则 | 说明 |
|------|------|
| 多条命令 | 用 `;` 分隔 |
| 命令格式 | `命令名(参数1,参数2,...)` |
| 无参命令 | `DoSomething()` 或 `DoSomething`（可省略括号） |
| 空白处理 | 参数两侧空格自动 trim |
| 尾部分号 | `SetFlag(a,b);` — 尾部空段自动跳过 |
| 括号未闭合 | `Bad(unclosed` — 输出警告并跳过该段 |

### 位置参数约定

字符串中的参数按位置映射到 `CommandContext`：

| 字符串参数位置 | CommandContext Key |
|---------------|-------------------|
| 第 1 个参数 | `arg0` |
| 第 2 个参数 | `arg1` |
| 第 N 个参数 | `arg{N-1}` |
| 参数总数 | `argCount`（int） |

**示例：** `SetFlag(quest1,true)` 解析后等价于：

```csharp
ctx.SetArg("arg0", "quest1");   // string
ctx.SetArg("arg1", true);       // bool（自动推断）
ctx.SetArg("argCount", 2);      // int
```

### 类型自动推断

参数值按以下优先级自动推断类型：

| 优先级 | 类型 | 匹配规则 | 示例 |
|--------|------|---------|------|
| 1 | `bool` | `true` / `false`（忽略大小写） | `true` → `bool` |
| 2 | `int` | 纯整数 | `42` → `int` |
| 3 | `float` | 带小数点的数字 | `3.14` → `float` |
| 4 | `string` | 以上均不匹配 | `hello` → `string` |

> 注意：float 解析使用 `InvariantCulture`，始终以 `.` 作为小数点。

### API

#### `CommandManager.ExecuteString(string commandString)`

解析命令字符串并**顺序执行**所有命令。用于 `execution` 字段。

```csharp
CommandManager.Instance.ExecuteString("SetFlag(quest1,true);AddItem(sword,1)");
```

#### `CommandManager.EvaluateString(string commandString)`

解析命令字符串并执行，返回所有命令结果的 **AND** 合并值。用于 `conditionFunc` 字段。

```csharp
bool allowed = CommandManager.Instance.EvaluateString("CheckFlag(quest1);HasItem(key)");
```

| 情况 | 返回值 |
|------|--------|
| 空字符串 / null | `true`（无条件 = 始终通过） |
| 所有命令 `SetResult(true)` | `true` |
| 任一命令 `SetResult(false)` | `false` |
| 任一命令未调用 `SetResult` | `false` |

### 编写命令方法示例

#### 执行型命令（无返回值）

```csharp
// 设置游戏标记
// 调用：CommandManager.Instance.ExecuteString("SetFlag(quest_complete,true)")
[Command("SetFlag", Group = "游戏状态")]
public static void SetFlag(CommandContext ctx)
{
    string flagName = ctx.GetArg<string>("arg0");
    bool value = ctx.GetArg<bool>("arg1", true);
    GameState.SetFlag(flagName, value);
    Debug.Log($"Flag '{flagName}' set to {value}");
}

// 添加物品到背包
// 调用：CommandManager.Instance.ExecuteString("AddItem(sword,5)")
[Command("AddItem", Group = "背包")]
public static void AddItem(CommandContext ctx)
{
    string itemId = ctx.GetArg<string>("arg0");
    int count = ctx.GetArg<int>("arg1", 1);
    Inventory.Instance.AddItem(itemId, count);
}

// 播放音效
// 调用：CommandManager.Instance.ExecuteString("PlaySound(click)")
[Command("PlaySound", Group = "音频")]
public static void PlaySound(CommandContext ctx)
{
    string soundId = ctx.GetArg<string>("arg0");
    AudioManager.Instance.Play(soundId);
}
```

#### 条件型命令（返回 bool）

```csharp
// 检查游戏标记
// 调用：bool ok = CommandManager.Instance.EvaluateString("CheckFlag(quest_complete)")
[Command("CheckFlag", Group = "游戏状态")]
public static void CheckFlag(CommandContext ctx)
{
    string flagName = ctx.GetArg<string>("arg0");
    bool hasFlag = GameState.GetFlag(flagName);
    ctx.SetResult(hasFlag);
}

// 检查背包中是否有物品
// 调用：bool ok = CommandManager.Instance.EvaluateString("HasItem(key,1)")
[Command("HasItem", Group = "背包")]
public static void HasItem(CommandContext ctx)
{
    string itemId = ctx.GetArg<string>("arg0");
    int minCount = ctx.GetArg<int>("arg1", 1);
    int actualCount = Inventory.Instance.GetItemCount(itemId);
    ctx.SetResult(actualCount >= minCount);
}

// 检查玩家等级
// 调用：bool ok = CommandManager.Instance.EvaluateString("CheckLevel(10)")
[Command("CheckLevel", Group = "角色")]
public static void CheckLevel(CommandContext ctx)
{
    int requiredLevel = ctx.GetArg<int>("arg0");
    int playerLevel = Player.Instance.Level;
    ctx.SetResult(playerLevel >= requiredLevel);
}
```

#### 实例方法示例（MonoBehaviour）

```csharp
public class QuestManager : MonoBehaviour
{
    // 完成任务
    // 调用：CommandManager.Instance.ExecuteString("CompleteQuest(quest_001)")
    [Command("CompleteQuest", Group = "任务")]
    public void CompleteQuest(CommandContext ctx)
    {
        string questId = ctx.GetArg<string>("arg0");
        var quest = GetQuest(questId);
        if (quest != null)
        {
            quest.Complete();
            Debug.Log($"Quest '{questId}' completed!");
        }
    }

    // 获取任务进度
    // 调用：int progress = CommandManager.Instance.Execute<int>("GetQuestProgress",
    //       CommandContext.WithArg("arg0", "quest_001"))
    [Command("GetQuestProgress", Group = "任务")]
    public void GetQuestProgress(CommandContext ctx)
    {
        string questId = ctx.GetArg<string>("arg0");
        var quest = GetQuest(questId);
        int progress = quest?.Progress ?? 0;
        ctx.SetResult(progress);
    }

    private Quest GetQuest(string questId) => /* 查询逻辑 */ null;
}
```

### 在对话系统中使用

`DialogChoice` 的 `conditionFunc` 和 `execution` 字段直接存储命令字符串：

```csharp
// 对话 UI 中评估选项
foreach (var choice in dialogData.choices)
{
    // 检查选项条件
    bool canShow = CommandManager.Instance.EvaluateString(choice.conditionFunc);
    if (!canShow) continue;

    // 显示选项...
    // 玩家选择后执行效果
    CommandManager.Instance.ExecuteString(choice.execution);
}
```

### 核心组件

| 文件 | 职责 |
|------|------|
| `CommandStringParser.cs` | 字符串解析器（`Parse` + `BuildContext`） |

`ParsedCommand`（struct）：解析结果，包含 `commandKey` 和 `args` 数组。
