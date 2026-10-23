# 模块正文：2. 当前运行入口与事实来源

来源：根文档按模块迁移，原文字段和历史结论保留。最后完整核验仍为2026-09-17；日期段落是对应时点记录，当前状态以根入口/正式队列为准，不构成重试授权。涉及本模块时核实际源码/资源，禁止仅凭旧结论改生产。

## 2. 当前运行入口与事实来源

| 入口 | 定位 |
| --- | --- |
| Assets/Scenes/Cooking/ShortCycle_P0P1.unity | 本阶段开发与人工验收场景 |
| Assets/Scenes/ToolTests/VisualEffectsLab.unity | 描边/阴影/泡泡统一试验入口 |
| Assets/Scenes/ToolTests/FridgeScrollLab.unity | 滚轮独立模块与普通内容样例 |
| CookingPrepare.unity、CookingProcess.unity | E1基线与接棒目录均以ShortCycle为index0、Process为index1；Prepare保留文件但移出Build，两个旧场景仍禁止随布局批次改写 |
| DataTables/、Assets/Generated/DataTables/ | 配表权威源与 Unity 生成结果 |
| 组件说明文档/ | 配置人员操作入口 |
| .ai-workspace/runtime/queue-state.json | 本机任务权威状态，保留全部145项与下一个编号146 |
| .ai-workspace/archive/2026-09-17/ | 完整历史、原路径映射、哈希及结算索引 |

已跟踪项目内容有249个 Assets/Scripts C#、63个 Assets/Editor C#（含4个长期迁移验证文件，不含H临时入口）、Assets/Scenes下15个场景，Prefabs/Resources共71个Prefab；14个schema，生成目录122个asset（含共享资源和catalog）。第三方示例场景不混入正式场景目录统计。
