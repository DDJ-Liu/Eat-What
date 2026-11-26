# 工具库 · CMD

最后完整核验：**2026-09-17**（不提高）。本次仅整理文档与核对源码归属；任务执行过程、当时的测试与许可记录移至历史，不能据历史状态启动业务。执行规则以 AGENTS.md、WORKFLOW.md 的现行优先条款为准。工具表保留原能力、使用限制与来源语义，旧候选／待验标签不代表本次重新验收。

## 现状

> 从 [INDEX.md](INDEX.md) 定位；状态与公开面沿用原登记。

<a id="tk-mot-07"></a>
<a id="tk-dbg-01"></a>
<a id="tk-dbg-04"></a>
<a id="tk-dbg-05"></a>
<a id="tk-dbg-06"></a>
<a id="tk-mot-09"></a>
<a id="tk-cmd-01"></a>
<a id="tk-cmd-02"></a>

## 8. CMD · 命令

本增补覆盖上述旧行中的本轮变化，其它条目及 TK-MOT-09 冻结状态不变。完整任务报告见 `.ai-workspace/outputs/CK01-K/FIX-T2_线索板可见性与调试参照框报告.md`。

| ID | 名称 / 路径 | 能力 | 公开面 | 状态 | 消费方 | 来源 | 备注 | 程序集／宿主 |
|---|---|---|---|---|---|---|---| --- |
| TK-CMD-01 | `CommandManager` + `CommandBindingAsset` `Command/` | string key→命令 ID | — | 改造 | 提示类命令 | CK01-A #15 | 业务动作走 ActionRouter | `Toolkit` |
| TK-CMD-02 | `ShortCycleCommandBridge` | `[Command]` 反射暴露具名动作 | 23 个手写 Command，含 `close_cover` | 沿用 | 调试/驱动 | C-T4 / C-T8 / AI-000071 | 单向消费唯一 ActionRouter；与 23 个具名动作的对应由既有 runner 校验 | `ShortCycle` |

---

## 已核程序集与文件定位

按当前 `.asmdef`／最近 `.asmref` 解析；下列只是源码归属，不代表重新编译、运行或验收。

| 程序集 | 定义／归属入口 | 本页相关源码例 |
| --- | --- | --- |
| `Editor.ShortCycle` | [Assets/Editor/ShortCycle/Editor.ShortCycle.asmdef](<../../Assets/Editor/ShortCycle/Editor.ShortCycle.asmdef>) | [CameraFrameGizmo.cs](<../../Assets/Editor/ShortCycle/CameraFrameGizmo.cs>)；[CameraFrameGizmoMenu.cs](<../../Assets/Editor/ShortCycle/CameraFrameGizmoMenu.cs>) |
| `ShortCycle` | [Assets/Scripts/Cooking/ShortCycle/ShortCycle.asmdef](<../../Assets/Scripts/Cooking/ShortCycle/ShortCycle.asmdef>) | [ShortCycleCommandBridge.cs](<../../Assets/Scripts/Cooking/ShortCycle/ShortCycleCommandBridge.cs>) |
| `Toolkit` | [Assets/Scripts/Tools/Toolkit.asmdef](<../../Assets/Scripts/Tools/Toolkit.asmdef>) | [CameraFrameGizmo.cs](<../../Assets/Scripts/Tools/Debug/CameraFrameGizmo.cs>)；[Tools.cs](<../../Assets/Scripts/Tools/Tools.cs>)；[TransitionBehaviour_Position.cs](<../../Assets/Scripts/Tools/Transition/TransitionBehaviour_Position.cs>) |
| `Toolkit` | [Assets/Scripts/Command/Toolkit.asmref](<../../Assets/Scripts/Command/Toolkit.asmref>) | [CommandBindingAsset.cs](<../../Assets/Scripts/Command/CommandBindingAsset.cs>)；[CommandContext.cs](<../../Assets/Scripts/Command/CommandContext.cs>)；[CommandManager.cs](<../../Assets/Scripts/Command/CommandManager.cs>) |

## 历史

[本页整理前全文与历史记录](<../../docs/archive/工程事实_20261124/history/.ai-workspace/toolkit/CMD.md>)；[归档与原字节恢复索引](<../../docs/archive/工程事实_20261124/README.md>)；[已知不符与保留边界](<../../docs/archive/工程事实_20261124/差异与保留边界.md>)。

### 历史章节兼容锚点

<a id="ai-000067-扩充登记2026-09-07未验收检查点"></a>
[原章节](<../../docs/archive/工程事实_20261124/history/.ai-workspace/toolkit/CMD.md#ai-000067-扩充登记2026-09-07未验收检查点>)
