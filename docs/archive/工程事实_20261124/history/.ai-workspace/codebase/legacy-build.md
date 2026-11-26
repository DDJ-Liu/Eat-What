> 来源：`.ai-workspace/codebase/legacy-build.md` 的整理前工作树全文。以下日期、任务与当时结论均为历史，不能替代当前说明；链接只作位置适配，原字节恢复副本见归档索引。

# 模块正文：6. 旧 Build 链与其边界

来源：根文档按模块迁移，原文字段和历史结论保留。最后完整核验仍为2026-09-17；日期段落是对应时点记录，当前状态以根入口/正式队列为准，不构成重试授权。涉及本模块时核实际源码/资源，禁止仅凭旧结论改生产。

## 6. 旧 Build 链与其边界

用户2026-10-04确定新P2用新scene从头开发，下列仅是遗留参考调用链，不作为本批写回或继承目标。旧链为 CookingPrepare → IngredientInventory/SceneSwitchManager → CookingProcess → CookingManager → Room/Container → Recipe 白名单 + RuleBook → InteractionController → CutDice/Stir。

主要目录：
- Assets/Scripts/Cooking/Prepare：PrepareIngredientManager、RecipeBookManager、FridgeManager。
- Assets/Scripts/Cooking/Cook：CookingManager、CookingDropZone。
- Assets/Scripts/Cooking/CookingRoom：CookingRoomManager、Container。
- Assets/Scripts/Cooking/Interaction：InteractionController、具体加工小游戏。
- Assets/Scripts/Cooking/IngredientInventory.cs：旧跨场景托盘库存。
- Assets/Scripts/Command、Dialog、DataBase：命令、对话与旧表格读取。
- Assets/Scripts/GridPlacement、Tools：独立网格、Transition、布局及路径工具。

ShortCycleDataHandoff 已携带 RecipeId 和 PrepZoneContents；当前 Phase2TransitionAdapter 仅发 Requested 事件，不代表已把 P2 生产场景、最终成菜、评分与变体保存全部接通。CookingManager 的 isFinal 分支仍只有下一阶段注释；onChooseRoom 仍只更新房间ID。旧问题不能因 P0/P1 阶段完成自动视为修复，也不能从旧报告直接建立新任务。
