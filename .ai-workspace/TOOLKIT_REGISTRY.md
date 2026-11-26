# 可复用工具库登记表 · 兼容入口

最后完整核验：**2026-09-17**（不提高）。本次仅整理文档与核对源码归属；任务执行过程、当时的测试与许可记录移至历史，不能据历史状态启动业务。执行规则以 AGENTS.md、WORKFLOW.md 的现行优先条款为准。工具表保留原能力、使用限制与来源语义，旧候选／待验标签不代表本次重新验收。

## 现状

> 完整名称、状态和域正文见 [toolkit/INDEX.md](toolkit/INDEX.md)。本页只保留旧路径的 TK 编号解析映射。

- TK-CMD-01: [CMD.md](toolkit/CMD.md#tk-cmd-01)
- TK-CMD-02: [CMD.md](toolkit/CMD.md#tk-cmd-02)
- TK-SEC-01: [SEC.md](toolkit/SEC.md#tk-sec-01)
- TK-DAT-01: [DAT.md](toolkit/DAT.md#tk-dat-01)
- TK-DAT-02: [DAT.md](toolkit/DAT.md#tk-dat-02)
- TK-DAT-03: [DAT.md](toolkit/DAT.md#tk-dat-03)
- TK-DAT-04: [DAT.md](toolkit/DAT.md#tk-dat-04)
- TK-DAT-05: [DAT.md](toolkit/DAT.md#tk-dat-05)
- TK-DAT-06: [DAT.md](toolkit/DAT.md#tk-dat-06)
- TK-DAT-07: [DAT.md](toolkit/DAT.md#tk-dat-07)
- TK-DAT-09: [PST.md](toolkit/PST.md#tk-dat-09)
- TK-DAT-08: [DAT.md](toolkit/DAT.md#tk-dat-08)
- TK-DAT-10: [DAT.md](toolkit/DAT.md#tk-dat-10)
- TK-DBG-01: [DBG.md](toolkit/DBG.md#tk-dbg-01)
- TK-DBG-02: [DBG.md](toolkit/DBG.md#tk-dbg-02)
- TK-DBG-03: [DBG.md](toolkit/DBG.md#tk-dbg-03)
- TK-DBG-04: [DBG.md](toolkit/DBG.md#tk-dbg-04)
- TK-DBG-05: [DBG.md](toolkit/DBG.md#tk-dbg-05)
- TK-DBG-06: [CMD.md](toolkit/CMD.md#tk-dbg-06)
- TK-DBG-07: [INT.md](toolkit/INT.md#tk-dbg-07)
- TK-DBG-08: [DBG.md](toolkit/DBG.md#tk-dbg-08)
- TK-DBG-09: [DBG.md](toolkit/DBG.md#tk-dbg-09)
- TK-INT-01: [INT.md](toolkit/INT.md#tk-int-01)
- TK-INT-02: [INT.md](toolkit/INT.md#tk-int-02)
- TK-INT-03: [INT.md](toolkit/INT.md#tk-int-03)
- TK-INT-04: [INT.md](toolkit/INT.md#tk-int-04)
- TK-INT-05: [INT.md](toolkit/INT.md#tk-int-05)
- TK-INT-06: [INT.md](toolkit/INT.md#tk-int-06)
- TK-INT-07: [INT.md](toolkit/INT.md#tk-int-07)
- TK-INT-08: [INT.md](toolkit/INT.md#tk-int-08)
- TK-INT-09: [INT.md](toolkit/INT.md#tk-int-09)
- TK-INT-10: [INT.md](toolkit/INT.md#tk-int-10)
- TK-INT-11: [INT.md](toolkit/INT.md#tk-int-11)
- TK-INT-12: [INT.md](toolkit/INT.md#tk-int-12)
- TK-INT-13: [INT.md](toolkit/INT.md#tk-int-13)
- TK-INT-14: [INT.md](toolkit/INT.md#tk-int-14)
- TK-INT-15: [INT.md](toolkit/INT.md#tk-int-15)
- TK-INT-16: [INT.md](toolkit/INT.md#tk-int-16)
- TK-INT-17: [INT.md](toolkit/INT.md#tk-int-17)
- TK-INT-18: [INT.md](toolkit/INT.md#tk-int-18)
- TK-INT-19: [INT.md](toolkit/INT.md#tk-int-19)
- TK-LAY-01: [LAY.md](toolkit/LAY.md#tk-lay-01)
- TK-LAY-02: [LAY.md](toolkit/LAY.md#tk-lay-02)
- TK-LAY-03: [LAY.md](toolkit/LAY.md#tk-lay-03)
- TK-LAY-04: [LAY.md](toolkit/LAY.md#tk-lay-04)
- TK-LAY-05: [LAY.md](toolkit/LAY.md#tk-lay-05)
- TK-LAY-06: [LAY.md](toolkit/LAY.md#tk-lay-06)
- TK-LAY-07: [LAY.md](toolkit/LAY.md#tk-lay-07)
- TK-LAY-08: [LAY.md](toolkit/LAY.md#tk-lay-08)
- TK-LOC-01: [LOC.md](toolkit/LOC.md#tk-loc-01)
- TK-LOC-02: [LOC.md](toolkit/LOC.md#tk-loc-02)
- TK-LOC-03: [LOC.md](toolkit/LOC.md#tk-loc-03)
- TK-MOT-01: [MOT.md](toolkit/MOT.md#tk-mot-01)
- TK-MOT-02: [MOT.md](toolkit/MOT.md#tk-mot-02)
- TK-MOT-03: [MOT.md](toolkit/MOT.md#tk-mot-03)
- TK-MOT-04: [MOT.md](toolkit/MOT.md#tk-mot-04)
- TK-MOT-05: [MOT.md](toolkit/MOT.md#tk-mot-05)
- TK-MOT-06: [MOT.md](toolkit/MOT.md#tk-mot-06)
- TK-MOT-07: [MOT.md](toolkit/MOT.md#tk-mot-07)
- TK-MOT-08: [MOT.md](toolkit/MOT.md#tk-mot-08)
- TK-MOT-09: [MOT.md](toolkit/MOT.md#tk-mot-09)
- TK-MOT-10: [MOT.md](toolkit/MOT.md#tk-mot-10)
- TK-MOT-11: [MOT.md](toolkit/MOT.md#tk-mot-11)
- TK-MOT-12: [MOT.md](toolkit/MOT.md#tk-mot-12)
- TK-MOT-13: [MOT.md](toolkit/MOT.md#tk-mot-13)
- TK-MOT-14: [MOT.md](toolkit/MOT.md#tk-mot-14)
- TK-RND-01: [RND.md](toolkit/RND.md#tk-rnd-01)
- TK-RND-02: [RND.md](toolkit/RND.md#tk-rnd-02)
- TK-RND-03: [RND.md](toolkit/RND.md#tk-rnd-03)
- TK-RND-04: [RND.md](toolkit/RND.md#tk-rnd-04)
- TK-RND-05: [RND.md](toolkit/RND.md#tk-rnd-05)
- TK-RND-06: [RND.md](toolkit/RND.md#tk-rnd-06)
- TK-RND-07: [RND.md](toolkit/RND.md#tk-rnd-07)
- TK-WF-01: [WF.md](toolkit/WF.md#tk-wf-01)
- TK-WF-02: [WF.md](toolkit/WF.md#tk-wf-02)
- TK-WF-03: [WF.md](toolkit/WF.md#tk-wf-03)
- TK-WF-04: [WF.md](toolkit/WF.md#tk-wf-04)
- TK-WF-05: [WF.md](toolkit/WF.md#tk-wf-05)

<a id="font-glyph-decision-current"></a>

### 当前离线字形判定入口

[Get-FontGlyphDecision.ps1](<../DevTools/Fonts/Get-FontGlyphDecision.ps1>) 是 PowerShell 7.5+ 编辑器外只读工具，用当前 legacy 字集、全部 Localization.zh_cn 与 Static 实际字表生成判定，另给剧情八表独立缺字报告。显式调用：`pwsh -NoProfile -File DevTools/Fonts/Get-FontGlyphDecision.ps1 -OutputDirectory <本任务输出>/glyph-decision`。用法／schema见 [字体工具说明](<../DevTools/Fonts/README.md>)。菜单、直接调用和批处理在成功导表后各运行一次判定。编辑器报告在 `Library/DataTableFontReports/`，隔离批处理报告在当轮输出的 `font-glyph-reports/`。默认只报告，字体重建单独授权。详见 [DATA 工具登记](<toolkit/DAT.md>)。

## 已核程序集与文件定位

按当前 `.asmdef`／最近 `.asmref` 解析；下列只是源码归属，不代表重新编译、运行或验收。

| 程序集 | 定义／归属入口 | 本页相关源码例 |
| --- | --- | --- |
| `Editor.ProjectValidation` | [Assets/Editor/ProjectValidation/Editor.ProjectValidation.asmdef](<../Assets/Editor/ProjectValidation/Editor.ProjectValidation.asmdef>) | [ShortCycleProbeCi.cs](<../Assets/Editor/ProjectValidation/ShortCycleProbeCi.cs>) |
| `LongLoop` | [Assets/Scripts/LongLoop/LongLoop.asmdef](<../Assets/Scripts/LongLoop/LongLoop.asmdef>) | [SceneFlow.cs](<../Assets/Scripts/LongLoop/SceneFlow.cs>) |
| `ShortCycle` | [Assets/Scripts/Cooking/ShortCycle/ShortCycle.asmdef](<../Assets/Scripts/Cooking/ShortCycle/ShortCycle.asmdef>) | [StickerNotesPiece.cs](<../Assets/Scripts/Cooking/ShortCycle/Content/StickerNotesPiece.cs>) |
| `Toolkit` | [Assets/Scripts/Tools/Toolkit.asmdef](<../Assets/Scripts/Tools/Toolkit.asmdef>) | [IGameInput.cs](<../Assets/Scripts/Tools/Input/IGameInput.cs>)；[LegacyInputProvider.cs](<../Assets/Scripts/Tools/Input/LegacyInputProvider.cs>)；[PlaceholderRule.cs](<../Assets/Scripts/Tools/PlaceholderRule.cs>) |

## 历史

[本页整理前全文与历史记录](<../docs/archive/工程事实_20261124/history/.ai-workspace/TOOLKIT_REGISTRY.md>)；[归档与原字节恢复索引](<../docs/archive/工程事实_20261124/README.md>)；[已知不符与保留边界](<../docs/archive/工程事实_20261124/差异与保留边界.md>)。
