# DATA 隔离导表管线

2026-10-05 AI199 CODE 有符号身份修复（未运行 Unity/发布）：dependencyProof schemaVersion 2 的主对象 localFileId、共享 Sprite localFileId 与 textureLocalFileId 均为完整有符号 Int64；非零正负值原样保留，零、缺失、非整数类型、溢出、主子重复及纹理归属/GUID不符拒绝。生产 C# 取 Unity 返回的 long，不转换为 int32、浮点或绝对值；PowerShell 共用校验器仅接收可表示的整数类型，防止隐式截断。Test-NativeRootProof 在隔离夹具调用实际生产 C# 与 PowerShell，以当前资产 Sprite ID -6724980738021171820、纹理 ID 2800000 做 JSON 完整往返及反例；离线通过不代签原 AI194 新一轮真实 Unity 依赖证明或发布。本节不提升下方完整核对日期。

2026-10-05 AI198 CODE 根类型合同修复（未运行 Unity/发布）：同一生产 BuildBatchDependencyProof 对生成目录全部 .asset 仍逐一列根，并调用真实 Unity GetDependencies(..., true) 覆盖递归闭包。根证据 schemaVersion 2 新增主对象 localFileId 和子资产身份；通常每根必须是单对象 ScriptableObject。唯一精确例外为注册路径 Shared/CK01-MissingSpriteTexture.asset：主对象须为具名 Texture2D，且恰有一个具名 Sprite 子对象；二者 GUID/本地文件 ID 稳定、Sprite.texture 必须指向该主纹理。主与子对象均扫描断裂序列化引用，未知子对象、派生 AnimationReferenceAsset/SkeletonDataAsset、缺文件或 meta 拒绝。PowerShell 同轮验证凭证的根/子对象结构、GUID/meta、完整候选清单与哈希、runId/nonce/源快照；AI197 ExpectedStagedHash 发布绑定未变。编辑器外 Test-NativeRootProof 调实际生产 C# 私有证明方法和共用校验器做正反例，旧 Spine 与发布事务回归仍需保持；原 AI194 必须由 DATA 新 Unity 无 Publish/审定后独立 Publish 验证，此处不代签。本节不提升下方完整核对日期。

2026-10-04 AI197 CODE 发布证明绑定（仅工具源码与隔离夹具，未运行 Unity/发布生产生成物）：正常入口在同轮 Confirm-DataBatchDependencyProof 通过后，将其 candidateTreeSha256 原样作为 Invoke-DataPublicationTransaction 的必填 ExpectedStagedHash。事务先核暂存与已验候选一致，零 diff 也复核；复制 incoming 后核实际字节，再在备份、目标移动、提升与发布后复核源/目标/暂存/incoming/previous/backup 相关冻结摘要。原目标的备份和 previous 均须与原摘要一致；失败回退只删除未漂移的本事务候选、只还原未漂移的 previous，目标被陌生写入或 previous 已变时保全原件/未知内容并报告双重失败，不强删覆盖。复制中的部分 incoming 或漂移备份保全检查；非合作进程在最后复核与文件系统操作间仍可造成极窄竞态，不能声称跨进程绝对原子。旧日志的 logPath 现在保持原 LogPath，不被旧骨骼版本 foreach 局部变量覆盖，SHA 与路径同指向原日志。Test-BatchImport 与 Test-SpineLegacyGate 使用共享生产核心在临时目录复现/拒绝证明后漂移及各故障边界；正式 DATA-R1 仍需原 AI194 的新无 Publish 真实 Unity 批次和后续审定的独立 Publish 运行。此专项不提升下方完整核对日期。

2026-10-04 AI196 CODE 限定例外（仅源码与离线验证，真实 Unity 待原 AI194 DATA 重跑）：Spine 的 AnimationReferenceAsset.Initialize 在骨骼数据为空时清除缓存并返回，旧版本拒绝逻辑不变。SkeletonBaker 在原 SkeletonData loading failed! 错误同行附带实际 SkeletonDataAsset 与 skeletonJSON 的 AssetDatabase 路径/GUID。生产 Get-DataBatchLogAssessment 默认严格拒绝所有该错误和 Debug.LogError；仅 AI-000194 / USER-U1-R1 / 审定 DONE manifest、八个文件 SHA、四 GUID、3.8.75 头和同条错误的 SkeletonBaker 调用栈均匹配时，标 USER_ACCEPTED_LEGACY_ERROR。NullReference、其它 LogError、编译/导入错误照常阻断；历史无结构化上下文日志仍阻断。报告 schemaVersion 5 的 batchLogAssessment 分别列 rawErrorCount（识别到的错误事件）、acceptedExceptionCount、blockingCount、clean（原始错误为零）和 allowedToProceed（阻断为零）；获批准但有错误时 clean=false，不得称日志无错。

同一 ImportAllBatch 在生成/保存后，对 Assets/Generated/DataTables 下全部 .asset（含 catalog、未变化资产与零 diff）执行 AssetDatabase.GetDependencies(..., true)，检查根/依赖 GUID、类型、缺文件及根的断裂序列化引用；旧 Sample/output4 路径和 AnimationReferenceAsset 命中均抛错。该段原 AI196 凭据为 dependencyProof schemaVersion 1；AI198 已替换为上段 schemaVersion 2 的严格主/子资产合同。运行器在日志例外和发布前重算候选清单、校验根/依赖/GUID及隔离源树；缺证、陈旧、未覆盖根、候选漂移、旧资产命中一律失败。真实 Unity 依赖闭包结果尚未产生，不能用离线夹具代签。

DATA 仅在控制台核验 AI196 后恢复原 AI194；不重新 Consume 或沿用旧 131 文件候选。先用原 DONE manifest 调 Run-DataPipeline.ps1 -RoundManifestPath <USER-U1-R1.json> -TaskId AI-000194 -UnityPath <6000.3.24f1 Editor> 做全新无 Publish 批次，审本轮日志例外、dependencyProof、候选 diff/catalog/GUID/白名单与 P2 延期；审定后才以同一正常入口加 -Publish 另开新运行。CurrentCsvValidation、其它 TaskId/轮次没有本例外，字体/场景/Prefab/材质仍禁发。离线验证：Test-SpineLegacyGate.ps1 与 Test-BatchImport.ps1；本节不更新下方最后完整核对日期。

2026-10-04 AI195 CODE 专项（仅源码/编辑器外回归，未重跑 Unity）：AI194 两轮首导的 11 条包资源路径长度为 260–280；现用批准的 .batch-workspace/b-<8位随机十六进制>/ 作为物理隔离工程目录，路径映射对应降为 235–255。完整 TaskId、RoundId、runId 仍保存在报告及完成凭据，目录存在时拒绝碰撞，根内/重解析点边界仍检查。路径长度只是风险证据，不证明包初始化时序或真实首导已经修好；DATA 恢复 AI194 后必须重跑 Unity 并审真实日志。

同一生产运行器在退出 0、当前 run/nonce 凭据通过后，且任何生成物发布前，读取完整 unity-batch.log 并检查明确的导入失败、编译错误和异常行；缺失、空白或不可读取日志直接失败。报告 schemaVersion 4 新增 isolationShortName、batchLogAssessment（logPath、SHA-256、总行数、阻断数及匹配行/相邻上下文）和 Unity-Import-Error-Gate 阶段。正常警告、“0 errors”和字段文字不按泛化 error 搜索误报。阻断时 runnerExitCode=1、published=false，原日志保留。CurrentCsvValidation 与正式 DONE 轮次共用门禁，且前者仍永不发布。

2026-10-04 U1-FORMAT 接口补记：xlsx 到 CSV 的每轮格式候选由 `Authoring/Export-AuthoringToCsv.ps1` 执行，生产应用仅在 DATA191 获授权且数据有效后发生。此处 `Run-DataPipeline.ps1` 只读取当前 CSV 或已 DONE 的轮次，不会反向改人工 xlsx；报告和回执中的 `sourceProvenance` 对直接 CSV 记 `DIRECT_CSV_NO_SOURCE_WORKBOOK_FORMAT`，对 DONE 轮次记 `ROUND_CSV_MANIFEST_WORKBOOK_FORMAT_NOT_RECHECKED` 并另存 manifest 的 `source`。批运行器不把 CSV 导表成功冒充工作簿格式或 Excel 客户端视觉验收。

**用户入口（2026-09-24端到端验收核实）**：常驻可见任务“AI流水线-配表DATA”（`01a0ce9d-8e5a-7542-82b6-6d27609d99e0`）已由控制台通过正式API绑定，读回`THREAD`/精确ID/routeRevision=1；应用可达、同工程及真实idle证据已留档。AI174路由回归最终6/6通过；AI175已SUCCEEDED/VERIFIED，控制台核实真实票据领取、检查点、Complete及原样handoff，6范围452文件前后0差异、物理锁已释放，外显DATA线端到端验收完成。本次仅路由及只读核验，未运行导表、发布或写USER-U1 DONE；原AI174测试观察及人工状态保留。后续所有配表处理统一在此线执行；CODE修工具/schema/业务源码，ENGINE做必要的接线/验证，内部子代理不代做。控制台消费真实DONE轮次并按票据派发的职责不变。详见[新增授权](../../.ai-workspace/inputs/DATA-VISIBLE_外显配表线授权与接力_20260923.md)。

**已验证与尚未发生的边界**：PRE-2完成的是注册/轮次契约/隔离导表实现及幂等、真实变化两用例；AI164强制不发布，尚不能声称一次真实USER-U1正式发布已经验收。当前USER-U1为TODO，没有可消费DONE轮次，本次说明不替用户写DONE。PRE-1字体正式迁移则另由AI167/170继续，不影响上述DATA入口事实。

最后核对：2026-09-23（AI164正常宿主两次真实Unity6000.3.24f1 batch、独立nonce凭据、0/1生成物差异、字体/字形、catalog引用与主Editor共存；未发布生产产物）。

当前结论：AI164经两个精确启动命令各自的正式正常宿主审批后，baseline与隔离overlay均取得真实Unity0/Runner0和有效独立nonce；生成物差异分别0/1，只有预期Localization.asset变化，字形均238、Static未重写，两个catalog均14表/120有效引用。5722生产文件与主Editor当前测试场景不变，两个测试均未发布。详见[两用例通过报告](../../.ai-workspace/outputs/U1-PRE/AI-000164/host-context-recovery-20260923/AI-000164_正常宿主两用例通过报告.md)。

历史保留：[首次假成功与等待缺陷](../../.ai-workspace/outputs/U1-PRE/AI-000164/AI-000164_PRE-2b_首跑阻断与修复接力.md)、[AI166修复](../../.ai-workspace/outputs/U1-PRE/AI-000166/AI-000166_PRE-2b-PROCESS-R1_修复报告.md)、[受限宿主许可失败](../../.ai-workspace/outputs/U1-PRE/AI-000164/user-retry-20260923/AI-000164_用户重试与宿主许可阻断.md)仍有效，不覆盖为旧轮次成功。主MCP连通不能代替batch宿主可用性；本机受限上下文LocalLow查询失败、正常宿主成功的对照和本轮真实batch共同确认恢复路径。

## 一页操作契约

在当前用户正常PowerShell7宿主执行。代理若受限上下文无法解析LocalLow/连接许可IPC，应先保存诊断，并让精确batch启动通过平台正式审批；只读检查获准不等于启动获准，不经MCP/计划任务/安全设置绕过拒绝。无需改变共享许可证或EditorPrefs。

首次可见线程绑定只由控制台执行，且不等同于格式校验。控制台先用应用工具核实目标任务可达、同项目且真实闲置，再调用：

```powershell
pwsh -NoProfile -File .ai-workspace/queue.ps1 -Action DataLaneStatus
pwsh -NoProfile -File .ai-workspace/queue.ps1 -Action BindDataPipeline `
  -PipelineThreadId '01a0ce9d-8e5a-7542-82b6-6d27609d99e0' `
  -UserDecision '<用户原始授权>' `
  -Verification '<应用可达、同项目、线程闲置的真实证据>'
pwsh -NoProfile -File .ai-workspace/queue.ps1 -Action DataLaneStatus
```

第二次相同绑定只读返回 `DATA_PIPELINE_ALREADY_BOUND`；不同目标需要新授权，DATA BUSY/RUNNING、有效预约或待消费 AUTOMATIC handoff 均拒绝且不写状态。当前生产绑定由控制台在AI174成功后执行，回执位于`.ai-workspace/outputs/control/DATA-VISIBLE-20260923/data-lane-bind.json`。

正常内容轮次保持原门禁：

1. 工作簿导出时传 `-RoundId USER-U1-R1`，只产生 `EXPORTED` 事件和 CSV 摘要，不触发导表。
2. 用户确认后运行 `Set-U1RoundStatus.ps1 -RoundId USER-U1-R1 -UserDecision '<明确决定>'`。工具重算 CSV 哈希，只在未漂移时写 `DONE`。
3. 控制台先确认 `DataLaneStatus` 的正式绑定为 `THREAD`/登记 ID，再调用 `queue.ps1 -Action ConsumeDataRound -RoundId USER-U1-R1`；未 DONE、哈希漂移、DATA 未注册或绑定损坏都会拒绝，同一 round 最多一项任务。
4. DATA 执行器在资源互斥成立后运行下列命令。`-Publish` 是显式选择；脚本不提交 Git。

```powershell
pwsh -NoProfile -File DevTools/DataTables/Run-DataPipeline.ps1 `
  -RoundManifestPath .ai-workspace/runtime/data-rounds/USER-U1-R1.json `
  -TaskId AI-xxxxxx `
  -UnityPath 'C:/Program Files/Unity/Hub/Editor/6000.3.24f1/Editor/Unity.exe' `
  -Publish
```

已授权的“当前 CSV 首跑”不是 USER-U1 内容完成轮次，使用独立验证模式；该模式强制禁止发布、不会生成或改写 USER-U1 状态：

```powershell
pwsh -NoProfile -File DevTools/DataTables/Run-DataPipeline.ps1 `
  -CurrentCsvValidation `
  -ValidationSource 'AI-000164 PRE-2b authorized first run' `
  -TaskId AI-000164 `
  -UnityPath 'C:/Program Files/Unity/Hub/Editor/6000.3.24f1/Editor/Unity.exe'
```

真实变化用例只能用隔离 overlay。overlay 来源须在 `DevTools/DataTables/Fixtures/` 或 `.ai-workspace/runtime/data-validation-inputs/`，只替换隔离副本中已有 CSV；同一模式仍禁止 `-Publish`：

```powershell
pwsh -NoProfile -File DevTools/DataTables/Run-DataPipeline.ps1 `
  -CurrentCsvValidation `
  -ValidationSource 'AI-000164 isolated real-change case' `
  -ValidationCsvOverlayPath '.ai-workspace/runtime/data-validation-inputs/AI-000164/Localization.csv' `
  -ValidationCsvRelativePath 'Localization/Localization.csv' `
  -TaskId AI-000164 `
  -UnityPath 'C:/Program Files/Unity/Hub/Editor/6000.3.24f1/Editor/Unity.exe'
```

## 固定执行顺序和最小输入输出

`Run-DataPipeline.ps1` 的顺序是：取得物理锁 → 记录完整复制范围及正式生成目录哈希 → Quick 校验 → 复制隔离工程 → 可选隔离 overlay → 核对输入和目标未漂移 → 用明确进程句柄启动并等待 Unity batch → 核对真实退出0与当前run完成凭据 → 原日志错误门 → 核对生产输入未变 → 生成报告 → 正常轮次可选事务发布。物理锁直到进程退出、凭据/日志校验和可选发布全部结束才释放。

实际隔离输入（快照与复制严格同范围）：

- `Assets/**`；
- `Packages/**`、`LocalPackages/**`、`ProjectSettings/**`；
- `DataTables/**`；
- 精确 Unity 版本 `6000.3.24f1`。

不复制活动 `Library`、`Temp`、`Logs`、`obj` 或 `.git`。完整 `Assets` 是当前可证明的编译依赖闭包；文档不再把实际全量复制描述成只复制 Importer/字体子目录。

隔离工作区固定在忽略目录 `DevTools/DataTables/.batch-workspace/b-<8位随机十六进制>/`；报告仍位于 `.ai-workspace/runtime/data-reports/<完整runId>/`，物理目录与逻辑身份在报告中映射。存在目录一律拒绝复用，不覆盖旧轮次。报告包含：

- `data-pipeline-report.json`：schemaVersion 4；完整任务/轮次/runId 与物理短目录映射、输入范围/哈希、进程PID/开始时间/exe/projectPath、可空的真实 `batchProcessExitCode`、独立 `runnerExitCode`、经验证后才记录的精确 API 调用、版本、锁、日志错误门与发布动作；
- `batch-completion.json`：从隔离工程复制的当前run结构化凭据；runId/随机nonce/新鲜时间、导表计数、字体manifest结果和项目CSV parser字形列表都必须匹配；
- `unity-batch.log`：真实 Unity 运行时才生成，失败含 `[DATA] BATCH_IMPORT_FAILED`；
- `generated-diff.json`、`generated-catalog.json`；
- 当前空diff序列不会创建对应diff JSON（AI164已留非阻碍WARNING）；零差异计数读取 `data-pipeline-report.json` 的Diff-Catalog-Glyphs-Fonts阶段，并可对完整generated-catalog清单独立比对，不把缺文件本身当成零差异证明。
- `font-diff.json`：隔离字体资产前后差异；字体不随生成物事务发布；
- `glyph-list.txt`：由 `DataTableCsvParser` 与字体Builder同语义解析 `Localization.csv/zh_cn` 后写出的 Unicode 码点清单；字形数按本轮实际内容核对，不沿用旧轮次的238或 PowerShell `Import-Csv` 对带引号多行字段的计数；
- `validation/`：Quick 校验报告；正常发布时另有 `backup/generated-before/`。

`ImportAllBatch` 只复用 `DataTableImportService.ImportAll()`，成功后调用已审 PRE-1 `ShortCycleFontAtlasBuilder.BuildAll()`。只有两者都成功后，才以 `FileMode.CreateNew` 在隔离工程内写本次run完成凭据；Runner核对真实进程退出0、runId、随机nonce、进程后新鲜时间、本轮解析得到的字形清单与字体manifest计数/哈希后，才把两个API登记为已执行。缺失/旧凭据、假0、超时或非零都失败；未启动或超时时真实进程退出码保持null，不能用Runner退出码填充。生成资产和字体均依自身幂等逻辑；事务检测到生成目录哈希相同会返回 `NO_CHANGES`，不建备份、不移动或重写正式目录。当前 CSV 验证模式无论是否产生差异均不能发布字体、生成物或测试 CSV。

## 发布、漂移与回退

- DATA 与接触同一输入/输出的 ENGINE/CODE 任务必须声明相同资源键；物理 `.ai-workspace/runtime/data-publication.lock` 从快照前持有到本次子进程真实退出、完成凭据核对和可选发布结束。
- 超时/等待异常只按本次持有的 Process 句柄终止其进程树，并在异常中保留 PID、StartTime、exe、projectPath 与 log；不枚举或终止用户 Editor、License Client 或其它共享进程。
- 发布校验完整输入快照、DataTables 摘要和正式生成目录摘要。源/Importer/包/ProjectSettings/字体输入或目标任一漂移即停止。
- incoming、previous 和 backup 都先验证绝对路径边界。准备 incoming 或备份失败且尚未移动 live 时，绝不删除 live。
- live 已移动后的失败会尝试恢复 previous；恢复失败时不清理 previous/backup，并在异常中报告二者绝对路径，供人工恢复。
- 只清理由本轮实际创建且已证明位于审定父目录的临时对象。测试故障注入只使用系统临时 fixture，不对生产目录注入。
- CSV 与 `Assets/Generated/DataTables/**` 的同轮真实结果未来应同一提交；字体单独审查/提交。脚本不执行 commit/push。

## Unity 6000.3.24f1 API 复核

本卡仅做程序集/源码复核，不启动 Editor。复核机上的 `Unity.exe` 产品版本为 `6000.3.24f1_4e7b9b5b6244`，项目 `ProjectVersion.txt` 为 `6000.3.24f1 (4e7b9b5b6244)`。

| 调用 | Unity 6.3.24f1 复核 | 用途与失败语义 |
| --- | --- | --- |
| `UnityEditor.EditorApplication.Exit(int)` | 从本机 `Editor/Data/Managed/UnityEditor.dll` 反射得到 `Void Exit(Int32)`，public static | batch 失败请求进程退出码 1；随后仍抛出原异常，不吞错 |
| `UnityEngine.Application.isBatchMode` | `UnityEngine.CoreModule.dll` 的既有 public static 属性；静态桩同时覆盖编译合同 | 只在 batch 进程显式 `Exit(1)`，非 batch 调用仍抛异常 |
| `-executeMethod EatWhat.DataTables.DataTableImportService.ImportAllBatch` | 方法已由 21 源码单元静态编译并反射确认 public static/无参 | Unity 命令行唯一入口；成功随 `-quit` 正常返回 0 |
| `ShortCycleFontAtlasBuilder.BuildAll()` | PRE-1 现有 public static 方法；本卡仅反射调用，不改 Builder 的 Unity6兼容修复 | 在隔离工程重建/复核 Static+Dynamic；不直接发布字体 |

`-PlanOnly` 只验证门禁、快照范围、项目版本和准确 batch 命令，不启动 Unity，也不能与 `-Publish` 并用。编辑器外 `Test-BatchImport.ps1` 当前85项覆盖既有事务/进程/凭据/锁契约，以及短路径唯一/碰撞/越界/映射、AI194实际日志片段阻断、编译/异常、缺失/不可读日志、警告和零错误不误报、阻断时零发布调用；夹具只写系统临时目录。AI195没有启动 Unity；恢复后的 AI194 DATA 仍须用同一正常 DONE 入口、当前 xlsx/CSV/manifest 指纹和原稳定 ProblemKey 重跑，核真实首导日志完全无资源/编译/异常错误及新nonce，再重审实际 diff/GUID/catalog/白名单并决定是否发布，不能沿用旧131文件候选或把 P2 场景延期写成已验收。
