> 来源：`.ai-workspace/TOOLKIT_REGISTRY.md` 的整理前工作树全文。以下日期、任务与当时结论均为历史，不能替代当前说明；链接只作位置适配，原字节恢复副本见归档索引。

2026-11-24 AI627 / FONT-ATLAS-AUTO-a：现有Fonts/DATA工具族追加 `DevTools/Fonts/Get-FontGlyphDecision.ps1` 与五用例 `Test-FontGlyphDecision.ps1`。复用实际DataTableCsvParser，只读legacy基线+全部Localization.zh_cn与Static实际字符表，输出schema1判定及独立八表剧情缺字报告；ERROR/null非零，标签名按本卡剥除（含本机TMP有效色码简写/数值scale，未知/字面/noparse保留）、布局控制另列，旧Builder原文收字逻辑未改。不改表/清单/资产，不进Unity或自动重建/发布。参数/格式与后继授权边界见DevTools/Fonts/README.md；导表后显式步骤见DATA_PIPELINE.md。沿TK-DAT-01/07，不新增编号或队列入口。

2026-11-12 AI480局部交付：TK-WF-03既有Invoke-TaskTool新增StandardPreflightPath，实际复用Preflight-Standard.ps1五组及场景/指定SHA/Console守卫；旧对象dirty/逐字/时间戳字面/listener/Begin仅warning。正式停止、错误目标、未知未保存成果和平台拒绝不降级。没有新TK/队列/身份框架；Case StandardPreflight与CLI黄金/Grant/QueueWriter/FailureUsage定向回归，原生待独立AI481。具体源映射、缺口及schema见outputs/INFRA-011-20261109/AI-000480/attempt1/ENGINE-HANDOFF.md、ROUTE-MAP.md和toolkit/WF.md；不宣全仓旧历史比较器已移除。

2026-11-12 AI478局部交付：TK-WF-03既有Invoke-TaskTool新增显式ProjectGrantPath/WriteIntentPath项目层校验；正式pin/原文完整性/当前执行/scope及原479动作名兼容，非平台批准，不防未声明写集合。原共享传输计量保持，独立S40前不作其他新写入口；Case Grant及CLI黄金/QueueWriter/FailureUsage离线通过。schema/具体SDK/API载体见outputs/INFRA-011-20261109/AI-000478/attempt1/ENGINE-HANDOFF.md与toolkit/WF.md，不新TK/框架。

# 可复用工具库登记表 · 兼容入口

2026-10-28 AI380 局部扩展 [TK-INT-19](<../../../../../.ai-workspace/toolkit/INT.md#tk-int-19>)：同一输入文件新增可选 `INavigationInput.BackPressed`，`LegacyInputProvider` 将 Escape 按下帧映射到该接口；原 `IGameInput` 三属性未改。`SceneFlow` 只消费接口，真实 Boot 接线和 Play 待 ENGINE。

2026-10-24 AI349 续修 PROBE-CI-01 Editor 会话归属：既有 ShortCycleProbeCi 现将结果路径归属与 Launcher 清理归属分开，凭本次 Arm 尝试及 Launcher 的 task/run/root/活动证据才 Stop，身份未知或改变时保全。既有 Test-EngineCli ShortCycleCi 定向检查35项通过，含真实 CI 源码的隔离可执行生命周期测试；Unity6真引用离线编译通过。仅 CODE 候选，原生验收和 AI339 HARD_STOP 不变。详见 [WF](<../../../../../.ai-workspace/toolkit/WF.md#tk-wf-03>) 与 [报告](<../../../../../.ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/AI-000349/REPORT.md>)。

2026-10-24 AI348 修复 ShortCycleNativeCi 结果归属与任务身份：Verify-All 先绑定 TaskId/上下文再计 Final；宿主仅认领精确新 run 根，既有完整结果只读，不覆盖失败、损坏或不匹配证据；Editor 结果仍 CreateNew 且须先验证路径。既有 ShortCycleCi 定向夹具扩至 24 项，真实 Unity6 引用离线编译通过。仅 CODE 候选，后续 CONTROL/ENGINE 验收与 AI339 HARD_STOP 不变。详见 [WF](<../../../../../.ai-workspace/toolkit/WF.md#tk-wf-03>) 与 [报告](<../../../../../.ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/AI-000348/REPORT.md>)。

2026-10-24 AI347 复用 TK-WF-03/04：新增显式 ShortCycleNativeCi profile、正式 batchmode 宿主/Editor 聚合入口和既有 Test-EngineCli 的 ShortCycleCi 隔离 Case；默认离线、无新 TK/runner/队列。仅 CODE 离线候选，原生两次连续及负例交固定 ENGINE 验收，339 HARD_STOP 不变。详见 [WF](<../../../../../.ai-workspace/toolkit/WF.md#tk-wf-03>) 与 [报告](<../../../../../.ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/AI-000347/REPORT.md>)。

> 完整名称、状态和域正文见 [toolkit/INDEX.md](<../../../../../.ai-workspace/toolkit/INDEX.md>)。本页只保留旧路径的 TK 编号解析映射。

2026-10-24 AI323：既有编号 TK-DAT-05/06、TK-LAY-06/07 已从“拟”转为 CODE 离线实现；四个公开入口位于 Assets/Scripts/Tools/{TagFilter,TidyLayout,StackedIcon,PlaceholderRule}.cs，详细使用限制见 DAT/LAY。StickerNotesPiece 仅消费 PlaceholderRule 的分类，原生格架/布局接线仍待后续窗口。无新 TK 编号。

- TK-CMD-01: [CMD.md](<../../../../../.ai-workspace/toolkit/CMD.md#tk-cmd-01>)
- TK-CMD-02: [CMD.md](<../../../../../.ai-workspace/toolkit/CMD.md#tk-cmd-02>)
- TK-SEC-01: [SEC.md](<../../../../../.ai-workspace/toolkit/SEC.md#tk-sec-01>)
- TK-DAT-01: [DAT.md](<../../../../../.ai-workspace/toolkit/DAT.md#tk-dat-01>)
- TK-DAT-02: [DAT.md](<../../../../../.ai-workspace/toolkit/DAT.md#tk-dat-02>)
- TK-DAT-03: [DAT.md](<../../../../../.ai-workspace/toolkit/DAT.md#tk-dat-03>)
- TK-DAT-04: [DAT.md](<../../../../../.ai-workspace/toolkit/DAT.md#tk-dat-04>)
- TK-DAT-05: [DAT.md](<../../../../../.ai-workspace/toolkit/DAT.md#tk-dat-05>)
- TK-DAT-06: [DAT.md](<../../../../../.ai-workspace/toolkit/DAT.md#tk-dat-06>)
- TK-DAT-07: [DAT.md](<../../../../../.ai-workspace/toolkit/DAT.md#tk-dat-07>)
- TK-DAT-09: [PST.md](<../../../../../.ai-workspace/toolkit/PST.md#tk-dat-09>)
- TK-DAT-08: [DAT.md](<../../../../../.ai-workspace/toolkit/DAT.md#tk-dat-08>)
- TK-DAT-10: [DAT.md](<../../../../../.ai-workspace/toolkit/DAT.md#tk-dat-10>)
- TK-DBG-01: [DBG.md](<../../../../../.ai-workspace/toolkit/DBG.md#tk-dbg-01>)
- TK-DBG-02: [DBG.md](<../../../../../.ai-workspace/toolkit/DBG.md#tk-dbg-02>)
- TK-DBG-03: [DBG.md](<../../../../../.ai-workspace/toolkit/DBG.md#tk-dbg-03>)
- TK-DBG-04: [DBG.md](<../../../../../.ai-workspace/toolkit/DBG.md#tk-dbg-04>)
- TK-DBG-05: [DBG.md](<../../../../../.ai-workspace/toolkit/DBG.md#tk-dbg-05>)
- TK-DBG-06: [CMD.md](<../../../../../.ai-workspace/toolkit/CMD.md#tk-dbg-06>)
- TK-DBG-07: [INT.md](<../../../../../.ai-workspace/toolkit/INT.md#tk-dbg-07>)
- TK-DBG-08: [DBG.md](<../../../../../.ai-workspace/toolkit/DBG.md#tk-dbg-08>)
- TK-DBG-09: [DBG.md](<../../../../../.ai-workspace/toolkit/DBG.md#tk-dbg-09>)
- TK-INT-01: [INT.md](<../../../../../.ai-workspace/toolkit/INT.md#tk-int-01>)
- TK-INT-02: [INT.md](<../../../../../.ai-workspace/toolkit/INT.md#tk-int-02>)
- TK-INT-03: [INT.md](<../../../../../.ai-workspace/toolkit/INT.md#tk-int-03>)
- TK-INT-04: [INT.md](<../../../../../.ai-workspace/toolkit/INT.md#tk-int-04>)
- TK-INT-05: [INT.md](<../../../../../.ai-workspace/toolkit/INT.md#tk-int-05>)
- TK-INT-06: [INT.md](<../../../../../.ai-workspace/toolkit/INT.md#tk-int-06>)
- TK-INT-07: [INT.md](<../../../../../.ai-workspace/toolkit/INT.md#tk-int-07>)
- TK-INT-08: [INT.md](<../../../../../.ai-workspace/toolkit/INT.md#tk-int-08>)
- TK-INT-09: [INT.md](<../../../../../.ai-workspace/toolkit/INT.md#tk-int-09>)
- TK-INT-10: [INT.md](<../../../../../.ai-workspace/toolkit/INT.md#tk-int-10>)
- TK-INT-11: [INT.md](<../../../../../.ai-workspace/toolkit/INT.md#tk-int-11>)
- TK-INT-12: [INT.md](<../../../../../.ai-workspace/toolkit/INT.md#tk-int-12>)
- TK-INT-13: [INT.md](<../../../../../.ai-workspace/toolkit/INT.md#tk-int-13>)
- TK-INT-14: [INT.md](<../../../../../.ai-workspace/toolkit/INT.md#tk-int-14>)
- TK-INT-15: [INT.md](<../../../../../.ai-workspace/toolkit/INT.md#tk-int-15>)
- TK-INT-16: [INT.md](<../../../../../.ai-workspace/toolkit/INT.md#tk-int-16>)
- TK-INT-17: [INT.md](<../../../../../.ai-workspace/toolkit/INT.md#tk-int-17>)
- TK-INT-18: [INT.md](<../../../../../.ai-workspace/toolkit/INT.md#tk-int-18>)
- TK-INT-19: [INT.md](<../../../../../.ai-workspace/toolkit/INT.md#tk-int-19>)
- TK-LAY-01: [LAY.md](<../../../../../.ai-workspace/toolkit/LAY.md#tk-lay-01>)
- TK-LAY-02: [LAY.md](<../../../../../.ai-workspace/toolkit/LAY.md#tk-lay-02>)
- TK-LAY-03: [LAY.md](<../../../../../.ai-workspace/toolkit/LAY.md#tk-lay-03>)
- TK-LAY-04: [LAY.md](<../../../../../.ai-workspace/toolkit/LAY.md#tk-lay-04>)
- TK-LAY-05: [LAY.md](<../../../../../.ai-workspace/toolkit/LAY.md#tk-lay-05>)
- TK-LAY-06: [LAY.md](<../../../../../.ai-workspace/toolkit/LAY.md#tk-lay-06>)
- TK-LAY-07: [LAY.md](<../../../../../.ai-workspace/toolkit/LAY.md#tk-lay-07>)
- TK-LAY-08: [LAY.md](<../../../../../.ai-workspace/toolkit/LAY.md#tk-lay-08>)
- TK-LOC-01: [LOC.md](<../../../../../.ai-workspace/toolkit/LOC.md#tk-loc-01>)
- TK-LOC-02: [LOC.md](<../../../../../.ai-workspace/toolkit/LOC.md#tk-loc-02>)
- TK-LOC-03: [LOC.md](<../../../../../.ai-workspace/toolkit/LOC.md#tk-loc-03>)
- TK-MOT-01: [MOT.md](<../../../../../.ai-workspace/toolkit/MOT.md#tk-mot-01>)
- TK-MOT-02: [MOT.md](<../../../../../.ai-workspace/toolkit/MOT.md#tk-mot-02>)
- TK-MOT-03: [MOT.md](<../../../../../.ai-workspace/toolkit/MOT.md#tk-mot-03>)
- TK-MOT-04: [MOT.md](<../../../../../.ai-workspace/toolkit/MOT.md#tk-mot-04>)
- TK-MOT-05: [MOT.md](<../../../../../.ai-workspace/toolkit/MOT.md#tk-mot-05>)
- TK-MOT-06: [MOT.md](<../../../../../.ai-workspace/toolkit/MOT.md#tk-mot-06>)
- TK-MOT-07: [MOT.md](<../../../../../.ai-workspace/toolkit/MOT.md#tk-mot-07>)
- TK-MOT-08: [MOT.md](<../../../../../.ai-workspace/toolkit/MOT.md#tk-mot-08>)
- TK-MOT-09: [MOT.md](<../../../../../.ai-workspace/toolkit/MOT.md#tk-mot-09>)
- TK-MOT-10: [MOT.md](<../../../../../.ai-workspace/toolkit/MOT.md#tk-mot-10>)
- TK-MOT-11: [MOT.md](<../../../../../.ai-workspace/toolkit/MOT.md#tk-mot-11>)
- TK-MOT-12: [MOT.md](<../../../../../.ai-workspace/toolkit/MOT.md#tk-mot-12>)
- TK-MOT-13: [MOT.md](<../../../../../.ai-workspace/toolkit/MOT.md#tk-mot-13>)
- TK-MOT-14: [MOT.md](<../../../../../.ai-workspace/toolkit/MOT.md#tk-mot-14>)
- TK-RND-01: [RND.md](<../../../../../.ai-workspace/toolkit/RND.md#tk-rnd-01>)
- TK-RND-02: [RND.md](<../../../../../.ai-workspace/toolkit/RND.md#tk-rnd-02>)
- TK-RND-03: [RND.md](<../../../../../.ai-workspace/toolkit/RND.md#tk-rnd-03>)
- TK-RND-04: [RND.md](<../../../../../.ai-workspace/toolkit/RND.md#tk-rnd-04>)
- TK-RND-05: [RND.md](<../../../../../.ai-workspace/toolkit/RND.md#tk-rnd-05>)
- TK-RND-06: [RND.md](<../../../../../.ai-workspace/toolkit/RND.md#tk-rnd-06>)
- TK-RND-07: [RND.md](<../../../../../.ai-workspace/toolkit/RND.md#tk-rnd-07>)
- TK-WF-01: [WF.md](<../../../../../.ai-workspace/toolkit/WF.md#tk-wf-01>)
- TK-WF-02: [WF.md](<../../../../../.ai-workspace/toolkit/WF.md#tk-wf-02>)
- TK-WF-03: [WF.md](<../../../../../.ai-workspace/toolkit/WF.md#tk-wf-03>)
- TK-WF-04: [WF.md](<../../../../../.ai-workspace/toolkit/WF.md#tk-wf-04>)
- TK-WF-05: [WF.md](<../../../../../.ai-workspace/toolkit/WF.md#tk-wf-05>)

2026-10-19 AI269/AI272 D-T1a/b复用TK-LAY-01：目录按GridLayout、食材按HorizontalLayout、步骤按VerticalLayout已有锚点序填内容；StepDetailModalPiece最多10行并清空旧标题/编号，StepDetailRowPiece只填文案和图标。本源码不调用ArrangeChildren、不写_TUNE Transform、不新建排布算法。步骤行/图标容量、8卡和清单实际接线与布局复核归D-T2窗口②；ENGINE-HANDOFF列精确字段。无新TK编号。

2026-10-19 AI273 P2L-01复用TK-DAT-01与TK-DBG-04：Catalog只读快照及既有ShortCycle runner扩充；纯ProcessingMatcher/StepProgress留ShortCycle/Core/Phase2，不登记新工具或拟议TK-DAT-08提升。S-02、18探针保留，实际配置待办交DATA/USER-U5，P2S-HANDOFF明确未做场景/Play。
