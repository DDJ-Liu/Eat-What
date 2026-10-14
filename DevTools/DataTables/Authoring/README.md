# 配表 Excel / CSV 双入口

AI-000189 表头说明：集中元数据位于 header-descriptions.mjs，由 authoring.mjs 按实时 schema 合并用途/填法、消费点、单位、示例、必填/空值/默认、关联/约束及原有警告为每字段一份中文注释。14 表 119 字段全覆盖；KitchenAreas 的列表分隔符为 |，RecipeSteps.standard_ingredients 为 ; 且可混用 ing_/prd_/既有 stp_。preset_tools 旧值保留但不再决定场景可用厨具。示例来自当前 CSV，只作为填法参考，不回写人工工作簿；未明确的物理单位、条件表达式和持续触发时机标记待业务确认。

只读人工工作簿并创建隔离注释候选的公开入口：Update-HeaderComments.ps1 -InputWorkbook <源或格式候选.xlsx> -CandidateWorkbook <新的隔离路径.xlsx> -ExpectedInputSha256 <源SHA256> [-DataTablesRoot <当前表目录>]。结果返回119字段覆盖、候选 SHA、工具旧线程合并数、内容/结构差异数及独立备注保护审计。AI-000193 后旧 Type/Reference 必须与当前 schema 的完整旧模板逐字匹配，AI189 新注释也必须与本轮生成文本逐字匹配；作者名或前缀单独不构成删除权。未知增补、用户回复和未归属的 tc= 兼容备注保留；无法安全分离时拒绝生成并删除不可信候选，不回写源。已归属旧工具次线程仅在没有回复、兼容备注与其原文精确对应时合并；重复执行不堆叠。DATA AI-000191 必须先复核备注保护审计与 Excel 客户端显示，再按 FormatOrCommentOnly 门、冻结 SHA、备份和审计发布；不能仅凭单元格内容/结构0差异代签备注安全。不得用旧 CSV 重建人工工作簿。

最近专项核对：2026-10-12，修复格式脚本删除旧规则后遗留的空 x14 条件格式扩展及空数据验证容器；生产工作簿仅移除这些空容器，14 表/2087 个已存单元格、类型、样式、批注均保持，内容/结构差异为 0。修复原件及新格式候选均经 Excel 普通模式只读打开通过；用户已确认 Excel 人工检查通过；按其“矩形底修正后标记通过”的决定，119条表头注释已恢复标准矩形并经原生预览确认，AI186/AI191 已正式 VERIFIED。详细签收见 `.ai-workspace/outputs/control/WORKBOOK-REPAIR-20261012/comment-shapes/ACCEPTANCE.md`。此前 2026-10-04 的 schema/并发漂移门/逐格来源核验范围不变；本次未导出 CSV 或进入 Unity。证据：`.ai-workspace/outputs/control/WORKBOOK-REPAIR-20261012/REPORT.md`。

本目录提供 `TK-DAT-07` 配表往返工具。Excel 与直接编辑 CSV 两条路径最终都调用同一份项目校验器；这里不复制 V-01～V-12，也不导入或改写 Unity 资产。Unity `Import All` 仍由后续 ENGINE 任务执行。

## 运行依赖

工具只使用 Codex 桌面随附的 Node.js 与 `@oai/artifact-tool`，不会联网或静默安装包。默认从 `%USERPROFILE%/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/` 定位；若运行时位于别处，可设置：

- `CODEX_ARTIFACT_NODE`：`node.exe` 的绝对路径；
- `CODEX_ARTIFACT_NODE_MODULES`：包含 `@oai/artifact-tool` 的 `node_modules` 绝对路径。

脚本运行期间会在本目录临时建立 `node_modules` 目录联接，结束后删除；若该路径已有非本工具内容，脚本会拒绝覆盖。

## Excel 入口

1. 从当前 CSV 生成或刷新工作簿：

   ```powershell
   pwsh -NoProfile -File DevTools/DataTables/Authoring/Import-CsvToAuthoring.ps1
   ```

2. 编辑 `DataTables_Authoring.xlsx`。每个 schema 对应一个 sheet：首行冻结；橙色表头表示必填；单值引用提供下拉；无效单值/列表引用使用红色条件格式；表头批注现由字段语义元数据与实时 schema 合成，不再仅有类型和警告。`KitchenAreas.containerTags`、`preset_tools` 使用 `|`，其余正式列表字段按 schema 使用 `;`。

`KitchenAreas.preset_tools` 旧列及用户现有值必须保留，但已弃用为“实际可用厨具”的依据；不要通过填它来声明场景厨具。真实可用性由已保存场景的 `SceneToolIdentity` 与所属 `CookingRoomManager` 导出清单证明。`core_facility` 仍是规则要求，不代表场景里自动存在。未提供或过期清单时，静态配表检查可继续报告内容问题，但 `isFullyVerified=false`，不能当作完整场景验收。
3. 导出到隔离目录并自动校验。每轮先从源工作簿创建独立格式候选，导出使用该候选，不修改原件：

   ```powershell
   pwsh -NoProfile -File DevTools/DataTables/Authoring/Export-AuthoringToCsv.ps1 `
     -InputWorkbook DevTools/DataTables/Authoring/DataTables_Authoring.xlsx `
     -OutputRoot C:/Temp/EatWhat-DataTables
   ```

确认差异后，才由内容流程决定是否以该输出更新权威 CSV。格式候选留在报告目录（`format-candidate-*.xlsx`），回执同时给出原源与候选 SHA；逐格来源仍指向原工作簿地址和 SHA，不把候选误称用户源。文件记录为 UTF-8 无 BOM、记录分隔符为 CRLF；单元格内部 LF/CRLF 保持工作簿原表示。每次导出在报告目录写 `cell-provenance.json`，逐格登记 sheet/cell/主键/字段/目标 CSV 行列和精确值。只有表头、没有数据的空 sheet 会被跳过，目标目录中同名 CSV 不会被覆盖。

格式候选也可单独生成，供 DATA 在生产发布前检查：

```powershell
pwsh -NoProfile -File DevTools/DataTables/Authoring/Format-AuthoringWorkbook.ps1 `
  -InputWorkbook <人工xlsx> -CandidateWorkbook <全新隔离路径.xlsx> `
  -ExpectedInputSha256 <源文件冻结SHA>
```

它只编辑 14 个 schema sheet 的已知格式区域：修复默认 style0、缺失的首行冻结/列宽，按 schema 重建引用列的数据验证和红色条件格式；列表按分隔符逐项核对，不沿用旧规则的“等于原值则跳过”。其它非零样式和隐藏/额外 sheet 保留。发布前必须由 `FormatOrCommentOnly` 门再次证明所有 sheet 的已存内容、类型、公式、空白结构不变。此处 OOXML/渲染检查不能替代用户 Excel 客户端对条件格式的最终视觉验收。

## 直接 CSV 入口

直接编辑 `DataTables/` 后运行：

```powershell
pwsh -NoProfile -File DevTools/DataTables/Authoring/Invoke-DataTableAuthoring.ps1 -Mode Csv
```

该入口先调用共享校验器的 `-Quick` 模式，再从通过校验的 CSV 刷新工作簿。`-Quick` 不省略规则，只输出错误/警告摘要与 slot 引用、孤儿本地化、步骤链、白名单覆盖四类核心报告路径。若仅想校验：

```powershell
pwsh -NoProfile -File DevTools/DataTables/Validate-DataTables.ps1 -Quick
```

需要带 BOM 的 CSV 仅可另存到工程外用于本地查看，不能回写权威源。

## 一键 Excel 导出

```powershell
pwsh -NoProfile -File DevTools/DataTables/Authoring/Invoke-DataTableAuthoring.ps1 `
  -Mode Excel -OutputRoot C:/Temp/EatWhat-DataTables
```

Excel 路径与 CSV 路径均使用 `Validate-DataTables.ps1`；通过只是编辑器外数据门，不等同于 Unity 导表、编译或 Play Mode 通过。

## 非空内容保护与公开停止线

`Authoring.Common.ps1` 是 New/Import/Export/Invoke 共用的保护核心，不应另写旁路：

- `Get-AuthoringWorkbookContentSnapshot` / `Write-AuthoringWorkbookContentSnapshot`：按 sheet、地址、行列、OOXML 类型、精确文本、公式及公式属性记录非空内容；`Compare-AuthoringWorkbookContent` 给出逐格差异。
- `Publish-AuthoringWorkbookChange`：`FormatOrCommentOnly` 与 `BlankStructureOnly` 都要求非空内容零差异；`FillPreviouslyBlankCells` 只允许填写基线中已经存在的空单元格，且禁止同时改变空白结构、既有非空内容/类型/公式或 sheet 关联。它不等于 `AuthorizedContent`，也不得用于格式批次绕过保护。
- `Restore-AuthoringWorkbookBackup`：冻结并复核当前文件与备份 SHA 后回退；任一并发漂移都拒绝覆盖。
- `New-AuthoringDerivedPublishPlan`：在耗时导出/校验之前冻结目标存在性、目标 SHA 和备份不存在状态。生产 Exporter 会先从 14 份 schema 建立该计划，再生成候选。
- `Publish-AuthoringDerivedFileSet`：消费冻结计划，把已验证候选带备份原子发布。文件在物理写入前就登记为“可能写入”，验证与写入分离；失败时按显式索引反序回退。只有当前目标仍等于本任务候选、备份仍等于冻结基线才可恢复/删除；未知并发修改会原样保留，并同时返回初始错误与回退错误。
- 共同的 `Invoke-AuthoringGuardedWrite` 用目标命名互斥、冻结源/候选/暂存字节、独占创建备份，防止候选冻结后至暂存期间的常规并发编辑/备份抢占。替换后还核对被替换的原件 SHA；非协作进程在最后释放文件句柄到物理替换间用重命名争抢的极窄窗口，若发生会拒绝返回成功并保留置换证据，不能把新外部字节追认为基线。回退也只触碰可证明属于本任务的字节；未知现场交人工处理。

已有工作簿的 CSV 回灌默认拒绝。若仅做格式/注释修复，调用 New/Import 时必须提供 `-TaskOrigin`、`-AuditPath`、可选 `-ExpectedWorkbookSha256`；候选若改动任何非空值、类型、公式、ID或位置仍会停止。经用户明确授权修改内容时，才额外传 `-AllowContentReplacement`，并保留逐格审计。新建不存在的工作簿仍可直接初始化。

向权威 `DataTables/` 发布派生 CSV 默认拒绝；DATA 任务须显式提供 `-AllowProductionCsvWrite -TaskOrigin <任务来源> -AuditPath <审计JSONL> -ExpectedWorkbookSha256 <冻结SHA>`，并建议显式给出本任务独占的 `-BackupDirectory` 与 `-ProvenancePath`。工具在导出前冻结 14 个 schema 目标基线，随后隔离导出、校验、复核同一工作簿和候选字节，再消费计划发布。格式任务、检查任务和 CODE 回归不得传生产写开关。

### U1-R1 后续任务调用边界

- AI-000186 格式规范化与 AI-000189 表头说明均只交付隔离候选，不在 CODE 轮次发布人工工作簿。AI-000191 DATA 发布时才将两者串接，并通过 `Publish-AuthoringWorkbookChange -Mode FormatOrCommentOnly -SourceWorkbook <人工xlsx> -CandidateWorkbook <格式和注释的隔离合并候选> -ExpectedSourceSha256 <发布前冻结SHA> -BackupPath <任务独占备份> -TaskOrigin <任务ID> -AuditPath <任务审计JSONL>`。不得使用 `FillPreviouslyBlankCells` 或 `AuthorizedContent`。
- AI-000191 只有在数据校验允许发布后，调用 `Export-AuthoringToCsv.ps1 -InputWorkbook <人工xlsx> -OutputRoot <仓库DataTables> -BaselineRoot <仓库DataTables> -AllowProductionCsvWrite -TaskOrigin AI-000191 -AuditPath <任务审计JSONL> -WorkbookBackupPath <独占xlsx备份> -BackupDirectory <任务独占CSV备份目录> -ExpectedWorkbookSha256 <发布轮次冻结SHA> -ProvenancePath <逐格来源JSON>`。先冻结 CSV 目标计划，再在隔离候选规范格式、导出和校验；生产授权时格式候选经 `FormatOrCommentOnly` 门发布到原 xlsx，然后派生 CSV 通过冻结计划发布。两次发布有各自审计/备份，不宣称跨 xlsx+CSV 的单一原子事务；若后一步失败，先保全 xlsx 备份与审计、核对当前归属，再决定受控回退，不以旧 CSV 重建用户工作簿。不得先导出后自行重建目标基线，也不得直接调用发布器省略冻结计划。
- 直接 CSV 模式没有工作簿作为来源，回执显式写 `sourceProvenance=DIRECT_CSV_NO_SOURCE_WORKBOOK_FORMAT`；这不代表人工 xlsx 已格式化，DATA 应按轮次事实处理。
- `-TestFaultInjector/-TestFixtureRoot` 仅用于确定性隔离回归；启用时所有候选、目标、备份和审计路径必须位于显式测试根内，生产包装器不会传入这些参数。

## G53 新增字形清单

每次工作簿导出都会在输出根目录生成 `新增字形清单_yyyyMMdd.txt`。报告明确列出基线 `Localization.csv`、比较输入、发生变化的 `loc_key`、相对基线新增的去重 Unicode 字符及其来源 key。报告只供 MIG-F09/字体保护复核；工具不会修改任何字体、材质或其它 `Assets/**` 文件。

## 回归

```powershell
pwsh -NoProfile -File DevTools/DataTables/Test-Authoring.ps1
```

回归在系统临时目录建立状态副本，覆盖 14 sheet、冻结/着色/引用辅助、逐格来源、LF 忠实派生、重复 CSV 字节稳定、空 sheet 不覆盖、G53 新字形与共享校验器；并直接调用生产保护核心证明非空删除/改字/公式/类型/移动被拒、格式-only/空结构/窄范围填空契约正确，以及两文件部分发布、写后验证失败、候选/目标/备份/暂存漂移、末尾审计失败和重复回退均按磁盘字节与错误证据收口。人工工作簿副本另验 26 个 style0、ProcessingRecords 误红公式、旧 73 条列表规则清理、隐藏/额外 sheet、公式和值/类型/空白结构及派生 CSV 字节不变；真实工作簿只读，权威 CSV 写入为零。
