# 工具库 · WF

2026-10-22 AI308 / G87 CLI SceneHandle 调用修复（独立 pipeline-repair，关联原290）：安装的 Unity6000.3.24f1 中 Scene.handle 类型为 UnityEngine.SceneManagement.SceneHandle，公开到 Int32 的转换；构造 JObject 时先写 ["handle"]=(int)scene.handle，保留每个已加载场景的实际标量身份及原 path/dirty 字段。直接写 ["handle"]=scene.handle 在 JObject 构造期即因 SceneHandle 无法隐式转 JToken 报 CS0029，后续 UnityEvidenceJson converter 无法兜底该编译错误。两份原完整 eval caller 均为预期失败负例，两份仅做该投影及当前任务身份/路径参数化的候选在真实 UnityEditor.CoreModule/CoreModule/Newtonsoft 引用下离线编译；原 helper 七类型及严格 JSON/共享反射 caller 回归保留。CODE 无原生调用，后续须 CONTROL 独立验收并正式安排只读 SMOKE。证据见 [AI308 报告](../outputs/AI290-CLI-CALLER-REPAIR-20261022/AI-000308/REPORT.md)。


2026-10-22 原生接入更新：AI303 attempt3已CONTROL独立5合同/13事实通过；18次官方CLI、共享Ready一次RPC/400重试0、298反射结构体和同fresh run三张screen/no-save原PNG均核实，Stop clean Edit/新增ConsoleError0。原305两处调用参数须用实际动作常量；原生通过不代表人工视觉通过，慢启动/旧400根因仍未证，PackageCache及native save_path/Refresh分支未改。证据[原生独立验收](../outputs/control/AI289-CLI-REPAIR-20261021/independent-review-303.json)，具体调用复用304与303 attempt3交接；下方带日期待验状态保留为历史。

2026-10-21 AI304（同AI303根的REPAIR，POLICY AI303:CODE304:retry1，pipeline-repair / CLI(磨合)）：以下覆盖AI300的等待数值及AI299的裸回执假设，旧记录保留。StartupReady默认45秒、允许1～60秒、250ms最多241次本地观察；Arm整体窗口90秒，入口Arm仍须30秒内，原健康新鲜度、精确身份/根/once marker及3次固定只读启动400、500ms间隔、CLI5秒/客户端≤7秒均保持。统一保留官方默认--format json完整封装：success/command/data/errors/warnings，data.command/parameters/result/target；eval再核data.result.success/result/output/diagnostics，capture_game_view直接核data.result的CaptureResult。严格单JSON、各层重复键、错误层/缺层、命令/项目/端口/回显code均失败闭合；不递归找result、不使用--result-only。CapturePng新增必填正数CaptureTargetPort，来自已核健康目标端口；仍source=screen/1024×576/no-save，完整PNG校验先于CreateNew且不改原字节。一次ProtocolRepair Final通过274断言/7组；收尾将真实303重放限于ProtocolRepair，通用ReadyHandshake另一次Final55断言通过（无失败/重试），避免通用测试依赖任务归档路径。前者含28.457秒合成延迟、真实303 eval原流重放和旧68B坏zlib拒绝；原流为未启动preflight，不能算Ready或截图。AI303实测Arm→Ready28.4569654秒、该轮RPC/capture/400均0；慢启动和旧400根因未证。CODE未调用Unity/CLI；CONTROL独立验收后才可正式配对续行303 fresh attempt。交接及边界见.ai-workspace/outputs/AI289-CLI-REPAIR-20261021/AI-000304/REPORT.md、CALL-EXAMPLE.md、ENGINE-HANDOFF.json。

2026-10-21 AI301 / G87-METRICS：仅扩充既有wrapper UsageCategory的pipeline-repair枚举，原setup/task/verification/default保持，实际ToolSource独立。Get-TaskDigest可选ClassificationMapPath+ClassificationMapSha256读取输入区批准映射，核固定SHA、批准来源SHA、taskId唯一/在队列；有映射时追加线标记/语义分类，索引保留来源，映射actualTool始终忽略，旧token/许可/失败史及原usage不重写。一次定向Final：Test-EngineCli UsageClassification12断言、Test-DocumentArchive原简表+新分类21断言通过，4合同待CONTROL独立验收。UsageMetrics/Import-ProviderUsage/Get-UsageReport未改，真实类别分组由既有实现保留；旧CODE_STOP入账缺口不伪造修补。本卡仅隔离测试，未写默认生产简表/账目；说明DevTools/Workflow/USAGE_REPORTING.md及.ai-workspace/outputs/AI289-CLI-REPAIR-20261021/AI-000301/REPORT.md。

2026-10-21 AI300 / REPAIR-CLI-05（CODE离线，独立pipeline-repair / CLI(磨合)）：Invoke-TaskTool显式StartupReadyContextPath模式通过现有正式Status API核当前task/attempt/owner/execution与scope，消费同execution/run/root的一次性启动标记；复用当前任务已有一次Get-UnityCliHeartbeat快照，不追加status/editor_status健康轮询。Pipeline /api/status不是验证宿主Ready，使用fresh validation-host.log严格enabled/factory1/Ready及目标场景/路径/时序，再发wrapper生成的固定标量只读eval；调用方argv/截图混用拒绝。仅该探针、30秒启动窗口内、精确已观察COMMAND_FAILED HTTP400且无stderr时有界重试，最多3次、间隔500ms，重试前再核宿主日志；其它调用/写操作/权限/503/UNKNOWN均不重试。Ready默认15秒（只允许调低至1秒）、250ms间隔/最多61次本地读，RPC默认5秒、客户端进程至多7秒且受剩余窗口约束。ProcessTimeoutSeconds为现有CaptureOutput分支的0～7秒可选上限，默认0保持旧行为，超时记录UNKNOWN而不重试。既有ReadyHandshake定向Case第二次Final完整32断言通过，首次UnityEditor重复引用编译失败及原流保留；实际传输探针对真实Unity6/ShortCycle DLL离线编译通过。延迟和400为显式合成夹具，不证明原AI289/a8的400根因；原根因仍未知，native待AI302。调用与边界见.ai-workspace/outputs/AI289-CLI-REPAIR-20261021/AI-000300/REPORT.md。

2026-10-21 AI299 attempt2 / REPAIR-CLI-04（POLICY AI299:retry1，独立pipeline-repair / CLI(磨合)，不扣原289）：共享Invoke-TaskTool新增显式CapturePng、CaptureTaskOutputRoot和CaptureSavePath；保存意图可绝对/工作区相对/省略（任务根/<progressId>.png），任务根须与正式卡businessScope相同，恢复approvedScope同时约束PNG和原流。capture_game_view缺opt-in及native save_path/camera/max_resolution等冲突启动前拒绝；只接受同官方source=screen/no-save裸CaptureResult及成功进程，argv不改写，原始双流保留。PNG在CreateNew前核严格UTF8/单对象/元数据/base64、签名/分块CRC/IHDR、连续IDAT的有界ZLibStream完成/Adler32/扫描行长度与filter及Bitmap解码；防读前探隐藏尾随或截断，不完整输入明确抛错。仅支持非交错8bit gray/RGB/gray-alpha/RGBA，宽高1～4096，解压量由IHDR限至扫描行字节+1，不重编码。路径拒逃逸/ADS/链接/未知覆盖，落盘核原字节。既有CapturePng定向Case本execution一次181断言完整通过，原14负例和其它Case主体保持；原三失败/停止/ZIP不删除。仅CODE离线，五合同待CONTROL独立验收；官方缓存未改，旧native save_path分支仍使用ProjectPaths/Refresh，受管入口禁用该分支，不称已native。调用说明和原件见.ai-workspace/outputs/AI289-CLI-REPAIR-20261021/AI-000299/attempt2/REPORT.md。

2026-10-21 AI298 attempt2（POLICY AI298:retry1，实际claim1/5；不扣原289）：既有EvidenceJson Case以ProcessStartInfo.ArgumentList并发分流原始stdout/stderr、退出码及各自BOM/UTF8解码；仅起始BOM允许，严格单JSON对象拒绝损坏UTF8、尾随内容/对象、重复/尾部BOM，字符串内UFEFF保留。MONO_PATH仅设子进程。一次定向完整165结构体+14驱动/帧/caller断言通过，原helper源码与原结构体/反例不变；无helper静态引用caller真实编译并反射载入哈希命名DLL回传JToken。五条真实离线进程stderr0；caller编译CS0162在stdout留证，旧合并BOM来源不倒推。原三失败/停止和根attempt1报告完整保留，稳定key已RESOLVED；四合同待CONTROL独立验收，Editor/官方CLI/SMOKE仍未执行。交付.ai-workspace/outputs/AI289-CLI-REPAIR-20261021/AI-000298/attempt2/REPORT.md。

2026-10-21 AI298 / REPAIR-CLI-03（独立pipeline-repair / CLI(磨合)，FEATURE技术根）：DevTools/Workflow/UnityEvidenceJson.cs提供Serialize/Deserialize，真实Vector2/3/4、Quaternion、Rect、Color、Bounds使用显式标量converter；Bounds存center/extents避免派生size往返舍入，Ignore仅用于其它引用环兜底。字段缺失/额外/重复和Single溢出明确拒绝，非有限值以既有JSON字符串标签往返。helper留Assets外，Test-EngineCli EvidenceJson用安装Unity6/Newtonsoft及Mono离线编译/执行；官方eval支持object Execute与反射，提供哈希命名DLL+Assembly.Load(byte[])最小调用，不需导入/重载，返回JToken避免后续默认序列化再遇Unity结构体。当前三次离线自验证均失败：首次Mono搜索路径冲突，后两次在合并输出的额外BOM读取处停止；真实结构体程序有165断言PASS输出，但反射载入caller未执行，sharedCaller合同未通过，候选不得视为可用于SMOKE。停止修复/测试，保留全部失败并按稳定key正式熔断回传；不声称Editor/SMOKE已验；报告.ai-workspace/outputs/AI289-CLI-REPAIR-20261021/AI-000298/REPORT.md。

2026-10-21 AI297 / REPAIR-CLI-02（独立pipeline-repair / CLI(磨合)，FEATURE技术根）：Invoke-TaskTool在原CheckManagedTool门之后、Report-Progress/start与进程启动之前，对整个ArgumentList做strict UTF8→escaped JSON→UTF8→反序列化逐参数Ordinal/字节自检，传给原生的仍是原字符串，不做shell拼接/改写。原生NUL及C0除TAB/LF/CR外拒绝，未配对UTF16拒绝；异常列argument/character/utf8Byte及码点，不回显完整载荷。允许多行脚本和合法JSON转义的全部U0000～001F；往返不证明语义正确，也不修复literal z等输入错误。旧AI289/a4文件在wrapper前已有两个U0008，不据此归因ArgumentList。Test-EngineCli新增PayloadBytes定向Case，真实wrapper/进程收参76/76断言一次通过，参数和utf8Byte位置均从0计；原门/mutex/批准/路由/回合保持。原生Unity/SMOKE待后继独立执行；报告.ai-workspace/outputs/AI289-CLI-REPAIR-20261021/AI-000297/REPORT.md。

2026-10-21 AI296 / REPAIR-CLI-01（G87，独立pipeline-repair / CLI(磨合)，FEATURE技术根，不扣原AI289）：Save-TextFile同目录PID+GUID临时路径，完整UTF8无BOM写入后用File.Move(src,dst,true)发布，沿用原mutex及所有批准/路由/消费/回合门。失败上抛并保留本次临时文件与原目标，不预删/吞错/重试/旧快照回写；历史queue-state.json.tmp.67176只登记不提升/删除。Test-EngineCli新增QueueWriter定向Case（EATWHAT_ENGINECLI_TEST_CASE），All/FailureUsage保留，真实共享函数、隔离queue及真实wrapper验证新建/覆盖/遗留证据/锁失败和start失败不启动进程。本卡一次定向18/18断言通过，原始输入/错误/temp/源副本保留；原Move-Item底层锁/并发根因和后续原生SMOKE仍未证明，交CONTROL独立验收；报告.ai-workspace/outputs/AI289-CLI-REPAIR-20261021/AI-000296/REPORT.md。

2026-10-15管线继承（既有队列工具扩充）：BindArtAuditAutomation增加可选-PipelineThreadId，配合-UserDecision/-Verification在ART完全空闲且目标不与其它角色复用时更新threadId并追加routeHistory，原日报jobs/成功基线/enabled不变。CODE/ENGINE用BindExecutionPipeline，DATA用BindDataPipeline；新地址与日程映射以PIPELINES为准。不是新的TK或业务续行入口。


2026-10-15 GOV-04后续AI-000245：Get-ThreadContext优先读精确线程原生token_usage_record.payload.usage.input_tokens，有界发现/尾读/缓存及30分钟时限；旧provider只可新鲜回退，显式fresh字节为估算，再无则null+原因。queue普通正式回执以显式ControllerThreadId/OwnerThreadId采样，MANUAL Status可承载提示；活跃监管心跳仍按heartbeatId去重。样本去重缓存由UsageMetrics管理；WATCH只在活动scope消费新鲜worker/console STRONG转入，Read-WatchInbox核绑定控制台并Ack，不自动重载/Yield/换chat。单次input不是完整窗口或任务计费；下述旧实现描述为历史，不重发旧周报。

## 下一批次通用续行（2026-10-12）
<a id="approve-resume"></a>

Status -TaskId <原卡> -FullStatus的rootRetry、ApproveResume回执与RESUME_APPROVED审计共用同一派生视图；scope/source仍绑定原grant。

当前政策（用户2026-10-19）：MANUAL每次真实人批且Runs=1；AUTOMATIC默认3、SUPERVISED默认5个失败后根周期，SCHEDULED无新默认。首次根执行/诊断0；修复和对应根续行在首次实际claim只消费一个周期，修复失败也消费。根家族跨key/卡/线程/模式累计；旧记录只按确证ID配对，不猜配或退款。

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

| ID | 名称/路径 | 能力 | 状态 | 使用边界 |
| --- | --- | --- | --- | --- |
| TK-WF-01 | DevTools/Workflow/Archive-Stage.ps1；Archive-Policy.ps1；Get-TaskDigest.ps1；内部Read-TarXzEvidence.py | 按任务关闭/依赖与PERMANENT_DOCUMENTS清单Plan；单任务≤10MB无损ZIP、显式分类/授权/SHA审查的冗余图片/机器转储/备份处置、逐条验证及Extract；≤200字符任务简表+完整关系索引 | 沿用；2026-10-17扩充同族 | PowerShell7、MANUAL；主控审查Plan后Create/Prune，路径/reparse/漂移/活动引用复核；旧v1 Verify/Extract兼容、旧Prune拒绝绕过；说明DevTools/Workflow/ARCHIVING.md，隔离Test-DocumentArchive.ps1；AI255完整元数据/永久ZIP条目证明，默认任务全范围合入显式家族 |
| TK-WF-02 | `.ai-workspace/queue.ps1` DATA API；`DevTools/DataTables/DataRound.Common.ps1`、`DataPipeline.Common.ps1`、`Run-DataPipeline.ps1` | DATA 注册、可见线程状态/原子绑定、route-key 票据防串线、U1 DONE 轮次幂等消费、共享锁、隔离 batch、明确GUI进程等待与当前run完成凭据、源/目标/完整复制范围快照冲突门和保全式回滚发布 | 沿用 | AI-000160/163/164/166/174；注册/绑定/重分类/消费仅控制端；绑定前需应用可达/闲置证据，绑定后无效路由拒绝回退；当前CSV验证模式强制不发布且不伪造DONE；隔离 batch 接 PRE-1 字体构建但字体不进入生成物事务；AI164正常宿主两真实用例0/1差异及独立nonce已通过；DATA/ENGINE 共享全程锁；不提交 |
| TK-WF-03 | `DevTools/Workflow/UsageMetrics.ps1`、`Initialize-TaskUsage.ps1`、`Invoke-TaskTool.ps1`、`Get-UsageReport.ps1`、`Invoke-UsageReporting.ps1`；`DevTools/Verify-All.ps1` | 任务生命周期真实/缺失用量 JSONL、可选工具包装、日/周聚合与按 profile 的统一验证入口 | 沿用 | AI-000157/173；不建第二队列，不估算 token/容量；`runtime/usage-YYYYMM.jsonl` 只追加；离线测试必须传临时 RuntimeRoot/StateRoot；Final 默认两次，超限留原因；子runner固定正常PS7 `-NoProfile -File`，不传ExecutionPolicy绕过参数 |

2026-10-14 GOV-04 扩展既有 TK-WF-03/04：Get-ThreadContext 只读线程最近精确 provider 响应 inputTokens，或对显式 fresh 字节给出标记的粗估；缺源 null。queue Poll/Complete 各记一次 contextTokens/contextSource，Complete 回执附 NONE/ADVISE/STRONG；RecordSupervisionHeartbeatContext 仅活跃控制台心跳、每 heartbeatId 一次，get_usage_limits 原始引用属全账号快照而非任务账单。Get-UsageReport 分 role/source 报上下文覆盖和 STRONG；WATCH CONTEXT_HIGH 仅阈值转入时入箱，不自动重载或 Yield。定向夹具 Test-ContextWarning；不重发旧周报。

2026-10-14 GOV-05 继续扩充既有 TK-WF-03/04：Enqueue必填S/M/L，预算由40/80/120派生；ReviseInstruction可正式补登记QUEUED旧卡，已运行旧卡按原规划来源兼容Complete，历史完成不回填。Complete记录四类空转、真实app turn去重比例及超额说明；未知null+原因，预算不改变GOV-02门。Get-UsageReport分级汇总真实覆盖、均值、超额和缺失。定向夹具Test-TaskTier，不建新TK、不进独立Final。

2026-09-17：增加阶段归档工具；配表v1.2兼容夹具固定到DevTools/DataTables/Fixtures，AI65历史审计改为按需提取图证。完整旧登记表在阶段ZIP中，所有原有TK编号及当前能力表保留。

TK-WF-04（PRE-5b）：DevTools/Workflow/watch.ps1、Manage-WatchHost.ps1、Compare-Acceptance.ps1、Import-ProviderUsage.ps1、Import-CoplayUsage.ps1、Summarize-CoplayLog.ps1 与队列修复预算门。WATCH 只观察、不领取；单轮或5分钟宿主定时，持久增量 inbox 与单批次适配器回执；桌面事件唤醒尚无正式接口，部署由控制台负责。Complete 强制预声明机器验收，根修复跨模式最多2轮；provider仅精确关联真实事件，Coplay无日志为null。隔离夹具见 Test-Watch/Test-Acceptance/Test-RepairBudget/Test-CoplayUsage。

2026-09-22：TK-WF-01 增加可配置常驻输入白名单与 `SelfTest`，避免排布指导书、Schema、需求基准、模块/路径主本和 USER 人工状态在阶段归档时被移动。

2026-09-23：AI-000157 新增 TK-WF-03，用现有 queue/supervised 生命周期挂钩记录实际可见数据；provider 未暴露项保持 null+原因，并由统一验证入口复用既有 runner。

2026-09-23：AI-000160 新增 TK-WF-02；沿用同一队列与监管生命周期，不建立第二队列。AI-000174 在同一 worker 状态中增加可查询、幂等、带前后值/UserDecision/Verification 审计的固定 DATA 线程绑定；预约保存 route key，换绑/旧票据/忙碌状态均安全拒绝。

2026-09-23：AI-000163 加固 TK-WF-02：事务按“未动live/已移动/已提升/回退失败”显式保全，增加目标与输入快照漂移门、固定Unity版本/隔离目录和不带USER轮次的只验证入口；真实Unity结果不由离线回归代签。

2026-09-23：AI-000166 再加固 TK-WF-02：Windows GUI batch 使用本次 Process 句柄等待/取码，锁保持到退出与凭据核验；报告拆分真实可空进程码和Runner码，完整输入快照对齐五个复制根，真实GUI延迟夹具覆盖非零/成功/缺旧凭据与锁生命周期。未启动Unity，AI164仍需DATA恢复复验。

2026-09-23：AI164已在两个精确命令各经正式审批的正常宿主通过Unity6000.3.24f1真实baseline/overlay，退出0/0、不同nonce、0/1生成差异、主Editor共存和生产不变。受限宿主LocalLow/许可失败的旧记录保留；零diff时空JSON缺失记录WARNING，独立清单补证，不新增工具或擅改运行器。详见 `.ai-workspace/archive/2026-10-17-workspace-correction/readable/.ai-workspace/outputs/U1-PRE/AI-000164/host-context-recovery-20260923/`。

2026-10-04：TK-WF-03增加 `DevTools/Workflow/Invoke-UsageReporting.ps1`、`USAGE_REPORTING.md`、`Test-UsageReporting.ps1`，原ai固定前置调用；北京时间周三上一整周、漏报补齐、真实补发标记、源快照和幂等原子发布。Get-UsageReport支持同一Records快照并将单次/合计排名分开。只写outputs，暂停后不自行Resume；保持原计量null口径。

2026-10-05 PRE-5c / AI-000201：TK-WF-03 增真实 app turn 绑定计数、跨 attempt 120 上限、provider 响应单列、明确分母 cache 比率；`RecordCodeValidation` 按稳定 key 留首败/重试/解决证据。TK-WF-04 增 `Get-UnityHeartbeat.ps1`、`Read-WatchInbox.ps1`；WATCH 读真实 worker 进度和嵌套监管等级，新增 WORKER_PROGRESS/UNITY_STATE_CHANGED/TURN_LIMIT 幂等事件。`Manage-WatchHost.ps1` 可选只读采集但未部署。长期回归 `Test-Pre5c.ps1`、`Test-ControlAdministration.ps1`、`Test-UserCodeResume.ps1` 纳入 Workflow profile。

TK-WF-04 回传修复（2026-10-05）：`Manage-SupervisionHandoff.ps1` 的 Prepare/Record/Receive 绑定、一次发送与控制接收去重；`Test-SupervisionHandoff.ps1` 纳入 Workflow。使用 `DevTools/Workflow/HANDOFF.md`；WATCH事件、app发送、控制验收分别计量。


AI208扩展TK-WF-03/04：queue.ps1 Sync-TaskTurnGate累计120软限/240临时保护/30分钟无实际进度；WorkCompleted证据SHA去重，观察不刷新计时。RejectVerification绑定原结果，ResolveControlRejection绑定完成修复与独立证据；取消scope不改成功。Test-TurnSoftRepair及Test-ControlCompletedReview纳入Workflow，保留UTC/预算/回传回归；正式硬限待瘦身后两周实测。旧AI201的120硬停为历史实现，已被本合同替代；其REJECTED历史保留，2026-10-05 20:39经独立控制复核后正式ResolveControlRejection解决本次阻断。

GOV-02 补齐软限实际链路：queue.ps1 WriteHandoff/CheckManagedTool、supervised.ps1 Yield/SHA接棒、Invoke-TaskTool调用前门禁、WATCH HANDOFF_STALLED/TURN_LIMIT路径；在固定职能chat接棒并保留累计历史。只覆盖受管入口；既有Test-TurnSoftRepair/Test-Pre5c/Test-Watch隔离夹具承担边界回归，不新增专用续行入口。

TK-WF-04 2026-10-06：ResumeApprovedTask + approved-resume.ps1 为原ENGINE/DATA失败任务提供一次证据绑定的获批续行，卡/恢复配对预算与失败历史不变；Test-ApprovedResume纳入Workflow。不是自动重试、不是清失败/人工签收。

2026-10-06 原只读诊断补验续行：ResumeApprovedTask 另接 APPROVED_VALIDATION_RESUME，仅对同根 ENGINE/DATA 的一次 TOOL_UNAVAILABLE 只读诊断中断；需人类续行原话、独立环境/编译/清理方案证据、全部失败史/原修复结果/源码与全序列化快照 SHA、fresh idle 与同 session/route 绑定。一次凭证只续原卡，保留 attempt/maxAttempts/错误/人工gate，不另占修复slot。只有该补验实际完成且独立 VERIFIED、凭证已消费且失败史未变，才可在根生产恢复时排除这条工具中断的时间屏障；原错误仍永久保留，新生产/验收失败不豁免。实现 .ai-workspace/approved-resume.ps1，真实 queue 回归 DevTools/Workflow/Test-ApprovedResume.ps1；不替代生产写阶段或用户清理。
2026-10-07 TK-WF-04：明确授权的APPROVED_COLD_PROCESS_VALIDATION仅补原214一次独立进程首载；120项真实队列隔离回归，原错误/次数/旧许可/预算保持，第三次失败拒绝再续。完整合同见SUPERVISED_FULL同日节。

2026-10-07 TK-WF-04宿主续行：新用户授权对应APPROVED_HOST_PROCESS_VALIDATION；同卡三失败/旧许可链与两种宿主只读证据绑定，166项queue隔离回归。Unity两启动各需独立平台审批；新失败停。见完整协议同日宿主节。

2026-10-09 TK-WF-04：原AI217探针场景一次补验入口及220项queue回归，保留全部失败/预算；边界见完整监管协议同日节。

2026-10-09：已批准Static恢复/恢复后只读预检入口与49项回归见 protocol/SUPERVISED_FULL.md 最新节、DevTools/Workflow/Test-ApprovedRestore.ps1；保持同根修复预算。

2026-10-14 AI-000230（TK-WF-03/04）：`Status`默认≤2 KiB摘要、TaskId/闭区间过滤与显式`-FullStatus`；旧全文消费者显式请求。`Verify-All`只对逐runner声明闭合依赖且无外部副作用的schemaVersion=2清单做同任务成功复用，最近失败/未知依赖/前后漂移均执行；默认无清单执行，Custom受影响runner优先于整域重跑。WATCH非活动态先停输出/线程/Unity观察，相同空轮限状态变化/跨日记录；报表到期/漏报独立补齐，已发布轻量签名无变化则不读JSONL，变化复核旧SHA且不覆盖。隔离回归`Test-UsageOptimization`、`Test-Watch`、`Test-UsageReporting`；旧WATCH日程未改，实际宿主生效须另看新run日志。

AI-000231 增量（同一 TK-WF-03/04，不另建工具）：`Status` 加状态计数、优先注意项及可复用的落盘全文路径，`-Full`兼容显式全文；`PendingReview`默认小摘要、`-Full`取旧明细。`Get-UsageReport`增加逐动作最大返回字节，Status无观测时为null及原因；测试沿用`Test-UsageOptimization`，不增加并行夹具或重发旧周报。

2026-10-16 AI249（既有TK-WF-03/04）：RegisterExecutionPipeline -Pipeline ENGINE_CLI在MANUAL初始化未绑定线，BindExecutionPipeline复用闲置绑定/历史；领取无静态回退。两引擎线和DATA在所有模式强制共享unity-project/data-batch。Get-UnityCliHeartbeat核官方实例/PID/新鲜状态并绑定执行；WATCH按实际活动owner选择CLI/Coplay，不将健康变化算进度。Invoke-TaskTool的CaptureOutput保留原始双流、耗时、退出/失败和摘要字节；CLI默认摘要/Observation，旧调用保持。Importer结算后精确attempt/execution/window、responseId去重；报告分队列车道/实际工具/成本类别。Test-EngineCli为同族隔离集成fixture，无新增TK族；真实接管/CLI/监管发送由后继验证。
2026-10-17 AI259扩充既有 TK-WF-01：补齐活动引用源及8个版本家族；派发包/cards按批次、文档身份比较，未关闭或被未关闭任务依赖时保留。简表优先读来源可核验的报告机器块、历史回填，归档后继续按manifest定位证据，缺失值保持unknown。没有新增TK、执行生产归档或调整ASM-01前后顺序；[交付与定向验证](../archive/2026-10-17-workspace-correction/readable/.ai-workspace/outputs/DOC-REVIEW-20261017/AI-000259/REPORT.md)。

已关闭任务和控制端批次的完整说明/报告在[归档索引](../archive/README.md)检索；[永久任务简表全文](../archive/tracking/TASK_DIGEST.md)保存在归档区，仅供用户/CONTROL复盘。活动区保留当前必要入口和未结事项，历史证据链接不等于活动依赖。

2026-10-17 AI261：Get-TaskDigest新增schema3 READY tar.xz只读消费；内部Python标准库辅助不解包/不写源文件，包SHA/成员/报告证据按单元验证，保留ZIP分支。实际工具合同3/3、定向46断言及补强后16断言通过；详情见DevTools/Workflow/ARCHIVING.md与[本批归档](../archive/2026-10-17-oversize-slim/README.md)。仍属TK-WF-01，无新TK或执行入口。

2026-10-18 AI262：既有 Test-DocumentationIndex 改查现行永久入口和 INSTRUCTIONS_ARCHIVE_INDEX 轮转证明，取消已归档AI159基线、历史50%目标和账本永不缩短限制。仍保护未关闭完整任务/活动依赖，兼容正常Enqueue、Instruction revision、ConsumeDataRound及Pipeline reclassification追加。既有Test-DocumentArchive中两定向组25项断言通过；生产轮转由CONTROL依DOC_MAINTENANCE第16条执行，见[本批记录](../archive/2026-10-18-instructions-slim/README.md)。无新TK或生产归档入口。


<a id="tk-wf-05"></a>
## TK-WF-05 · 显式 Git 内容比较

2026-10-19 AI276：既有GitHooks族新增 `DevTools/GitHooks/compare-git-content.py`，公开命令为必填repo/完整既存baseline commit/明确相对文件列表；只读比较baseline blob、index与按当前Git内建clean规则规范化的工作树，raw SHA独立保留，LFS按真实内容OID/SHA+size而非指针字节。缺失/未合并/不可读、未知外部filter、目标重定向/未知indexed配置等失败关闭。未找到现行通用文本消费者，工具未安装生产hook或全仓自动启用，原字体/批准SHA/main/backup保护不变。

复用真实Git clean及既有字体guard/Verify-All Custom入口，原字体能力不承担一般文本归一化；无需新库、队列/保护架构或平行runner。`test-git-content.py`/`Test-GitContent.ps1`真实双clone242项及原字体37项通过；新增中文ASCII JSON协议/配置边界，E2退休冻结两条夹具期望经真实用户批准更正，前四失败/旧grant不变。详细支持范围、调用/诊断与生产安装状态见[GitHooks README](../../DevTools/GitHooks/README.md)，证据[attempt5报告](../outputs/DETACH-B13-20261019/AI-000276/attempt5/REPORT.md)。原卡用户S40保留，实际累计9文件及新长期工具通常L的差异在报告记账，120/240门不变。
