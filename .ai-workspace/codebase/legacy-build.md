# 模块正文：6. 旧 Build 链与其边界

最后完整核验：**2026-09-17**（不提高）。本次仅整理文档与核对源码归属；任务执行过程、当时的测试与许可记录移至历史，不能据历史状态启动业务。执行规则以 AGENTS.md、WORKFLOW.md 的现行优先条款为准。工具表保留原能力、使用限制与来源语义，旧候选／待验标签不代表本次重新验收。

## 现状

## 6. 旧 Build 链与其边界

用户2026-10-04确定新P2用新scene从头开发，下列仅是遗留参考调用链，不作为本批写回或继承目标。旧链为 CookingPrepare → IngredientInventory/SceneSwitchManager → CookingProcess → CookingManager → Room/Container → Recipe 白名单 + RuleBook → InteractionController → CutDice/Stir。

旧源码的实际归属见下表，历史中旧目录／交接完成度描述不作为当前接线事实。

## 八份旧链分析与保留的 Spine 参考

| 原入口 | 历史阅读 | 处置 |
| --- | --- | --- |
| `ChildrenManagementPatterns_Report.md` | [ChildrenManagementPatterns_Report.md](<../../docs/archive/工程事实_20261124/history/ChildrenManagementPatterns_Report.md>) | 原文归档，根路径保留兼容指针 |
| `CookingPrepare场景脚本与功能整理.md` | [CookingPrepare场景脚本与功能整理.md](<../../docs/archive/工程事实_20261124/history/CookingPrepare场景脚本与功能整理.md>) | 原文归档，根路径保留兼容指针 |
| `CookingProcess场景脚本与功能整理.md` | [CookingProcess场景脚本与功能整理.md](<../../docs/archive/工程事实_20261124/history/CookingProcess场景脚本与功能整理.md>) | 原文归档，根路径保留兼容指针 |
| `CutDice_Refactoring_Analysis.md` | [CutDice_Refactoring_Analysis.md](<../../docs/archive/工程事实_20261124/history/CutDice_Refactoring_Analysis.md>) | 原文归档，根路径保留兼容指针 |
| `CutDice_Reference_Template.md` | [CutDice_Reference_Template.md](<../../docs/archive/工程事实_20261124/history/CutDice_Reference_Template.md>) | 原文归档，根路径保留兼容指针 |
| `Ingredient_Interaction_System_Report.md` | [Ingredient_Interaction_System_Report.md](<../../docs/archive/工程事实_20261124/history/Ingredient_Interaction_System_Report.md>) | 原文归档，根路径保留兼容指针 |
| `Spine Sample场景脚本与功能整理.md` | [Spine Sample场景脚本与功能整理.md](<../../Spine Sample场景脚本与功能整理.md>) | 原位保留；预研未支持归档 |
| `做菜短循环设计文档与代码评估_2026-08-11_191051.md` | [做菜短循环设计文档与代码评估_2026-08-11_191051.md](<../../docs/archive/工程事实_20261124/history/做菜短循环设计文档与代码评估_2026-08-11_191051.md>) | 原文归档，根路径保留兼容指针 |
| `食材交互反馈系统分析报告.md` | [食材交互反馈系统分析报告.md](<../../docs/archive/工程事实_20261124/history/食材交互反馈系统分析报告.md>) | 原文归档，根路径保留兼容指针 |

## 已核程序集与文件定位

按当前 `.asmdef`／最近 `.asmref` 解析；下列只是源码归属，不代表重新编译、运行或验收。

| 程序集 | 定义／归属入口 | 本页相关源码例 |
| --- | --- | --- |
| `Generated.DataTables` | [Assets/Scripts/DataBase/CookingData/Generated.DataTables.asmref](<../../Assets/Scripts/DataBase/CookingData/Generated.DataTables.asmref>) | [RuleBook.cs](<../../Assets/Scripts/DataBase/CookingData/RuleBook.cs>) |
| `Legacy` | [Assets/Scripts/Legacy/Legacy.asmdef](<../../Assets/Scripts/Legacy/Legacy.asmdef>) | [CookingDropZone.cs](<../../Assets/Scripts/Legacy/Cooking/Cook/CookingDropZone.cs>)；[CookingManager.cs](<../../Assets/Scripts/Legacy/Cooking/Cook/CookingManager.cs>)；[Container.cs](<../../Assets/Scripts/Legacy/Cooking/CookingRoom/Container.cs>) |
| `Legacy` | [Assets/Scripts/Controller/Legacy.asmref](<../../Assets/Scripts/Controller/Legacy.asmref>) | [SceneSwitchManager.cs](<../../Assets/Scripts/Controller/SceneSwitchManager.cs>) |
| `Toolkit` | [Assets/Scripts/Tools/Toolkit.asmdef](<../../Assets/Scripts/Tools/Toolkit.asmdef>) | [Tools.cs](<../../Assets/Scripts/Tools/Tools.cs>) |

## 历史

[本页整理前全文与历史记录](<../../docs/archive/工程事实_20261124/history/.ai-workspace/codebase/legacy-build.md>)；[归档与原字节恢复索引](<../../docs/archive/工程事实_20261124/README.md>)；[已知不符与保留边界](<../../docs/archive/工程事实_20261124/差异与保留边界.md>)。
