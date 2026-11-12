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

2026-11-05 AI440 局部维护（不提高完整核验日期）：现Enqueue新增显式ControllerRepairReview/RepairReviewPath/Sha256，仅当前SUPERVISED ACTIVE绑定CONTROL、HUMAN一次且完整Scope/root-family/当前正式失败/source/owner/session固定SHA审查可替代旧编号诊断门。来源全队列去重含旧HUMAN修复，独立hardstop/权限/失败上限/运行owner/预约及停止门保持，旧分支与5/3/1不变。最终Test-ApprovedResume完整648项通过，隔离正式API证5/0/5→5/1/6→6/0/6及配对不双扣、失败同源拒；前两次夹具失败和第三640通过均保留。原435 whole逐字段SHA保持，未生产批准/派发/领取435或做Street修复；本卡为独立机制根，不消耗/退款435。调用例仅文档，CONTROL独立后另按真实人批审定业务范围。报告.ai-workspace/outputs/PLAYABLE-DEMO011-20261104/AI-000440/attempt1/REPORT.md。

2026-11-05 AI443 attempt2局部机制验证通过（不提高完整核验日期）：Get-RepairGrantClaimCycle 共用于两个既有领取入口，以当前 task/attempt/execution/grant 的唯一正式claim为准，核原源/owner/route/Scope/次数/root cycle；合法领取只更新当前派生字段，保bounded Scope。已成功 HUMAN 多轮grant仅在可证同grant首失败周期覆盖模式时按实际成功claim配对，回执/audit分列raw/effective，不改旧成功卡；旧正确单轮/POLICY分支保留。现Test-ApprovedResume用既有BuildAutoChain核实际AUTO子卡READY，完整911项Final exit0（真实原源反例、两实际入口、旧成功配对/rootclaim10/0/10、六Fail及全部负例）；首轮三exit1和CONTROL独立0/3不倒改。本轮AI443:retry1为技术根1/0/5，不消费435；5/3/1和全部独立停止门保持。仅CODE机制通过，CONTROL独立后才原435配对442实际0e257周期，生产未调用。证据与完整调用候选见 [AI443 attempt2报告](../outputs/PLAYABLE-DEMO011-20261104/AI-000443/attempt2/REPORT.md)。AI389旧报告局限保留为历史，不把本轮领取验证扩大为所有指令修订情形已验。

# 工具库 · WF

2026-10-30 AI393 / TK-WF-03/04 局部管线修复（独立 pipeline-repair，关联原 AI392）：`UsageMetrics.ps1` 的 writer 在原 20 秒 mutex 总预算内只对尚未打开的 JSONL 文件做共享/锁冲突有界等待，打开后一次 Write+Flush，写后未知绝不重放；`Read-TaskUsageRecords` 同 mutex 短时读取完整月文件快照，再解锁解析，完成的坏行仍报错、历史行/时间筛选/null 语义保留。`Invoke-TaskTool.ps1` CaptureOutput 将真实子进程 PID/退出/耗时/原始双流和摘要以 CreateNew proof 先落盘，再做 usage/finish；任一记账失败保留 FAILED/UNKNOWN 状态及原异常，host 仍失败，不补发原生调用。旧 AI392 第七 Console 的真实 exit/正式回执仍 UNKNOWN，既有 Build 成功子项不撤销。既有 Test-UsageMetrics Concurrency 与 Test-EngineCli CaptureAccounting 隔离定向通过，旧源拒绝、有限占用/持久冲突/写后未知和一次进程 proof 均有原流；首个夹具布局失败已留史并按稳定 key 解决。无 Unity/Player/生产 runtime 写；完整无重建续行候选与离线编译只读 eval 见 [AI393 输出](../outputs/PLAYABLE-DEMO01-20261026/AI-000393/)。原 AI392 三合同、SUP5 与停止门不改，须 CONTROL 独立审查及正式 ApproveResume，不能由 CODE 自续。

2026-10-30 AI388 局部 CapturePng 范围兼容（不改变队列/续行授权）：`Invoke-TaskTool.ps1` 在正式 `CheckManagedTool` 后、进程启动前，FEATURE 仍以 `businessScope` 声明完整当前任务输出根；合法 REPAIR 可用正式 `repairContext.repairScope` 作为该根来源，但须同时核当前 `execution.approvedScope`，两者均有有效路径和 `capture-screen-no-save` 操作，且 PNG 与原始双流都落在授权交集。REPAIR 若已有 `businessScope`，继续作为额外约束；缺根身份、缺/错/空范围或当前批准缩窄均启动前拒绝。原截图 argv、screen/no-save、路径安全和 PNG/CreateNew 守卫未改。既有 `Test-EngineCli.ps1 -Case CapturePng` 用隔离正式 Enqueue/Claim 的空 businessScope REPAIR 与旧源码原字节负例验证，第三次定向 Final 235 断言通过；前两次夹具搭建失败已按同一稳定 key 留存并 RESOLVED。仅 CODE 离线，没有 Unity 截图；AI386 原 6/0/6 已耗尽，须新人工批准后 CONTROL 才可正式续行。见 [AI388 报告](../outputs/PLAYABLE-DEMO01-20261026/AI-000388/REPORT.md)。下文 AI299 的 businessScope 描述是当时 FEATURE 实现的历史记录。

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

2026-10-24 AI343 / G87 进度持久化管线修复（独立 pipeline-repair，关联原339且不配对/扣退339）：Save-TextFile 保持同目录唯一 PID+GUID 完整临时写入→单次 File.Move(overwrite)；写失败在原异常 Data 和 stderr 补 stage、目标/临时路径、PID、逐层 exception type/HResult 与仅 FACILITY_WIN32 的低码，再原样抛出，绝不删旧目标/temp、重试或启动后继工具。queue Read-JsonFile 与 Archive-Policy Read-ArchiveJson 只在各自 JSON 读取入口用只读 FileStream/FileShare.Read|Delete 单句柄读完即 Dispose，之后解析；保 UTF8 BOM/中文/DateKind String 与缺失/损坏失败。隔离实测：当前 Windows/.NET File.Move 覆盖在目标仍被声明 Delete 共享的真实读句柄持有时仍拒绝访问；原目标与本次完整 temp 保留，释放句柄后才成功，因此本修复仅缩短读取窗口并改善可观测性，不保证所有并发替换成功，不证明原339事故由 Get-TaskDigest 或固定 ACL 所致。既有 QueueWriter 窄入口含两真实读取函数 AST 与受管 start 失败边界，修正一次测试路径断言后 35/35 通过；首次失败原流保留。原339四失败、PERMISSION/HARD_STOP、根 used3/5 余2、七合同及人工门保持，须 CONTROL 新人批后唯一 ApproveResume，不能凭此卡自续。报告见 .ai-workspace/outputs/BATCH2-INDEPENDENT-20261023/AI-000343/REPORT.md。

2026-10-24 AI349 同根补修 AI348 独立审查指出的 Editor 会话归属：ShortCycleProbeCi 只有在本次 Arm 尝试且 Launcher 当前 SessionState task/run/root 精确匹配、存在 armed/entered/restore 或同根 Bootstrap 活动证据时才允许 Stop；预检失败、旧同身份残留、身份缺失/改变均仅失败写自有结果，不清外来会话。Arm 前持久化尝试状态，部分 Arm 后和跨状态读取可凭同一真实身份恢复；Finish 成功另要求本次已归属及 Launcher 真恢复。既有 ShortCycleCi 隔离检查由24扩至35，真实源码可执行生命周期替身覆盖外来/本次/超时/变更/重入，Unity6真引用离线编译通过。无 Editor 原生验证；CONTROL 独立验收和固定 ENGINE 原始两连续+负例仍待，AI339 HARD_STOP 不变。见 [AI349 报告](../outputs/BATCH2-INDEPENDENT-20261023/AI-000349/REPORT.md)。

2026-10-24 AI348 同根修复 AI347 独立验收发现的 PROBE_CI_RESULT_OWNERSHIP：ShortCycleNativeCi 在任何 Final 历史创建前要求显式合法 TaskId 与上下文精确一致；宿主先校验身份、路径、链接、正式 ENGINE_CLI 门与队列 attempt/execution，再原子认领 fresh run 根。未认领前拒绝不写；认领后的真实失败以 CreateNew 留诊断，不覆盖既有结果。已完成报告仅在 host/unity 全量身份、结果、路径及无链接证据一致时只读返回 ALREADY_RECORDED，不能计新 Final PASS。Editor 入口仅对验证过的精确结果路径 CreateNew，失败预检不输出到未获准路径。CODE 仅离线 24 项夹具及 Unity6 真引用编译；CONTROL 独立验收和后续固定 ENGINE 原生两次连续+负例仍待完成，AI339 HARD_STOP 不变。见 [AI348 报告](../outputs/BATCH2-INDEPENDENT-20261023/AI-000348/REPORT.md)。

2026-10-24 AI347（既有 TK-WF-03/04，CODE 离线段）：Verify-All 仅显式 ShortCycleNativeCi -Mode Final -NativeCiContextPath 启动 Invoke-ShortCycleProbeCi.ps1；Auto/All/ShortCycle/普通 Custom 不隐式运行 Unity。宿主要求正式 ENGINE_CLI 运行身份与 CheckManagedTool、精确 Unity6000.3.24f1/project/task/run 新根，拒同项目 Editor/已有或链接根；官方 Unity.exe -batchmode -nographics -executeMethod ShortCycleProbeCi.Start 不带 -quit，与 RPC CLI 分离。原 Launcher 负责隔离 Play/恢复，原 1～27 Probe 与只读 LayoutAudit 聚合为同 task/run/attempt/execution JSON；只有清理、场景恢复和真实进程退出均成立才 PASS。已完成 run 只读原报告，不作为新的 Final PASS；负例需显式 injectFailure 并在第三次 Final 标 RepeatReason。Test-EngineCli 的 ShortCycleCi 窄隔离 Case 仅模拟宿主进程，不运行 Unity。CODE 真引用编译和夹具不是原生验收；ENGINE 须正式跑两次 fresh 连续与一次故意失败捕获，AI339 HARD_STOP 不变。见 [AI347 报告](../outputs/BATCH2-INDEPENDENT-20261023/AI-000347/REPORT.md)。

2026-10-15管线继承（既有队列工具扩充）：BindArtAuditAutomation增加可选-PipelineThreadId，配合-UserDecision/-Verification在ART完全空闲且目标不与其它角色复用时更新threadId并追加routeHistory，原日报jobs/成功基线/enabled不变。CODE/ENGINE用BindExecutionPipeline，DATA用BindDataPipeline；新地址与日程映射以PIPELINES为准。不是新的TK或业务续行入口。


2026-10-15 GOV-04后续AI-000245：Get-ThreadContext优先读精确线程原生token_usage_record.payload.usage.input_tokens，有界发现/尾读/缓存及30分钟时限；旧provider只可新鲜回退，显式fresh字节为估算，再无则null+原因。queue普通正式回执以显式ControllerThreadId/OwnerThreadId采样，MANUAL Status可承载提示；活跃监管心跳仍按heartbeatId去重。样本去重缓存由UsageMetrics管理；WATCH只在活动scope消费新鲜worker/console STRONG转入，Read-WatchInbox核绑定控制台并Ack，不自动重载/Yield/换chat。单次input不是完整窗口或任务计费；下述旧实现描述为历史，不重发旧周报。

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

2026-10-24 AI337 同根二级拒验解除：ResolveControlRejection 仍只接受已完成且 VERIFIED、晚于被拒时刻的真实修复，并要求绑定控制台独立证据精确覆盖被拒卡/修复卡当前 result SHA、拒验证据 SHA、稳定 problemKey 与审阅时间。被拒卡若也是 REPAIR，先沿 parentTaskId 逐项核至其真实 root，再从候选修复逐项核到该被拒卡；各节点须存在、已完成、同 root/key、无自指/循环/跨根或缺失，不能用同根兄弟顶替后裔。仅当被拒对象是原根且候选修复的实际父对象也是该根时，历史直接修复才可缺 parentTaskId 或 nature；显式 nature 非 REPAIR 一律拒绝。候选显式指向中间卡时属于间接后裔，其自身及所经中间节点均须 nature=REPAIR，不能借原根目标取得历史豁免。CONTROL 须先用已验后裔解除中间拒验，再用更新后的中间 result SHA 独立举证解除原根；此修复不自动调用 Resolve、不改 runtime、旧失败/许可/预算/人工门。隔离正式 API 回归仍用既有 .ai-workspace/tests/Test-ControlCompletedReview.ps1；本机制不授权窗口③业务。

GOV-02 补齐软限实际链路：queue.ps1 WriteHandoff/CheckManagedTool、supervised.ps1 Yield/SHA接棒、Invoke-TaskTool调用前门禁、WATCH HANDOFF_STALLED/TURN_LIMIT路径；在固定职能chat接棒并保留累计历史。只覆盖受管入口；既有Test-TurnSoftRepair/Test-Pre5c/Test-Watch隔离夹具承担边界回归，不新增专用续行入口。

TK-WF-04 2026-10-06：ResumeApprovedTask + approved-resume.ps1 为原ENGINE/DATA失败任务提供一次证据绑定的获批续行，卡/恢复配对预算与失败历史不变；Test-ApprovedResume纳入Workflow。不是自动重试、不是清失败/人工签收。

2026-10-06 原只读诊断补验续行：ResumeApprovedTask 另接 APPROVED_VALIDATION_RESUME，仅对同根 ENGINE/DATA 的一次 TOOL_UNAVAILABLE 只读诊断中断；需人类续行原话、独立环境/编译/清理方案证据、全部失败史/原修复结果/源码与全序列化快照 SHA、fresh idle 与同 session/route 绑定。一次凭证只续原卡，保留 attempt/maxAttempts/错误/人工gate，不另占修复slot。只有该补验实际完成且独立 VERIFIED、凭证已消费且失败史未变，才可在根生产恢复时排除这条工具中断的时间屏障；原错误仍永久保留，新生产/验收失败不豁免。实现 .ai-workspace/approved-resume.ps1，真实 queue 回归 DevTools/Workflow/Test-ApprovedResume.ps1；不替代生产写阶段或用户清理。
2026-10-07 TK-WF-04：明确授权的APPROVED_COLD_PROCESS_VALIDATION仅补原214一次独立进程首载；120项真实队列隔离回归，原错误/次数/旧许可/预算保持，第三次失败拒绝再续。完整合同见SUPERVISED_FULL同日节。

2026-10-07 TK-WF-04宿主续行：新用户授权对应APPROVED_HOST_PROCESS_VALIDATION；同卡三失败/旧许可链与两种宿主只读证据绑定，166项queue隔离回归。Unity两启动各需独立平台审批；新失败停。见完整协议同日宿主节。

2026-10-09 TK-WF-04：原AI217探针场景一次补验入口及220项queue回归，保留全部失败/预算；边界见完整监管协议同日节。

2026-10-09：已批准Static恢复/恢复后只读预检入口与49项回归见 protocol/SUPERVISED_FULL.md 最新节、DevTools/Workflow/Test-ApprovedRestore.ps1；保持同根修复预算。

2026-10-14 AI-000230（TK-WF-03/04）：`Status`默认≤2 KiB摘要、TaskId/闭区间过滤与显式`-FullStatus`；旧全文消费者显式请求。`Verify-All`只对逐runner声明闭合依赖且无外部副作用的schemaVersion=2清单做同任务成功复用，最近失败/未知依赖/前后漂移均执行；默认无清单执行，Custom受影响runner优先于整域重跑。WATCH非活动态先停输出/线程/Unity观察，相同空轮限状态变化/跨日记录；报表到期/漏报独立补齐，已发布轻量签名无变化则不读JSONL，变化复核旧SHA且不覆盖。隔离回归`Test-UsageOptimization`、`Test-Watch`、`Test-UsageReporting`；旧WATCH日程未改，实际宿主生效须另看新run日志。

AI-000231 增量（同一 TK-WF-03/04，不另建工具）：`Status` 加状态计数、优先注意项及可复用的落盘全文路径，`-Full`兼容显式全文；`PendingReview`默认小摘要、`-Full`取旧明细。`Get-UsageReport`增加逐动作最大返回字节，Status无观测时为null及原因；测试沿用`Test-UsageOptimization`，不增加并行夹具或重发旧周报。

2026-10-26 AI356（C10）：监管 `Get-SupervisionResources` 对 CODE 的 businessScope 与执行期 approvedScope 检查路径；覆盖 `Assets` 或项目根、范围未知时强制追加 `unity-project`，显式 workflow/documentation 资源不得剥离。ENGINE_CLI/MCP/DATA 原共锁保持；明确工程外且有范围的 CODE 工作可并行。正式 Dispatch 与 Poll 共用资源冲突门；窄 `Test-EngineCli -Case AssetsMutex` 离线核对双向冲突及外部并行。该门只约束正式队列任务，不能阻止用户或外部进程手工改 Assets。小场景修复按 C10 仅核目标字段，人工保存后用 git diff 与精确对象/Prefab target GUID、fileID、propertyPath 白名单只读核对，不用整对象冻结证明。

2026-10-16 AI249（既有TK-WF-03/04）：RegisterExecutionPipeline -Pipeline ENGINE_CLI在MANUAL初始化未绑定线，BindExecutionPipeline复用闲置绑定/历史；领取无静态回退。两引擎线和DATA在所有模式强制共享unity-project/data-batch。Get-UnityCliHeartbeat核官方实例/PID/新鲜状态并绑定执行；WATCH按实际活动owner选择CLI/Coplay，不将健康变化算进度。Invoke-TaskTool的CaptureOutput保留原始双流、耗时、退出/失败和摘要字节；CLI默认摘要/Observation，旧调用保持。Importer结算后精确attempt/execution/window、responseId去重；报告分队列车道/实际工具/成本类别。Test-EngineCli为同族隔离集成fixture，无新增TK族；真实接管/CLI/监管发送由后继验证。
2026-10-17 AI259扩充既有 TK-WF-01：补齐活动引用源及8个版本家族；派发包/cards按批次、文档身份比较，未关闭或被未关闭任务依赖时保留。简表优先读来源可核验的报告机器块、历史回填，归档后继续按manifest定位证据，缺失值保持unknown。没有新增TK、执行生产归档或调整ASM-01前后顺序；[交付与定向验证](../archive/2026-10-17-workspace-correction/readable/.ai-workspace/outputs/DOC-REVIEW-20261017/AI-000259/REPORT.md)。

已关闭任务和控制端批次的完整说明/报告在[归档索引](../archive/README.md)检索；[永久任务简表全文](../archive/tracking/TASK_DIGEST.md)保存在归档区，仅供用户/CONTROL复盘。活动区保留当前必要入口和未结事项，历史证据链接不等于活动依赖。

2026-10-17 AI261：Get-TaskDigest新增schema3 READY tar.xz只读消费；内部Python标准库辅助不解包/不写源文件，包SHA/成员/报告证据按单元验证，保留ZIP分支。实际工具合同3/3、定向46断言及补强后16断言通过；详情见DevTools/Workflow/ARCHIVING.md与[本批归档](../archive/2026-10-17-oversize-slim/README.md)。仍属TK-WF-01，无新TK或执行入口。

2026-10-18 AI262：既有 Test-DocumentationIndex 改查现行永久入口和 INSTRUCTIONS_ARCHIVE_INDEX 轮转证明，取消已归档AI159基线、历史50%目标和账本永不缩短限制。仍保护未关闭完整任务/活动依赖，兼容正常Enqueue、Instruction revision、ConsumeDataRound及Pipeline reclassification追加。既有Test-DocumentArchive中两定向组25项断言通过；生产轮转由CONTROL依DOC_MAINTENANCE第16条执行，见[本批记录](../archive/2026-10-18-instructions-slim/README.md)。无新TK或生产归档入口。


<a id="tk-wf-05"></a>
## TK-WF-05 · 显式 Git 内容比较

2026-10-19 AI276：既有GitHooks族新增 `DevTools/GitHooks/compare-git-content.py`，公开命令为必填repo/完整既存baseline commit/明确相对文件列表；只读比较baseline blob、index与按当前Git内建clean规则规范化的工作树，raw SHA独立保留，LFS按真实内容OID/SHA+size而非指针字节。缺失/未合并/不可读、未知外部filter、目标重定向/未知indexed配置等失败关闭。未找到现行通用文本消费者，工具未安装生产hook或全仓自动启用，原字体/批准SHA/main/backup保护不变。

复用真实Git clean及既有字体guard/Verify-All Custom入口，原字体能力不承担一般文本归一化；无需新库、队列/保护架构或平行runner。`test-git-content.py`/`Test-GitContent.ps1`真实双clone242项及原字体37项通过；新增中文ASCII JSON协议/配置边界，E2退休冻结两条夹具期望经真实用户批准更正，前四失败/旧grant不变。详细支持范围、调用/诊断与生产安装状态见[GitHooks README](../../DevTools/GitHooks/README.md)，证据[attempt5报告](../outputs/DETACH-B13-20261019/AI-000276/attempt5/REPORT.md)。原卡用户S40保留，实际累计9文件及新长期工具通常L的差异在报告记账，120/240门不变。

### CLI argv 黄金组（INFRA-CLIARG-01，G102离线）

沿用现TK-WF工具链：Invoke-TaskTool公开参数为-ArgumentList，Get-TaskToolArgumentPlan只将命令路径/原argv组为现进程参数；.ps1沿现PowerShell7解析路径加-NoProfile/-File前缀，其余字符串（含空项、引号、Unicode、多行、尾反斜线）逐项保留。不拼shell命令、不改许可/守卫/会计，不自动把错误选项改正确。

- fresh status：status --project-path <当前项目> --format json --non-interactive；不带--timeout。只有明确请求等待ready时，--until-ready与--timeout一起提供。
- Console：command console --tail 2000 --since <cursor> --since_session <原session>加原项目/timeout/format参数；禁止--since-seq和--session。原返回session/dropped/reset含义保持。
- 包装调用用-ArgumentList @(...)；-CommandArguments不存在，457a9在CLI启动前绑定失败，不加兼容别名。
- preflight/prepare/cleanup/edit/readback/build是角色；现Street与Build路径多用command eval_file，启动Ready RPC用command eval。capture对应capture_game_view，play/stop对应editor_play/editor_stop；不编造prepare/build等CLI子命令。具体角色源仍由原任务审定，黄金argv不是执行许可，CapturePng等原门保持。

聚焦离线入口：Test-EngineCli.ps1 -Case ArgumentGolden -OutputRoot <本任务输出>。黄金表保存参数数组、参数成功路径的期望exit和有来源的字段骨架；CLI参数错误由透明窄桩重现，模型不生成或验证CLI输出。已知JSON字段逐例引用历史真实成功原流SHA；只有SDK类型声明的例保留明确局部字段，CLI外层/位置及未列继承字段unknown。help/version未请求JSON，按默认文本面记录，精确布局/exit unknown待SMOKE，不能硬套JSON外壳。wrapper绑定用实际ParamBlock/PowerShell binder和无副作用桩体。没有运行官方CLI/Unity，也不证明当前原生数据/时序/截图/Build。G102：ENGINE独立SMOKE明确待用户手动调试结束，离线结果不得冒已过或作为新引擎门投入使用。旧失败/脚本原样保留，参见P-070与AI458交付。
