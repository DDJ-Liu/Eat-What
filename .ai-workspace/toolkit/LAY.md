# 工具库 · LAY

最后完整核验：**2026-09-17**（不提高）。本次仅整理文档与核对源码归属；任务执行过程、当时的测试与许可记录移至历史，不能据历史状态启动业务。执行规则以 AGENTS.md、WORKFLOW.md 的现行优先条款为准。工具表保留原能力、使用限制与来源语义，旧候选／待验标签不代表本次重新验收。

## 现状

> 从 [INDEX.md](INDEX.md) 定位；状态与公开面沿用原登记。

<a id="tk-lay-08"></a>
<a id="tk-mot-08"></a>
<a id="tk-mot-07"></a>
<a id="tk-int-11"></a>
<a id="tk-lay-01"></a>
<a id="tk-lay-02"></a>
<a id="tk-lay-03"></a>
<a id="tk-lay-04"></a>
<a id="tk-lay-05"></a>
<a id="tk-lay-06"></a>
<a id="tk-lay-07"></a>

## 3. LAY · 排布

| ID | 名称 / 路径 | 能力 | 公开面 | 状态 | 消费方 | 来源 | 备注 | 程序集／宿主 |
|---|---|---|---|---|---|---|---| --- |
| TK-LAY-01 | `Layout` 基类 + `GridLayout` / `HorizontalLayout` / `VerticalLayout` / `ArcLayout` / `ArcLayoutStrategy` + Editor `Tools/LayoutTools/` | 规则阵列点位生成与管理，`ChildSizeMode`；Grid 有约束/对齐/起角/轴向枚举 | 见各类枚举 | 沿用 | 目录 8 卡 / 格架列 / 食材清单 / 步骤行 / 筛选栏 | 指导书 §五 | **托盘 20 锚点与区块 8 锚点手工定点，不用工具**（R-05） | `Toolkit` |
| TK-LAY-02 | `ShortCycleTrayAnchorLayout` `Cooking/ShortCycle/` | 20 手工锚点按序占用/释放 | 见类 | 沿用 | Tray_Group | C-T6 v2.2 | 通用化候选：AnchorSlotLayout | `ShortCycle` |
| TK-LAY-03 | `ShortCycleIconScaleTarget` + `ShortCycleV22LayoutContracts` | 图标七类画布统一缩放常量 | 常量表 | 沿用 | 六 prefab | C-T6 v2.2 | 512 画布规则（G19） | `ShortCycle` |
| TK-LAY-04 | `GridPlacement`（×3） `GridPlacement/` | 占位网格：cell 状态、多格、旋转 | — | 冻结 | —（P2 / 长循环） | 总档 A-6 | 复用裁决改【沿用·保留】 | 见下方已核源码归属 |
| TK-LAY-05 | `IgnoreParentScale` / `IgnoreParentRotation` | 子对象抵消父级缩放/旋转 | — | 沿用 | 猫伸展下图标稳定 | CK01-A #33/34 | | `Toolkit` |
| TK-LAY-06 | `TidyLayout` `Assets/Scripts/Tools/TidyLayout.cs` | 可见项稳定压缩到既有点位快照，输出只读 placement 计划 | `TidyLayout.Plan<TItem,TPoint>(items,isVisible,layoutPoints)` | AI323 CODE候选·待CONTROL验收及原生消费 | 一键整理（B1 3.9） | TK-01 / AI-000323 | `layoutPoints` 由既有 TK-LAY-01 布局/锚点提供；不产生点位、不调 ArrangeChildren、不写 Transform 或 _TUNE；容量不足拒绝 | `Toolkit` |
| TK-LAY-07 | `StackedIcon` `Assets/Scripts/Tools/StackedIcon.cs` | 严格识别 Items.stack_type 的 single/stack/pack/container，按数量和调用方显式容量输出形态/可见图层/数量文字计划 | `StackedIcon.TryParseShape`、`StackedIcon.Plan(stackType,quantity,visibleCapacityByShape,showQuantity)` | AI323 CODE候选·待CONTROL验收及原生消费 | 冰箱格件（F2-a 5.4） | TK-01 / AI-000323 | 不提供未经 D-004/005/009 核实的默认图层数、偏移/缩放或实际画面接线；未知类型/负数/容量缺项拒绝 | `Toolkit` |
| TK-LAY-08 | `FridgeCatRig` + `FridgeCatTier` `Cooking/ShortCycle/Presentation/` | 固定层 + 池化扩展层的五列 Shelf 锚点排布与按层滚动；可选按真实 Anchor 间距对齐并自动拟合纵向 Content 边界 | `SetTierCount(int)` `GetShelfAnchor(row,col)` `ScrollToTier(int)` `TierCount`；序列化 `tierScrollMapping` / `tierAlignmentTarget_TUNE` / `tierAlignmentColumn_TUNE` | 沿用·扩充 | ShortCycleFridgeBinder；CAT-T2 Prefab 接线 | AI-000063 / AI-000128 | 默认 `UniformTierIndex` 保持旧行为；`AnchorAlignedAutoBounds` 需固定 Target、Vertical/BoxCollider 接线和严格下降 Anchor，按真实世界跨度及非单位缩放求解；0/1层安全，扩展层回收为 inactive，不销毁；正式P0启用/六层实测归AI129 | `ShortCycle` |

## 已核程序集与文件定位

按当前 `.asmdef`／最近 `.asmref` 解析；下列只是源码归属，不代表重新编译、运行或验收。

| 程序集 | 定义／归属入口 | 本页相关源码例 |
| --- | --- | --- |
| `ShortCycle` | [Assets/Scripts/Cooking/ShortCycle/ShortCycle.asmdef](<../../Assets/Scripts/Cooking/ShortCycle/ShortCycle.asmdef>) | [ShortCycleFridgeBinder.cs](<../../Assets/Scripts/Cooking/ShortCycle/Content/ShortCycleFridgeBinder.cs>)；[FridgeCatRig.cs](<../../Assets/Scripts/Cooking/ShortCycle/Presentation/FridgeCatRig.cs>)；[FridgeCatTier.cs](<../../Assets/Scripts/Cooking/ShortCycle/Presentation/FridgeCatTier.cs>) |
| `Toolkit` | [Assets/Scripts/Tools/Toolkit.asmdef](<../../Assets/Scripts/Tools/Toolkit.asmdef>) | [IgnoreParentRotation.cs](<../../Assets/Scripts/Tools/IgnoreParentRotation.cs>)；[IgnoreParentScale.cs](<../../Assets/Scripts/Tools/IgnoreParentScale.cs>)；[ArcLayout.cs](<../../Assets/Scripts/Tools/LayoutTools/ArcLayout.cs>) |

## 历史

[本页整理前全文与历史记录](<../../docs/archive/工程事实_20261124/history/.ai-workspace/toolkit/LAY.md>)；[归档与原字节恢复索引](<../../docs/archive/工程事实_20261124/README.md>)；[已知不符与保留边界](<../../docs/archive/工程事实_20261124/差异与保留边界.md>)。
