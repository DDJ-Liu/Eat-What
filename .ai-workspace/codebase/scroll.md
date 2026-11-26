# 模块正文：3. 冰箱与通用滚动

最后完整核验：**2026-09-17**（不提高）。本次仅整理文档与核对源码归属；任务执行过程、当时的测试与许可记录移至历史，不能据历史状态启动业务。执行规则以 AGENTS.md、WORKFLOW.md 的现行优先条款为准。工具表保留原能力、使用限制与来源语义，旧候选／待验标签不代表本次重新验收。

## 现状

## 3. 冰箱与通用滚动

```text
MouseManager → MouseScrollableObject → ShortCycleScrollAdapter.MapPhysicalStep
→ ActionRouter → Session → FridgeBinder → FridgeCatRig → ScrollArea_Controller
```

- 正式 P0 的 ScrollRegion 启用 Invert Physical Scroll Direction；业务 Dispatch(+1)=下一层、(-1)=上一层不变，Lab 保留独立参考方向。
- 滚轮手感配置链为 `CookingGameConfig CSV/schema → CK01GeneratedDataCatalog.ResolveFridgeScrollSettings → ShortCycleSessionManager.Start → MouseManager.ConfigureScrollInput`；解析使用invariant culture，缺失/非法值逐项回退并告警。
- 源 Prefab：Assets/Prefabs/Cooking/ShortCycle/FridgeCat/FridgeCat_Rig.prefab；格件：Assets/Prefabs/Cooking/ShortCycle/GridPieces/FridgeSlot.prefab。
- VisualContent_TUNE 包含整猫视觉、层、双眼与随动装饰。运行时挂到 ContentArea_TUNE；ScrollController/Viewport/TierAlignmentTarget 与场景 ScrollRegion 固定。
- AnchorAlignedAutoBounds 使用真实 Anchor 计算层目标和边界；不可只靠层索引断言实际可见。
- 当前场景后15个数量标签经过 AI145 实例修复；源 FridgeSlot 默认值与已修场景覆盖并不相同，不可盲目 Revert 到源 Prefab。
- 通用组件位于 Assets/Scripts/MouseInteractive/ScrollArea/ScrollArea_Controller.cs、Drag/DragContainer.cs、Drag/DragLimit.cs；可独立复用，冰箱业务适配器是可选桥接。

配置入口、参数与探针操作见[区域滚动说明](../../组件说明文档/区域滚动_配置与复用说明.md)、[整猫说明](../../组件说明文档/视觉特效/冰箱猫整体滚动_配置与使用说明.md)。FridgeScrollLabDriver 的 SIMULATED 只调 Adapter，不伪造硬件事件；真实鼠标验收已由用户通过。

## 已核程序集与文件定位

按当前 `.asmdef`／最近 `.asmref` 解析；下列只是源码归属，不代表重新编译、运行或验收。

| 程序集 | 定义／归属入口 | 本页相关源码例 |
| --- | --- | --- |
| `Generated.DataTables` | [Assets/Scripts/DataBase/CookingData/Generated.DataTables.asmref](<../../Assets/Scripts/DataBase/CookingData/Generated.DataTables.asmref>) | [CK01GeneratedDataCatalog.cs](<../../Assets/Scripts/DataBase/CookingData/CK01GeneratedDataCatalog.cs>) |
| `ShortCycle` | [Assets/Scripts/Cooking/ShortCycle/ShortCycle.asmdef](<../../Assets/Scripts/Cooking/ShortCycle/ShortCycle.asmdef>) | [FridgeScrollLabDriver.cs](<../../Assets/Scripts/Cooking/ShortCycle/Debug/FridgeScrollLabDriver.cs>)；[FridgeSlot.cs](<../../Assets/Scripts/Cooking/ShortCycle/FridgeSlot.cs>)；[ShortCycleScrollAdapter.cs](<../../Assets/Scripts/Cooking/ShortCycle/Interaction/ShortCycleScrollAdapter.cs>) |
| `Toolkit` | [Assets/Scripts/MouseInteractive/Toolkit.asmref](<../../Assets/Scripts/MouseInteractive/Toolkit.asmref>) | [DragContainer.cs](<../../Assets/Scripts/MouseInteractive/Drag/DragContainer.cs>)；[DragLimit.cs](<../../Assets/Scripts/MouseInteractive/Drag/DragLimit.cs>)；[MouseManager.cs](<../../Assets/Scripts/MouseInteractive/MouseManager.cs>) |

## 历史

[本页整理前全文与历史记录](<../../docs/archive/工程事实_20261124/history/.ai-workspace/codebase/scroll.md>)；[归档与原字节恢复索引](<../../docs/archive/工程事实_20261124/README.md>)；[已知不符与保留边界](<../../docs/archive/工程事实_20261124/差异与保留边界.md>)。
