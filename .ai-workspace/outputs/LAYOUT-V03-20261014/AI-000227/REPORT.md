# AI-000227｜封面气泡文案源与忠实导出

结果：已在权威工作簿 Localization 表追加 ui_p0_cover_bubble_a.text=吃什么（第159行）和 ui_p0_cover_bubble_b.text=汪！（第160行），comment 留空。正式导出将两条追加到 DataTables/Localization/Localization.csv，创建 USER-U1-R2 事件；状态保持 EXPORTED，doneAt 和 userDecision 均为空。未标记 DONE、未 Consume、未运行 Unity/字体/生成资产/场景流程。

## 变更与保护

- 源：DevTools/DataTables/Authoring/DataTables_Authoring.xlsx。从实时源 SHA 建本任务独占备份与候选；候选仅改 xl/worksheets/sheet14.xml，加四个非空文本单元格和必要的范围标记。通过既有 Publish-AuthoringWorkbookChange -Mode AuthorizedContent 的源冻结、备份和原子发布门。
- 既有 1,442 个非空单元格的值、类型、公式和表归属未变；最终为 1,446 个，差异仅上述四格。14 表其他结构未变；Localization 仅新增两行和对应范围。
- 原工作簿的 42 个 comments/threadedComments/VML 包部件在最终工作簿中逐字节相同；既有矩形批注未被本次写入重建。格式标准化通过现有 Export-AuthoringToCsv.ps1 执行，修复 style-zero 数为 0。
- 隔离预导出比对 14 张表：13 张 CSV 字节不变，Localization 保留原 157 条，仅末尾新增两条。正式发布的 14 张 CSV 与隔离预导出逐字节相同；新增六个来源单元格（含两格空 comment）均为逐格精确一致。
- 正式导出校验 isValid=true、0 错误、48 警告；未改写非空旧业务值或把既有警告“清红”。允许清单内 git diff 只显示 Localization.csv 两个新增记录。

## 证据与接力

evidence.json、acceptance.json 四项均为 true；源差异见 content-protection.json、final-content-audit.json，CSV 差异见 staged-csv-diff.json，批注与正式发布见 publication-audit.json、authoring-audit.jsonl，逐格来源见 cell-provenance.json。工作簿备份与候选保存在本任务输出目录；轮次事件为 .ai-workspace/runtime/data-rounds/USER-U1-R2.json。

本卡只完成 DATA 源与派生导出。控制台须独立核对 R2，再按用户已批准的两条精确文案办理 DONE/Consume，并派后继 DATA 单资产导入；最终三弹层/气泡视觉验收由 ENGINE 阶段合并提供代表截图，不由本卡代签。

文档影响判断：未改变数据结构、Resources 路径、代码入口或项目推进结论，CODEBASE_MAP.md、项目推进基线与 AGENTS.md 无需修改。工具登记影响：复用 TK-DAT-07 现有工作簿发布、格式与导出入口，未新增或变更登记工具。

AI-000190 同轮状态（2026-10-14 实时队列）：任务 SUCCEEDED，验证状态 PENDING_USER。已完成真实 Unity 6 最小 Play/组件边界核验；CookingProcess 的 Room sceneAreaId 与旧工具 SceneToolIdentity 仍未配置，完整烹饪交互及可用性须在区域/工具身份明确并接线后人工复核。
