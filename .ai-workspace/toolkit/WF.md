# 工具库 · WF

最后完整核验：**2026-09-17**（不提高）。本次仅整理文档与核对源码归属；任务执行过程、当时的测试与许可记录移至历史，不能据历史状态启动业务。执行规则以 AGENTS.md、WORKFLOW.md 的现行优先条款为准。工具表保留原能力、使用限制与来源语义，旧候选／待验标签不代表本次重新验收。

## 现状

<!-- AI507-PLAY-CONTROL-20261112 -->
2026-11-12 AI507 局部离线修复：schemaVersion=1 新增明确 intent=EnterPlay / ExitPlay。普通 ReadOnly / Write 语义保持，Write 在 playing/paused 仍失败。EnterPlay 要求当前 stopped、clean 和全部原守卫通过；ExitPlay 要求当前 playing/paused，并以 proofs.playEntry 引用本 task/owner/execution 通过 EnterPlay 真正返回“Entered play mode”的官方调用摘要。同目标 PID/版本及原生项目/端口必须匹配；退出的 editorStatus/scenes/consoleAfter 必须在进入调用之后真实采集，不能复用进入前 stopped 证据。

既有 wrapper 的 Assert-StandardPreflightCommand 消费状态意图：只允许本机现有 `%LOCALAPPDATA%/Unity/bin/unity.exe` 的精确 `command editor_play` / `command editor_stop`，匹配项目及显式 `--timeout 30 --format json --non-interactive`。拒绝额外/重复参数、GRANT 持久写、StartupReady/Capture 或其它可执行文件替代；ReadOnly/Write 不能伪装这两个命令。未声明标准参数的旧路由保持兼容，不表示授权任意业务绕过标准入口。

退出专用 stopCleanupOnly 只有在“本任务已进入、当前其它所有守卫通过、Console 增量完整且仅新增 Error/Assert/Exception 导致失败”时为 true；passed 仍 false、原错误及 consoleBaseline 失败保持。只有官方 Stop 可消费；Stop 进程结束后 wrapper 仍以 STANDARD_PREFLIGHT_CLEANUP_ONLY 记录 failure 并抛错，保留实际 native exit/原流，不可当验收成功。Console 缺口、错误目标、dirty/untitled、文件漂移、编译/重载、失租、正式停止或平台拒绝不放宽。独立脚本仍输出失败结果并抛错，不能用 standalone 成功冒 Stop 许可。

完整调用链、schema 与失败清理见 [AI507 ENGINE 交接](../outputs/PLAYABLE-DEMO012-20261112/AI-000507/attempt1/ENGINE-HANDOFF.md)。本卡仅离线；未执行 Unity/Play，AI506 原验收累计 2/3、实际 Play0 保持，只有 CONTROL 独审后可安排最后第3轮；不得第四轮/配对续验。完整项目核验日期不提高。
<!-- /AI507-PLAY-CONTROL-20261112 -->

<!-- AI480-STANDARD-PREFLIGHT-20261112 -->
2026-11-12 TK-WF-03局部补充：Preflight-Standard.ps1是新标准预检唯一逻辑来源。Invoke-TaskTool.ps1新增可选StandardPreflightPath，在原CheckManagedTool/GRANT之后、progress start与进程启动之前实际消费；Capture只为声明调用附standardPreflight（源proof/SHA、五组、四类守卫和warning），原stdout/stderr/参数/计量保持。独立AI481只读SMOKE前其它ENGINE任务不消费新封装。

五组既有入口映射：queue CheckManagedTool→managedExecution；status/editor_status及Get-UnityCliHeartbeat原30秒语义健康→targetRoute；editor_status+公开Editor编译状态→assemblyState；SceneManager.GetSceneAt(i).isDirty→sceneSafety；官方console的session/cursor/连续seq→consoleBaseline。指定文件SHA按输入的准确路径逐项比较；不扩全路径哈希。五组不是新增验收五断言，AI481仍原两合同。

Context schemaVersion=1，intent为ReadOnly或Write；target含本轮projectPath/unityVersion/pid/port；proofs含status/editorStatus/scenes/consoleBefore/consoleAfter五个实际wrapper摘要路径；files为[{path,sha256}]，legacy可选。模板、公开SDK查询及同轮填值顺序见outputs/INFRA-011-20261109/AI-000480/attempt1。原始流必须来自同task/owner/execution的成功受管调用，不能把自报true JSON冒实际结果；源SHA只表示本次消费字节完整性，不作平台签名。

规范化同项目路径通过并记字面差异warning；真不同目标仍停。对象dirty旧观测（不新扫描）、作者逐字/时间戳字面、listener/Begin旧条件只warning。场景未知dirty/untitled不能获写，ReadOnly只观察并标writeAllowed=false。语义陈旧健康、编译失败、新Console Error/Assert/Exception、当前增量缺口、文件漂移、正式停止及实际权限拒绝保持阻断。输入不声明或声明不实的C#写集合不在本封装自动识别范围；旧未声明调用兼容，不能冒全仓旧载体已统一。当前生产/冻结/白名单外调用清单见ROUTE-MAP.md。
<!-- /AI480-STANDARD-PREFLIGHT-20261112 -->

## AI478 项目grant（2026-11-12，TK-WF-03现入口扩展）

`Invoke-TaskTool.ps1` 新增可选 `-ProjectGrantPath` / `-WriteIntentPath`。已声明的写调用必须带intent；grant可从正式Instruction的PROJECT_GRANT_PATH取默认。CONTROL在QUEUED派发前生成有期限grant，并将实际文件路径+SHA通过正式ReviseInstruction追加原完整指令，Scope不变；实际领取后执行器生成绑定owner/execution的intent。读取实际canonical CheckManagedTool及当前task，scope不超过原businessScope/当前approvedScope；不信调用者随意自报JSON。

Schema及实际原479流程/8个SDK引用/载体/两个验收项见 `.ai-workspace/outputs/INFRA-011-20261109/AI-000478/attempt1/ENGINE-HANDOFF.md`。grant共schemaVersion/grantId/taskId/ownerThreadId/projectRoot/validFromUtc/validUntilUtc/authorizationSource(path,sha256)/scope(paths,operations)九字段；intent共schemaVersion/taskId/ownerThreadId/executionId/scope五字段，UTF-8小manifest。原文SHA只证完整性，不生成签名、人类messageId或平台许可。

保留显式动作白名单：scene-save、project-settings、build、asset-move、asset-delete，以及原479的create-owned-empty-scene/single-save-owned-scene/delete-owned-smoke-scene/restore-author-scene-setup。必须逐字在正式scope和grant内；不做别名放大。原479只允许Smoke_Grant.unity及meta，不能写到别的业务场景。

Capture的可选projectGrant是项目校验证明，platformApprovalProvided=false表示不提供平台批准；传输/黄金参数、原流、计量、停止/次数保持。未传新参数的旧调用兼容，不能声称防护未声明C#写集合。项目通过不承诺Codex auto-review/sandbox放行；实际平台拒绝仍保存原文独立停止，不改入口绕过。独立原479 S40通过前其它ENGINE任务不消费新授权调用。离线Case Grant使用真实生产函数/封装和隔离正式queue，绝非平台审批模拟。

<!-- OBSERVATION-LOCK-488-20261111 -->
2026-11-11 AI488局部CODE实现交付（不提高完整核验日期）：queue内部CheckManagedTool/RecordTaskProgress在原完整状态事务保存后向Write-Result传递已验证任务/执行快照，再释放队列锁做上下文/去重/统计/输出；附加观测不重读后来执行归属，普通会计补充原executionId，未跳过finish或会计。其它动作仍沿原锁边界；正式API/schema/政策/额度/owner/scope/stop/lease/turn/幂等与跨状态提交不变，未新建入口或参数。约9MB合成历史的实际旧源反例和当前隔离52项、既有QueueWriter35/FailureUsage28项及唯一Final通过；原473a8实际锁归属仍UNKNOWN，不能冒自然Unity已消除超时。待CONTROL独立和AI489只读SMOKE，原八Fail及两额外人批耗尽，无第九轮许可。原probe486/.5和1s预算及人工值保持，详情AI488/attempt1/REPORT.md和ENGINE-HANDOFF.md；本段仅说明现实现，不改外部政策或扩充授权。
<!-- /OBSERVATION-LOCK-488-20261111 -->

<!-- VALIDATION-LAYERS-20261106 -->
2026-11-06 最新规则与处置以 AGENTS.md 页首K1～K7为准（原件SHA3F6807262B8E3FBEBDE237A8A3E04CF3DD64F54024F9AD9151B97D0B4E3B5148）。AI435正式CLOSED_SUPERSEDED，9Fail+attempt10安全Yield保留，旧余额执行许可撤销；新卡首次+2，普通管线修复CONTROL按G87自主安排，不再立余额机制卡。短探针必要REPAIR→SMK-011五项/单Play→V011-05→真人USERb；32项PRB-FULL后台non-blocking。禁截图/CI/Build作SMK门，验收期间框架冻结；三处项目诊断warning须独立修复落实，平台权限及实际错误目标不放行。此段覆盖下方旧435续行与综合版本门快照，不提高完整核验日期。
<!-- /VALIDATION-LAYERS-20261106 -->
2026-11-06 AI445 attempt2 局部机制安装（不提高完整核验日期）：真实人批明确批准安装并续行1轮，三个审批脚本及现Test-ApprovedResume已按审定SHA精确安装回读。既有ApproveResume新增显式ReassignReservedCycle/固定SHA CONTROL审查，逐次转给同根失败ENGINE一个既有HUMAN预留cycle；源成功直接CODE修复卡旧grant/claims/result不改。原execution ecf382b5c06c4611bea376b3e5a89c47 的1164检查/706正式API事件及原PID/双流明确复用，19项前置记录与有效依赖未变；本轮没有重跑矩阵或操作生产435/444。完整用法见本节“成功修复卡的同源预留余额”及[本轮报告](../outputs/PLAYABLE-DEMO011-20261104/AI-000445/attempt2/REPORT.md)。原PERMISSION Fail、两内部失败及本轮证据读取首败/RESOLVED完整保留；5/3/1、所有身份与独立停止门保持，CONTROL独立后才可使用原业务余额。

## AI514 · 暂停监管窗口中的行政关闭（2026-11-13）

沿用 `queue.ps1 -Action CloseWithoutAcceptance`：MANUAL 原语义保持；SUPERVISED 只接受 `status=PAUSED`、布尔 `stopRequested=true`、`exitRequested=false` 的显式暂停窗口。调用须提供当前绑定 `ControllerThreadId`，目标在该 session 的 `taskIds` 数组内，预约数组明确为空、全队列无 RUNNING、所有 worker 为 IDLE 且无 activeTaskId/lease；缺失或未知状态拒绝。不会切模式、领取任务或恢复停止业务。

原 `UserDecision`/Reason/ClosureReason、SUCCEEDED+result、已批准人工门、PENDING_USER/REJECTED 与幂等关闭条件全部保留。结果仍为 `CLOSED_WITHOUT_ACCEPTANCE`、`accepted=false`，不把旧失败或被替代执行改成产品验收成功；原结果、错误/许可/历史及证据保留。最终产品签收由 CONTROL 分开记录。真实关闭只由获授权 CONTROL 在满足上述条件后执行。

定向离线入口 `DevTools/Workflow/Test-SupervisedClosure.ps1 -EvidenceRoot <本任务输出根>` 只复制现有 queue 和必要库、构造隔离状态并实际调用同一 API；不对生产 runtime 关闭任务或归档。此项不改变 SupersedeTask、续行/额度或其它行政入口，不新增通用管理 API。

## Demo 构建与临时输入清理模板（INFRA-BUILD-01，2026-11-09）

两个模板的原样 argumentList 沿 AI457 attempt10 `11-build-issued-command.txt`：`@('command','--project-path',项目,'--timeout','30','eval_file',payload,'--format','json','--non-interactive')`。`eval_file` 后紧跟文件位置参数，command 选项可位于它前面；禁止 `--file` 别名，也不要求 argv[1] 就是 eval_file。生产纯函数校验项目/文件（仅规范路径比较，不改传输原文）及唯一 JSON 格式，保留该次审定的其它参数原字节；不运行 help/SDK 探测来猜命令。

`DevTools/Build-Demo.ps1 -Version demo-0.1.2 -WhatIf` 返回只读计划，默认 Boot+Street，Windows64 Mono2x、非 Development、CompressWithLz4HC + DetailedBuildReport。可传 `-Scenes` 精确已有场景列表及 `-OutputDirectory`（项目 release 的新直接子目录）；现有目录一律拒绝，包括现 `release/demo-0.1.1`。不改共享 Build 场景表、版本或后端，不导出源/准备指纹，也不调用独立清理。

未来真实执行另传 `-InvocationContextPath`：当前 ENGINE 任务已审的 taskId/executionId/ownerThreadId/evidenceRoot、payloadPath/payloadSha256、原样 argumentList，以及与参数相同的 version/scenes/outputDirectory/target/backend/development/options。payload 必须在当前任务 evidenceRoot 内，复用 AI403/AI457a10 的生产 `BuildPrepared` 准备/守卫/异步登记与结果路径；CLI 参数由该次 ENGINE 审定，脚本不猜 SDK 开关，不生成新调度器或绕过原门。原 `Invoke-TaskTool` 正式 ENGINE_CLI live-claim/目标/停止/会计门照常处理，只投递一次，无自动重试。当前版本/Mono/干净 Edit/已审 staging/编译指纹及完整作者状态保护仍是该已审 native payload 的前置，不由宿主模板代证。结果等待上限 `-WaitSeconds`（1～600，默认180），超时保全、不补清理。读取真实 flat BuildReport 的 status/buildId/result/platform/outputPath/totalSizeBytes/buildTimeMs/buildStartedAt/buildEndedAt/totalWarnings/totalErrors，**不访问 root scenes**；场景来自单独实际 build-result.actualScenes。验证原 gzip/map SHA 后以 CreateNew 写 BuildReport.summary.json 和 CHANGELOG.template.md（待填写变更/待真人验收）。

`DevTools/Clean-TempInputs.ps1 -ApprovedManifestPath <本次精确批准清单> -ApprovedManifestSha256 <SHA256> -WhatIf` 独立显示精确影响。清单 schemaVersion=1、root=`Assets/__CatalogBlobBuild_<32位小写hex>`、journalPath=`Library/CatalogBlobBuild.journal.json`、journalSha256、ownedPaths（逐项精确路径，含 root）、paths（完整明确列出 ownedPaths、各自 .meta 和 journal）；只能该 owned root 内或明确的 Assets/StreamingAssets/data、catalog.bin，不允许模式匹配、递归宿主删除或重放旧 a4 清理。真实执行同样需本次 ENGINE 已审 InvocationContextPath（task/execution/owner/evidence/payload/SHA/argumentList，并绑定 approvedManifestSha256）；再次逐项匹配实际 journal owned/root/pending，再经现生产 `RecoverOwnedInputs` 校验原物/元数据、未知写入和 dirty 门。清单与 SHA 是输入绑定，不能代替当前真人/任务路径授权或平台审批。构建与清理互不隐式触发。

两个脚本的 `-WhatIf` 在任何 CLI/queue 调用、目录/文件创建、源码导出或 native 操作之前返回；只读校验可以报错。AI460 仅离线参数/真正 WhatIf 与真实历史 flat 报告投影核验，设计 S30、正式队列 S40 分列，不扩设计范围。native 分支未在本卡执行，G102 独立 ENGINE SMOKE 待用户明确结束手动调试后安排；模板不自动成为引擎新门。

## 下一批次通用续行（2026-10-12）
<a id="approve-resume"></a>

2026-11-11覆盖：以下5/3/1及配对语义仅适用于未被新规则覆盖的开发/操作；引擎验收累计3轮，所有原因计入，不申请追加、不以独立修复配对续验。纯验收可正式配置FailureStopLimit=3；混合两类分列。见[现行验收规则](../protocol/WORKFLOW_FULL.md#acceptance-20261111)。

Status -TaskId <原卡> -FullStatus的rootRetry、ApproveResume回执与RESUME_APPROVED审计共用同一派生视图；scope/source仍绑定原grant。

当前政策（用户2026-10-19）：MANUAL每次真实人批且Runs=1；AUTOMATIC默认3、SUPERVISED默认5个失败后根周期，SCHEDULED无新默认。首次根执行/诊断0；修复和对应根续行在首次实际claim只消费一个周期，修复失败也消费。根家族跨key/卡/线程/模式累计；旧记录只按确证ID配对，不猜配或退款。

2026-10-25用户再次明确上述5/3/1政策并批准AI339权限硬停后恢复1次，原问答见[重申与本次批准](../inputs/派发/BATCH2-INDEPENDENT-20261023/AI339_用户恢复批准与续行政策重申_20261025.json)。每次新链/修复下派必须按CONTROL规划入口核实时mode和rootRetry；POLICY逐轮Runs=1与SUPERVISED根默认5/AUTOMATIC根默认3分别记录。MANUAL的自主默认0表示必须每次人批1，不是禁止合法人工续行。旧session的maxRepairsPerProblem=3/maxRepairsPerTask=6不是当前消费上限，不手改runtime纠正历史元数据。原长期授权SHA固定，原件的“待实现”只保留接收时历史；当前实现由repair-budget/approved-resume和正式根视图证明，不改原件冒充新授权。

本次CONTROL使用既有Test-ApprovedResume隔离入口一次验证当前机制，334/334通过；结果及覆盖见[政策落实证据](../outputs/control/BATCH2-INDEPENDENT-20261023/retry-policy-20261025-validation.json)。默认额度不因新卡号、错误key、chat或模式切换重新发放；普通额度内自主续行不重复询问，HARD_STOP等独立停止门保持。AI339这次HUMAN Runs=1只使用原余2中的1，不扩limit、不配对独立343、不清失败史；失败即停。

ApproveResume仍为唯一续行入口，AuthorizationSource=HUMAN|POLICY（默认HUMAN）。POLICY须核[真实原件](../inputs/续行许可分模式_用户长期授权_20261019.json)的ApprovalId、原话与SHA，每次Runs=1及稳定RetryCycleId；机器grantId与来源分开。预留不等于消费，同卡已有attempt不证明新cycle执行；消费必须绑定当前execution或实际grant claim。新周期保留旧失败/消费，配对只用最新成功execution的cycle，不能重用失败cycle。HUMAN追加只扩到真实批准所需总额，不把默认额度重复相加。

失败先正式结算并即时回传，CONTROL独立验收后核mode/scope/fresh idle再Approve及正常派发。CONTROL_REPAIR_ELIGIBLE在额度内可引用已核政策；NEEDS_HUMAN_ADDITIONAL才询问追加；HARD_STOP不得用政策解锁。POLICY不是新业务授权，CODE范围不授给DATA，运行中的卡不得为改计数重Poll或手改runtime。stop/exit、人工业务门、GLOBAL/未知写入、真实权限拒绝、120/240与工具安全边界保持。

CODE稳定key首败后本周期最多2次内部重试；新合法根周期可重新观察，但旧史和累计回合不清。AI271 attempt2六入口离线通过，CONTROL另已独立核对11合同、32场景/126原始断言；原AI266 attempt3已按最新成功cycle配对领取并成功，CONTROL独立6合同/12复核通过，根仍3/5余2。当前结果与历史首轮失败分开保存在[AI271 attempt2报告](../outputs/DETACH-B13-20261019/AI-000271/attempt2/REPORT.md)，原DATA只在CONTROL独立通过后派发。

四条硬上限以 [AGENTS.md](../../AGENTS.md#下一批次四条硬上限2026-10-12-用户决定) 为准。2026-10-12初始记录为MANUAL、watch-60暂停；当前状态以正式队列为准，本入口不触发生产重试。

唯一批准入口 `ApproveResume(task, runs=N, scope)` 对应 `.ai-workspace/queue.ps1 -Action ApproveResume`，实现共用 `approved-resume.ps1`。MANUAL/AUTOMATIC/SCHEDULED/SUPERVISED 共用同一份授权逻辑；监管暂停时须先有明确的恢复监管决定。`ResumeApprovedTask`、`ResumeCodeValidationStop` 已停止发放许可；`ResetTask` 不允许清除失败任务历史。新入队任务自动带 `resumePolicy=BOUNDED_APPROVAL_V1`，禁止从旧 `RecoverSupervisionTask` 或普通 Poll 绕过授权；该旧恢复 API 仅保留前批次记录的兼容行为。旧 `APPROVED_RESTORE` 专用卡停止新建。旧专用许可/读证据代码只用于历史记录，不是下一批次入口。

`Scope` 为 JSON：`{"paths":["Assets/Prefabs/Example.prefab"],"operations":["bind"]}`。paths 为明确的项目相对路径/目录（目录包含子路径），拒绝通配符、越界、控制状态和重定向路径；operations 为本次实际操作名称。自动包含同范围 `idempotence-check`，不授权范围外写入。用户原话、稳定消息ID `ApprovalId`、原线程 `OwnerThreadId`、控制台身份和已有的 fresh idle 证据一并记录，不要求源码/资产全量哈希或 Editor 内存证据。

- `Runs` 是允许新领取的轮数，不是每个 API 调用次数；领取时消耗一轮，幂等检查和该轮验收不再额外发许可。一次性工具仍受单次 Editor 执行上限约束。
- 同一HUMAN ApprovalId或同一POLICY来源+cycle重复提交不增额、不扩范围；POLICY来源可用于下一合法cycle，默认额度耗尽才要真实人类追加。普通失败仍先结算停止，不自动重放；用户明确“失败即停”时不得使用余量擅自继续。新的平台权限拒绝或 GLOBAL 停止封存余量，不能借旧批准绕过。
- 领取 `Poll` 必须传实际 `-Scope`；监管任务每批 `SupervisionCheckpoint` 也传实际范围。派发和自动接力消息已包含领取范围。队列检查次数、范围、原线程/路由、任务定义和失败史；这是入口约束，执行器仍须按用户批准操作，不宣称队列能拦截任意 Editor/外部工具写入。
- 不改 maxAttempts、累计失败、修复预算或人工验收；默认额度内由CONTROL引用已核长期政策，超出才须真实人类追加；续行许可本身不生成修复卡。成功任务不能靠剩余次数再领取。

### 成功修复卡的同源预留余额（AI445，2026-11-06）

仅对原SUPERVISED失败ENGINE根，显式使用既有`ApproveResume -ReassignReservedCycle -ReservationReviewPath <CONTROL审查> -ReservationReviewSha256 <SHA> -AuthorizationSource HUMAN -Runs 1 -RetryCycleId <指定旧预留>`，带原ApprovalId/完整UserDecision、相同business Scope、当前完整Instruction、原ENGINE OwnerThreadId、绑定ControllerThreadId及两车道IdlePipelines/Verification。审查必须在15分钟内，绑定原实际人批文件/总量/批准时间、已SUCCEEDED/VERIFIED直接CODE REPAIR当前完整状态/grant/claims/result、原根最新正式Fail/execution/旧grant/失败史、Scope/route/两个owner/session/controller、指定未消费cycle和完整新指令SHA；当前审查时间与原人批时间分列。新指令仅承接当前attempt/证据路径且带`[NO_NORMAL_IMPORT]`，不借余额扩大业务。源REPAIR Scope单列，不冒与ENGINE Scope相同。

源旧runs/usedRuns/claims/cycleIds/result/receipt不回写；根追加唯一`reservationAssignments`和旧grant/完整指令历史，归属、批准和审计用同一次原子queue保存。真实Dispatch/Poll仍分开，两个领取入口共用唯一归属检查，实际claim才追加`reservationClaimAudit`与一个rootExecution；批准12/3/15不变，领取13/2/15，下一个原余额领取14/1/15。同proof幂等，已消费只报告真实execution，不生成第二轮许可。源/目标/其它grant不能重领同cycle，不增cycle或ceiling、不退款。所有权限、未知、hardstop、failureStop、turn、stop/exit、active worker/dispatch和身份门保留；无opt-in走原分支。queue成功而后续监督日志/回执落盘失败视为未知，先只读核状态，不自动重放。

完整CONTROL调用、22字段schema和摘要生成要求见[交接说明](../outputs/PLAYABLE-DEMO011-20261104/AI-000445/attempt2/CONTROLLER_HANDOFF.md)。隔离回归与生产安装是分开事实，CODE不得为验证此机制批准生产435/444；CONTROL须独立验收再按原余额安排业务。

### 人批追加额度同时绑定已审指令（AI350，2026-10-26）

仅在原 SUPERVISED 根任务失败、旧 grant 已全部真实消费、车道 fresh idle 且同 session/owner/controller/Scope 时，CONTROL 可对唯一 `ApproveResume` 显式传 `-Instruction`，并同时传固定 SHA 的 `-InstructionApprovalPath/-InstructionApprovalSha256` 与 `-InstructionEvidencePath/-InstructionEvidenceSha256`。前者为 inputs 中真实 HUMAN 批准 JSON，必须绑定 task/root、消息 ApprovalId/原话、追加 Runs、批准时间、已审恢复及其 review/source SHA；后者为 inputs 中已审完整指令文本，内容按现有 `Trim()` 语义与参数一致。POLICY、空指令、未审文本、旧 ID、未消费 grant、改变 Scope、运行中/预约/停止/非原归属都拒绝。未传 `-Instruction` 保留原 ApproveResume 行为；普通 `ReviseInstruction` 仍走原诊断门。新指令和新 grant 指纹在同一队列锁、同一次 queue 写入中绑定；grant 内保存旧/新完整指令、旧/新指纹及人批/审阅 SHA。队列写成功后才追加 RESUME_APPROVED 审计；跨 queue/worker/ledger 多文件不宣称文件系统事务。重放同一许可须带同一指令/证明，仅复用未耗尽轮数，不新签 grant 或加额度；每次普通失败仍先结算，由 CONTROL 独立审阅后再走同一 ApproveResume。

### 已失败 REPAIR 子卡的人批追加与完整指令（AI387，2026-10-30）

在上述根任务路径外，`ApproveResume -Instruction` 现有严格 REPAIR 子卡分支：当前卡必须未完成、已正式失败且 `workKind`/`repairContext.nature` 均为 REPAIR，`rootTaskId`/`parentTaskId` 指向当前解析的同一保留根；根和子卡无硬停，根未运行。旧 grant 须完整消费，原 session/owner/controller/route、失败摘要/下一 attempt、每条 claim 对应的真实 execution、正式 failure 与同根已消费 cycle 均一致；最新失败须属于当前 attempt/execution，不能是 PERMISSION/UNKNOWN。旧 grant 的 Scope 与请求相同，仍核本节固定 SHA 的 HUMAN 来源、已审指令全文、批准晚于最新失败、fresh idle、预约及累计停止门。根任务原分支保持；POLICY、普通 ReviseInstruction 与同 grant amendment 没有得到新权限。

隔离回归用既有 `DevTools/Workflow/Test-ApprovedResume.ps1`：旧源对合法子卡拒绝，修复源核拒绝矩阵、同批准重放与实际 claim，根累计从 5/0/5 经新 HUMAN 批准到 5/1/6，真实领取才到 6/0/6；原根成功结果、失败史和子卡旧 grant 的 claims/cycles 保留。一次定向 Final 首轮因测试把已按既有机制写入 `revokedAt` 的旧 grant 误作逐字节相等而失败；修正断言后 446 项通过，失败原流保留。证据见 [AI387 报告](../outputs/PLAYABLE-DEMO01-20261026/AI-000387/REPORT.md)。这只证明 CODE 离线机制；生产 AI386 的新 grant、派发和实际执行仍须 CONTROL 独立验收后正式办理，AI386 用户批准的额外1次未由 CODE 消费，默认5/3/1不变。

### 直接 REPAIR 子卡的同 HUMAN grant 指令修订（AI389，2026-10-30）

`ApproveResume -AmendReviewedInstruction` 在原根任务路径外，允许已正式失败的直接 REPAIR 子卡复用**已部分消费且有预留余量**的原 HUMAN grant。`workKind` 与 `repairContext.nature` 均须为 REPAIR，`parentTaskId` 与 `rootTaskId` 均指向当前解析的同一保留根；根不得运行，根与子卡均无硬停。请求 Scope 必须与 grant Scope、子卡 `repairScope`、当前执行 `approvedScope` 完全相同；若已有 `businessScope`，仍须额外满足。每个旧 claim 必须绑定该 grant 的真实执行、正式 Fail 与同根已消费周期，其余周期仍为 reserved 且未被其它任务领取。原 session/owner/controller/route、fresh idle、最新失败和独立审查的固定 SHA、停止门及下节审查字段全部沿用。非 REPAIR、间接子卡、PERMISSION/UNKNOWN、未消费或耗尽 grant、范围/身份/历史漂移均拒绝。

成功只追加原 grant 的 `instructionAmendments` 并更新子卡完整指令和指纹；原批准、grant 身份/来源、`instructionRevision`、claims、cycles、used/reserved/limit、根结果和失败史保持。相同审查重放只读；下一次真实 claim 才消耗一个已预留周期。再次正式失败后，CONTROL 可用普通 `ApproveResume`、同一 grant 和当前已审指令续排，不新增 grant。既有隔离正式 API 回归：旧源拒绝，新源及负例通过，根 7/2/9 修订前后不变、claim 后 8/1/9；第二次定向 Final 484 项通过，首次测试断言失败与修正留证。**限制**：现有队列在同一 REPAIR 子卡第二次 claim 后，grant claim/根周期正确，但 `execution.retryCycleId` 仍可保留旧值；本入口的严格执行周期检查因此不能保证第二次失败后再次修订全文，本卡只验证本次一次修订及其后按当前指令普通续行。仅 CODE 离线候选，生产 AI386 须 CONTROL 独立验收并生成新的审查 SHA，不能由本卡代办。见 [AI389 报告](../outputs/PLAYABLE-DEMO01-20261026/AI-000389/REPORT.md)及同目录 CONTROL-CALL-EXAMPLE.md。

### 已批 HUMAN grant 剩余额度内的已审纠错（AI351，2026-10-26）

原 SUPERVISED 根任务已实际领取至少一轮、HUMAN grant 仍有已预留轮次且本轮正式失败后，CONTROL 可在独立验收失败事实和完整纠错指令后，使用**同一** `ApproveResume -AmendReviewedInstruction`。必须同时传原 `ApprovalId/UserDecision/Runs/Scope/OwnerThreadId/ControllerThreadId`、原人批 `-InstructionApprovalPath/-InstructionApprovalSha256`、新完整候选 `-Instruction/-InstructionEvidencePath/-InstructionEvidenceSha256`，以及新建的 CONTROL 独立审查 `-AmendmentReviewPath/-AmendmentReviewSha256`。不得用 POLICY、额外 grant 或普通 `ReviseInstruction` 代替；此入口只更正执行细节，不扩大业务范围或增加额度。

审查 JSON 的 `decision=APPROVED_SAME_GRANT_INSTRUCTION_AMENDMENT` 必须精确绑定 task/grant/原人批、session/route/owner/controller、原 Scope 摘要、当前 usedRuns 与根 used/reserved/limit、旧指纹及旧/新完整指令 SHA、新候选路径/SHA、最新正式 Fail receipt 路径/SHA 和其独立失败审查路径/SHA；`scopeUnchanged=true`、`executionDetailOnly=true`、`permissionOrUnknownWrite=false`。固定 SHA 的证明文件在锁内核验。最新失败为 PERMISSION/UNKNOWN、停止、非原 session、车道繁忙/有预约、旧审查、路径/内容漂移或累计门不通均拒绝。成功时只在原 grant 追加 `instructionAmendments` 历史并同步更新任务完整指令与当前指纹；原 `instructionRevision` 人批来源、grant/cycles/claims、已用/预留次数和失败史保持。相同审查路径、SHA、文本在待领取状态重放只读复用；下一次真实 claim 才消耗一轮。后续普通 Fail 仍须独立审阅，按原 grant 剩余次数与**当前**已审指令调用普通 ApproveResume，不重签许可。生产调用前须独立验收 [AI351 报告](../outputs/BATCH2-INDEPENDENT-20261023/AI-000351/REPORT.md)；精确 CONTROL 证据形态见同目录 CONTROL-CALL-EXAMPLE.md。
AI339 的人批追加 5 次包含恢复审查单：原 used5/reserved0/limit5，批准后应为 used5/reserved5/limit10；第一次领取才成为 used6/reserved4/limit10。此时 root remaining=0 表示未预约政策余量为0，已批剩余应看该 grant 的 runs-usedRuns=4；不得再加5、退款、重置六次失败或据此让执行器自续。批准原件：[HUMAN记录](../inputs/派发/BATCH2-INDEPENDENT-20261023/AI339_追加五次含恢复审查单用户批准_20261026.json)、[完整已审指令](../inputs/派发/BATCH2-INDEPENDENT-20261023/AI339_ENGINE追加五次已批恢复_20261026.txt)。AI350只交机制候选；生产 AI339 的实际批准与后继派发须 CONTROL 独立验收后执行，原七合同、场景安全门和最终人工验收不变。

### 未消费许可的显式重新核验（AI306，2026-10-22）

AI307（2026-10-22）空值形状补充：只在上述显式新分支的结构对比使用Get-UnusedGrantDigest，以ConvertTo-Json -InputObject保留null、[]、[null]、标量、单/多元素及嵌套集合的区别。不改历史Get-ResumeDigest/Get-SupervisionFingerprint；已存failureHistorySha256仍由原算法核对，不能用新结构摘要重解释历史值。不滤空、不补造art值、不改原proof/approval/request。既有runner内保留完整真实批准定义/grant数据快照，避免依赖会归档的活动outputs；隔离正式API重建历史形状和原空review集合，并保留原273/48拒绝用例。实际结果与同原请求复用前提见outputs/AI289-CLI-REPAIR-20261021/AI-000307/REPORT.md、CONTROL-CALL-EXAMPLE.md；306原通过和实际集成拒验均保留，CONTROL先正式ResolveControlRejection，再核原grant/request所有锁内条件，不把307配对成289业务新许可。

既有 ApproveResume 增加 opt-in：-RevalidateUnusedGrant -RevalidationApprovalPath/-RevalidationApprovalSha256 -RevalidationEvidencePath/-RevalidationEvidenceSha256。不开关时普通批准/claim指纹检查保持；ConfigureSupervisionTask、UpdateDependencies均不自动更新grant。原Runs、ApprovalId、UserDecision、AuthorizationSource、OwnerThreadId、完整Scope和真实fresh idle仍须传原值。新的人批ID/原话只写重新核验审计，不覆盖原业务来源、不签新grant或加次数。

仅SUPERVISED同session ACTIVE且无stop/exit、QUEUED、usedRuns0/claims空、原周期未消费且仍预留、无根retryHardStop/worker熔断/累计回合停止或交接要求时可用；同固定controller/owner/route。所有预约按taskId查队列判车道，同车道任何任务已有预约或未知预约均拒绝；须由原监管入口正式清理，不能改runtime消除。累计门用任务副本调用原Sync-TaskTurnGate，不改原execution/计数。

独立真实人批文件放inputs并核固定SHA、ID/原话、审查单SHA及前后proof SHA；proof旧定义须重算匹配旧grant，新定义须重算匹配锁内当前任务。定义只允许普通/review依赖单调追加（保留原边及顺序），以及相应reason/updatedAt；review须直接依赖。指令/标题/车道/art字段、资源/级别、人工门、未知定义字段、纯metadata修改均拒绝。每个新增边须有当前SUCCEEDED/VERIFIED、正式gate及匹配本execution的独立PASSED审阅/正式Complete；再用既有Compare-Acceptance核当前合同/证据，不信调用者true。旧run/错回执/缺证据拒绝。

新evidence schemaVersion1绑定taskId/grantId/sessionId/routeKey/ownerThreadId、stateBinding（实际历史/旧execution/业务scope/根cycles等快照）及dependencies审阅路径/SHA。只有taskFingerprint和任务unusedGrantRevalidations审计变化；原grantId/cycles/runs/claims/expectedAttempt/失败和原许可历史/根计数保持。相同人批与SHA重复检查幂等，不改queue或追加消费/审计；当前状态变化仍重新拒绝。精确schema和CONTROL操作例见outputs/AI289-CLI-REPAIR-20261021/AI-000306/CONTROL-CALL-EXAMPLE.md。CODE仅离线；原AI289仍由CONTROL独立验收后执行本来未用的许可，不在本卡操作生产289。

示例参数形态（占位值须替换为真实任务与人类决定，不能直接执行）：

```powershell
$scope = '{"paths":["Assets/Prefabs/Example.prefab"],"operations":["bind"]}'
& './.ai-workspace/queue.ps1' -Action ApproveResume -TaskId '<原任务ID>' -Runs 1 -Scope $scope -ApprovalId '<用户消息ID>' -UserDecision '<批准1轮及上述范围的原话>' -ControllerThreadId '<控制台ID>' -OwnerThreadId '<原执行线程ID>' -IdlePipelines ENGINE_MCP -Verification '<实际闲置证据>'
```

验证只维护共享 `DevTools/Workflow/Test-ApprovedResume.ps1` 及既有模式回归，不再为任务、错误或单次许可增加专属夹具。历史 `Test-ApprovedRestore.ps1`、`Test-UserCodeResume.ps1` 已退出 Workflow Final；`Test-FontSwapOnce.ps1` 已退出 Fonts Final。Final 的 Custom runner 同样检查 `DevTools/one-time-tools.json`，不能把已登记的一次性 runner 手动塞回聚合。

> 从 [INDEX.md](INDEX.md) 定位；状态与公开面沿用原登记。

<a id="tk-wf-01"></a>
<a id="tk-wf-02"></a>
<a id="tk-wf-03"></a>
<a id="tk-wf-04"></a>

## 10. WF · 工作区与留档

| ID | 名称/路径 | 能力 | 状态 | 使用边界 | 程序集／宿主 |
| --- | --- | --- | --- | --- | --- |
| TK-WF-01 | DevTools/Workflow/Archive-Stage.ps1；Archive-Policy.ps1；Get-TaskDigest.ps1；内部Read-TarXzEvidence.py | 按任务关闭/依赖与PERMANENT_DOCUMENTS清单Plan；单任务≤10MB无损ZIP、显式分类/授权/SHA审查的冗余图片/机器转储/备份处置、逐条验证及Extract；≤200字符任务简表+完整关系索引 | 沿用；2026-10-17扩充同族 | PowerShell7、MANUAL；主控审查Plan后Create/Prune，路径/reparse/漂移/活动引用复核；旧v1 Verify/Extract兼容、旧Prune拒绝绕过；说明DevTools/Workflow/ARCHIVING.md，隔离Test-DocumentArchive.ps1；AI255完整元数据/永久ZIP条目证明，默认任务全范围合入显式家族 | PowerShell／编辑器外入口 |
| TK-WF-02 | `.ai-workspace/queue.ps1` DATA API；`DevTools/DataTables/DataRound.Common.ps1`、`DataPipeline.Common.ps1`、`Run-DataPipeline.ps1` | DATA 注册、可见线程状态/原子绑定、route-key 票据防串线、U1 DONE 轮次幂等消费、共享锁、隔离 batch、明确GUI进程等待与当前run完成凭据、源/目标/完整复制范围快照冲突门和保全式回滚发布 | 沿用 | AI-000160/163/164/166/174；注册/绑定/重分类/消费仅控制端；绑定前需应用可达/闲置证据，绑定后无效路由拒绝回退；当前CSV验证模式强制不发布且不伪造DONE；隔离 batch 接 PRE-1 字体构建但字体不进入生成物事务；AI164正常宿主两真实用例0/1差异及独立nonce已通过；DATA/ENGINE 共享全程锁；不提交 | PowerShell／编辑器外入口 |
| TK-WF-03 | `DevTools/Workflow/UsageMetrics.ps1`、`Initialize-TaskUsage.ps1`、`Invoke-TaskTool.ps1`、`Get-UsageReport.ps1`、`Invoke-UsageReporting.ps1`；`DevTools/Verify-All.ps1` | 任务生命周期真实/缺失用量 JSONL、可选工具包装、日/周聚合与按 profile 的统一验证入口 | 沿用 | AI-000157/173；不建第二队列，不估算 token/容量；`runtime/usage-YYYYMM.jsonl` 只追加；离线测试必须传临时 RuntimeRoot/StateRoot；Final 默认两次，超限留原因；子runner固定正常PS7 `-NoProfile -File`，不传ExecutionPolicy绕过参数 | PowerShell／编辑器外入口 |

TK-WF-04（PRE-5b）：DevTools/Workflow/watch.ps1、Manage-WatchHost.ps1、Compare-Acceptance.ps1、Import-ProviderUsage.ps1、Import-CoplayUsage.ps1、Summarize-CoplayLog.ps1 与队列修复预算门。WATCH 只观察、不领取；单轮或5分钟宿主定时，持久增量 inbox 与单批次适配器回执；桌面事件唤醒尚无正式接口，部署由控制台负责。Complete 强制预声明机器验收，根修复跨模式最多2轮；provider仅精确关联真实事件，Coplay无日志为null。隔离夹具见 Test-Watch/Test-Acceptance/Test-RepairBudget/Test-CoplayUsage。

TK-WF-04 回传修复（2026-10-05）：`Manage-SupervisionHandoff.ps1` 的 Prepare/Record/Receive 绑定、一次发送与控制接收去重；`Test-SupervisionHandoff.ps1` 纳入 Workflow。使用 `DevTools/Workflow/HANDOFF.md`；WATCH事件、app发送、控制验收分别计量。

AI208扩展TK-WF-03/04：queue.ps1 Sync-TaskTurnGate累计120软限/240临时保护/30分钟无实际进度；WorkCompleted证据SHA去重，观察不刷新计时。RejectVerification绑定原结果，ResolveControlRejection绑定完成修复与独立证据；取消scope不改成功。Test-TurnSoftRepair及Test-ControlCompletedReview纳入Workflow，保留UTC/预算/回传回归；正式硬限待瘦身后两周实测。旧AI201的120硬停为历史实现，已被本合同替代；其REJECTED历史保留，2026-10-05 20:39经独立控制复核后正式ResolveControlRejection解决本次阻断。

GOV-02 补齐软限实际链路：queue.ps1 WriteHandoff/CheckManagedTool、supervised.ps1 Yield/SHA接棒、Invoke-TaskTool调用前门禁、WATCH HANDOFF_STALLED/TURN_LIMIT路径；在固定职能chat接棒并保留累计历史。只覆盖受管入口；既有Test-TurnSoftRepair/Test-Pre5c/Test-Watch隔离夹具承担边界回归，不新增专用续行入口。

TK-WF-04 2026-10-06：ResumeApprovedTask + approved-resume.ps1 为原ENGINE/DATA失败任务提供一次证据绑定的获批续行，卡/恢复配对预算与失败历史不变；Test-ApprovedResume纳入Workflow。不是自动重试、不是清失败/人工签收。

AI-000231 增量（同一 TK-WF-03/04，不另建工具）：`Status` 加状态计数、优先注意项及可复用的落盘全文路径，`-Full`兼容显式全文；`PendingReview`默认小摘要、`-Full`取旧明细。`Get-UsageReport`增加逐动作最大返回字节，Status无观测时为null及原因；测试沿用`Test-UsageOptimization`，不增加并行夹具或重发旧周报。

已关闭任务和控制端批次的完整说明/报告在[归档索引](../archive/README.md)检索；[永久任务简表全文](../archive/tracking/TASK_DIGEST.md)保存在归档区，仅供用户/CONTROL复盘。活动区保留当前必要入口和未结事项，历史证据链接不等于活动依赖。

<a id="tk-wf-05"></a>
## TK-WF-05 · 显式 Git 内容比较

复用真实Git clean及既有字体guard/Verify-All Custom入口，原字体能力不承担一般文本归一化；无需新库、队列/保护架构或平行runner。`test-git-content.py`/`Test-GitContent.ps1`真实双clone242项及原字体37项通过；新增中文ASCII JSON协议/配置边界，E2退休冻结两条夹具期望经真实用户批准更正，前四失败/旧grant不变。详细支持范围、调用/诊断与生产安装状态见[GitHooks README](../../DevTools/GitHooks/README.md)，证据[attempt5报告](../outputs/DETACH-B13-20261019/AI-000276/attempt5/REPORT.md)。原卡用户S40保留，实际累计9文件及新长期工具通常L的差异在报告记账，120/240门不变。

### CLI argv 黄金组（INFRA-CLIARG-01，G102离线）

沿用现TK-WF工具链：Invoke-TaskTool公开参数为-ArgumentList，Get-TaskToolArgumentPlan只将命令路径/原argv组为现进程参数；.ps1沿现PowerShell7解析路径加-NoProfile/-File前缀，其余字符串（含空项、引号、Unicode、多行、尾反斜线）逐项保留。不拼shell命令、不改许可/守卫/会计，不自动把错误选项改正确。

- fresh status：status --project-path <当前项目> --format json --non-interactive；不带--timeout。只有明确请求等待ready时，--until-ready与--timeout一起提供。
- Console：command console --tail 2000 --since <cursor> --since_session <原session>加原项目/timeout/format参数；禁止--since-seq和--session。原返回session/dropped/reset含义保持。
- 包装调用用-ArgumentList @(...)；-CommandArguments不存在，457a9在CLI启动前绑定失败，不加兼容别名。
- preflight/prepare/cleanup/edit/readback/build是角色；现Street与Build路径多用command eval_file，启动Ready RPC用command eval。capture对应capture_game_view，play/stop对应editor_play/editor_stop；不编造prepare/build等CLI子命令。具体角色源仍由原任务审定，黄金argv不是执行许可，CapturePng等原门保持。

聚焦离线入口：Test-EngineCli.ps1 -Case ArgumentGolden -OutputRoot <本任务输出>。黄金表保存参数数组、参数成功路径的期望exit和有来源的字段骨架；CLI参数错误由透明窄桩重现，模型不生成或验证CLI输出。已知JSON字段逐例引用历史真实成功原流SHA；只有SDK类型声明的例保留明确局部字段，CLI外层/位置及未列继承字段unknown。help/version未请求JSON，按默认文本面记录，精确布局/exit unknown待SMOKE，不能硬套JSON外壳。wrapper绑定用实际ParamBlock/PowerShell binder和无副作用桩体。没有运行官方CLI/Unity，也不证明当前原生数据/时序/截图/Build。G102：ENGINE独立SMOKE明确待用户手动调试结束，离线结果不得冒已过或作为新引擎门投入使用。旧失败/脚本原样保留，参见P-070与AI458交付。

### 未领取的空格拼接 Scope 登记纠正（AI530，2026-11-14）

沿用正式 `queue.ps1 -Action ReviseInstruction`，可选 `-Scope` 只用于已审定的“原多路径被登记成一个空格串”。不带Scope时仍是原正文修订行为。此分支不修正文、不改等级/前置/职能/模式/次数/历史；成功仍返回INSTRUCTION_REVISED，另有scopeCorrected=true。保存前后的Scope、原定义摘要、理由、当前controller/session及审查路径/SHA追加原INSTRUCTIONS ledger中的UNCLAIMED_SCOPE_CORRECTED审计。

仅当前SUPERVISED ACTIVE、无stop/exit、绑定CONTROL显式调用，目标在session完整taskIds内；目标须QUEUED/BLOCKED_DEPENDENCY、FEATURE根、attempts=0，无领取/execution/失败/续行grant/修复family或派发预约、无独立停止。已领取527一律不倒改。不会通过本入口消费/重置重试额度或解除停止；不走ApproveResume伪造失败。CODE只交付本API和自有隔离测试，不实际纠正528；CONTROL独审、fresh idle后另行正式安装。

公开参数示例（由CONTROL按当前原件填值）：

```powershell
& .ai-workspace/queue.ps1 -Action ReviseInstruction -TaskId $task.id `
  -Instruction $task.instruction -ControllerThreadId $controllerId `
  -Scope $candidateJson -AmendmentReviewPath $reviewPath `
  -AmendmentReviewSha256 $reviewFileSha256 -UserDecision $review.reason
```

必须传原Instruction逐字全文，不可同时传Complexity。Scope的paths必须为审定的原顺序多个字面路径，按一个空格Join后与旧businessScope.paths唯一字符串逐字相同；各路径再走原ConvertTo-ResumeScope保护。候选规范化后不能去重缩为更少路径，operations必须与旧规范化列表一致。正常合法含空格的单路径不自动拆分；原顺序清单明确独审才可纠正，路径扩大/改operation/换正文均拒绝。

复用AmendmentReviewPath/Sha256传审查JSON，文件必须在当前项目`.ai-workspace/outputs/control/`，SHA必须为真实文件大写SHA256；原Assert-ResumeFile拒绝漂移和重定向，原严格JSON读取拒绝重复/多余/缺失字段。证明恰含下列12字段：

```json
{
  "schemaVersion": 1,
  "decision": "APPROVED_UNCLAIMED_SCOPE_CORRECTION",
  "taskId": "AI-000528",
  "sessionId": "当前正式sessionId",
  "controllerThreadId": "当前绑定CONTROL",
  "taskSha256": "Get-ResumeDigest对当前完整task对象的摘要",
  "instructionSha256": "Get-InstructionTextSha256对原Instruction UTF-8原文的摘要",
  "oldScopeSha256": "Get-ResumeDigest对当前businessScope对象的摘要",
  "candidateScopeSha256": "Get-ResumeDigest对候选ConvertTo-ResumeScope规范化输出的摘要",
  "literalPaths": ["逐项原顺序字面路径，不由空格Split猜测"],
  "independentlyReviewed": true,
  "reason": "独立核对该字面清单的具体理由；必须与UserDecision相同"
}
```

摘要沿用现有实现：Get-ResumeDigest是PowerShell7的`ConvertTo-Json -Depth 50 -Compress`所得UTF-8无BOM字节SHA256（保持对象属性/数组顺序），Instruction摘要直接取UTF-8原文字节；文件SHA是审查JSON落盘后的真实SHA。候选规范化会排序路径及operation并保留idempotence-check，所以literalPaths额外绑定原顺序；不据排序后清单反推旧串。应在正式queue锁外只读最新task、完成独审并保存证明，若调用前定义漂移则重审，不能替换摘要绕过门。证明只记录CONTROL的显式审查，不是平台签名或用户代签。

窄回归：`Test-ApprovedResume.ps1 -UnclaimedScopeCorrectionOnly -FixtureRoot <本任务全新绝对路径>`，或既有环境变量EATWHAT_APPROVED_RESUME_GROUP=UnclaimedScopeCorrection；证据仍用EATWHAT_APPROVED_RESUME_EVIDENCE。真实调用隔离queue，不读写真实528/其它family；新组覆盖合法列表（含内部空格）、缺失/漂移审查、定义变化、越界候选、已有grant/领取/预约、错误controller/停止session和原无Scope修订兼容。原CurrentHumanRepairScope及默认大矩阵原断言保持，本卡未运行旧大矩阵，不冒称全量工作流验收。

## 已核程序集与文件定位

按当前 `.asmdef`／最近 `.asmref` 解析；下列只是源码归属，不代表重新编译、运行或验收。

| 程序集 | 定义／归属入口 | 本页相关源码例 |
| --- | --- | --- |
| `Editor.Project` | [Assets/Editor/Editor.Project.asmdef](<../../Assets/Editor/Editor.Project.asmdef>) | [FontSwapOnce.cs](<../../Assets/Editor/Fonts/FontSwapOnce.cs>) |
| `Editor.ProjectValidation` | [Assets/Editor/ProjectValidation/Editor.ProjectValidation.asmdef](<../../Assets/Editor/ProjectValidation/Editor.ProjectValidation.asmdef>) | [ShortCycleProbeCi.cs](<../../Assets/Editor/ProjectValidation/ShortCycleProbeCi.cs>) |
| `Toolkit` | [Assets/Scripts/DataBase/Toolkit.asmref](<../../Assets/Scripts/DataBase/Toolkit.asmref>) | [Data.cs](<../../Assets/Scripts/DataBase/Data.cs>) |

## 历史

[本页整理前全文与历史记录](<../../docs/archive/工程事实_20261124/history/.ai-workspace/toolkit/WF.md>)；[归档与原字节恢复索引](<../../docs/archive/工程事实_20261124/README.md>)；[已知不符与保留边界](<../../docs/archive/工程事实_20261124/差异与保留边界.md>)。
