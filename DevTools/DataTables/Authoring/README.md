# 配表 Excel / CSV 双入口

2026-11-16 AI562/V013-12 已在主 `DataTables_Authoring.xlsx` 的既有八表页追加 `test_` 占位，并按本出口格式化、隔离导出与逐格来源审计后发布 8 份实际变化的 CSV（7 份 Narrative/Street 加 Localization）；CGs 保持表头且未重写，13 份 Cooking CSV 未改。原 1551 个非空单元格及批注受保护，配置差异为 140 个授权新增单元格，格式阶段内容/结构差异均为 0。正式 22 表 0 error/46 warning；新增两条 V-25 均为场景接线前未核。当前数据行、共用 ID、字体前置和未来清理方法见[剧情八表配表说明](../../../docs/剧情八表配表说明.md)。后续修改以主 xlsx 为权威源，保留非空旧值，整体格式化、记录逐格来源并校验后再发布派生 CSV；不要用旧 CSV 回建源表。Unity 导入/生成绑定仍由 V013-14 执行。

表头说明：旧14表119列的集中元数据位于header-descriptions.mjs；新增八表105列的用途/填法直接读取schema.description。authoring.mjs按实时schema合并消费点、单位、示例、必填/空值/条件、关联/约束为每字段一份中文注释，现代码范围22表224列。KitchenAreas列表分隔符为|，RecipeSteps.standard_ingredients为;且可混用ing_/prd_/既有stp_。preset_tools旧值保留但不再决定场景可用厨具。示例来自CSV，作为填法参考，不回写人工值。

2026-11-15 N004 DATA 已建立八份仅表头CSV，并在原主工作簿追加八个仅表头页。首次注释更新因既有 `Items!M1` 用户注释停下；独立工具修复后，DATA 仅对新增八页初始化105条表头注释，并将注释和22页格式候选经 FormatOrCommentOnly 事务发布。旧14页的值、类型、公式和结构核对无差异，旧注释未更新。正式22表离线校验通过（0错误、44警告、场景清单仍未核）；ImportContracts、Schemas、Authoring、HeaderComments四个原完整回归入口均已各运行一次并通过。DATA机器合同待CONTROL独立审查；新增八页的Excel客户端显示和场景相关检查仍未人工/原生签收。以下2026-10-12的14表人工签收不代表新增八页的Excel客户端显示已验收。

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

确认差异后，才由内容流程决定是否以该输出更新权威 CSV。格式候选留在报告目录（`format-candidate-*.xlsx`），回执同时给出原源与候选 SHA；逐格来源仍指向原工作簿地址和 SHA，不把候选误称用户源。文件记录为 UTF-8 无 BOM、记录分隔符为 CRLF；单元格内部 LF/CRLF 保持工作簿原表示。每次导出在报告目录写 `cell-provenance.json`，逐格登记 sheet/cell/主键/字段/目标 CSV 行列和精确值。空sheet遇到已有目标CSV时继续跳过，配置数据和header-only文件的原字节均保留。目标缺失时，只有对应baseline不存在或为合法相同表头且无配置数据，才创建零行表头CSV；baseline有配置行时仍跳过，不推断删行许可，也不回填旧数据。读取/UTF-8编码/表头错误直接失败。新表头经源SHA二次检查后按pendingWrites用独占创建写入，若目标期间出现则拒绝覆盖。回执written及headerOnlyCsv标明新物化文件，headerOnlyCsv记录sheet/headers/零数据行来源，不伪造业务cell provenance。

格式候选也可单独生成，供 DATA 在生产发布前检查：

```powershell
pwsh -NoProfile -File DevTools/DataTables/Authoring/Format-AuthoringWorkbook.ps1 `
  -InputWorkbook <人工xlsx> -CandidateWorkbook <全新隔离路径.xlsx> `
  -ExpectedInputSha256 <源文件冻结SHA>
```

它只编辑22个schema sheet的已知格式区域：修复默认style0、缺失的首行冻结/列宽，按schema重建引用列的数据验证和红色条件格式；列表按分隔符逐项核对。超过Z列的列名换算已使用整数，支持Dialogues40列。既有批准body/header样式是前置契约，缺失仍拒绝；其它非零样式和隐藏/额外sheet保留。发布前由FormatOrCommentOnly证明所有sheet的已存内容、类型、公式、空白结构不变。OOXML检查不替代Excel客户端视觉验收；条件引用/父段等级等语义以共享校验器为准。

## 在原工作簿追加新表页

N004由DATA先按schema创建八个仅表头的正式CSV，再在已有主xlsx上调用下列参数。此操作从原xlsx复制候选，直接追加指定缺失页，不重建旧页；所有旧页、额外页的内容/类型/公式/顺序/隐藏状态保持。缺失页只有第一行表头；已存在页须有完全相符表头，保留其已有数据；重复执行不写文件。

```powershell
$newTables = @('Characters','Dialogues','DialogueLines','CGs','Scenes','SceneStates','SceneStatePlacements','AmbientPools')
& ./DevTools/DataTables/Authoring/New-AuthoringWorkbook.ps1 `
  -OutputWorkbook <原主xlsx> -AddSchemaTables $newTables `
  -TaskOrigin <DATA任务ID> -AuditPath <审计JSONL> `
  -ExpectedWorkbookSha256 <冻结源SHA> -BackupPath <本轮独占备份.xlsx>
```

New入口将新增页表头清单传给既有BlankStructureOnly发布事务，唯一新增内容必须是这份清单中的页和对应首行表头。任何旧非空内容或旧空白结构变化都会拒绝；不与AllowContentReplacement共用。TaskOrigin/Audit/SHA、备份与并发漂移保护保持。

新页尚无工具注释时，更新与初始化两个名单都显式指定这八表，生成隔离注释候选；原14页不进入更新循环，所有旧注释/回复/传统伴随及人工内容原样保留，完整22表224列plan、表头与全局注释审计仍执行：

```powershell
& ./DevTools/DataTables/Authoring/Update-HeaderComments.ps1 `
  -InputWorkbook <追加后主xlsx> -CandidateWorkbook <新注释候选.xlsx> `
  -ExpectedInputSha256 <追加后冻结SHA> `
  -UpdateSchemaTables $newTables -InitializeSchemaTables $newTables
```

UpdateSchemaTables省略时保持原全表更新行为；显式提供时必须为非空、精确大小写、合法且无重复的表名。InitializeSchemaTables仍默认不初始化，其表名必须属于实际更新名单；它只为指定且没有任何已有注释的表头地址补工具注释/必要关系与作者元数据。选中表的未知或人工编辑注释仍拒绝替换，不凭AI189标记或作者名重认所有权。只传初始化名单仍会检查全部表，不能用它绕开旧Items!M1归属门。回执tableCount/fieldCount仍是完整22/224，updateTableCount/updateTables另列实际选择。后续范围相同的重复执行不增加注释。再用Format入口生成新候选，复核所有值/类型/公式与注释保护后，由已授权DATA按现有FormatOrCommentOnly事务发布。新增表配置规则见[项目八表说明](../../../docs/剧情八表配表说明.md)。

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
- `Publish-AuthoringWorkbookChange`：FormatOrCommentOnly要求非空内容零差异；BlankStructureOnly默认同样要求，仅AddSchemaTables显式清单中的全新表头页可追加，旧页所有内容和结构仍零差异。FillPreviouslyBlankCells只允许填写基线中已经存在的空单元格，且禁止同时改变空白结构、既有非空内容/类型/公式或sheet关联；不能用于格式批次绕过保护。
- `Restore-AuthoringWorkbookBackup`：冻结并复核当前文件与备份 SHA 后回退；任一并发漂移都拒绝覆盖。
- `New-AuthoringDerivedPublishPlan`：在耗时导出/校验之前冻结目标存在性、目标 SHA 和备份不存在状态。生产 Exporter 会先从22份schema建立该计划，再生成候选。
- `Publish-AuthoringDerivedFileSet`：消费冻结计划，把已验证候选带备份原子发布。文件在物理写入前就登记为“可能写入”，验证与写入分离；失败时按显式索引反序回退。只有当前目标仍等于本任务候选、备份仍等于冻结基线才可恢复/删除；未知并发修改会原样保留，并同时返回初始错误与回退错误。
- 共同的 `Invoke-AuthoringGuardedWrite` 用目标命名互斥、冻结源/候选/暂存字节、独占创建备份，防止候选冻结后至暂存期间的常规并发编辑/备份抢占。替换后还核对被替换的原件 SHA；非协作进程在最后释放文件句柄到物理替换间用重命名争抢的极窄窗口，若发生会拒绝返回成功并保留置换证据，不能把新外部字节追认为基线。回退也只触碰可证明属于本任务的字节；未知现场交人工处理。

已有工作簿的 CSV 回灌默认拒绝。若仅做格式/注释修复，调用 New/Import 时必须提供 `-TaskOrigin`、`-AuditPath`、可选 `-ExpectedWorkbookSha256`；候选若改动任何非空值、类型、公式、ID或位置仍会停止。经用户明确授权修改内容时，才额外传 `-AllowContentReplacement`，并保留逐格审计。新建不存在的工作簿仍可直接初始化。

向权威 `DataTables/` 发布派生 CSV 默认拒绝；DATA 任务须显式提供 `-AllowProductionCsvWrite -TaskOrigin <任务来源> -AuditPath <审计JSONL> -ExpectedWorkbookSha256 <冻结SHA>`，并建议显式给出本任务独占的 `-BackupDirectory` 与 `-ProvenancePath`。工具在导出前冻结22个schema目标基线，随后隔离导出、校验、复核同一工作簿和候选字节，再消费计划发布。格式任务、检查任务和 CODE 回归不得传生产写开关。

### U1-R1 后续任务调用边界

- AI-000186 格式规范化与 AI-000189 表头说明均只交付隔离候选，不在 CODE 轮次发布人工工作簿。AI-000191 DATA 发布时才将两者串接，并通过 `Publish-AuthoringWorkbookChange -Mode FormatOrCommentOnly -SourceWorkbook <人工xlsx> -CandidateWorkbook <格式和注释的隔离合并候选> -ExpectedSourceSha256 <发布前冻结SHA> -BackupPath <任务独占备份> -TaskOrigin <任务ID> -AuditPath <任务审计JSONL>`。不得使用 `FillPreviouslyBlankCells` 或 `AuthorizedContent`。
- AI-000191 只有在数据校验允许发布后，调用 `Export-AuthoringToCsv.ps1 -InputWorkbook <人工xlsx> -OutputRoot <仓库DataTables> -BaselineRoot <仓库DataTables> -AllowProductionCsvWrite -TaskOrigin AI-000191 -AuditPath <任务审计JSONL> -WorkbookBackupPath <独占xlsx备份> -BackupDirectory <任务独占CSV备份目录> -ExpectedWorkbookSha256 <发布轮次冻结SHA> -ProvenancePath <逐格来源JSON>`。先冻结 CSV 目标计划，再在隔离候选规范格式、导出和校验；生产授权时格式候选经 `FormatOrCommentOnly` 门发布到原 xlsx，然后派生 CSV 通过冻结计划发布。两次发布有各自审计/备份，不宣称跨 xlsx+CSV 的单一原子事务；若后一步失败，先保全 xlsx 备份与审计、核对当前归属，再决定受控回退，不以旧 CSV 重建用户工作簿。不得先导出后自行重建目标基线，也不得直接调用发布器省略冻结计划。
- 直接 CSV 模式没有工作簿作为来源，回执显式写 `sourceProvenance=DIRECT_CSV_NO_SOURCE_WORKBOOK_FORMAT`；这不代表人工 xlsx 已格式化，DATA 应按轮次事实处理。
- `-TestFaultInjector/-TestFixtureRoot` 仅用于确定性隔离回归；启用时所有候选、目标、备份和审计路径必须位于显式测试根内，生产包装器不会传入这些参数。

## G53 新增字形清单

每次工作簿导出都会在输出根目录生成 `新增字形清单_yyyyMMdd.txt`。报告明确列出基线 `Localization.csv`、比较输入、发生变化的 `loc_key`、相对基线新增的去重 Unicode 字符及其来源 key。报告只供 MIG-F09/字体保护复核；工具不会修改任何字体、材质或其它 `Assets/**` 文件。

## 回归

2026-11-15 包关系路径：表头注释工具支持以 `/` 开头的包根绝对 Target 和相对 Target，统一返回包内 part 名；相对 `../`、`./` 和反斜杠兼容保持，归一化越过包根仍拒绝。既有 Narrative 配表宿主分别实际更新两种关系工作簿，保留人工回复及全部值/类型/公式/结构，并验证两类越根拒绝；该修复不改变注释归属或全局审计规则。

2026-11-15 测试输入确定化：Test-Authoring先在TEMP把真实主表副本准备成规范基线；独立确认Items不含固定无效ID后，仅为TEMP的RecipeSteps!G10准备prd_cabbage_washed，记录唯一cell差异并重新冻结内容基线（保原共享字符串类型、公式和结构，不删除Items条目）。再只损坏Dialogues的A1～Z1共26处（13处缺s、13处s=0），独立统计并证明样式损坏及被测Format前后值/类型/公式/结构不变；此前有意准备的G10输入单独留证，不计入style-only不变证明。原“恰26处修复”及sheet7 ProcessingRecords、sheet10 RecipeSteps等断言保持。Test-HeaderComments使用现工具从22表新建TEMP工作簿，再按header-comment-plan的精确legacy定义准备真实Type/Reference旧工具线程和传统伴随，先核其存在及准确性，再走原合并/224解释/幂等/人工新增备注流程；真实主表source/sourceSha末尾保护保持。两入口的全部旧Assert条件、消息、次序不改，真实主表/CSV不回写。


N004的四旧入口整套执行与正式数据交付由DATA车道收口；CODE使用隔离临时输入验证，不代签正式数据或整批验收。四PS的原数量/遍历/报告调整沿用已批准的16行，输入准备边界见上文，全部旧Assert条件、消息和次序保持。

```powershell
pwsh -NoProfile -File DevTools/DataTables/Test-Authoring.ps1
```

完整回归在系统临时目录建立状态副本，覆盖22个schema sheet、冻结/着色/引用辅助、逐格来源、LF 忠实派生、重复 CSV 字节稳定、空 sheet 不覆盖、G53 新字形与共享校验器；并直接调用生产保护核心证明非空删除/改字/公式/类型/移动被拒、格式-only/空结构/窄范围填空契约正确，以及两文件部分发布、写后验证失败、候选/目标/备份/暂存漂移、末尾审计失败和重复回退均按磁盘字节与错误证据收口。人工工作簿副本另验 26 个 style0、ProcessingRecords 误红公式、旧 73 条列表规则清理、隐藏/额外 sheet、公式和值/类型/空白结构及派生 CSV 字节不变；真实工作簿只读，权威 CSV 写入为零。
