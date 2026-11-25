> 来源：`.ai-workspace/toolkit/LAY.md` 的整理前工作树全文。以下日期、任务与当时结论均为历史，不能替代当前说明；链接只作位置适配，原字节恢复副本见归档索引。

# 工具库 · LAY

> 从 [INDEX.md](<../../../../../../.ai-workspace/toolkit/INDEX.md>) 定位；状态与公开面沿用原登记。

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

2026-09-07 AI-000064 消费状态增补：TK-LAY-08 的 `FridgeCatTier` 已独立同名 MonoScript 文件，Rig/Tier 唯一 Prefab 位于 `Assets/Prefabs/Cooking/ShortCycle/FridgeCat/` 并接入 ShortCycle_P0P1；第三层池化复用和按层程序位移已测。TK-MOT-08 同目录 Rig/Eye 接线后 15 秒双眼同步 5 次已测，继续消费 TK-MOT-07 与 TK-INT-11；未新增 API。层距/眼位/接缝、实际滚轮和 Ear/Arm/Tail 动画选择交最终人工段，不以机器采样替代视觉验收。

| ID | 名称 / 路径 | 能力 | 公开面 | 状态 | 消费方 | 来源 | 备注 |
|---|---|---|---|---|---|---|---|
| TK-LAY-01 | `Layout` 基类 + `GridLayout` / `HorizontalLayout` / `VerticalLayout` / `ArcLayout` / `ArcLayoutStrategy` + Editor `Tools/LayoutTools/` | 规则阵列点位生成与管理，`ChildSizeMode`；Grid 有约束/对齐/起角/轴向枚举 | 见各类枚举 | 沿用 | 目录 8 卡 / 格架列 / 食材清单 / 步骤行 / 筛选栏 | 指导书 §五 | **托盘 20 锚点与区块 8 锚点手工定点，不用工具**（R-05） |
| TK-LAY-02 | `ShortCycleTrayAnchorLayout` `Cooking/ShortCycle/` | 20 手工锚点按序占用/释放 | 见类 | 沿用 | Tray_Group | C-T6 v2.2 | 通用化候选：AnchorSlotLayout |
| TK-LAY-03 | `ShortCycleIconScaleTarget` + `ShortCycleV22LayoutContracts` | 图标七类画布统一缩放常量 | 常量表 | 沿用 | 六 prefab | C-T6 v2.2 | 512 画布规则（G19） |
| TK-LAY-04 | `GridPlacement`（×3） `GridPlacement/` | 占位网格：cell 状态、多格、旋转 | — | 冻结 | —（P2 / 长循环） | 总档 A-6 | 复用裁决改【沿用·保留】 |
| TK-LAY-05 | `IgnoreParentScale` / `IgnoreParentRotation` | 子对象抵消父级缩放/旋转 | — | 沿用 | 猫伸展下图标稳定 | CK01-A #33/34 | |
| TK-LAY-06 | `TidyLayout` `Assets/Scripts/Tools/TidyLayout.cs` | 可见项稳定压缩到既有点位快照，输出只读 placement 计划 | `TidyLayout.Plan<TItem,TPoint>(items,isVisible,layoutPoints)` | AI323 CODE候选·待CONTROL验收及原生消费 | 一键整理（B1 3.9） | TK-01 / AI-000323 | `layoutPoints` 由既有 TK-LAY-01 布局/锚点提供；不产生点位、不调 ArrangeChildren、不写 Transform 或 _TUNE；容量不足拒绝 |
| TK-LAY-07 | `StackedIcon` `Assets/Scripts/Tools/StackedIcon.cs` | 严格识别 Items.stack_type 的 single/stack/pack/container，按数量和调用方显式容量输出形态/可见图层/数量文字计划 | `StackedIcon.TryParseShape`、`StackedIcon.Plan(stackType,quantity,visibleCapacityByShape,showQuantity)` | AI323 CODE候选·待CONTROL验收及原生消费 | 冰箱格件（F2-a 5.4） | TK-01 / AI-000323 | 不提供未经 D-004/005/009 核实的默认图层数、偏移/缩放或实际画面接线；未知类型/负数/容量缺项拒绝 |
| TK-LAY-08 | `FridgeCatRig` + `FridgeCatTier` `Cooking/ShortCycle/Presentation/` | 固定层 + 池化扩展层的五列 Shelf 锚点排布与按层滚动；可选按真实 Anchor 间距对齐并自动拟合纵向 Content 边界 | `SetTierCount(int)` `GetShelfAnchor(row,col)` `ScrollToTier(int)` `TierCount`；序列化 `tierScrollMapping` / `tierAlignmentTarget_TUNE` / `tierAlignmentColumn_TUNE` | 沿用·扩充 | ShortCycleFridgeBinder；CAT-T2 Prefab 接线 | AI-000063 / AI-000128 | 默认 `UniformTierIndex` 保持旧行为；`AnchorAlignedAutoBounds` 需固定 Target、Vertical/BoxCollider 接线和严格下降 Anchor，按真实世界跨度及非单位缩放求解；0/1层安全，扩展层回收为 inactive，不销毁；正式P0启用/六层实测归AI129 |


2026-10-19 AI269/AI272 D-T1a/b复用TK-LAY-01：目录按GridLayout、食材按HorizontalLayout、步骤按VerticalLayout已有锚点序填内容；StepDetailModalPiece最多10行并清空旧标题/编号，StepDetailRowPiece只填文案和图标。本源码不调用ArrangeChildren、不写_TUNE Transform、不新建排布算法。步骤行/图标容量、8卡和清单实际接线与布局复核归D-T2窗口②；ENGINE-HANDOFF列精确字段。无新TK编号。
