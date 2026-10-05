# 工具库 · CMD

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

### AI-000067 扩充登记（2026-09-07，未验收检查点）

| ID | 本轮公开面与消费说明 | 状态/边界 |
|---|---|---|
| TK-MOT-07 | Traveler 继续消费 `TransitionBehaviour_Position.MoveTo` 对应的既有 `PlayTo`；新增 `SnapTo(space)` 复用原锚点并取消活动移动；Set 新增 `DeactivateMode.RendererHidden`，避免宿主停用杀死运动协程 | 沿用；AI-000067 编辑器外初始 P0、P0 Clue 校正、P0↔P1 路径通过；真实场景由 FIX-T3 验收 |
| TK-DBG-01 | 既有世界空间 TMP 在 LateUpdate 跟随显式 Camera 或 Main Camera，3% 左/上边距；字号按 ortho/7.2 缩放；退出恢复位置/旋转/字号/对齐/pivot | 沿用·保护；不新增 UI 输入通道，键位不变；实际画面待引擎验收 |
| TK-DBG-04 | 增加共享引用合并、过期 arrival、防停宿主、Renderer/gate 恢复表、五步日志短路和相机/状态板测试 | 沿用·保护；使用原 runner/stubs，非 Unity 实测 |
| TK-DBG-05 | `DeactivateMode` 默认 ContainerInactive；RendererHidden 缓存并隐藏全部子 Renderer，恢复原始 enabled；Director 汇总各 Space 引用后应用 Set 激活/焦点 | 沿用；保留 overflowRenderers 名单，本 CODE 任务不做序列化改动 |
| TK-DBG-06 | `Assets/Scripts/Tools/Debug/CameraFrameGizmo.cs`：运行时数据组件，显式 camera、P1/P0-Left/P0-Center/Right marker、`FrameSize(Camera)`/`GetMarker(int)`；`Assets/Editor/ShortCycle/CameraFrameGizmo.cs` 的静态 `[DrawGizmo]` 绘制器及同目录 `CameraFrameGizmoMenu.cs` 保留 10% 网格/刻度、±2% 安全框、四色/最近机位加粗和四项对齐菜单 | 沿用·扩充；AI-000074/AI-000155 真实 Unity DLL 双段编译通过，runtime 不引用 UnityEditor；正式场景第四 Marker/引用和最终画面归 AI-000156，不新增平行工具 |

本增补覆盖上述旧行中的本轮变化，其它条目及 TK-MOT-09 冻结状态不变。完整任务报告见 `.ai-workspace/outputs/CK01-K/FIX-T2_线索板可见性与调试参照框报告.md`。

2026-09-08 AI-000074 完成复核：控制台已核实运行时新文件、Editor 绘制器与菜单存在，SHA-256 分别为 `B5971C87…667E5`、`77CC07FF…A0D1`、`2C7AECCC…B4746`，并合并到唯一 TK-DBG-06 登记。源码程序集修复完成，不等同于 AI-000069 场景挂载或最终人工段通过。报告：`.ai-workspace/outputs/CK01-K/FIX-T2d_CameraFrameGizmo_程序集归属修复报告.md`。

| ID | 名称 / 路径 | 能力 | 公开面 | 状态 | 消费方 | 来源 | 备注 |
|---|---|---|---|---|---|---|---|
| TK-CMD-01 | `CommandManager` + `CommandBindingAsset` `Command/` | string key→命令 ID | — | 改造 | 提示类命令 | CK01-A #15 | 业务动作走 ActionRouter |
| TK-CMD-02 | `ShortCycleCommandBridge` | `[Command]` 反射暴露具名动作 | 23 个手写 Command，含 `close_cover` | 沿用 | 调试/驱动 | C-T4 / C-T8 / AI-000071 | 单向消费唯一 ActionRouter；与 23 个具名动作的对应由既有 runner 校验 |

---

