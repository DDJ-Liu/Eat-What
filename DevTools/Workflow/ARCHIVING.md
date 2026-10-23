# TK-WF-01 按任务归档与任务简表

2026-10-16：同族入口为 `Archive-Stage.ps1`、只读策略 `Archive-Policy.ps1`、派生表 `Get-TaskDigest.ps1`。唯一永久清单为 `.ai-workspace/PERMANENT_DOCUMENTS.json`。不改队列状态，不控制 Unity；正式 Create/Prune 由主控按当轮授权审查后执行。

## 先 Plan，再审查

使用 PowerShell 7，在工程根运行（示例任务号需换成本轮已批准对象）：

```powershell
& ./DevTools/Workflow/Archive-Stage.ps1 -Action Plan -Stage 2026-10-16 -TaskIds AI-000169,AI-000170 -OutputPath .ai-workspace/outputs/control/archive-plans/2026-10-16-plan.json
```

`TaskIds` 限定任务；可用 `SelectionPath` 进一步指定记录文件或目录。省略 TaskIds 时选取当前符合关闭语义的任务；不会要求整个队列关闭。路径归属依据精确 AI-xxxxxx 目录段和明确的任务输入/产物引用，文件正文出现任务号不构成归属。嵌套同一任务目录归入同一包，重叠选择按完整路径去重；混合任务、共享、不明归属留原位。

旧版主文档可能没有唯一 AI 任务归属。使用同入口显式 `-DocumentFamilyIds design-master -SelectionPath .ai-workspace/inputs/AI开发工作流总档_设计端_v1_5_2.md` 生成 `DOC-design-master-v1_5_2` 家族版本单元；不生成虚假任务、不改 runtime。省略SelectionPath时合入明确家族实际成员；必须是清单内明确家族、已确认当前版本的旧版，仍复核永久保护、未关闭读取引用与当前文档链接。包与文件保留 family/currentPaths 替代映射；Create/Prune/Extract 沿用同一 Plan 流程。

Plan 只读原件并写计划。逐文件记录路径、归属、资格/保留原因、字节数；待归档/处置件记录 SHA。逐任务 ZIP 目标 1,000,000B，硬限 10,000,000B，不分包。超限时 `BLOCKED`，整个任务原件保留；流式压缩触及上限即停，packedBytes=null、packedLowerBound=10,000,001 表示已确认超限、未继续算精确包长；unpackedBytes 为完整原始长度。不会为求包长继续处理数 GB 无关内容，也不生成隐藏旁包。任务若有保留原件，包报告 retainedBytes/retainedFiles、partial=true、PARTIAL_SOURCE_RETAINED；显式路径选择也报告 EXPLICIT_SELECTION_ONLY，不能把局部 ZIP 达标当作整个任务达标，wholeTaskLimitSatisfied/targetMet 均 false。无包的保留项仍列逐文件长度及计划 retainedBytes。

完整文本/JSON/CSV/日志按原字节压缩，`.stdout.bin` / `.stderr.bin` 内的文本同样保全。默认不丢弃。仅经审查的明确冗余非文本可通过 `DispositionPath` 的 `files` 数组提出：

```json
{"files":[{"taskId":"AI-000169","path":".ai-workspace/outputs/AI-000169/duplicate.png","redundant":true,"uniqueRecovery":false,"reason":"已核对的冗余截图；完整原文与独有恢复基线在任务包保全"}]}
```

图片分类识别明确 PNG/JPEG/GIF/WebP/MDMP 内容签名；用户追加机器转储及备份证明见下文AI255节。扩展名不能证明可丢弃。未知格式一律保留。永久件、活动依赖、独有恢复基线不能借此删除。处置要求至少还有完整任务包；整任务只剩丢弃项时拒绝。上方旧示例只展示基本形状；AI255起每条还必须补齐下文classification/contentEvidence/authorization及原件SHA、长度。主控必须审查全部依据。共享/未知记录和 BLOCKED 原件需要后续单独判断，不能据计划留存原因擅删。

## 执行与恢复

```powershell
& ./DevTools/Workflow/Archive-Stage.ps1 -Action Create -Stage 2026-10-16 -PlanPath .ai-workspace/outputs/control/archive-plans/2026-10-16-plan.json
& ./DevTools/Workflow/Archive-Stage.ps1 -Action Verify -Stage 2026-10-16
& ./DevTools/Workflow/Archive-Stage.ps1 -Action Prune -Stage 2026-10-16 -PlanPath .ai-workspace/outputs/control/archive-plans/2026-10-16-plan.json
& ./DevTools/Workflow/Archive-Stage.ps1 -Action Extract -Stage 2026-10-16 -OriginalPath .ai-workspace/outputs/AI-000169/report.md
```

Create 接受旧调用形式（省略 PlanPath 则现场生成同一新规则计划，禁止隐式丢弃），新包采用 schemaVersion=2、每任务一个 ZIP。旧 schemaVersion=1 `records.zip` 与 manifest 继续 Verify/Extract；旧 Prune 直接拒绝，须重新按当前资格准备任务计划。Extract 仅写同阶段 unpacked 缓存，不覆盖不同字节的缓存，更不恢复到生产源路径。

Create/Prune 使用现有队列互斥边界；MANUAL、当前关闭资格、人工门、未关闭 dependsOn/路径引用、永久规则、活动文档引用均复核。源文件变化/新增、永久清单变化、包或计划不同即拒绝。所有归档条目的长度和 SHA 先读回核验，全部通过后才逐个删除明确清单中的原件；不递归删目录。路径必须在工程/隔离 fixture 内且无 reparse。失败保留既有包和原件供检查，不覆盖半成品包；任务范围应拆成可独立复核的计划，以便漂移时仅重做受影响选择。

## 永久版本与一次性保全

清单 exactPaths/directories/patterns 优先保护；原 PermanentInputPatterns 作为兼容兜底。家族必须使用锚定 pattern、明确 currentPaths 和数字版本分段比较；旧版仅在归属/关闭/依赖条件通过后可归档。未确认更新版、并列版本或 currentPaths 不唯一时整个家族保留。显式调用 PermanentInputPatterns 可额外保护旧版。清单记录当前 ROOT 核实版本：总档1.5.4（2026-10-17接收并核对M5）、追踪1.2.4、Schema2.4.1、排布0.3.1、规划2.3；无版本排布入口及回填CSV独立保留。

AI190九原件（含七 validation JSON）是下一次归档的一次性保全：PENDING_NEXT_ARCHIVE_VERIFICATION 状态保留原位，ROOT在完成该次保全核验后显式改为 FULFILLED，不自动延长为永久业务规则。当前活动源码、在线状态、协议、人工索引、ART基线、MIG63工程约束、codebase与project-status规则族均受保护。

## 简表与验证

```powershell
& ./DevTools/Workflow/Get-TaskDigest.ps1
& ./DevTools/Verify-All.ps1 -TaskId AI-000254 -Profile Custom -Mode Final -RunnerPath DevTools/Workflow/Test-DocumentArchive.ps1
```

简表与 TASK_DIGEST_INDEX.json 永久派生，均不增加 worker 必读。按 module 与实际 batch 分节；未登记 batch 写 unknown，不从目录/标题猜。每任务行≤200可见字符，所有字段保留；长关系、原文总结、父根、supersedes、失败/批准史在 JSON 索引按 task id 检索。输出有可解析原件路径时直接链接，否则输出字段指向索引中的原始路径证据。状态缩写和“详”均由简表图例解释。

仅聚合现有 usage 的 provider_response，按 responseId 去重；输入/缓存/输出均为部分观测，缺失为 unknown，不代表整任务账单。实际工具来自明确 usage/result 记录，不从 lane 推断。预算与最新执行 app turn 有依据才显示，不把响应数当轮次；续行批准按 approvalId 去重，并分别累计 runs 与 usedRuns，旧不明许可保守 unknown。相同输入稳定刷新不重写文件。生成器不写 runtime；生成时仍运行的任务显示 OPEN，之后由控制台在批末刷新。

单一定向 runner 仅写隔离 fixture，验证状态/归属/永久版本/包体积/文本/漂移/路径/旧格式及简表；不跑 Unity、不复验全仓。实际工程 Create/Prune 不属于该 runner，也不由本次CODE任务执行。

## AI255：机器转储和备份的显式审查

DispositionPath 为 `{ "files": [review, ...] }`。以下字段现在固定，处置默认关闭，须逐文件填值。生产执行由主控按本轮授权审查，不能以分类代替关闭/依赖/永久保护。

每条公共字段：path（工程相对正斜杠路径）、taskId、classification、sha256、bytes、redundant=true、uniqueRecovery=false、reason、authorization={path,sha256,bytes,description}、contentEvidence={kind,description,...}。授权证据可直接指向本批用户授权.md，绑定当前完整字节SHA/长度；执行前若追加变化须重做计划。

classification及其证据：

- image：kind=recognized-binary-signature，原有图片/MDMP签名识别。
- machine-dump：kind=unity-serialized-json，signatureSha256=下述projection.signatureSha256；还须 preservedMetadata={path,sha256,bytes}。所有机器dump均先保存projection，纯Font也保留非大数组字段，避免默默丢元数据。支持Font/m_FontData、Texture2D/m_ImageData；objects中path/type/json一致的Font、Texture2D、TMP_FontAsset、Material。另支持明确otf数组的Font包装：id为数值、type为Font/UnityEngine.Font、json内为Font+m_FontData；此变体不要求path，其余taskId/ok/scenes/targets等均保留。未知嵌套JSON不放行。
- redundant-backup，证明A：kind=identical-retained-resource，duplicate={path,sha256,bytes}，指向Assets/Packages/ProjectSettings/LocalPackages现存相同字节资源。普通文本不可用此证明；序列化资源副本允许。
- redundant-backup，证明B：kind=verified-archive-entry，archivePath=永久.ai-workspace/archive/.../records.zip，archiveManifest={path,sha256,bytes}，entryPath=包内原路径，entrySha256、entryBytes须与待删副本相等。逐条读回验证后可处理包含文本的恢复演练副本；不按后缀宽泛授权。ZIP只按所需条目证明，不重复哈希整个GB包；同调用只打开一次并持有只读锁。
- generated-build：kind=explicit-build-manifest，manifest={path,sha256,bytes}。该清单 schemaVersion=1, buildId, provenance，entries=[{path,sha256,bytes,role:generated-binary或resource-copy}]。内容必须非普通文本且有精确清单；.asset/.meta等文本序列化资源还须duplicate保留源证明。普通.log/.md/.txt/.csv/.json等不借build分类丢弃。

机器转储元数据派生（不复制巨型payload）：

```powershell
. ./DevTools/Workflow/Archive-Policy.ps1
$projection = Get-ArchiveDumpProjection (Get-Location).Path '.ai-workspace/outputs/.../AI-000218/attempt2/handle-release-before.json'
Save-ArchiveJson (Join-Path (Get-Location) '.ai-workspace/outputs/.../AI-000218/attempt2/handle-release-before.metadata.json') $projection
# 将保存后的.metadata.json完整SHA/长度填preservedMetadata；signatureSha256直接取$projection.signatureSha256。
```

projection schemaVersion=1，sourcePath/sourceSha256/sourceBytes，derivation=unity-dump-projection-v1，signatures={kinds,removedPaths}、signatureSha256、metadataJson。metadataJson是保留所有非dump字段/文本/检查结果的JSON字符串，数字原始表示不经PowerShell数值重序列化；指定payload替换null，removedPaths逐项标明。Create/Prune会由原件重新派生并逐字段比对。元数据须在同一个任务包中archive，不能放在丢弃清单/外部旁包，也不替代原报告。不要把projection完整结果打印进chat，只落盘/输出计数与路径。

组合计划：-TaskIds <全部已关闭ID> -DocumentFamilyIds <明确家族ID>，省略SelectionPath即可取默认完整任务范围并合入明确家族实际成员，不造成任务假partial。SelectionPath仍表示显式子范围。当前/歧义/活动引用照常保留。
无扩展名生成缓存：仅generated-build可用，仍要求逐文件构建清单；contentEvidence另填binarySignature={offset:0,hex:'前8～128字节的十六进制'}，程序核对精确前缀并确认二进制内容。无扩展名本身不是许可；纯文本不因签名证明而放行。已有资源扩展名可选填同一签名作为额外约束。

当前文档中的records根目录导航（精确等于recordRoots、工程根或.ai-workspace）不计活动任务依赖；具体任务目录/文件引用继续保护。

## AI256：实际Unity6包装与归档检索

实际核对AI218三份既有文件的结构后，otf数组支持有明确id/type的Font及关联Texture2D；objects仍要求path/type与内部类型一致。Texture2D新增精确“image data”数值头及_typelessdata数组/字符串，保留头及其它纹理元数据，仅剔除实际数据体。Material仅额外允许stringTagMap/disabledShaderPasses；TMP_FontAsset仅额外允许已核InternalDynamicOS、fontWeights、normalStyle、normalSpacingOffset、boldStyle、boldSpacing、italicStyle、tabSize、fallbackFontAssets、atlas。其它非m_报告字段或未确认包装仍拒绝。

嵌套json现在保留为可解析的序列化元数据字符串，只将明确Font/Texture payload置null并逐路径登记；TMP/Material字段完整保留，不再整段丢弃。只有TMP/Material而没有明确可移除payload的文件仍保留。现有直接纯Font的projection字段、metadataJson字节及signatureSha256保持兼容；新增包装重新生成其projection后再审查。

Get-TaskDigest只读archive各直接子目录的schema2 manifest，把READY且现存的AI任务ZIP和manifest路径登记到每任务archiveOutputs；不重新核验ZIP内容，不将仅存在的文件冒称本次哈希验证。原输出路径完整保留；有现存非空原输出时优先原件，否则链接现存任务ZIP，没有可用包则回索引。空输出目录视为原件已清理。短标题18/总结32字符，接近总行200字符时收缩短文/长计量字段，完整值仍在索引；不丢字段、不改变worker必读。本批真实归档后由CONTROL生成永久简表，CODE只在隔离fixture核验。

## AI258：旧包装、instrumentation快照与同包资源去重

Get-ArchiveDumpProjection 额外识别完整font/material/atlas（或texture）字符串三元组；逐项要求TMP_FontAsset或Font、Material、Texture2D真实序列化结构，保留全部外层/内层非payload字段和数值字面量。新增derivation为unity-named-wrapper-projection-v1；只有Font/Texture数据体可置null。未知JSON不因键名或文件名获准处置。

严格snapshot-N.memory.txt可使用同一调用。必须存在同目录identity.txt、font.json、material.json、texture.json、imageHash.txt五分件，按原instrumentation的LF次序精确重建完整原文，并验证三个整数ID、32位hex hash、TMP/Material/Texture类型。新增derivation=unity-instrumented-memory-projection-v1，metadataJson完整保留五段非payload内容；signatures.components登记各分件路径/SHA/长度。Create/Prune重新核对分件重建结果。普通txt及不匹配格式保留。review仍使用machine-dump/unity-serialized-json公共分类，signatureSha256及preservedMetadata照常绑定；新元数据必须在同任务archive。原分件的大payload若另行获准处置，仍各自保留其完整元数据；报告/诊断脚本和其它普通文档原字节归档。

redundant-backup新增contentEvidence.kind=identical-archived-peer，peer={path,sha256,bytes}。仅资源备份（包括.unitypackage/.psd等已知格式），须真实相同SHA/长度；普通.md/.txt/.json/.log/.cs等不能采用此证明。peer必须是同一AI任务、同一archiveUnitId内的archive条目，不能自指、跨任务、retain、discard或构成环。Plan可以报告BLOCKED预览，但该peer方案执行或恢复前必须是READY；Create/Prune继续先核全部源、关闭/依赖/永久资格及全部包内条目，再执行明确删除清单。不是按扩展名自动去重，也不允许整任务只剩丢弃项。

只有新peer证明在discardValidation末尾增加requiredArchivePeer={path,sha256,bytes}，旧分类的字段顺序和对象形状、直接/objects/otf旧投影格式与签名保持不变。Assert-PlanShape/Assert-PlanCurrent中的inventoryPaths成员查询使用每次调用新建的InvariantCultureIgnoreCase HashSet，与PowerShell字符串-in/-notin语义相同；没有跨调用缓存或省略检查。

Extract可对带上述peer证明的原discard路径恢复：读同一READY包中完整peer条目，写入unpacked/原请求路径，返回restoredFrom标明peer；先核完整包及映射SHA/长度。图片或机器payload处置不因此具备无损恢复能力，不带peer的discard仍拒绝Extract。既有缓存不同字节仍拒绝覆盖。

本次实际只读核对AI214/before-guard-memory.json、AI217/attempt2/old-order-negative-memory.json、AI215/snapshot-1.memory.txt及原采集/重建代码；验证沿用Test-DocumentArchive.ps1受影响用例与相应派生核验，没有执行生产Plan/Create/Prune。最后核对：2026-10-16。

## AI259：活动引用、分组版本和用量证据

2026-10-17实际核对：M2逐项14个引用源、M1 CLAUDE、S3四稳定入口已登记；派生TASK_DIGEST.md/TASK_DIGEST_INDEX.json与archive/**/README.md显式排除活动引用扫描，避免索引链接使全部任务不可归档。只有明确referenceDocuments进入扫描，不递归追踪所有被链接文档。

保留原5个家族，新增恰8个：p2-layout、p2-modules、parallel-framework、dispatch-batch、resident-excerpt、human-process、art-request、art-daily-template。当前前5个待投放且currentPaths空，不根据文件名猜最新；后3个当前实际成员分别为人工段流程v2_20261014、美术需求单P0P1_v1_20261014、ART每日模板v1_0。数字版本支持点号/下划线，8位日期只作后缀；同数值版本、未确认更新版、缺current均保留。美术需求按主题、派发按批次与卡身份比较，不能以P2或另一批次较大版本替代P0P1/当前批次。

目录保护仅对family.directoryVersionExceptions精确列出的目录允许已确认旧版成员进入候选；当前/未知/独有原件和其它exact/oneTime/custom/pattern保护仍优先。templates仍整体保护，仅已登记人工流程旧版可通过对应目录例外；inputs/art同理。ART旧模板的精确当前项改由当前家族保护，未来版本须明确更新currentPaths，不自动迁移权威版。

派发索引与cards使用同一dispatch-batch族、同批次/版本归档单元；currentPaths须逐身份登记。未版本化或未确认card使该批次保留；batchBindings须显式给出batchId/taskIds，任一所属任务未关闭、缺失、归属未知，或任何未关闭任务dependsOn这些任务（即使没有文档路径引用），均保留。现有未关闭refs与活动文档引用保护继续叠加。新增族未投放，不将本次注册误称已确认设计版本；实际候选仍走Plan/Create/Prune原门禁，本卡没有生产归档。

完成模板使用TASK_USAGE_V1机器块。生成器从正式Artifacts/usageReportPath指定报告读取，身份schemaVersion/taskId/attempt/executionId须与当前正式execution一致，非空字段须有SHA/bytes和JSON valuePath证明；误配、漂移或冲突保留拒绝原因，不能串任务/尝试。报告或其JSON证据归档后，可从同任务READY manifest定位唯一条目并读回SHA/长度；最多读2MiB文本，ZIP内仍使用实际大小写条目名，不恢复原位或改历史报告。

优先级为有效机器块→有逐行原文来源的usage-backfill→正式queue/usage。工具、实际app轮次、预算分别选择，null不覆盖已有可用来源；token仅相同覆盖口径且tokenSetId证明同一测量集合时补缺，绝不合计不同口径。完整索引保留所有provider观测、选择依据、未知原因、原标题、关系及失败史；简表短名去批次前缀并保留卡号/动作，≤200字符。没有有效证据时写unknown，不从ENGINE_MCP车道推断Coplay，也不把支持CLI的CODE卡记作CLI业务。

AI230～249回填文件为.ai-workspace/inputs/DOC-REVIEW-20261017/usage-backfill.json，作为永久派生依据精确保护；20份最新attempt报告逐项读取并留ZIP/条目/行/原文/SHA，旧报告与queue历史均不修改。全部实际app turn缺证仍unknown，预算单列；AI248为ENGINE_MCP车道实际CLI共存验证，AI249为CODE接入且真实CLI/Editor为0。报告中的上下文单样本、其它任务首响应及测试/调用数不转成任务消耗。

## 2026-10-17 归档位置纠偏

已结全文属于归档区；不能只处理AI目录而遗漏CONTROL批次，不能把历史证据链接作为长期留原位的理由。真实运行消费仍保护。无AI编号批次可由控制端按显式完整清单、关闭依据和同样的10MB/逐文件SHA门禁集中保全；不要伪造队列编号。此次实际范围与恢复映射见`.ai-workspace/archive/2026-10-17-workspace-correction/manifest.json`。

Get-TaskDigest默认输出改为`.ai-workspace/archive/tracking/TASK_DIGEST.md`及同目录索引/usage-backfill.json；历史根路径仅留短指针。归档区派生全文不进入执行器日常必读。


## AI261：完整任务固体压缩与只读证据消费

2026-10-17超限材料使用标准tar.xz整包和同包精确字节hardlink共享；报告/必要文本不截断，不拆任务绕10,000,000B硬限。超过1,000,000B逐单元给必要性、已精简内容和不能再减的原因。现有Archive-Stage继续使用ZIP；本批是控制端显式清单下的操作，见 `.ai-workspace/archive/2026-10-17-oversize-slim/README.md`，未经同等资格/证明/读回/删除前核对不能直接复用其删除脚本。

Get-TaskDigest兼容schema3 manifest的READY AI单元：package字段为unitId/taskId/archiveFile/format=tar.xz/archiveSha256/packedBytes/unpackedBytes；files仍登记originalPath/archiveUnitId/taskId/disposition/bytes/sha256。保留的 `__archive__/manifest.json` 也在对应单元登记archiveOnly=true，不能跨单元混读。CONTROL批次仍由归档索引检索，不虚造AI编号。TRIAL/BLOCKED不作为READY证据。

`Read-TarXzEvidence.py` 是TK-WF-01生成器内部只读辅助，不解包、不写生产路径、不下载解释器。先核包SHA/长度，取证时核完整成员与hardlink关系及所取文字SHA；拒绝越界路径、symlink、循环/跨单元链接和声明不符。包≤10MB，所取报告≤2MiB，AI单元≤10,000条/512MiB声明解压量；全局manifest≤32MiB，流式解压有576MiB硬界与128MiB解码内存界。Python不可用时明确诊断，不切换格式假读。

当前客户端入口AGENTS/CORE/CLAUDE仍以永久规范为准；本次无需Unity、日程或额度监管机制变更。旧归档不重写。被引用截图若已在既有永久归档有同字节原件，可凭逐条读回证明移除活动重复副本并保留恢复映射；不以缩略图替代原图。历史轻量交付ZIP可保留完整条目及容器重建信息，但只有从新包重建的原ZIP全字节SHA一致后，才按本批可恢复转换清理源容器。

## INSTRUCTIONS账本轮转（2026-10-18）

依DOC_MAINTENANCE第16条，活动账本路径仍永久保护，普通Archive-Stage不能将其整体Prune。CONTROL在正式任务结算安全点，核关闭/依赖、未结USER块及追加消费者，整份原字节入包且读回验证后，在现有queue互斥下重新核源并轮转。历史编号、修订、许可和事件按原件UTF-8字节范围检索，runtime和编号不修改；本次使用显式一次性清单，不新增通用清理入口。

现有Test-DocumentationIndex.ps1改查现行永久入口和archive/tracking/INSTRUCTIONS_ARCHIVE_INDEX.json归档证明，不再依赖已归档的AI159基线、历史50%阈值或“原账本永不缩短”断言。最新轮转核授权/ZIP/原文SHA及长度、活动前缀、留存块和当前未关闭/依赖，允许正常追加后缀。现有Test-DocumentArchive.ps1提供隔离的定向回归；不运行Unity或借此新增业务任务。后续每批更新索引，不自动执行轮转或日程。

## 每批空目录与遗留项收尾（2026-10-18）

依DOC_MAINTENANCE第14、15条，源文件Prune结束后还须由CONTROL核查并清除空任务目录及空父目录，不能把“已删除文件”当成目录也已清洁。现有Archive-Stage的Prune只清理核准文件；目录步骤仍由控制端按显式路径、永久/活动引用保护、自底向上及非递归删除单独执行和留回执。普通空记录/备份根目录若没有当前消费者或永久保护可一并移除；实际运行根目录保持。重解析点不得随递归遍历进入共享目标。

对遗留组先识别内容、精简和试算，全部合格并读回后才迁移；机器提取物不因.txt后缀自动等同普通报告，也不因大而自动获准删除。必须有实际结构、来源、载荷边界/哈希和完整非载荷元数据证明。新包装不自动获得既有生产工具支持，本批CONTROL试算不是Archive-Policy已支持新格式的声明；若未来扩充永久工具，仍交固定CODE实施定向验证。被引用图像的原件保留规则若造成超限，由用户明确保留范围调整后执行；不得擅作有损替换、拆包、隔离迁移或外移绕限。

2026-10-18资产审查组已获得本组67张引用图板的明确压缩例外，见`.ai-workspace/archive/2026-10-18-asset-review/authorization.json`。CONTROL采用显式原路径清单的一次性tar.xz整组处理：原文档原字节保留、30份结构已核验的纹理提取物只去除载荷并完整保留其它字节、图板派生映射及冗余预览/相同资源证明入包。记录有损派生不能恢复原图，不能宣称所有源件可无损恢复；这不改变现有Archive-Stage/Archive-Policy的支持格式或执行范围。
